//#define WRITE_LOG

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Entities.BuildUtils;

using TypeGenInfoList = System.Collections.Generic.List<Unity.ZeroPlayer.TypeGenInfo>;

namespace Unity.ZeroPlayer
{
    internal class FieldInfoGen
    {
        private TypeGenInfoList typeGenInfoList;
        private int archBits;
        private AssemblyDefinition typeRegAssembly;
        private AssemblyDefinition entityAssembly;

        internal FieldInfoGen(TypeGenInfoList typeGenInfoList, int archBits, AssemblyDefinition typeRegAssembly,
            AssemblyDefinition entityAssembly)
        {
            this.typeGenInfoList = typeGenInfoList;
            this.archBits = archBits;
            this.typeRegAssembly = typeRegAssembly;
            this.entityAssembly = entityAssembly;
        }


        private int FieldNameToID(string name)
        {
            // Absolutely, positively must match PrimitiveFieldTypes in TypeManager.cs
            switch (name)
            {
                case "System.Boolean": return 0;
                case "System.Byte": return 1;
                case "System.SByte": return 2;
                case "System.Double": return 3;
                case "System.Single": return 4;
                case "System.Int32": return 5;
                case "System.UInt32": return 6;
                case "System.Int64": return 7;
                case "System.UInt64": return 8;
                case "System.Int16": return 9;
                case "System.UInt16": return 10;
                case "Unity.Mathematics.quaternion": return 11;
                case "Unity.Mathematics.float2": return 12;
                case "Unity.Mathematics.float3": return 13;
                case "Unity.Mathematics.float4": return 14;
                case "Unity.Tiny.Color": return 15;
            }

            throw new InvalidOperationException($"Unsupported PrimitiveFieldTypes: '{name}'");
        }

        internal void PatchFieldInfoCalls(TypeDefinition type, AssemblyDefinition asm, MethodDefinition method)
        {
            const string CALL_0 = "Unity.Entities.TypeManager::GetField(";
            const string CALL_1 = "Unity.Entities.TypeManager/FieldInfo::op_Implicit(";

            TypeDefinition extension = entityAssembly.MainModule.GetAllTypes()
                .First(t => t.FullName == "Unity.Entities.TypeManager");
            MethodDefinition getFieldArgsMethod = extension.Methods.First(m => m.Name == "GetFieldArgs");

            Instruction callField = null;

            // Demented linear search. However, since we are modifying the collection that we are searching
            // over, I haven't figured out a way to do better without the collection asserting.
            while ((callField = method.Body.Instructions.FirstOrDefault(bc =>
                       (bc.OpCode == OpCodes.Call) &&
                       (bc.Operand is MethodReference) &&
                       ((bc.Operand as MethodReference).FullName.Contains(CALL_0) ||
                        (bc.Operand as MethodReference).FullName.Contains(CALL_1)
                       ))) != null)
            {
                var instructions = method.Body.Instructions;
                Instruction ldstr = callField.Previous;
                if (ldstr.OpCode != OpCodes.Ldstr)
                    throw new Exception(
                        $"In method '{method.FullName}': call to TypeManager.GetField() must use a string literal.");

                string lookup = ldstr.Operand.ToString();

#if WRITE_LOG
                Console.WriteLine($"  patching method '{method.Name}' for string '{lookup}'");
#endif
                ConstructFieldInfo(lookup,
                    out int componentTypeIndex,
                    out int primitiveType,
                    out int byteOffsetInComponent);

                // Change GetField -> GetFieldArgs. The methods are as close as possible to try to avoid
                // any side effects or behaviors in the IL code.
                int index = instructions.IndexOf(callField);
                instructions[index] = Instruction.Create(OpCodes.Call, asm.MainModule.ImportReference(getFieldArgsMethod));

                // Replace the ldstr -> ldc.r4, ldc.r4, ldc.r4
                int ldstrIndex = index - 1;
                instructions.RemoveAt(ldstrIndex);
                instructions.Insert(ldstrIndex, Instruction.Create(OpCodes.Ldc_I4, byteOffsetInComponent));
                instructions.Insert(ldstrIndex, Instruction.Create(OpCodes.Ldc_I4, primitiveType));
                instructions.Insert(ldstrIndex, Instruction.Create(OpCodes.Ldc_I4, componentTypeIndex));
            }
        }

