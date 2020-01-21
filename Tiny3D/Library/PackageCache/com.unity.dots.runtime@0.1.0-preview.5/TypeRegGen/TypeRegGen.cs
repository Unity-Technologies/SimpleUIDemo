using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Entities.BuildUtils;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using TypeGenInfoList = System.Collections.Generic.List<Unity.ZeroPlayer.TypeGenInfo>;
using TypeInfoMap = System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<Unity.ZeroPlayer.TypeGenInfo>>;
using SystemTypeGen = TypeRegGen.SystemTypeGen;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Unity.ZeroPlayer
{
    // Mirrors the definition in Unity.Entities.TypeManager
    public enum TypeCategory : int
    {
        ComponentData = 0,
        BufferData,
        ISharedComponentData,
        EntityData,
        Class,

        Null // Added sentinel value
    }

    // Mirrors the definition in Unity.LowLevel.Allocator
    internal enum Allocator
    {
        Invalid = 0,
        None = 1,
        Temp = 2,
        TempJob = 3,
        Persistent = 4
    }

    public enum Profile : int
    {
        DotNet = 0,
        DOTSDotNet,
        DOTSNative
    }

    internal struct ECSTypeInfo
    {
        public TypeCategory TypeCategory;
        public string FullTypeName;
    }

    internal struct TypeGenInfo
    {
        public TypeDefinition TypeDefinition;
        public TypeCategory TypeCategory;
        public List<int> EntityOffsets;
        public int EntityOffsetIndex;
        public List<int> BlobAssetRefOffsets;
        public int BlobAssetRefOffsetIndex;
        public HashSet<int> WriteGroupTypeIndices;
        public int WriteGroupsIndex;
        public int TypeIndex;
        public bool IsManaged;
        public TypeUtils.AlignAndSize AlignAndSize;
        public int BufferCapacity;
        public int MaxChunkCapacity;
        public int ElementSize;
        public int SizeInChunk;
        public int Alignment;
        public ulong StableHash;
        public ulong MemoryOrdering;
    }

    public class Autoclose : IDisposable
    {
        Action disposeClose;
        private bool called = false;

        public Autoclose(Action disposeClose)
        {
            this.disposeClose = disposeClose;
        }

        public void Dispose()
        {
            if (!called)
            {
                called = true;
                disposeClose();
            }
        }
    }

    internal static class Extensions
    {
        // Extension function to Mono.Cecil to allow for creating a reference to a method for a type with generic parameters such as NativeArray<T>
        // https://groups.google.com/forum/#!topic/mono-cecil/mCat5UuR47I
        public static MethodReference MakeHostInstanceGeneric(this MethodReference self, params TypeReference[] args)
        {
            GenericInstanceType generic = self.DeclaringType.MakeGenericInstanceType(args);
            var reference = new MethodReference(self.Name, self.ReturnType, generic)
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
            {
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var genericParam in self.GenericParameters)
            {
                reference.GenericParameters.Add(new GenericParameter(genericParam.Name, reference));
            }

            return reference;
        }

        public static void WriteNameValue<T>(this JsonTextWriter jw, in string name, in T value)
        {
            jw.WritePropertyName(name);
            jw.WriteValue(value);
        }

        public static Autoclose WriteStartObjectAuto(this JsonTextWriter jw)
        {
            jw.WriteStartObject();
            return new Autoclose(() => jw.WriteEndObject());
        }

        public static Autoclose WriteStartArrayAuto(this JsonTextWriter jw)
        {
            jw.WriteStartArray();
            return new Autoclose(() => jw.WriteEndArray());
        }

        public static Autoclose WriteStartConstructorAuto(this JsonTextWriter jw, in string name)
        {
            jw.WriteStartConstructor(name);
            return new Autoclose(() => jw.WriteEndConstructor());
        }
    }

    public class TypeRegGen
    {
        internal class AssemblyResolver : DefaultAssemblyResolver
        {
            public Dictionary<string, int> AssemblyNameToIndexMap;
            public List<AssemblyDefinition> AssemblyDefinitions;

            public AssemblyResolver(ref List<AssemblyDefinition> assemblyDefinitions)
            {
                AssemblyNameToIndexMap = new Dictionary<string, int>();
                AssemblyDefinitions = assemblyDefinitions;
            }

            public void Add(AssemblyDefinition knownAssembly)
            {
                int index = AssemblyDefinitions.Count;
                AssemblyDefinitions.Add(knownAssembly);
                AssemblyNameToIndexMap.Add(knownAssembly.Name .FullName, index);
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                if (AssemblyNameToIndexMap.TryGetValue(name.FullName, out var asmIndex))
                {
                    return AssemblyDefinitions[asmIndex];
                }

                return base.Resolve(name, parameters);
            }

            protected override void Dispose(bool disposing)
            {
                foreach (var asm in AssemblyDefinitions)
                    asm.Dispose();

                AssemblyDefinitions.Clear();
                base.Dispose(disposing);
            }
        }

        public static void Main(string[] args)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            TypeRegGen typeRegGen = new TypeRegGen();
            typeRegGen.GenerateTypeRegistry(args);

            stopwatch.Stop();
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            Console.WriteLine("Static Type Registry Generation Time: {0}ms", elapsedMs);
        }

        public TypeRegGen()
        {
            m_ArchBits = 64;
            m_Profile = Profile.DOTSNative;

            m_AssemblyDefs = new List<AssemblyDefinition>();
            m_TypeDefToTypeIndex = new Dictionary<TypeDefinition, int>();
        }

        public void GenerateTypeRegistry(string[] args)
        {
            var assemblyResolver = new AssemblyResolver(ref m_AssemblyDefs);
            var symbolReaderProvider = new DefaultSymbolReaderProvider();

            ProcessArgs(args, assemblyResolver, symbolReaderProvider);

            // If the assembly already exists use the one we read in, otherwise create it
            m_TypeRegAssembly = m_AssemblyDefs.FirstOrDefault(asm => asm.Name.Name == kStaticRegAsmName);
            if (m_TypeRegAssembly == null)
            {
                m_TypeRegAssembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(kStaticRegAsmName, new Version()),
                kStaticRegAsmNameWithExtension, ModuleKind.Dll);
            }
            else
            {
                // We need to remove the pre-exiting StaticTypeRegistry class as we will replace it with our own and we don't want multiple definitions
                var type = m_TypeRegAssembly.MainModule.Types.FirstOrDefault(t => t.Name == kStaticRegClassName);

                if (type != null)
                    m_TypeRegAssembly.MainModule.Types.Remove(type);
            }

            InitializeTypeReferences();

            var assemblySet = new HashSet<AssemblyNameDefinition>();
            TypeGenInfoList typeGenInfoList = PopulateWithTypesFromAssemblies(ref assemblySet);

            // add references to all the m_AssemblyDefs that the individual component data types use
            foreach (var asm in assemblySet)
                m_TypeRegAssembly.MainModule.AssemblyReferences.Add(asm);

            // create a class with a static function
            m_RegClass = new TypeDefinition(kStaticRegAsmName, kStaticRegClassName, TypeAttributes.Class | TypeAttributes.Public, m_TypeRegAssembly.MainModule.ImportReference(typeof(object)));

            GenerateDeclarations();
            GenerateDefinitions(in typeGenInfoList);

            m_fieldInfoGen = new FieldInfoGen(typeGenInfoList, m_ArchBits, m_TypeRegAssembly, m_EntityAssembly);
            m_executeGen = new ExecuteGen(m_AssemblyDefs);
            m_interfaceGen = new InterfaceGen(m_AssemblyDefs, m_ArchBits);

            m_interfaceGen.AddMethods();
            m_interfaceGen.PatchJobsCode();
            m_interfaceGen.AddGenExecuteMethodMethods();
            PatchCalls();

            m_TypeRegAssembly.MainModule.Types.Add(m_RegClass);

            var writerParams = new WriterParameters() { WriteSymbols = m_TypeRegAssembly.MainModule.HasSymbols };
            var outPath = Path.Combine(m_OutputDir, m_TypeRegAssembly.Name.Name) + ".dll";
            m_TypeRegAssembly.Write(outPath, writerParams);

            // Add the `typeGenInfoList` and `m_Systems` list, then remove dupes, pass
            // the now unique list in for fixup.
            var typeList = typeGenInfoList.Select(t => t.TypeDefinition).ToList();
            if(m_Systems != null && m_Systems.Count > 0)
                typeList.AddRange(m_Systems);

            typeList = typeList.Distinct().Where(t=>t != null).ToList();
            FixupAssemblies(in typeList, in m_interfaceGen.typesToMakePublic);

            SaveDebugMeta(typeGenInfoList, m_Systems, m_interfaceGen.jobList);

            assemblyResolver.Dispose();
            m_MscorlibAssembly.Dispose();
            m_EntityAssembly.Dispose();
            m_TypeRegAssembly.Dispose();
        }

        internal void ProcessArgs(string[] args, AssemblyResolver assemblyResolver, ISymbolReaderProvider symbolReaderProvider)
        {
            int argIndex = 0;
            m_OutputDir = Path.GetFullPath(args[argIndex++]);
            
            var archBitsStr = args[argIndex++];
            var profileStr = args[argIndex++];
            var configStr = args[argIndex++].ToLower();

            if (!int.TryParse(archBitsStr, out m_ArchBits) || (m_ArchBits != 32 && m_ArchBits != 64))
                throw new ArgumentException($"Invalid architecture-bits passed in as second argument. Received '{archBitsStr}', Expected '32' or '64'.");

            if (!Enum.TryParse(profileStr, out m_Profile))
                throw new ArgumentException($"Invalid Profile Type passed in as third argument. Received '{profileStr}', Expected '0' (DotNet) or '1' (DOTSDotNet) or '2' (DOTSNative).");

            if (configStr != "debug" && configStr != "release")
                throw new ArgumentException($"Invalid Config passed in as fourth argument. Received '{configStr}', Expected 'debug' or 'release'.");
            else
                m_IsDebug = configStr == "debug";

            for (int i = argIndex; i < args.Length; ++i)
            {
                string normalizedPath = Path.GetFullPath(args[i]);
                if (!File.Exists(normalizedPath))
                {
                    Console.WriteLine($"Could not find assembly '{normalizedPath}': Please check your commandline arguments.");
                    continue;
                }

                // We don't want to read in any old StaticTypeRegisty assemblies. Just skip them if passed in
                if (Path.GetFileNameWithoutExtension(normalizedPath) == kStaticRegAsmName)
                {
                    continue;
                }

                // If we know we are reading from where we will be writing, ensure we open for readwrite
                bool bReadWrite = Path.GetDirectoryName(normalizedPath) == m_OutputDir;
                // This assembly tends to be in escalated privledge directories so we only open for read (we shouldn't need to write to it anyway)
                if (Path.GetFileNameWithoutExtension(normalizedPath) == "UnityEngine.CoreModule")
                {
                    bReadWrite = false;
                }

                var pdbPath = Path.ChangeExtension(normalizedPath, "pdb");
                var readerParams = new ReaderParameters() { AssemblyResolver = assemblyResolver, ReadWrite = bReadWrite, InMemory = true };
                if (File.Exists(pdbPath))
                {
                    readerParams.SymbolReaderProvider = symbolReaderProvider;
                    readerParams.ReadSymbols = true;
                }

                var asm = AssemblyDefinition.ReadAssembly(normalizedPath, readerParams);
                assemblyResolver.Add(asm);
            }

            // Entity is special so we maintain a specific reference to it so we can ensure it is always registed as typeIndex 1 (0 being reserved for null)
            m_EntityAssembly = m_AssemblyDefs.First(asm => asm.Name.Name == "Unity.Entities");
            m_EntityTypeDef = m_EntityAssembly.MainModule.GetType("Unity.Entities.Entity");

            m_MscorlibAssembly = m_AssemblyDefs.FirstOrDefault(asm => asm.Name.Name == "mscorlib");
            if (m_MscorlibAssembly == null)
            {
                var readerParams = new ReaderParameters() { AssemblyResolver = assemblyResolver };
                m_MscorlibAssembly = AssemblyDefinition.ReadAssembly(typeof(object).Assembly.Location, readerParams);
                assemblyResolver.Add(m_MscorlibAssembly);
            }
        }

        internal TypeGenInfo CreateTypeGenInfo(TypeDefinition type, TypeCategory typeCategory)
        {
            TypeUtils.AlignAndSize alignAndSize = new TypeUtils.AlignAndSize();
            List<int> entityOffsets = new List<int>();
            List<int> blobAssetRefOffsets = new List<int>();
            bool isManaged = type != null && type.IsManagedType();

            if (type == m_EntityTypeDef)
            {
                // Entity is special. We require Entity to have an EntityOffset at position 0
                entityOffsets.Add(0);
                alignAndSize = TypeUtils.AlignAndSizeOfType(type, m_ArchBits);
            }
            else if (!isManaged && type != null)
            {
                entityOffsets = TypeUtils.GetEntityFieldOffsets(type, m_ArchBits);
                blobAssetRefOffsets = TypeUtils.GetFieldOffsetsOf("Unity.Entities.BlobAssetReference`1", type, m_ArchBits);
                alignAndSize = TypeUtils.AlignAndSizeOfType(type, m_ArchBits);
            }

            int typeIndex = m_TotalTypeCount++;
            bool isSystemStateBufferElement = DoesTypeInheritInterface(type, "Unity.Entities.ISystemStateBufferElementData");
            bool isSystemStateSharedComponent = DoesTypeInheritInterface(type, "Unity.Entities.ISystemStateSharedComponentData");
            bool isSystemStateComponent = DoesTypeInheritInterface(type, "Unity.Entities.ISystemStateComponentData") || isSystemStateSharedComponent || isSystemStateBufferElement;

            if (typeIndex != 0)
            {
                if (alignAndSize.empty || typeCategory == TypeCategory.ISharedComponentData)
                    typeIndex |= ZeroSizeInChunkTypeFlag;

                if (typeCategory == TypeCategory.ISharedComponentData)
                    typeIndex |= SharedComponentTypeFlag;

                if (isSystemStateComponent)
                    typeIndex |= SystemStateTypeFlag;

                if (isSystemStateSharedComponent)
                    typeIndex |= SystemStateSharedComponentTypeFlag;

                if (typeCategory == TypeCategory.BufferData)
                    typeIndex |= BufferComponentTypeFlag;

                if (entityOffsets.Count == 0)
                    typeIndex |= HasNoEntityReferencesFlag;

                if (isManaged)
                    typeIndex |= ManagedComponentTypeFlag;
            }

            CalculateMemoryOrderingAndStableHash(type, out ulong memoryOrdering, out ulong stableHash);

            // No TypeCategory.Null in Hybrid system - use TypeCategory.ComponentType for null type
            if (typeCategory == TypeCategory.Null)
                typeCategory = TypeCategory.ComponentData;

            // Determine if there is a special buffer capacity set for the type
            int bufferCapacity = -1;
            if (typeCategory == TypeCategory.BufferData && type.CustomAttributes.Count > 0)
            {
                var forcedCapacityAttribute = type.CustomAttributes.FirstOrDefault(ca => ca.Constructor.DeclaringType.Name == "InternalBufferCapacityAttribute");
                if (forcedCapacityAttribute != null)
                {
                    bufferCapacity = (int)forcedCapacityAttribute.ConstructorArguments
                        .First(arg => arg.Type.Name == "Int32")
                        .Value;
                }
            }

            // Determine max chunk capacity constratins if any are specified
            int maxChunkCapacity = int.MaxValue;
            if (type != null && type.CustomAttributes.Count > 0)
            {
                var maxChunkCapacityAttribute = type.CustomAttributes.FirstOrDefault(ca => ca.Constructor.DeclaringType.Name == "MaximumChunkCapacityAttribute");
                if (maxChunkCapacityAttribute != null)
                {
                    maxChunkCapacity = (int)maxChunkCapacityAttribute.ConstructorArguments
                        .First(arg => arg.Type.Name == "Int32")
                        .Value;
                }
            }

            int elementSize = 0;
            int sizeInChunk = 0;
            int alignment = 0;
            if (type != null && typeCategory != TypeCategory.ISharedComponentData && !isManaged)
            {
                elementSize = alignAndSize.empty ? 0 : alignAndSize.size;
                sizeInChunk = elementSize;
                //alignment = alignAndSize.align;

                // We need to match what the dynamic type registry code currently does:
                // - Default and maximum alignment is 16.
                // - If the size of the data is a power of two, use it as the alignment.
                // - Otherwise, 16.
                alignment = 16;
                if (sizeInChunk == 0)
                {
                    alignment = 1;
                }
                else if (sizeInChunk < 16 && (sizeInChunk & (sizeInChunk - 1)) == 0)
                {
                    alignment = sizeInChunk;
                }
            }

            if (typeCategory == TypeCategory.BufferData)
            {
                // If we haven't overidden the bufferSize via an attribute
                if (bufferCapacity == -1)
                {
                    bufferCapacity = 128 / elementSize;
                }

                var bufferHeaderAlignAndSize = TypeUtils.AlignAndSizeOfType(m_BufferHeaderDef, m_ArchBits);
                if (bufferHeaderAlignAndSize.size != 16)
                    throw new Exception("Size of BufferHeader has been changed and our code needs updating");

                sizeInChunk = (bufferCapacity * elementSize) + bufferHeaderAlignAndSize.size;
            }

            var typeGenInfo = new TypeGenInfo()
            {
                TypeDefinition = type,
                TypeIndex = typeIndex,
                TypeCategory = typeCategory,
                EntityOffsets = entityOffsets,
                EntityOffsetIndex = type == null ? -1 : m_TotalEntityOffsetCount,
                BlobAssetRefOffsets = blobAssetRefOffsets,
                BlobAssetRefOffsetIndex = m_TotalBlobAssetRefOffsetCount,
                WriteGroupTypeIndices = new HashSet<int>(),
                WriteGroupsIndex = 0,
                IsManaged = isManaged,
                AlignAndSize = alignAndSize,
                BufferCapacity = bufferCapacity,
                MaxChunkCapacity = maxChunkCapacity,
                ElementSize = elementSize,
                SizeInChunk = sizeInChunk,
                Alignment = alignment,
                StableHash = stableHash,
                MemoryOrdering = memoryOrdering,
            };

            m_TotalEntityOffsetCount += entityOffsets.Count;
            m_TotalBlobAssetRefOffsetCount += blobAssetRefOffsets.Count;

            return typeGenInfo;
        }

        /// <summary>
        /// Iterates over all assemblies and returns a TypeGenInfo list which contains, in typeIndex order, enough information per type to generate constant type information for the registry
        /// </summary>
        internal TypeGenInfoList PopulateWithTypesFromAssemblies(ref HashSet<AssemblyNameDefinition> assemblySet)
        {
            var typeGenInfoList = new TypeGenInfoList();

            // Create 'special' types and insert them before any others

            // Unity.Entities relies on this being index 0
            typeGenInfoList.Insert(0, CreateTypeGenInfo(null, TypeCategory.Null));
            // Unity.Entities relies on this being index 1
            typeGenInfoList.Insert(1, CreateTypeGenInfo(m_EntityTypeDef, TypeCategory.EntityData));

            var typesToRegister = new TypeInfoMap();

            foreach (var ecsType in ECSTypesToRegister)
            {
                typesToRegister[(int)ecsType.TypeCategory] = new List<TypeGenInfo>();
            }

            var foundGenericTypes = new List<TypeDefinition>();

            // Go through list of m_AssemblyDefs and collect all types that implement one of the interfaces
            foreach (var asm in m_AssemblyDefs)
            {
                foreach (var ecsType in ECSTypesToRegister)
                {
                    var foundList = typesToRegister[(int)ecsType.TypeCategory];
                    var interfaceType = m_EntityAssembly.MainModule.GetType(ecsType.FullTypeName).Resolve();

                    foreach (var type in asm.MainModule.GetAllTypes().Where(t => (t.IsValueType || t.IsClass)))
                    {
                        bool IsValidType(TypeDefinition t) => t.Interfaces.Select(f => f.InterfaceType.Resolve()).Contains(interfaceType);

                        bool validType = IsValidType(type);
                        if (!validType && type.BaseType != null)
                        {
                            var typeToInspect = type.BaseType.Resolve();
                            while (!(validType = IsValidType(typeToInspect)))
                            {
                                if (typeToInspect == null || typeToInspect.BaseType == null)
                                    break;
                                typeToInspect = typeToInspect.BaseType.Resolve();
                            }
                        }

                        if (!validType)
                            continue;

                        if (type.HasGenericParameters)
                        {
                            foundGenericTypes.Add(type);
                        }
                        else
                        {
                            foundList.Add(CreateTypeGenInfo(type, ecsType.TypeCategory));
                        }

                        assemblySet.Add(type.Module.Assembly.Name);
                    }
                }
            }

            // we need to do more work to figure out the actual type params of generic component etc. types
            foreach (var genericType in foundGenericTypes)
            {
                //foreach (var asm in m_AssemblyDefs) {
                //foreach (var type in asm.MainModule.Types.Where(t => t.IsGenericInstance(genericType)
            }

            typeGenInfoList.AddRange(typesToRegister.SelectMany(p => p.Value).ToList());
            typeGenInfoList.Sort((t1, t2) => (t1.TypeIndex & ClearFlagsMask).CompareTo((t2.TypeIndex & ClearFlagsMask)));

            if (typeGenInfoList.Count != m_TotalTypeCount)
                throw new InvalidProgramException("The TypeGenInfo list must contain the same number of types in total as found across all assemblies. There must be a bug in TypeRegGen.cs");

            foreach(var typeGenInfo in typeGenInfoList)
            {
                if (typeGenInfo.TypeDefinition == null)
                    continue;

                m_TypeDefToTypeIndex[typeGenInfo.TypeDefinition] = typeGenInfo.TypeIndex;
            }

            PopulateWriteGroups(ref typeGenInfoList);

            return typeGenInfoList;
        }


        void RecursePatchCalls(TypeDefinition type, AssemblyDefinition asm)
        {
            foreach (TypeDefinition td in type.NestedTypes)
            {
                RecursePatchCalls(td, asm);
            }

            foreach (var method in type.Methods)
            {
                if (method.HasBody)
                {
                    // The goal here is to scan a subset of methods; there just happens to be a one to one
                    // mapping. When we scan something else, it can bucked into the Call or Callvirt group.
                    var outerGIList = method.Body.Instructions.Where(i => i.OpCode == OpCodes.Call);
                    m_executeGen.PatchScheduleCallsAndGenerateIL(type, asm, method, outerGIList);
                    m_fieldInfoGen.PatchFieldInfoCalls(type, asm, method);
                }
            }
        }

        /// <summary>
        /// This does call-site patching of Schedule() calls.
        /// Transforming:
        ///     job.Schedule(system);
        /// into:
        ///     JobProcessComponentDataExtensions.Schedule_D<Game.RotateSpriteSystem.RotateSpritesJob, Rotation>(system);
        ///
        /// generically the above is:
        ///     Schedule_D<TJob, T0>(TJob job, ComponentSystemBase system);
        /// </summary>
        public void PatchCalls()
        {
            foreach (AssemblyDefinition asm in m_AssemblyDefs)
            {
                foreach (ModuleDefinition mod in asm.Modules)
                {
                    foreach (TypeDefinition t in mod.Types)
                    {
                        RecursePatchCalls(t, asm);
                    }
                }
            }
        }


        /// <summary>
        /// Declares all fields and member functions for the Static Type Registry
        /// </summary>
        internal void GenerateDeclarations()
        {
            // Note: The actual construction of fields is done in the generated .CCTOR

            //////////////////
            // Declare fields
            //////////////////

            // Declares: static public readonly TypeInfo[] sTypeInfoArray
            var typeInfoArrayRef = m_TypeInfoRef.MakeArrayType();
            m_RegTypeInfoArrayDef = new FieldDefinition("TypeInfos", Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.InitOnly, typeInfoArrayRef);
            m_RegClass.Fields.Add(m_RegTypeInfoArrayDef);

            // Declares: static public readonly int[] sEntityOffsetArray
            var entityOffsetInfoArrayRef = m_TypeRegAssembly.MainModule.ImportReference(typeof(int)).MakeArrayType();
            m_RegEntityOffsetArrayDef = new FieldDefinition("EntityOffsets", Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.InitOnly, entityOffsetInfoArrayRef);
            m_RegClass.Fields.Add(m_RegEntityOffsetArrayDef);

            // Declares: static public readonly int[] sBlobAssetReferenceOffsetArray
            var blobAssetReferenceOffsetsArrayRef = m_TypeRegAssembly.MainModule.ImportReference(typeof(int)).MakeArrayType();
            m_RegBlobAssetReferneceOffsetsArrayDef = new FieldDefinition("BlobAssetReferenceOffsets", Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.InitOnly, blobAssetReferenceOffsetsArrayRef);
            m_RegClass.Fields.Add(m_RegBlobAssetReferneceOffsetsArrayDef);

            // Declares: static public readonly int[] sWriteGroupArrayDef
            var writeGroupArrayRef = m_TypeRegAssembly.MainModule.ImportReference(typeof(int)).MakeArrayType();
            m_WriteGroupArrayDef = new FieldDefinition("WriteGroups", Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.InitOnly, writeGroupArrayRef);
            m_RegClass.Fields.Add(m_WriteGroupArrayDef);

            // Declares: static public readonly Type[] sTypeArray
            var systemTypeArrayRef = m_SystemTypeRef.MakeArrayType();
            m_RegSystemTypeArrayDef = new FieldDefinition("Types", Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.InitOnly, systemTypeArrayRef);
            m_RegClass.Fields.Add(m_RegSystemTypeArrayDef);

            var stringTypeRef = m_TypeRegAssembly.MainModule.ImportReference(typeof(string));
            var sStringArrayRef = stringTypeRef.MakeArrayType();
            m_RegSystemTypeNameArrayDef = new FieldDefinition("TypeNames", Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.InitOnly, sStringArrayRef);
            m_RegClass.Fields.Add(m_RegSystemTypeNameArrayDef);

            if (m_Profile != Profile.DotNet) // Currently not supported in Hybrid builds
            {
                // static public readonly Type[] Systems
                var systemSystemsArrayRef = m_SystemTypeRef.MakeArrayType();
                m_RegSystemSystemsArrayDef = new FieldDefinition("Systems", Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.InitOnly, systemSystemsArrayRef);
                m_RegClass.Fields.Add(m_RegSystemSystemsArrayDef);

                // static public readonly bool[] SystemIsGroup
                var boolTypeRef = m_TypeRegAssembly.MainModule.ImportReference(typeof(bool));
                var sBoolArrayRef = boolTypeRef.MakeArrayType();
                m_RegSystemIsGroupArrayDef = new FieldDefinition("SystemIsGroup", Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.InitOnly, sBoolArrayRef);
                m_RegClass.Fields.Add(m_RegSystemIsGroupArrayDef);

                // static public readonly string[] SystemName
                m_RegSystemNameArrayDef = new FieldDefinition("SystemName", Mono.Cecil.FieldAttributes.Static | Mono.Cecil.FieldAttributes.Public | Mono.Cecil.FieldAttributes.InitOnly, sStringArrayRef);
                m_RegClass.Fields.Add(m_RegSystemNameArrayDef);
            }

            ///////////
            // Methods
            ///////////

            // Declares: static public StaticTypeRegistry() (the static ctor)
            m_RegCCTOR = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, m_TypeRegAssembly.MainModule.ImportReference(typeof(void)));
            m_RegClass.Methods.Add(m_RegCCTOR);
            m_RegClass.IsBeforeFieldInit = true;

            // Declares & implements: static public RegisterStaticTypes()
            var registerStaticTypesFn = GenerateRegisterStaticTypesFn();
            m_RegClass.Methods.Add(registerStaticTypesFn);

            if (m_Profile != Profile.DotNet) // Currently not supported in Hybrid builds
            {
                // Declare & implements: static public CreateSystem()
                m_Systems = SystemTypeGen.GetSystems(m_AssemblyDefs);
                var createSystemsFn = SystemTypeGen.GenCreateSystems(m_Systems, m_AssemblyDefs, m_TypeRegAssembly.MainModule, m_GetTypeFromHandleFnRef, m_InvalidOpExceptionCTORRef);
                m_RegClass.Methods.Add(createSystemsFn);

                // Declares: static public GetSystemAttributes()
                var getSystemAttributesFn = SystemTypeGen.GenGetSystemAttributes(m_Systems, m_AssemblyDefs, m_TypeRegAssembly.MainModule, m_GetTypeFromHandleFnRef, m_InvalidOpExceptionCTORRef);
                m_RegClass.Methods.Add(getSystemAttributesFn);
            }

            // Declares: static public bool Equals(object lhs, object rhs, int typeIndex)
            // This function is required to allow users to query for equality when a Generic <T> param isn't available but the 'int' typeIndex is
            m_RegBoxedEqualsFn = new MethodDefinition("Equals", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig, m_TypeRegAssembly.MainModule.ImportReference(typeof(bool)));
            m_RegBoxedEqualsFn.Parameters.Add(new ParameterDefinition("lhs", Mono.Cecil.ParameterAttributes.None, m_TypeRegAssembly.MainModule.ImportReference(typeof(object))));
            m_RegBoxedEqualsFn.Parameters.Add(new ParameterDefinition("rhs", Mono.Cecil.ParameterAttributes.None, m_TypeRegAssembly.MainModule.ImportReference(typeof(object))));
            m_RegBoxedEqualsFn.Parameters.Add(new ParameterDefinition("typeIndex", Mono.Cecil.ParameterAttributes.None, m_TypeRegAssembly.MainModule.ImportReference(typeof(int))));
            m_RegClass.Methods.Add(m_RegBoxedEqualsFn);

            // Declares: static public bool Equals(object lhs, void* rhs, int typeIndex)
            // This function is required to allow users to query for equality when a Generic <T> param isn't available but the 'int' typeIndex is
            m_RegBoxedPtrEqualsFn = new MethodDefinition("Equals", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig, m_TypeRegAssembly.MainModule.ImportReference(typeof(bool)));
            m_RegBoxedPtrEqualsFn.Parameters.Add(new ParameterDefinition("lhs", Mono.Cecil.ParameterAttributes.None, m_TypeRegAssembly.MainModule.ImportReference(typeof(object))));
            m_RegBoxedPtrEqualsFn.Parameters.Add(new ParameterDefinition("rhs", Mono.Cecil.ParameterAttributes.None, m_TypeRegAssembly.MainModule.ImportReference(typeof(void*))));
            m_RegBoxedPtrEqualsFn.Parameters.Add(new ParameterDefinition("typeIndex", Mono.Cecil.ParameterAttributes.None, m_TypeRegAssembly.MainModule.ImportReference(typeof(int))));
            m_RegClass.Methods.Add(m_RegBoxedPtrEqualsFn);

            // Declares: static public int GetHashCode(object val, int typeIndex)
            // This function is required to allow users to query for equality when a Generic <T> param isn't available but the 'int' typeIndex is
            m_RegBoxedGetHashCodeFn = new MethodDefinition("BoxedGetHashCode", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig, m_TypeRegAssembly.MainModule.ImportReference(typeof(int)));
            m_RegBoxedGetHashCodeFn.Parameters.Add(new ParameterDefinition("val", Mono.Cecil.ParameterAttributes.None, m_TypeRegAssembly.MainModule.ImportReference(typeof(object))));
            m_RegBoxedGetHashCodeFn.Parameters.Add(new ParameterDefinition("typeIndex", Mono.Cecil.ParameterAttributes.None, m_TypeRegAssembly.MainModule.ImportReference(typeof(int))));
            m_RegClass.Methods.Add(m_RegBoxedGetHashCodeFn);
        }

        internal void GenerateDefinitions(in TypeGenInfoList typeGenInfoList)
        {
            // Static constructor where most logic is contained for filling the registry's readonly fields
            GenerateStaticTypeRegistryCCTOR(in typeGenInfoList);

            // Declares: public static unsafe object ConstructComponentFromBuffer(int typeIndex, void* data)
            var constructComponentFromBufferFn = GenConstructComponentFromBuffer(in typeGenInfoList);
            m_RegClass.Methods.Add(constructComponentFromBufferFn);
        }

        internal MethodDefinition GenConstructComponentFromBuffer(in TypeGenInfoList typeGenInfoList)
        {
            // Check out HardcodedCreateSystem for the C# and IL of how this works.
            var createComponentFn = new MethodDefinition(
                "ConstructComponentFromBuffer",
                MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
                m_TypeRegAssembly.MainModule.ImportReference(typeof(object)));

            createComponentFn.Parameters.Add(
                new ParameterDefinition("typeIndexNoFlags",
                Mono.Cecil.ParameterAttributes.None,
                m_TypeRegAssembly.MainModule.ImportReference(typeof(int))));

            var srcPtrArg =
                new ParameterDefinition("buffer",
                Mono.Cecil.ParameterAttributes.None,
                m_TypeRegAssembly.MainModule.ImportReference(typeof(void*)));
            createComponentFn.Parameters.Add(srcPtrArg);

            createComponentFn.Body.InitLocals = true;
            var bc = createComponentFn.Body.Instructions;

            foreach(var typeInfo in typeGenInfoList)
            {
                if (typeInfo.TypeDefinition == null)
                    continue;

                var componentRef = m_TypeRegAssembly.MainModule.ImportReference(typeInfo.TypeDefinition);

                bc.Add(Instruction.Create(OpCodes.Ldarg_0));
                bc.Add(Instruction.Create(OpCodes.Ldc_I4, typeInfo.TypeIndex & ClearFlagsMask));
                bc.Add(Instruction.Create(OpCodes.Ceq));
                int branchToNext = bc.Count;
                bc.Add(Instruction.Create(OpCodes.Nop));    // will be: Brfalse_S nextTestCase

                // Work inside if block
                var local = new VariableDefinition(componentRef);
                createComponentFn.Body.Variables.Add(local);
                bc.Add(Instruction.Create(OpCodes.Ldloca, local));
                bc.Add(Instruction.Create(OpCodes.Initobj, componentRef));

                bc.Add(Instruction.Create(OpCodes.Ldloca, local));
                bc.Add(Instruction.Create(OpCodes.Ldarg, srcPtrArg));
                bc.Add(Instruction.Create(OpCodes.Ldc_I8, (long) typeInfo.AlignAndSize.size));
                bc.Add(Instruction.Create(OpCodes.Call, m_TypeRegAssembly.MainModule.ImportReference(m_MemCpyFnRef)));

                bc.Add(Instruction.Create(OpCodes.Ldloc, local));
                bc.Add(Instruction.Create(OpCodes.Box, componentRef));

                bc.Add(Instruction.Create(OpCodes.Ret));

                var nextTest = Instruction.Create(OpCodes.Nop);
                bc.Add(nextTest);

                bc[branchToNext] = Instruction.Create(OpCodes.Brfalse_S, nextTest);
            }
            bc.Add(Instruction.Create(OpCodes.Ldstr, "FATAL: Tried to construct a type that is unknown to the StaticTypeRegistry"));
            bc.Add(Instruction.Create(OpCodes.Newobj, m_InvalidOpExceptionCTORRef));
            bc.Add(Instruction.Create(OpCodes.Throw));
            return createComponentFn;
        }

        /// <summary>
        /// Initializes all fields and static variables for the StaticTypeRegistry.
        /// For debug configs, the registry will also generate additional validation code to ensure the TypeRegGen's constant TypeInfo/EntityOffset data is correct for the target platform
        /// </summary>
        internal void GenerateStaticTypeRegistryCCTOR(in TypeGenInfoList typeGenInfoList)
        {
            var il = m_RegCCTOR.Body.GetILProcessor();

            GeneratePlatformValidation(ref il);

            // Type Information
            {
                GenerateEntityOffsetInfoArray(ref il, in typeGenInfoList);
                GenerateBlobAssetReferenceArray(ref il, in typeGenInfoList);
                GenerateWriteGroupArray(ref il, in typeGenInfoList);
                GenerateTypeArray(ref il, typeGenInfoList.Select(t => t.TypeDefinition).ToList(), m_TotalTypeCount, m_RegSystemTypeArrayDef);
                var typeNames = GetTypeNames(typeGenInfoList);
                GenerateStringArray(ref il, m_RegSystemTypeNameArrayDef, typeNames);
                GenerateTypeInfoArray(ref il, in typeGenInfoList);

                GenerateEqualityFunctions(ref il, typeGenInfoList);
            }

            // System Information
            if (m_Profile != Profile.DotNet) // Currently not supported in Hybrid builds
            {
                GenerateTypeArray(ref il, m_Systems, m_Systems.Count, m_RegSystemSystemsArrayDef);

                var systemIsGroup = SystemTypeGen.GetSystemIsGroup(m_AssemblyDefs, m_Systems);
                GenerateBoolArray(ref il, m_RegSystemIsGroupArrayDef, systemIsGroup);

                var systemNames = SystemTypeGen.GetSystemNames(m_Systems);
                GenerateStringArray(ref il, m_RegSystemNameArrayDef, systemNames);
            }

            il.Emit(OpCodes.Ret); // Return from static constructor
        }

        /// <summary>
        /// Generate a small platform check to validate the runtime platform matches what the TypeRegGen thought it was targeting when generating the IL.
        /// </summary>
        internal void GeneratePlatformValidation(ref ILProcessor il)
        {
            Instruction platformValidationEnd = il.Create(OpCodes.Nop);

            // Check if IntPtr.Size == 8 when running code generated assuming a 64-bit platform.
            // If the comparison is false, throw an exception
            il.Emit(OpCodes.Call, m_IntPtrGetSizeFnRef);
            if(m_ArchBits == 64)
            {
                EmitLoadConstant(ref il, 8);
            }
            else
            {
                EmitLoadConstant(ref il, 4);
            }

            il.Emit(OpCodes.Beq, platformValidationEnd); // if check passes jump to end of function (ret)

            // These instructions only run if the comparison was false
            il.Emit(OpCodes.Ldstr, $"FATAL: Runtime platform architecture does not match the architecture targeted when generating the Static Type Registry! Are you using the correct {kStaticRegAsmNameWithExtension}?");
            il.Emit(OpCodes.Newobj, m_InvalidOpExceptionCTORRef);
            il.Emit(OpCodes.Throw);

            il.Append(platformValidationEnd);
        }

        /// <summary>
        /// Populates the registry's entityOffset int array.
        /// Offsets are laid out contiguously in memory such that the memory layout for Types A (2 entites), B (3 entities), C (0 entities) D (2 entities) is as such: aabbbdd
        /// </summary>
        internal void GenerateEntityOffsetInfoArray(ref ILProcessor il, in TypeGenInfoList typeGenInfoList)
        {
            PushNewArray(ref il, m_TypeRegAssembly.MainModule.ImportReference(typeof(int)), m_TotalEntityOffsetCount);

            int entityOffsetIndex = 0;
            foreach (var typeGenInfo in typeGenInfoList)
            {
                foreach (var offset in typeGenInfo.EntityOffsets)
                {
                    PushNewArrayElement(ref il, entityOffsetIndex++);
                    EmitLoadConstant(ref il, offset);
                    il.Emit(OpCodes.Stelem_Any, m_TypeRegAssembly.MainModule.ImportReference(typeof(int)));
                }
            }

            StoreTopOfStackToStaticField(ref il, m_RegEntityOffsetArrayDef);
        }

        /// <summary>
        /// Populates the registry's entityOffset int array.
        /// Offsets are laid out contiguously in memory such that the memory layout for Types A (2 entites), B (3 entities), C (0 entities) D (2 entities) is as such: aabbbdd
        /// </summary>
        internal void GenerateBlobAssetReferenceArray(ref ILProcessor il, in TypeGenInfoList typeGenInfoList)
        {
            PushNewArray(ref il, m_TypeRegAssembly.MainModule.ImportReference(typeof(int)), m_TotalBlobAssetRefOffsetCount);

            int blobOffsetIndex = 0;
            foreach (var typeGenInfo in typeGenInfoList)
            {
                foreach (var offset in typeGenInfo.BlobAssetRefOffsets)
                {
                    PushNewArrayElement(ref il, blobOffsetIndex++);
                    EmitLoadConstant(ref il, offset);
                    il.Emit(OpCodes.Stelem_Any, m_TypeRegAssembly.MainModule.ImportReference(typeof(int)));
                }
            }

            StoreTopOfStackToStaticField(ref il, m_RegBlobAssetReferneceOffsetsArrayDef);
        }

        internal void PopulateWriteGroups(ref TypeGenInfoList typeGenInfoList)
        {
            var writeGroupMap = new Dictionary<int, HashSet<int>>();

            foreach (var typeGenInfo in typeGenInfoList)
            {
                if (typeGenInfo.TypeDefinition == null)
                    continue;

                var typeDef = typeGenInfo.TypeDefinition;
                var typeIndex = typeGenInfo.TypeIndex;

                foreach (var attribute in typeDef.CustomAttributes.Where(a => a.AttributeType.FullName == "Unity.Entities.WriteGroupAttribute"))
                {
                    var targetType = attribute.ConstructorArguments[0].Value as TypeReference;
                    int targetTypeIndex = m_TypeDefToTypeIndex[targetType.Resolve()];

                    if (!writeGroupMap.ContainsKey(targetTypeIndex))
                    {
                        var targetList = new HashSet<int>();
                        writeGroupMap.Add(targetTypeIndex, targetList);
                    }

                    writeGroupMap[targetTypeIndex].Add(typeIndex);
                }
            }

            m_TotalWriteGroupCount = 0;
            for(int i = 0; i <  typeGenInfoList.Count; ++i)
            {
                var typeGenInfo = typeGenInfoList[i];

                if(writeGroupMap.TryGetValue(typeGenInfo.TypeIndex, out var writeGroups))
                {
                    typeGenInfo.WriteGroupTypeIndices = writeGroups;
                    typeGenInfo.WriteGroupsIndex = m_TotalWriteGroupCount;
                    typeGenInfoList[i] = typeGenInfo;
                    m_TotalWriteGroupCount += writeGroups.Count();
                }
            }
        }

        /// <summary>
        /// Populates the registry's writeGroup int array.
        /// WriteGroup TypeIndices are laid out contiguously in memory such that the memory layout for Types A (2 writegroup elements),
        /// B (3 writegroup elements), C (0 writegroup elements) D (2 writegroup elements) is as such: aabbbdd
        /// </summary>
        internal void GenerateWriteGroupArray(ref ILProcessor il, in TypeGenInfoList typeGenInfoList)
        {
            PushNewArray(ref il, m_TypeRegAssembly.MainModule.ImportReference(typeof(int)), m_TotalWriteGroupCount);

            int writeGroupIndex = 0;
            foreach(var typeGenInfo in typeGenInfoList)
            {
                foreach (var wgTypeIndex in typeGenInfo.WriteGroupTypeIndices)
                {
                    PushNewArrayElement(ref il, writeGroupIndex++);
                    EmitLoadConstant(ref il, wgTypeIndex);
                    il.Emit(OpCodes.Stelem_Any, m_TypeRegAssembly.MainModule.ImportReference(typeof(int)));
                }
            }

            StoreTopOfStackToStaticField(ref il, m_WriteGroupArrayDef);
        }

        internal List<string> GetTypeNames(TypeGenInfoList typeGenInfoList)
        {
            var typeNames = new List<string>();
            if (m_IsDebug)
            {
                typeNames = typeGenInfoList.Select(t => t.TypeDefinition == null ? "null" : t.TypeDefinition.FullName).ToList();
            }
            return typeNames;
        }

        /// <summary>
        ///  Populates the registry's System.Type array for all types in typeIndex order.
        /// </summary>
        internal void GenerateTypeArray(ref ILProcessor il, List<TypeDefinition> typeDefinitions, int typeCount,
            FieldDefinition fieldDefinition)
        {
            if (typeDefinitions.Count != typeCount)
                throw new InvalidProgramException("GenerateTypeArray counts don't match. There must be a bug in TypeRegGen.cs");
            PushNewArray(ref il, m_SystemTypeRef, typeDefinitions.Count);

            for (int typeIndex = 0; typeIndex < typeDefinitions.Count; ++typeIndex)
            {
                if (typeDefinitions[typeIndex] == null)
                {
                    PushNewArrayElement(ref il, 0);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Stelem_Ref);
                }
                else
                {
                    TypeReference typeRef = m_TypeRegAssembly.MainModule.ImportReference(typeDefinitions[typeIndex]);

                    PushNewArrayElement(ref il, typeIndex);
                    il.Emit(OpCodes.Ldtoken,
                        typeRef); // Push our meta-type onto the stack as it will be our arg to System.Type.GetTypeFromHandle
                    il.Emit(OpCodes.Call,
                        m_GetTypeFromHandleFnRef); // Call System.Type.GetTypeFromHandle with the above stack arg. Return value pushed on the stack
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }

            StoreTopOfStackToStaticField(ref il, fieldDefinition);
        }

        internal void GenerateBoolArray(ref ILProcessor il, FieldDefinition fieldDefinition, List<bool> values)
        {
            var boolTypeDef = m_TypeRegAssembly.MainModule.ImportReference(typeof(bool));

            PushNewArray(ref il, boolTypeDef, values.Count);
            // Only need to load true values; false is default.
            for(int i=0; i<values.Count; ++i)
            {
                PushNewArrayElement(ref il, i);
                il.Emit(values[i] ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stelem_I1);
            }
            StoreTopOfStackToStaticField(ref il, fieldDefinition);
        }

        internal void GenerateStringArray(ref ILProcessor il, FieldDefinition fieldDefinition, List<string> values)
        {
            var stringTypeRef = m_TypeRegAssembly.MainModule.ImportReference(typeof(string));

            PushNewArray(ref il, stringTypeRef, values.Count);
            // Only need to load true values; false is default.
            for(int i=0; i<values.Count; ++i)
            {
                PushNewArrayElement(ref il, i);
                il.Emit(OpCodes.Ldstr, values[i]);
                il.Emit(OpCodes.Stelem_Ref);
            }
            StoreTopOfStackToStaticField(ref il, fieldDefinition);
        }

        /// <summary>
        /// Populates the registry's TypeInfo array in typeIndex order.
        /// </summary>
        internal void GenerateTypeInfoArray(ref ILProcessor il, in TypeGenInfoList typeGenInfoList)
        {
            PushNewArray(ref il, m_TypeInfoRef, m_TotalTypeCount);

            for (int i = 0; i < typeGenInfoList.Count; ++i)
            {
                var typeGenInfo = typeGenInfoList[i];

                if (i != (typeGenInfo.TypeIndex & ClearFlagsMask))
                    throw new ArgumentException("The typeGenInfo list is not in the correct order. This is a bug.");

                PushNewArrayElement(ref il, i);

                // Push constructor arguments on to the stack
                EmitLoadConstant(ref il, typeGenInfo.TypeIndex);
                EmitLoadConstant(ref il, (int)typeGenInfo.TypeCategory);
                EmitLoadConstant(ref il, typeGenInfo.EntityOffsetIndex == -1 ? -1 : typeGenInfo.EntityOffsets.Count);
                EmitLoadConstant(ref il, typeGenInfo.EntityOffsetIndex);
                EmitLoadConstant(ref il, typeGenInfo.MemoryOrdering);
                EmitLoadConstant(ref il, typeGenInfo.StableHash);
                EmitLoadConstant(ref il, typeGenInfo.BufferCapacity);
                EmitLoadConstant(ref il, typeGenInfo.SizeInChunk);
                EmitLoadConstant(ref il, typeGenInfo.ElementSize);
                EmitLoadConstant(ref il, typeGenInfo.Alignment);
                EmitLoadConstant(ref il, typeGenInfo.MaxChunkCapacity);
                EmitLoadConstant(ref il, typeGenInfo.WriteGroupTypeIndices.Count);
                EmitLoadConstant(ref il, typeGenInfo.WriteGroupsIndex);
                EmitLoadConstant(ref il, typeGenInfo.BlobAssetRefOffsets.Count);
                EmitLoadConstant(ref il, typeGenInfo.BlobAssetRefOffsetIndex);
                EmitLoadConstant(ref il, 0); // FastEqualityIndex - should be 0 until we can remove field altogether
                EmitLoadConstant(ref il, typeGenInfo.AlignAndSize.size);

                il.Emit(OpCodes.Newobj, m_TypeInfoConstructorRef);

                il.Emit(OpCodes.Stelem_Any, m_TypeInfoRef);
            }

            StoreTopOfStackToStaticField(ref il, m_RegTypeInfoArrayDef);
        }

        internal void GenerateEqualityFunctions(ref ILProcessor il, TypeGenInfoList typeGenInfoList)
        {
            // List of instructions in an array where index == typeIndex
            var boxedEqJumpTable = new List<Instruction>[typeGenInfoList.Count];
            var boxedPtrEqJumpTable = new List<Instruction>[typeGenInfoList.Count];
            var boxedHashJumpTable = new List<Instruction>[typeGenInfoList.Count];

            // Begin iterating at 1 to skip the null type
            for (int i = 1; i < typeGenInfoList.Count; ++i)
            {
                var typeGenInfo = typeGenInfoList[i];
                var thisTypeRef = m_TypeRegAssembly.MainModule.ImportReference(typeGenInfo.TypeDefinition);

                var typeRef = m_TypeRegAssembly.MainModule.ImportReference(typeGenInfo.TypeDefinition);

                // Store new equals fn to Equals member
                MethodReference equalsFn;
                {
                    // Equals function for operating on (object lhs, object rhs, int typeIndex) where the type isn't known by the user
                    {
                        var eqIL = m_RegBoxedEqualsFn.Body.GetILProcessor();

                        boxedEqJumpTable[i] = new List<Instruction>();
                        var instructionList = boxedEqJumpTable[i];

                        if (thisTypeRef.IsValueType)
                        {
                            instructionList.Add(eqIL.Create(OpCodes.Ldarg_0));
                            instructionList.Add(eqIL.Create(OpCodes.Unbox, thisTypeRef));
                            instructionList.Add(eqIL.Create(OpCodes.Ldarg_1));
                            instructionList.Add(eqIL.Create(OpCodes.Unbox, thisTypeRef));
                            instructionList.Add(eqIL.Create(OpCodes.Ldc_I4, typeGenInfo.AlignAndSize.size));
                            instructionList.Add(eqIL.Create(OpCodes.Call, m_MemCmpFnRef));
                            instructionList.Add(eqIL.Create(OpCodes.Ldc_I4_0));
                            instructionList.Add(eqIL.Create(OpCodes.Ceq));
                        }
                        else
                        {
                            equalsFn = GenerateEqualsFunction(typeGenInfo);
                            instructionList.Add(eqIL.Create(OpCodes.Ldarg_0));
                            instructionList.Add(eqIL.Create(OpCodes.Castclass, thisTypeRef));
                            instructionList.Add(eqIL.Create(OpCodes.Ldarg_1));
                            instructionList.Add(eqIL.Create(OpCodes.Castclass, thisTypeRef));
                            instructionList.Add(eqIL.Create(OpCodes.Call, equalsFn));
                        }                       

                        instructionList.Add(eqIL.Create(OpCodes.Ret));
                    }

                    // Equals function for operating on (object lhs, void* rhs, int typeIndex) where the type isn't known by the user
                    {
                        var eqIL = m_RegBoxedPtrEqualsFn.Body.GetILProcessor();

                        boxedPtrEqJumpTable[i] = new List<Instruction>();
                        var instructionList = boxedPtrEqJumpTable[i];

                        
                        if (thisTypeRef.IsValueType)
                        {
                            instructionList.Add(eqIL.Create(OpCodes.Ldarg_0));
                            instructionList.Add(eqIL.Create(OpCodes.Unbox, thisTypeRef));
                            instructionList.Add(eqIL.Create(OpCodes.Ldarg_1));
                            instructionList.Add(eqIL.Create(OpCodes.Ldc_I4, typeGenInfo.AlignAndSize.size));
                            instructionList.Add(eqIL.Create(OpCodes.Call, m_MemCmpFnRef));
                            instructionList.Add(eqIL.Create(OpCodes.Ldc_I4_0));
                            instructionList.Add(eqIL.Create(OpCodes.Ceq));
                            instructionList.Add(eqIL.Create(OpCodes.Ret));
                        }
                        else
                        {
                            instructionList.Add(eqIL.Create(OpCodes.Ldstr, "Equals(object, void*) is not supported for managed types in DOTSRuntime"));
                            var notSupportedExConstructor = m_TypeRegAssembly.MainModule.ImportReference(typeof(NotSupportedException)).Resolve().GetConstructors()
                                .Single(c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType.MetadataType == MetadataType.String);
                            instructionList.Add(eqIL.Create(OpCodes.Newobj, m_TypeRegAssembly.MainModule.ImportReference(notSupportedExConstructor)));
                            instructionList.Add(eqIL.Create(OpCodes.Throw));
                        }
                    }
                }

                // Store new Hash fn to Hash member
                {
                    // Hash function for operating on (object val, int typeIndex) where the type isn't known by the user
                    {
                        var hashIL = m_RegBoxedGetHashCodeFn.Body.GetILProcessor();
                        boxedHashJumpTable[i] = new List<Instruction>();
                        var instructionList = boxedHashJumpTable[i];

                        if (thisTypeRef.IsValueType)
                        {
                            instructionList.Add(hashIL.Create(OpCodes.Ldarg_0));
                            instructionList.Add(hashIL.Create(OpCodes.Unbox, thisTypeRef));
                            instructionList.Add(hashIL.Create(OpCodes.Ldc_I4, typeGenInfo.AlignAndSize.size));
                            instructionList.Add(hashIL.Create(OpCodes.Ldc_I4_0));
                            instructionList.Add(hashIL.Create(OpCodes.Call, m_Hash32FnRef));
                        }
                        else
                        {
                            var hashFn = GenerateHashFunction(typeGenInfo.TypeDefinition);
                            instructionList.Add(hashIL.Create(OpCodes.Ldarg_0));
                            instructionList.Add(hashIL.Create(OpCodes.Castclass, thisTypeRef));
                            instructionList.Add(hashIL.Create(OpCodes.Call, hashFn));
                        }
                        instructionList.Add(hashIL.Create(OpCodes.Ret));
                    }
                }
            }

            // We now have a list of instructions for each type on how to invoke the correct Equals/Hash call.
            // Now generate the void* Equals and Hash functions by making a jump table to those instructions

            // object Equals
            {
                var eqIL = m_RegBoxedEqualsFn.Body.GetILProcessor();
                List<Instruction> jumps = new List<Instruction>(boxedEqJumpTable.Length);
                Instruction loadTypeIndex = eqIL.Create(OpCodes.Ldarg_2);
                Instruction loadDefault = eqIL.Create(OpCodes.Ldc_I4_0); // default to false
                eqIL.Append(loadTypeIndex); // Load typeIndex

                foreach (var instructionList in boxedEqJumpTable)
                {
                    if(instructionList == null)
                    {
                        jumps.Add(loadDefault);
                        continue;
                    }

                    // Add starting instruction to our jump table so we know which Equals IL block to execute
                    jumps.Add(instructionList[0]);

                    foreach (var instruction in instructionList)
                    {
                        eqIL.Append(instruction);
                    }
                }

                // default case
                eqIL.Append(loadDefault);
                eqIL.Append(eqIL.Create(OpCodes.Ret));

                // Since we are using InsertAfter these instructions are appended in reverse order to how they will appear
                eqIL.InsertAfter(loadTypeIndex, eqIL.Create(OpCodes.Br, loadDefault));
                eqIL.InsertAfter(loadTypeIndex, eqIL.Create(OpCodes.Switch, jumps.ToArray()));
            }

            // object, void* Equals
            {
                var eqIL = m_RegBoxedPtrEqualsFn.Body.GetILProcessor();
                List<Instruction> jumps = new List<Instruction>(boxedPtrEqJumpTable.Length);
                Instruction loadTypeIndex = eqIL.Create(OpCodes.Ldarg_2);
                Instruction loadDefault = eqIL.Create(OpCodes.Ldc_I4_0); // default to false
                eqIL.Append(loadTypeIndex); // Load typeIndex

                foreach (var instructionList in boxedPtrEqJumpTable)
                {
                    if(instructionList == null)
                    {
                        jumps.Add(loadDefault);
                        continue;
                    }

                    // Add starting instruction to our jump table so we know which Equals IL block to execute
                    jumps.Add(instructionList[0]);

                    foreach (var instruction in instructionList)
                    {
                        eqIL.Append(instruction);
                    }
                }

                // default case
                eqIL.Append(loadDefault);
                eqIL.Append(eqIL.Create(OpCodes.Ret));

                // Since we are using InsertAfter these instructions are appended in reverse order to how they will appear
                eqIL.InsertAfter(loadTypeIndex, eqIL.Create(OpCodes.Br, loadDefault));
                eqIL.InsertAfter(loadTypeIndex, eqIL.Create(OpCodes.Switch, jumps.ToArray()));
            }

            // object Hash
            {
                var hashIL = m_RegBoxedGetHashCodeFn.Body.GetILProcessor();
                List<Instruction> jumps = new List<Instruction>(boxedHashJumpTable.Length);
                Instruction loadTypeIndex = hashIL.Create(OpCodes.Ldarg_1);
                Instruction loadDefault = hashIL.Create(OpCodes.Ldc_I4_0); // default to 0 for the hash
                hashIL.Append(loadTypeIndex); // Load typeIndex

                foreach (var instructionList in boxedHashJumpTable)
                {
                    if (instructionList == null)
                    {
                        jumps.Add(loadDefault);
                        continue;
                    }

                    // Add starting instruction to our jump table so we know which Equals IL block to execute
                    jumps.Add(instructionList[0]);

                    foreach (var instruction in instructionList)
                    {
                        hashIL.Append(instruction);
                    }
                }

                // default case
                hashIL.Append(loadDefault);
                hashIL.Append(hashIL.Create(OpCodes.Ret));

                // Since we are using InsertAfter these instructions are appended in reverse order to how they will appear in generated code
                hashIL.InsertAfter(loadTypeIndex, hashIL.Create(OpCodes.Br, loadDefault));
                hashIL.InsertAfter(loadTypeIndex, hashIL.Create(OpCodes.Switch, jumps.ToArray()));
            }
        }

        private static MethodReference GetTypesEqualsMethodReference(TypeDefinition typeDef)
        {
            return typeDef.Methods.FirstOrDefault(
                m => m.Name == "Equals"
                    && m.Parameters.Count == 1
                    && m.Parameters[0].ParameterType == typeDef);
        }

        private static MethodReference GetTypesGetHashCodeMethodReference(TypeDefinition typeDef)
        {
            // This code is kind of weak. We actually want to confirm this function is overriding System.Object.GetHashCode however 
            // as far as I can tell, cecil is not detecting legitimate overrides so we resort to this.
            return typeDef.Methods.FirstOrDefault(
                m => m.Name == "GetHashCode" && m.Parameters.Count == 0);
        }

        internal MethodReference GenerateEqualsFunction(TypeGenInfo typeGenInfo)
        {
            var typeRef = m_TypeRegAssembly.MainModule.ImportReference(typeGenInfo.TypeDefinition);
            var equalsFn = new MethodDefinition("DoEquals", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig, m_TypeRegAssembly.MainModule.ImportReference(typeof(bool)));
            var arg0 = new ParameterDefinition("i0", Mono.Cecil.ParameterAttributes.None, typeRef.IsValueType ? new ByReferenceType(typeRef) : typeRef);
            var arg1 = new ParameterDefinition("i1", Mono.Cecil.ParameterAttributes.None, typeRef.IsValueType ? new ByReferenceType(typeRef) : typeRef);
            equalsFn.Parameters.Add(arg0);
            equalsFn.Parameters.Add(arg1);
            m_RegClass.Methods.Add(equalsFn);

            var il = equalsFn.Body.GetILProcessor();

            var userImpl = GetTypesEqualsMethodReference(typeGenInfo.TypeDefinition)?.Resolve();
            if (userImpl != null)
            {
                if (typeRef.IsValueType)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldobj, typeRef);
                    il.Emit(OpCodes.Call, m_TypeRegAssembly.MainModule.ImportReference(userImpl));
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Callvirt, m_TypeRegAssembly.MainModule.ImportReference(userImpl));
                    // Ret is called outside of this block
                }
            }
            else
            {
                GenerateEqualsFunctionRecurse(ref il, arg0, arg1, typeGenInfo);
            }

            il.Emit(OpCodes.Ret);

            return equalsFn;
        }

        internal void GenerateEqualsFunctionRecurse(ref ILProcessor il, ParameterDefinition arg0, ParameterDefinition arg1, TypeGenInfo typeGenInfo)
        {
            int typeSize = typeGenInfo.AlignAndSize.size;

            // Raw memcmp of the two types
            // May need to do something more clever if this doesn't pan out for all types
            il.Emit(OpCodes.Ldarg, arg0);
            il.Emit(OpCodes.Ldarg, arg1);

            // The DotNet UnsafeUtility.MemCmp requires a long instead of an int
            if (m_Profile == Profile.DotNet)
            {
                il.Emit(OpCodes.Ldc_I8, (long)typeSize);
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, typeSize);
            }

            il.Emit(OpCodes.Call, m_MemCmpFnRef);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ceq);
        }

        internal MethodReference GenerateHashFunction(TypeDefinition typeDef)
        {
            // http://www.isthe.com/chongo/tech/comp/fnv/index.html#FNV-1a
            const int FNV1a_32_OFFSET = unchecked((int)0x811C9DC5);

            var typeRef = m_TypeRegAssembly.MainModule.ImportReference(typeDef);
            var hashFn = new MethodDefinition("DoHash", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig, m_TypeRegAssembly.MainModule.ImportReference(typeof(int)));
            var arg0 = new ParameterDefinition("val", Mono.Cecil.ParameterAttributes.None, typeDef.IsValueType ? new ByReferenceType(typeRef) : typeRef);
            hashFn.Parameters.Add(arg0);
            m_RegClass.Methods.Add(hashFn);

            var il = hashFn.Body.GetILProcessor();

            MethodDefinition userImpl = GetTypesGetHashCodeMethodReference(typeDef)?.Resolve();
            if (userImpl != null)
            {
                il.Emit(OpCodes.Ldarg_0);
                if(typeDef.IsValueType)
                {
                    il.Emit(OpCodes.Constrained, m_TypeRegAssembly.MainModule.ImportReference(typeDef)); // avoid boxing if we know the type is a value type
                }
                il.Emit(OpCodes.Callvirt, m_TypeRegAssembly.MainModule.ImportReference(userImpl));
            }
            else
            {
                List<Instruction> fieldLoadChain = new List<Instruction>();
                List<Instruction> hashInstructions = new List<Instruction>();

                GenerateHashFunctionRecurse(ref il, ref hashInstructions, ref fieldLoadChain, arg0, typeDef);
                if (hashInstructions.Count == 0)
                {
                    // If the type doesn't contain any value types to hash we want to return 0 as the hash
                    il.Emit(OpCodes.Ldc_I4_0);
                }
                else
                {
                    EmitLoadConstant(ref il, FNV1a_32_OFFSET); // Initial Hash value

                    foreach (var instruction in hashInstructions)
                    {
                        il.Append(instruction);
                    }
                }

            }

            il.Emit(OpCodes.Ret);

            return hashFn;
        }

        internal void GenerateHashFunctionRecurse(ref ILProcessor il, ref List<Instruction> hashInstructions, ref List<Instruction> fieldLoadChain, ParameterDefinition val, TypeDefinition typeDef)
        {
            // http://www.isthe.com/chongo/tech/comp/fnv/index.html#FNV-1a
            const int FNV1a_32_PRIME = 16777619;

            foreach (var field in typeDef.Fields)
            {
                if (!field.IsStatic)
                {
                    var fieldDef = field.FieldType.Resolve();

                    // https://cecilifier.appspot.com/ outputs what you would expect here, that there is
                    // a bit in the attributes for 'Fixed.'. Specifically:
                    //     FieldAttributes.Fixed
                    // Haven't been able to find the actual numeric value. Until then, use this approach:
                    bool isFixed = fieldDef.ClassSize != -1 && fieldDef.Name.Contains(">e__FixedBuffer");
                    if (isFixed || field.FieldType.IsPrimitive || field.FieldType.IsPointer || fieldDef.IsEnum)
                    {
                        /*
                         Equivalent to:
                            hash *= FNV1a_32_PRIME;
                            hash ^= value;
                        */
                        hashInstructions.Add(il.Create(OpCodes.Ldc_I4, FNV1a_32_PRIME));
                        hashInstructions.Add(il.Create(OpCodes.Mul));


                        hashInstructions.Add(il.Create(OpCodes.Ldarg, val));
                        // Since we need to find the offset to nested members we need to chain field loads
                        hashInstructions.AddRange(fieldLoadChain);

                        if (isFixed)
                        {
                            hashInstructions.Add(il.Create(OpCodes.Ldflda, m_TypeRegAssembly.MainModule.ImportReference(field)));
                            hashInstructions.Add(il.Create(OpCodes.Ldind_I4));
                        }
                        else
                        {
                            if (field.FieldType.IsPointer && m_ArchBits == 64)
                            {
                                // Xor top and bottom of pointer
                                //
                                // Bottom 32 Bits
                                hashInstructions.Add(il.Create(OpCodes.Ldfld, m_TypeRegAssembly.MainModule.ImportReference(field)));
                                hashInstructions.Add(il.Create(OpCodes.Conv_I8)); // do I need this if we know the ptr is 64-bit
                                hashInstructions.Add(il.Create(OpCodes.Ldc_I4_M1)); // 0x00000000FFFFFFFF
                                hashInstructions.Add(il.Create(OpCodes.Conv_I8));

                                hashInstructions.Add(il.Create(OpCodes.And));

                                // Top 32 bits
                                hashInstructions.Add(il.Create(OpCodes.Ldarg, val));
                                hashInstructions.AddRange(fieldLoadChain);
                                hashInstructions.Add(il.Create(OpCodes.Ldfld, m_TypeRegAssembly.MainModule.ImportReference(field)));
                                hashInstructions.Add(il.Create(OpCodes.Conv_I8)); // do I need this if we know the ptr is 64-bit
                                hashInstructions.Add(il.Create(OpCodes.Ldc_I4, 32));
                                hashInstructions.Add(il.Create(OpCodes.Shr_Un));
                                hashInstructions.Add(il.Create(OpCodes.Ldc_I4_M1)); // 0x00000000FFFFFFFF
                                hashInstructions.Add(il.Create(OpCodes.Conv_I8));
                                hashInstructions.Add(il.Create(OpCodes.And));

                                hashInstructions.Add(il.Create(OpCodes.Xor));
                            }
                            else
                            {
                                hashInstructions.Add(il.Create(OpCodes.Ldfld, m_TypeRegAssembly.MainModule.ImportReference(field)));
                            }
                        }

                        // Subtle behavior. Aside from pointer types, we only load the first 4 bytes of the field.
                        // Makes hashing fast and simple, at the cost of more hash collisions.
                        hashInstructions.Add(il.Create(OpCodes.Conv_I4));
                        hashInstructions.Add(il.Create(OpCodes.Xor));
                    }
                    else if (field.FieldType.IsValueType)
                    {
                        // Workaround: We shouldn't need to special case for System.Guid however accessing the private members of types in mscorlib
                        // is problematic as eventhough we may elevate the field permissions in a new mscorlib assembly, Windows may load the assembly from the
                        // Global Assembly Cache regardless resulting in us throwing FieldAccessExceptions
                        if(field.FieldType.FullName == "System.Guid")
                        {
                            /*
                             Equivalent to:
                                hash *= FNV1a_32_PRIME;
                                hash ^= value;
                            */
                            hashInstructions.Add(il.Create(OpCodes.Ldc_I4, FNV1a_32_PRIME));
                            hashInstructions.Add(il.Create(OpCodes.Mul));

                            hashInstructions.Add(il.Create(OpCodes.Ldarg, val));
                            // Since we need to find the offset to nested members we need to chain field loads
                            hashInstructions.AddRange(fieldLoadChain);

                            hashInstructions.Add(il.Create(OpCodes.Ldflda, m_TypeRegAssembly.MainModule.ImportReference(field)));
                            hashInstructions.Add(il.Create(OpCodes.Call, m_TypeRegAssembly.MainModule.ImportReference(m_SystemGuidHashFn)));
                            hashInstructions.Add(il.Create(OpCodes.Xor));
                        }
                        else
                        {
                            fieldLoadChain.Add(Instruction.Create(OpCodes.Ldfld, m_TypeRegAssembly.MainModule.ImportReference(field)));
                            GenerateHashFunctionRecurse(ref il, ref hashInstructions, ref fieldLoadChain, val, fieldDef);
                            fieldLoadChain.RemoveAt(fieldLoadChain.Count - 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Function for the TypeManager to call to populate the TypeManager.s_Type type array the StaticTypeRegistry types.
        /// The logic for populating the TypeManager is done in the TypeManager.AddStaticTypesFromRegistry(ref TypeInfo[] tiArray, int count) function which we invoke here
        /// </summary>
        internal MethodDefinition GenerateRegisterStaticTypesFn()
        {
            var methodDefinition = new MethodDefinition("RegisterStaticTypes", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig, m_TypeRegAssembly.MainModule.ImportReference(typeof(void)));

            methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, m_RegTypeInfoArrayDef));
            methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Call, m_AddStaticTypesFromRegistryFnRef));

            if (m_Profile != Profile.DotNet) // Currently not supported in Hybrid builds
            {
                methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsflda, m_RegSystemSystemsArrayDef));
                methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Call, m_AddStaticSystemsFromRegistryFnRef));
            }

            methodDefinition.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            return methodDefinition;
        }

        private static void ForceTypeAsInternalRecurse(TypeDefinition typeDef, HashSet<AssemblyDefinition> assemblySet)
        {
            if (typeDef == null)
                return;

            if (typeDef.IsNested)
            {
                if (!typeDef.IsNestedPublic)
                {
                    typeDef.IsNestedFamilyOrAssembly = true;
                    assemblySet.Add(typeDef.Module.Assembly);
                }

                ForceTypeAsInternalRecurse(typeDef.DeclaringType, assemblySet);
            }
            else if(!typeDef.IsPublic)
            {
                typeDef.IsNotPublic = true;
                assemblySet.Add(typeDef.Module.Assembly);
            }
        }

        internal static void ForceTypeAsInternal(TypeDefinition typeDef, HashSet<AssemblyDefinition> assemblySet)
        {
            ForceTypeAsInternalRecurse(typeDef, assemblySet);
        }

        internal static void ForceTypeMembersAsInternalRecurse(TypeDefinition typeDef, HashSet<AssemblyDefinition> assemblySet)
        {
            if (typeDef == null)
                return;

            foreach (var field in typeDef.Fields)
            {
                if (field.IsStatic)
                    continue;

                if (!field.IsPublic)
                {
                    field.IsFamilyOrAssembly = true;
                    assemblySet.Add(typeDef.Module.Assembly);
                }

                if (!field.FieldType.IsPrimitive && !field.FieldType.IsGenericParameter)
                {
                    var fieldDef = field.FieldType.Resolve();
                    if (!fieldDef.IsEnum && field.FieldType.IsValueType)
                    {
                        ForceTypeMembersAsInternalRecurse(fieldDef, assemblySet);
                    }
                }
            }
        }

        internal static void ForceTypeMembersAsInternal(TypeDefinition typeDef, HashSet<AssemblyDefinition> assemblySet)
        {
            ForceTypeMembersAsInternalRecurse(typeDef, assemblySet);
        }

        private static void ForceTypeAsPublicRecurse(TypeDefinition typeDef, HashSet<AssemblyDefinition> assemblySet)
        {
            if (typeDef == null)
                return;

            if (typeDef.IsNested)
            {
                if (!typeDef.IsNestedPublic)
                {
                    typeDef.IsNestedPublic = true;
                    assemblySet.Add(typeDef.Module.Assembly);
                }

                ForceTypeAsInternalRecurse(typeDef.DeclaringType, assemblySet);
            }
            else if(!typeDef.IsPublic)
            {
                typeDef.IsPublic = true;
                assemblySet.Add(typeDef.Module.Assembly);
            }
        }

        internal static void ForceTypeAsPublic(TypeDefinition typeDef, HashSet<AssemblyDefinition> assemblySet)
        {
            ForceTypeAsPublicRecurse(typeDef, assemblySet);
        }

        internal static void ForceTypeMembersAsPublicRecurse(TypeDefinition typeDef, HashSet<AssemblyDefinition> assemblySet)
        {
            if (typeDef == null)
                return;

            foreach (var field in typeDef.Fields)
            {
                if (field.IsStatic)
                    continue;

                if (!field.IsPublic)
                {
                    field.IsPublic = true;
                    assemblySet.Add(typeDef.Module.Assembly);
                }

                if (!field.FieldType.IsPrimitive && !field.FieldType.IsGenericParameter)
                {
                    var fieldDef = field.FieldType.Resolve();
                    if (!fieldDef.IsEnum && field.FieldType.IsValueType)
                    {
                        ForceTypeMembersAsPublicRecurse(fieldDef, assemblySet);
                    }
                }
            }
        }

        internal static void ForceTypeMembersAsPublic(TypeDefinition typeDef, HashSet<AssemblyDefinition> assemblySet)
        {
            ForceTypeMembersAsPublicRecurse(typeDef, assemblySet);
        }

        internal void FixupAssemblies(in List<TypeDefinition> typeDefList, in List<TypeDefinition> publicTypeDefList)
        {
            HashSet<AssemblyDefinition> changedAsmDef = new HashSet<AssemblyDefinition>();
            // Force all ECS types to be public
            foreach (var type in typeDefList)
            {
                ForceTypeMembersAsInternal(type, changedAsmDef);
                ForceTypeAsInternal(type, changedAsmDef);
            }

            foreach (var type in publicTypeDefList)
            {
                ForceTypeMembersAsPublic(type, changedAsmDef);
                ForceTypeAsPublic(type, changedAsmDef);
            }

            // Write out all passed in assemblies
            foreach (var asm in m_AssemblyDefs)
            {
                if (asm.FullName.Contains(kStaticRegAsmName))
                    continue;

                if (changedAsmDef.Contains(asm))
                {
                    var internalsAccessorAttr = new CustomAttribute(asm.MainModule.ImportReference(typeof(InternalsVisibleToAttribute).GetConstructor(new[] { typeof(string) })));
                    internalsAccessorAttr.ConstructorArguments.Add(new CustomAttributeArgument(asm.MainModule.TypeSystem.String, m_TypeRegAssembly.Name.Name));
                    asm.MainModule.Assembly.CustomAttributes.Add(internalsAccessorAttr);

                    // TODO: We can clean this up so it is all contained in the JobsGen cs for better separation
                    if (m_Profile != Profile.DotNet)
                    {
                        internalsAccessorAttr = new CustomAttribute(asm.MainModule.ImportReference(typeof(InternalsVisibleToAttribute).GetConstructor(new[] { typeof(string) })));
                        internalsAccessorAttr.ConstructorArguments.Add(new CustomAttributeArgument(asm.MainModule.TypeSystem.String, "Unity.ZeroJobs"));
                        asm.MainModule.Assembly.CustomAttributes.Add(internalsAccessorAttr);

                        internalsAccessorAttr = new CustomAttribute(asm.MainModule.ImportReference(typeof(InternalsVisibleToAttribute).GetConstructor(new[] { typeof(string) })));
                        internalsAccessorAttr.ConstructorArguments.Add(new CustomAttributeArgument(asm.MainModule.TypeSystem.String, "Unity.Entities"));
                        asm.MainModule.Assembly.CustomAttributes.Add(internalsAccessorAttr);
                    }
                }

                string asmPath = asm.MainModule.FileName;
                string asmFileName = asmPath.Substring(asmPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                var outPath = Path.Combine(m_OutputDir, asmFileName);

                var writerParams = new WriterParameters() { WriteSymbols = asm.MainModule.HasSymbols };

                try
                {
                    asm.MainModule.Write(outPath, writerParams);
                }
                catch (DllNotFoundException e)
                {
                    /*
                     * on mac, cecil can't find ole32.dll, so just copy it.
                     */
                    if (!e.Message.Contains("ole32.dll"))
                        throw e;
                    File.Copy(asm.MainModule.FileName, outPath, true);
                    if (asm.MainModule.HasSymbols)
                    {
                        Console.WriteLine(
                            $"Warning: TypeRegGen could not write new '{asm.MainModule.Name}'. Copying original instead.");
                        File.Copy(
                            Path.ChangeExtension(asm.MainModule.FileName, "pdb"),
                            Path.ChangeExtension(outPath, "pdb"),
                            true);
                    }
                }
            }
        }

        private void SaveDebugMeta(TypeGenInfoList typeGenInfoList, List<TypeDefinition> systemList, List<InterfaceGen.JobDesc> jobList)
        {
            System.Text.StringBuilder build = new System.Text.StringBuilder();
            StringWriter sw = new StringWriter(build);

            using (JsonTextWriter jw = new JsonTextWriter(sw))
            {
                jw.Formatting = Formatting.Indented;

                using (var closeA = jw.WriteStartObjectAuto())
                {

                    jw.WritePropertyName("TypeGenInfoList");
                    using (var closeB = jw.WriteStartObjectAuto())
                    {

                        for (int i = 0; i < typeGenInfoList.Count; i++)
                        {
                            var typeGenInfo = typeGenInfoList[i];

                            if (typeGenInfo.TypeDefinition == null)
                                jw.WritePropertyName("null");
                            else
                                jw.WritePropertyName(typeGenInfo.TypeDefinition.Name);
                            using (var closeC = jw.WriteStartObjectAuto())
                            {

                                // Implicit info
                                jw.WriteNameValue("Element", i);

                                // In same order as written in constructor
                                jw.WriteNameValue("TypeIndex", typeGenInfo.TypeIndex);
                                jw.WriteNameValue("TypeCategory", typeGenInfo.TypeCategory.ToString());
                                jw.WriteNameValue("EntityOffsets.Count", typeGenInfo.EntityOffsets.Count);
                                jw.WriteNameValue("EntityOffsetIndex", typeGenInfo.EntityOffsetIndex);
                                jw.WriteNameValue("MemoryOrdering", typeGenInfo.MemoryOrdering);
                                jw.WriteNameValue("StableHash", typeGenInfo.StableHash);
                                jw.WriteNameValue("BufferCapacity", typeGenInfo.BufferCapacity);
                                jw.WriteNameValue("SizeInChunk", typeGenInfo.SizeInChunk);
                                jw.WriteNameValue("ElementSize", typeGenInfo.ElementSize);
                                jw.WriteNameValue("Alignment", typeGenInfo.Alignment);
                                jw.WriteNameValue("MaxChunkCapacity", typeGenInfo.MaxChunkCapacity);
                                jw.WriteNameValue("WriteGroupTypeIndices.Count", typeGenInfo.WriteGroupTypeIndices.Count);
                                jw.WriteNameValue("WriteGroupsIndex", typeGenInfo.WriteGroupsIndex);
                                jw.WriteNameValue("BlobAssetRefOffsets.Count", typeGenInfo.BlobAssetRefOffsets.Count);
                                jw.WriteNameValue("BlobAssetRefOffsetIndex", typeGenInfo.BlobAssetRefOffsetIndex);
                                jw.WriteNameValue("FastEqualityIndex", 0);
                            }
                        }
                    }

                    jw.WritePropertyName("SystemList");
                    using (var closeB = jw.WriteStartObjectAuto())
                    {

                        for (int i = 0; i < systemList.Count; i++)
                        {
                            var sys = systemList[i];
                            jw.WritePropertyName(sys.Name);
                            using (var closeC = jw.WriteStartObjectAuto())
                            {

                                // Implicit info
                                jw.WriteNameValue("SystemIndex", i);

                                jw.WritePropertyName("UpdateInGroup");
                                using (var closeD = jw.WriteStartArrayAuto())
                                {
                                    var uig = sys.CustomAttributes.Where(t => t.AttributeType.FullName == "Unity.Entities.UpdateInGroupAttribute");
                                    foreach (var attr in uig)
                                    {
                                        if (attr.ConstructorArguments.Count != 1)
                                            jw.WriteValue("[ERROR: Wrong number of constructor arguments]");
                                        else
                                            jw.WriteValue(attr.ConstructorArguments[0].Value.ToString());
                                    }
                                }

                                jw.WritePropertyName("UpdateAfter");
                                using (var closeD = jw.WriteStartArrayAuto())
                                {
                                    var uaa = sys.CustomAttributes.Where(t => t.AttributeType.FullName == "Unity.Entities.UpdateAfterAttribute");
                                    foreach (var attr in uaa)
                                    {
                                        if (attr.ConstructorArguments.Count != 1)
                                            jw.WriteValue("[ERROR: Wrong number of constructor arguments]");
                                        else
                                            jw.WriteValue(attr.ConstructorArguments[0].Value.ToString());
                                    }
                                }

                                jw.WritePropertyName("UpdateBefore");
                                using (var closeD = jw.WriteStartArrayAuto())
                                {
                                    var uba = sys.CustomAttributes.Where(t => t.AttributeType.FullName == "Unity.Entities.UpdateBeforeAttribute");
                                    foreach (var attr in uba)
                                    {
                                        if (attr.ConstructorArguments.Count != 1)
                                            jw.WriteValue("[ERROR: Wrong number of constructor arguments]");
                                        else
                                            jw.WriteValue(attr.ConstructorArguments[0].Value.ToString());
                                    }
                                }
                            }
                        }
                    }

                    jw.WritePropertyName("JobList");
                    using (jw.WriteStartObjectAuto())
                    {
                        for (int i = 0; i < jobList.Count; i++)
                        {
                            var jobDesc = jobList[i];

                            jw.WritePropertyName(jobDesc.JobInterface.FullName);
                            using (jw.WriteStartObjectAuto())
                            {
                                jw.WriteNameValue("Producer", jobDesc.JobProducer.FullName);
                                jw.WriteNameValue("Interface", jobDesc.JobInterface.FullName);
                                jw.WriteNameValue("JobData", jobDesc.JobData.FullName);
                                jw.WriteNameValue("JobType", jobDesc.JobType == InterfaceGen.JobType.ParallelFor ? "ParallelFor" : "Single");
                                if (jobDesc.JobDataField != null)
                                    jw.WriteNameValue("JobDataField", jobDesc.JobDataField.Name);
                            }
                        }
                    }
                }
            }

            var outPath = Path.Combine(m_OutputDir, m_TypeRegAssembly.Name.Name) + ".json";
            System.Console.WriteLine("Logging to " + outPath);
            System.IO.File.WriteAllText(outPath, build.ToString());
        }

        internal static void EmitLoadConstant(ref ILProcessor il, int val)
        {
            if(val >= -128 && val < 128)
            {
                switch(val)
                {
                    case -1:
                        il.Emit(OpCodes.Ldc_I4_M1); break;
                    case 0:
                        il.Emit(OpCodes.Ldc_I4_0); break;
                    case 1:
                        il.Emit(OpCodes.Ldc_I4_1); break;
                    case 2:
                        il.Emit(OpCodes.Ldc_I4_2); break;
                    case 3:
                        il.Emit(OpCodes.Ldc_I4_3); break;
                    case 4:
                        il.Emit(OpCodes.Ldc_I4_4); break;
                    case 5:
                        il.Emit(OpCodes.Ldc_I4_5); break;
                    case 6:
                        il.Emit(OpCodes.Ldc_I4_6); break;
                    case 7:
                        il.Emit(OpCodes.Ldc_I4_7); break;
                    case 8:
                        il.Emit(OpCodes.Ldc_I4_8); break;
                    default:
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte) val); break;
                }
            }
            else
            {
                il.Emit(OpCodes.Ldc_I4, val);
            }
        }

        internal static void EmitLoadConstant(ref ILProcessor il, long val)
        {
            // III.3.40 ldc.<type> load numeric constant (https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf)
            long absVal = Math.Abs(val);

            // Value is represented in more than 32-bits
            if((absVal & 0x7FFFFFFF00000000) != 0)
            {
                il.Emit(OpCodes.Ldc_I8, val);
            }
            // Value is represented in 9 - 32 bits
            else if ((absVal & 0xFFFFFF00) != 0)
            {
                il.Emit(OpCodes.Ldc_I4, val);
                il.Emit(OpCodes.Conv_I8);
            }
            else
            {
                EmitLoadConstant(ref il, (int) val);
                il.Emit(OpCodes.Conv_I8);
            }
        }

        internal static void EmitLoadConstant(ref ILProcessor il, ulong val)
        {
            // III.3.40 ldc.<type> load numeric constant (https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf)

            // Value is represented in more than 32-bits
            if ((val & 0xFFFFFFFF00000000) != 0)
            {
                il.Emit(OpCodes.Ldc_I8, (long) val);
            }
            // Value is represented in 9 - 32 bits
            else if ((val & 0xFFFFFF00) != 0)
            {
                il.Emit(OpCodes.Ldc_I4, (int) val);
            }
            else
            {
                EmitLoadConstant(ref il, (int)val);
            }
            il.Emit(OpCodes.Conv_U8);
        }

        internal static void EmitInstructions(ref ILProcessor il, List<Instruction> instructions)
        {
            foreach (var i in instructions)
                il.Append(i);
        }

        internal static bool DoesTypeInheritInterface(TypeDefinition typeDef, string interfaceName)
        {
            if (typeDef == null)
                return false;

            return (typeDef.Interfaces.Any(i => i.InterfaceType.FullName.Equals(interfaceName)) || typeDef.NestedTypes.Any(t => DoesTypeInheritInterface(t, interfaceName)));
        }

        internal static void StoreTopOfStackToStaticField(ref ILProcessor il, FieldReference fieldRef)
        {
            il.Emit(OpCodes.Stsfld, fieldRef);
        }

        internal static void PushNewArray(ref ILProcessor il, TypeReference elementTypeRef, int arraySize)
        {
            EmitLoadConstant(ref il, arraySize);        // Push Array Size
            il.Emit(OpCodes.Newarr, elementTypeRef);    // Push array reference to top of stack
        }

        /// <summary>
        /// NOTE: This functions assumes the array is at the top of the stack
        /// </summary>
        internal static void PushNewArrayElement(ref ILProcessor il, int elementIndex)
        {
            il.Emit(OpCodes.Dup);                   // Duplicate top of stack (the array)
            EmitLoadConstant(ref il, elementIndex); // Push array index onto the stack
        }

        internal void InitializeTypeReferences()
        {
            // TypeManager
            var typeManagerDef = m_EntityAssembly.MainModule.Types
                    .First(t => t.FullName == "Unity.Entities.TypeManager");

            m_AddStaticTypesFromRegistryFnRef = m_TypeRegAssembly.MainModule.ImportReference(typeManagerDef
                .Methods
                .First(m => m.Name == "AddStaticTypesFromRegistry"));
            m_AddStaticSystemsFromRegistryFnRef = m_TypeRegAssembly.MainModule.ImportReference(typeManagerDef
                .Methods
                .First(m => m.Name == "AddStaticSystemsFromRegistry"));

            // TypeManager.TypeInfo
            var typeInfoDef = typeManagerDef
                    .NestedTypes
                    .First(m => m.Name == "TypeInfo");
            m_TypeInfoRef = m_TypeRegAssembly.MainModule.ImportReference(typeInfoDef);
            m_TypeInfoConstructorRef = m_TypeRegAssembly.MainModule.ImportReference(typeInfoDef
                    .Methods
                    .First(m => m.IsSpecialName && m.Name == ".ctor" && m.IsPublic));

            // Entities.BufferHeader
            m_BufferHeaderDef = m_EntityAssembly.MainModule.GetType("Unity.Entities.BufferHeader");

            // System.* Types
            var systemTypeRef = m_MscorlibAssembly.MainModule.GetType("System.Type");
            m_SystemTypeRef = m_TypeRegAssembly.MainModule.ImportReference(systemTypeRef);
            m_GetTypeFromHandleFnRef = m_TypeRegAssembly.MainModule.ImportReference(systemTypeRef
                .Methods
                .First(m => m.Name == "GetTypeFromHandle"));
            var objectRef = m_MscorlibAssembly.MainModule.GetType("System.Object");
            m_GetTypeFnRef = m_TypeRegAssembly.MainModule.ImportReference(objectRef
                .Methods
                .First(m => m.Name == "GetType"));
            var guidRef = m_MscorlibAssembly.MainModule.GetType("System.Guid");
            m_SystemGuidHashFn = m_TypeRegAssembly.MainModule.ImportReference(guidRef
                .Methods
                .First(m => m.Name == "GetHashCode"));
            m_IntPtrGetSizeFnRef = m_TypeRegAssembly.MainModule.ImportReference(m_MscorlibAssembly.MainModule.GetType("System.IntPtr")
                .Methods
                .First(m => m.Name == "get_Size"));
            m_InvalidOpExceptionCTORRef = m_TypeRegAssembly.MainModule.ImportReference(m_MscorlibAssembly.MainModule.GetType("System.InvalidOperationException")
                .Methods
                .First(m => m.Name == ".ctor" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name == "String"));

            MethodDefinition memCmpMethod;
            switch (m_Profile)
            {
                case Profile.DotNet:
                {
                    var coreModule = m_AssemblyDefs.FirstOrDefault(asm => asm.Name.Name == "UnityEngine.CoreModule");
                    memCmpMethod = coreModule.MainModule.GetType("Unity.Collections.LowLevel.Unsafe.UnsafeUtility")
                        .Methods
                        .First(m => m.Name == "MemCmp");
                    break;
                }
                case Profile.DOTSNative:
                case Profile.DOTSDotNet:
                {
                    var unityLowLevel = m_AssemblyDefs.FirstOrDefault(a => a.Name.Name == "Unity.LowLevel");
                    // grab the slow version from UnsafeUtility as the native version won't be linked in
                    memCmpMethod = unityLowLevel.MainModule.GetType("Unity.Collections.LowLevel.Unsafe.UnsafeUtility")
                        .Methods
                        .First(m => m.Name == "MemoryCompare");
                    break;
                }
                default:
                {
                    throw new NotImplementedException($"Memory compare for profile '{m_Profile}' is not implemented.");
                }
            }

            m_MemCmpFnRef = m_TypeRegAssembly.MainModule.ImportReference(memCmpMethod);

            var unityLowLevelForMemCmp = m_AssemblyDefs.FirstOrDefault(a => a.Name.Name == "Unity.LowLevel");
            m_MemCpyFnRef = unityLowLevelForMemCmp.MainModule.GetType("Unity.Collections.LowLevel.Unsafe.UnsafeUtility")
                .Methods
                .First(m => m.Name == "MemCpy");

            var xxHashTypeRef = m_EntityAssembly.MainModule.GetType("Unity.Core.XXHash");
            m_Hash32FnRef = m_TypeRegAssembly.MainModule.ImportReference(xxHashTypeRef.GetMethods().Single(m => m.Name == "Hash32"));
        }

        internal static MethodReference MakeGenericMethodSpecialization(MethodReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException();

            var instance = new GenericInstanceMethod(self);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        internal static FieldReference MakeGenericFieldSpecialization(FieldReference self, params TypeReference[] arguments)
        {
            if (self.DeclaringType.GenericParameters.Count != arguments.Length)
                throw new ArgumentException();

            var instance = new GenericInstanceType(self.DeclaringType);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return new FieldReference(self.Name, self.FieldType, instance);
        }

        internal static TypeReference MakeGenericTypeSpecialization(TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException();

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        void CalculateMemoryOrderingAndStableHash(TypeDefinition typeDef, out ulong memoryOrder, out ulong stableHash)
        {
            if (typeDef == null)
            {
                memoryOrder = 0;
                stableHash = 0;
                return;
            }

            stableHash = typeDef.CalculateStableTypeHash();
            memoryOrder = stableHash; // They are equivalent unless overridden below

            if (typeDef.GetHashCode() == m_EntityTypeDef.GetHashCode())
            {
                memoryOrder = 0;
            }
            else
            {
                if (typeDef.CustomAttributes.Count > 0)
                {
                    var forcedMemoryOrderAttribute = typeDef.CustomAttributes.FirstOrDefault(ca => ca.Constructor.DeclaringType.Name == "ForcedMemoryOrderingAttribute");
                    if (forcedMemoryOrderAttribute != null)
                    {
                        memoryOrder = (ulong) forcedMemoryOrderAttribute.ConstructorArguments
                            // despite the field being a 'ulong', Mono.Cecil says it's a UInt64. Not a big issue but a possible inconsistency so we check for either
                            .First(arg => arg.Type.Name == "UInt64" || arg.Type.Name == "ulong")
                            .Value;
                    }
                }
            }
        }

        List<ECSTypeInfo> ECSTypesToRegister = new List<ECSTypeInfo>
        {
            new ECSTypeInfo(){ TypeCategory = TypeCategory.ComponentData, FullTypeName = "Unity.Entities.IComponentData" },
            new ECSTypeInfo(){ TypeCategory = TypeCategory.ISharedComponentData, FullTypeName = "Unity.Entities.ISharedComponentData" },
            new ECSTypeInfo(){ TypeCategory = TypeCategory.BufferData, FullTypeName = "Unity.Entities.IBufferElementData" }
        };

        const string kStaticRegClassName = "StaticTypeRegistry";
        const string kStaticRegAsmName = "Unity.Entities.StaticTypeRegistry";
        const string kStaticRegAsmNameWithExtension = "Unity.Entities.StaticTypeRegistry.dll";

        // NOTE: These flags must match what is in Unity.Entities.TypeManager
        public const int HasNoEntityReferencesFlag = 1 << 24; // this flag is inverted to ensure the type id of Entity can still be 1
        public const int SystemStateTypeFlag = 1 << 25;
        public const int BufferComponentTypeFlag = 1 << 26;
        public const int SharedComponentTypeFlag = 1 << 27;
        public const int ChunkComponentTypeFlag = 1 << 28;
        public const int ZeroSizeInChunkTypeFlag = 1 << 29;
        public const int ManagedComponentTypeFlag = 1 << 30; // TODO: If we can ensure TypeIndex is unsigned we can use the top bit for this
        public const int ClearFlagsMask = 0x00FFFFFF;
        public const int SystemStateSharedComponentTypeFlag = SystemStateTypeFlag | SharedComponentTypeFlag;

        int m_TotalTypeCount;
        int m_TotalEntityOffsetCount;
        int m_TotalBlobAssetRefOffsetCount;
        int m_TotalWriteGroupCount;
        int m_ArchBits;
        string m_OutputDir;
        Profile m_Profile;
        bool m_IsDebug;
        private FieldInfoGen m_fieldInfoGen;
        private ExecuteGen m_executeGen;
        private InterfaceGen m_interfaceGen;

        AssemblyDefinition m_TypeRegAssembly;
        AssemblyDefinition m_MscorlibAssembly;
        AssemblyDefinition m_EntityAssembly;
        TypeDefinition m_EntityTypeDef;
        List<AssemblyDefinition> m_AssemblyDefs;
        List<TypeDefinition> m_Systems;
        Dictionary<TypeDefinition, int> m_TypeDefToTypeIndex;

        // TypeReferences required for code gen
        TypeReference m_TypeInfoRef;
        MethodReference m_TypeInfoConstructorRef;
        MethodReference m_AddStaticTypesFromRegistryFnRef;
        MethodReference m_AddStaticSystemsFromRegistryFnRef;
        TypeReference m_SystemTypeRef;
        MethodReference m_SystemGuidHashFn;
        MethodReference m_GetTypeFromHandleFnRef;
        MethodReference m_GetTypeFnRef;
        MethodReference m_MemCmpFnRef;
        MethodReference m_MemCpyFnRef;
        TypeDefinition m_BufferHeaderDef;
        MethodReference m_IntPtrGetSizeFnRef;
        MethodReference m_InvalidOpExceptionCTORRef;
        MethodReference m_Hash32FnRef;

        TypeDefinition m_RegClass;

        // Static Registry Fields
        FieldDefinition m_RegTypeInfoArrayDef;
        FieldDefinition m_RegSystemTypeArrayDef;
        FieldDefinition m_RegSystemTypeNameArrayDef;
        FieldDefinition m_RegSystemSystemsArrayDef;
        FieldDefinition m_RegEntityOffsetArrayDef;
        FieldDefinition m_RegBlobAssetReferneceOffsetsArrayDef;
        FieldDefinition m_WriteGroupArrayDef;
        FieldDefinition m_RegSystemIsGroupArrayDef;
        FieldDefinition m_RegSystemNameArrayDef;

        // Static Registry Methods
        MethodDefinition m_RegCCTOR;
        MethodDefinition m_RegBoxedEqualsFn;
        MethodDefinition m_RegBoxedPtrEqualsFn;
        MethodDefinition m_RegBoxedGetHashCodeFn;
    }
}
