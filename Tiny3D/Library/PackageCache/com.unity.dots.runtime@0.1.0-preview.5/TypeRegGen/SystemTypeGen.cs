//#define WRITE_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace TypeRegGen
{
    static class TypeHelpers
    {
        public static bool TypeHasSuperClass(this TypeDefinition type, TypeDefinition baseClass)
        {
            while (!baseClass.Equals(type))
            {
                if (type == null || type.BaseType == null)
                    return false;
                type = type.BaseType.Resolve();
            }

            return true;
        }

        public static CustomAttribute GetAttribute(this ICustomAttributeProvider @this, string fullAttrName)
            => @this.CustomAttributes.SingleOrDefault(a => a.AttributeType.FullName == fullAttrName);

        public static bool HasAttribute(this ICustomAttributeProvider @this, string fullAttrName)
            => @this.GetAttribute(fullAttrName) != null;

        public const string DisableAutoCreationAttrName = "Unity.Entities.DisableAutoCreationAttribute";
    }

    class SystemTypeGen
    {
        public static List<TypeDefinition> GetSystems(List<AssemblyDefinition> assemblies)
        {
            var entitiesAssembly = assemblies.First(asm => asm.Name.Name == "Unity.Entities");
            var componentSystemBaseClass =  entitiesAssembly.MainModule.GetAllTypes().First(t => t.FullName == "Unity.Entities.ComponentSystemBase");

            var componentSystems = new List<TypeDefinition>();
            var invalidAutoSystems = new List<TypeDefinition>();
            foreach (var asm in assemblies)
            {
                // IMPORTANT: keep this logic ALMOST in sync with Unity.Entities.DefaultWorldInitialization.GetAllSystems.
                //
                // unlike DefaultWorldInitialization.GetAllSystems, which _only_ returns the auto-creation systems (i.e.
                // will filter out systems where [DisableAutoCreation] is applied), this function (SystemTypeGen.GetSystems)
                // needs to return _all systems that can be auto-created_, independent of that attribute.
                //
                // instead, the attribute is checked and stored in GenCreateSystems, which generates the code for
                // Unity.Entities.StaticTypeRegistry.StaticTypeRegistry.GetSystemAttributes, which is ultimately used
                // by TypeManager.GetSystemAttributes to decide auto-create-or-not.
                //
                // it's important that we still always have all auto-creatable systems in `componentSystems` here, so that
                // a manual GetOrCreateSystem, which ends up in codegen'd CreateSystem, finds the type and succeeds.

                // only bother to check assemblies that reference the assembly with ComponentSystemBase
                if (asm != entitiesAssembly && !asm.Modules.SelectMany(m => m.AssemblyReferences).Any(r => r.Name == entitiesAssembly.Name.Name))
                    continue;

                // the entire assembly can be marked for no-auto-creation (test assemblies are good candidates for this)
                var disableAsmAutoCreation = asm.HasAttribute(TypeHelpers.DisableAutoCreationAttrName);

                foreach (var type in asm.MainModule.GetAllTypes())
                {
                    // only derivatives of ComponentSystemBase are systems
                    if (!type.TypeHasSuperClass(componentSystemBaseClass))
                        continue;

                    // these types obviously cannot be instantiated
                    if (type.IsAbstract || type.HasGenericParameters)
                        continue;

                    // the auto-creation system instantiates using the default ctor, so if we can't find one, exclude from list
                    if (type.GetConstructors().All(c => c.HasParameters))
                    {
                        var disableTypeAutoCreation = type.HasAttribute(TypeHelpers.DisableAutoCreationAttrName);

                        // we want users to be explicit
                        if (!disableAsmAutoCreation && !disableTypeAutoCreation)
                            invalidAutoSystems.Add(type);

                        continue;
                    }

                    componentSystems.Add(type);
                }
            }

            if (invalidAutoSystems.Any())
            {
                throw new ArgumentException(
                    "A default constructor is necessary for automatic system scheduling for Component Systems not marked with [DisableAutoCreation]: "
                    + string.Join(", ", invalidAutoSystems.Select(cs => cs.FullName)));
            }

            return componentSystems;
        }

        public static List<bool> GetSystemIsGroup(List<AssemblyDefinition> assemblies, List<TypeDefinition> systems)
        {
            var entities = assemblies.First(asm => asm.Name.Name == "Unity.Entities");
            var baseClass = entities.MainModule.GetAllTypes().First(t => t.FullName == "Unity.Entities.ComponentSystemGroup");
            var inGroup = systems.Select(s => s.TypeHasSuperClass(baseClass)).ToList();
            return inGroup;
        }

        public static List<string> GetSystemNames(List<TypeDefinition> systems)
        {
            return systems.Select(s => s.FullName).ToList();
        }

        public static MethodDefinition GenCreateSystems(List<TypeDefinition> systems, List<AssemblyDefinition> assemblies, ModuleDefinition typeRegModule,
            MethodReference getTypeFromHandleRef, MethodReference invalidOpExceptionRef)
        {
            var createSystemsFunction = new MethodDefinition(
                "CreateSystem",
                MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
                typeRegModule.ImportReference(typeof(object)));

            createSystemsFunction.Parameters.Add(
                new ParameterDefinition("systemType",
                ParameterAttributes.None,
                typeRegModule.ImportReference(typeof(Type))));

            createSystemsFunction.Body.InitLocals = true;
            var bc = createSystemsFunction.Body.Instructions;

            foreach (var sys in systems)
            {
                var constructor =
                    typeRegModule.ImportReference(sys.GetConstructors()
                        .FirstOrDefault(param => param.HasParameters == false));

                bc.Add(Instruction.Create(OpCodes.Ldarg_0));
                bc.Add(Instruction.Create(OpCodes.Ldtoken, typeRegModule.ImportReference(sys)));
                bc.Add(Instruction.Create(OpCodes.Call, getTypeFromHandleRef));
                bc.Add(Instruction.Create(OpCodes.Ceq));
                int branchToNext = bc.Count;
                bc.Add(Instruction.Create(OpCodes.Nop));    // will be: Brfalse_S nextTestCase
                bc.Add(Instruction.Create(OpCodes.Newobj, constructor));
                bc.Add(Instruction.Create(OpCodes.Ret));

                var nextTest = Instruction.Create(OpCodes.Nop);
                bc.Add(nextTest);

                bc[branchToNext] = Instruction.Create(OpCodes.Brfalse_S, nextTest);
            }
            bc.Add(Instruction.Create(OpCodes.Ldstr, "FATAL: CreateSystem asked to create an unknown type. Only subclasses of ComponentSystemBase can be constructed."));
            bc.Add(Instruction.Create(OpCodes.Newobj, invalidOpExceptionRef));
            bc.Add(Instruction.Create(OpCodes.Throw));
            return createSystemsFunction;
        }

        static TypeDefinition FindClass(List<AssemblyDefinition> assemblies, string fullName)
        {
            foreach (var asm in assemblies)
            {
                var type = asm.MainModule.GetAllTypes().FirstOrDefault(t => t.FullName == fullName);
                if (type != null)
                    return type;
            }
            throw new InvalidProgramException("FindClass could not find: " + fullName);
        }

        public static MethodDefinition GenGetSystemAttributes(List<TypeDefinition> systems, List<AssemblyDefinition> assemblies, ModuleDefinition typeRegModule,
            MethodReference getTypeFromHandleRef, MethodReference invalidOpExceptionRef)
        {
            var attributeTypeRef = typeRegModule.ImportReference(typeof(Attribute));
            var attributeArrayTypeRef = attributeTypeRef.MakeArrayType();

            // Check out HardcodedGetSystemAttributes for the C# and IL of how this works.
            // Also essentially a more complex version of GenCreateSystems - so start there!
            var createSystemsFunction = new MethodDefinition(
                "GetSystemAttributes",
                MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
                typeRegModule.ImportReference(attributeArrayTypeRef));

            createSystemsFunction.Parameters.Add(
                new ParameterDefinition("systemType",
                ParameterAttributes.None,
                typeRegModule.ImportReference(typeof(Type))));

            createSystemsFunction.Body.InitLocals = true;

            var bc = createSystemsFunction.Body.Instructions;

            var allGroups = new string[]
            {
                "Unity.Entities.UpdateBeforeAttribute",
                "Unity.Entities.UpdateAfterAttribute",
                "Unity.Entities.UpdateInGroupAttribute",
                "Unity.Entities.AlwaysUpdateSystemAttribute",
                "Unity.Entities.AlwaysSynchronizeSystemAttribute"
            };

            foreach (var sys in systems)
            {
#if WRITE_LOG
                Console.WriteLine("System: " + sys.FullName);
#endif
                bc.Add(Instruction.Create(OpCodes.Ldarg_0));
                bc.Add(Instruction.Create(OpCodes.Ldtoken, typeRegModule.ImportReference(sys)));
                bc.Add(Instruction.Create(OpCodes.Call, getTypeFromHandleRef));
                // Stack: argtype Type
                bc.Add(Instruction.Create(OpCodes.Ceq));
                // Stack: bool
                int branchToNext = bc.Count;
                bc.Add(Instruction.Create(OpCodes.Nop));    // will be: Brfalse_S nextTestCase

                // Stack: <null>
                List<CustomAttribute> attrList = new List<CustomAttribute>();
                foreach (var g in allGroups)
                {
                    var list = sys.CustomAttributes.Where(t => t.AttributeType.FullName == g);
                    attrList.AddRange(list);
                }

                var disableAutoCreationAttr =
                    sys.Module.Assembly.GetAttribute(TypeHelpers.DisableAutoCreationAttrName)
                    ?? sys.GetAttribute(TypeHelpers.DisableAutoCreationAttrName);
                if (disableAutoCreationAttr != null)
                    attrList.Add(disableAutoCreationAttr);

                int arrayLen = attrList.Count;
                bc.Add(Instruction.Create(OpCodes.Ldc_I4, arrayLen));
                // Stack: arrayLen
                bc.Add(Instruction.Create(OpCodes.Newarr, attributeTypeRef));
                // Stack: array[]

                for (int i = 0; i < attrList.Count; ++i)
                {
                    var attr = attrList[i];

                    // The stelem.ref will gobble up the array ref we need to return, so dupe it.
                    bc.Add(Instruction.Create(OpCodes.Dup));
                    bc.Add(Instruction.Create(OpCodes.Ldc_I4, i));       // the index we will write
                    // Stack: array[] array[] array-index

                    // If it has a parameter, then load the Type that is the only param to the constructor.
                    if (attr.HasConstructorArguments)
                    {
                        if (attr.ConstructorArguments.Count > 1)
                            throw new InvalidProgramException("Attribute with more than one argument.");

                        var cArgName = attr.ConstructorArguments[0].Value.ToString();

                        var cArgType = FindClass(assemblies, cArgName);
                        if (cArgType == null)
                            throw new InvalidProgramException("SystemTypeGen couldn't find class: " + cArgName);

                        var arg = typeRegModule.ImportReference(cArgType);
                        bc.Add(Instruction.Create(OpCodes.Ldtoken, arg));
                        bc.Add(Instruction.Create(OpCodes.Call, getTypeFromHandleRef));

#if WRITE_LOG
                        Console.WriteLine("  Attr: {0} {1}", attr.AttributeType.Name, cArgName);
#endif
                    }
                    else
                    {
#if WRITE_LOG
                        Console.WriteLine("  Attr: {0}", attr.AttributeType.Name);
#endif
                    }
                    // Stack: array[] array[] array-index type-param OR
                    //        array[] array[] array-index

                    // Construct the attribute; push it on the list.
                    var cctor = typeRegModule.ImportReference(attr.Constructor);
                    bc.Add(Instruction.Create(OpCodes.Newobj, cctor));

                    // Stack: array[] array[] array-index value(object)
                    bc.Add(Instruction.Create(OpCodes.Stelem_Ref));
                    // Stack: array[]
                }

                // Stack: array[]
                bc.Add(Instruction.Create(OpCodes.Ret));

                // Put a no-op to start the next test.
                var nextTest = Instruction.Create(OpCodes.Nop);
                bc.Add(nextTest);

                // And go back and patch the IL to jump to the next test no-op just created.
                bc[branchToNext] = Instruction.Create(OpCodes.Brfalse_S, nextTest);
            }
            bc.Add(Instruction.Create(OpCodes.Ldstr, "FATAL: GetSystemAttributes asked to create an unknown Type."));
            bc.Add(Instruction.Create(OpCodes.Newobj, invalidOpExceptionRef));
            bc.Add(Instruction.Create(OpCodes.Throw));
            return createSystemsFunction;
        }
    }
}