        /*
         * How hard could it be? Turns out a bit tricky.
         * A function to find the offset of a field in a TypeDefinition.
         */
        private FieldDefinition FindField(TypeDefinition td, string fieldName, out int offset)
        {
            // If the field uses accessors: bool foo {get; internal set}
            // then a backing name is generated. This obviously internal and likely
            // dangerous string can *almost* be worked around. We can find the getter from
            // the PropertyDefinition:
            // PropertyDefinition propertyDefinition = td.Properties.FirstOrDefault(x => x.Name == fieldName);
            // Which is much cleaner!
            // But, because we need the field offset, it still needs to be mapped back to a field, which
            // seems like it's only in the byte code for the getter. And reading the
            // bytecode seems more peril fraught than the string.

            var backingName = $"<{fieldName}>k__BackingField";

            foreach (var field in td.Fields)
            {
                string lookupName = null;
                if (field.Name == fieldName)
                    lookupName = fieldName;
                if (field.Name == backingName)
                    lookupName = backingName;

                if (lookupName != null)
                {
                    var offList = TypeUtils.GetFieldOffsetsOfByFieldName(lookupName, td, archBits);
                    offset = offList[0];
                    return field;
                }
            }

            offset = 0;
            return null;
        }

        private void ConstructFieldInfo(
            string val,
            out int componentTypeIndex,
            out int primitiveType,
            out int byteOffsetInComponent)
        {
            componentTypeIndex = 0;
            primitiveType = -1;
            byteOffsetInComponent = -1;

            string[] tokens = val.Split('.');

            if (tokens.Length < 2)
                throw new ArgumentException(
                    $"Syntax error in string parameter of TypeManager.GetField(): '{val}'");

            int typeDefIdx = -1;
            for (int i = 1; i < typeGenInfoList.Count; ++i)
            {
                if (typeGenInfoList[i].TypeDefinition.Name == tokens[0])
                {
                    typeDefIdx = i;
                    componentTypeIndex = typeGenInfoList[i].TypeIndex;
#if WRITE_LOG
                    Console.WriteLine("  Type '{0}' typeArrayIndex={1} type={2}", tokens[0], i, componentTypeIndex);
#endif
                    break;
                }
            }

            if (typeDefIdx < 0)
                throw new ArgumentException(
                    $"TypeManager.GetField() does recognize ComponentData: '{val}'");

            TypeDefinition td = typeGenInfoList[typeDefIdx].TypeDefinition;

            int offset = 0;
            FieldDefinition field = FindField(td, tokens[1], out offset);
            if (field == null)
                throw new ArgumentException(
                    $"Syntax error in string parameter of TypeManager.GetField(): '{val}'. Could not find '{tokens[1]}'.");

            if (tokens.Length == 2)
                primitiveType = FieldNameToID(field.FieldType.FullName);

            byteOffsetInComponent = offset;

#if WRITE_LOG
            Console.WriteLine("    Field '{0}' type '{1}' byteOffsetInComponent={2}", tokens[1], field.FieldType.Name,
                byteOffsetInComponent);
#endif

            FieldDefinition subField = field;
            for (int idx = 2; idx < tokens.Length; ++idx)
            {
                TypeDefinition subTD = subField.FieldType.Resolve();
                subField = FindField(subTD, tokens[idx], out offset);

                if (subField == null)
                    throw new ArgumentException(
                        $"Syntax error in string parameter of TypeManager.GetField(): '{val}'. Could not find '{tokens[idx]}'.");

                byteOffsetInComponent += offset;
                primitiveType = FieldNameToID(subField.FieldType.FullName);
#if WRITE_LOG
                Console.WriteLine("    Field '{0}' type '{1}' byteOffsetInComponent={2}", tokens[idx],
                    subField.FieldType.Name, byteOffsetInComponent);
#endif
            }

            if (primitiveType < 0)
                throw new ArgumentException($"TypeManager.GetField() failed to find PrimitiveFieldType for '{val}'");
        }
    }
}
