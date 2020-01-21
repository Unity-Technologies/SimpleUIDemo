#define USE_VOID_PTR // if defined, the Execute_Gen method will have `void*` or `int` parameter for everything.
                     // This works around a Burst issue, but makes the decompiled code more difficult to read.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Unity.ZeroPlayer
{
    internal class ExecuteGen
    {
        internal const string ExecuteFnName = "Execute_Gen";

        private List<AssemblyDefinition>
            m_assemblies;

        private AssemblyDefinition
            m_entityAssembly,
            m_zeroJobsAssembly,
            m_burstAssembly;

        enum Access
        {
            ReadWrite,
            ReadOnly,
            Exclude
        }

        struct Component
        {
            public TypeReference type;
            public Access access;
        }

        public ExecuteGen(List<AssemblyDefinition> assemblies)
        {
            m_assemblies = assemblies;
            m_entityAssembly = assemblies.First(asm => asm.Name.Name == "Unity.Entities");
            m_zeroJobsAssembly = assemblies.First(asm => asm.Name.Name == "Unity.ZeroJobs");
            m_burstAssembly = assemblies.First(asm => asm.Name.Name == "Unity.Burst");
        }

        bool ParamIsReadOnly(ParameterDefinition param)
        {
            if (param.HasCustomAttributes &&
                param.CustomAttributes.FirstOrDefault(p =>
                    p.AttributeType.FullName == "Unity.Collections.ReadOnlyAttribute") != null)
            {
                return true;
            }

            return false;
        }

        /// Generates the outer, non-burstable method that calls the inner Execute_Gen.
        ///
        /// JobTyping_System and JobTyping_Query (in TestExecuteGen) show the source
        /// code template this IL is based on.
        ///
        /// Tries to remove as many generics as possible, and only instance types
        /// are passed to the Execute/Execute_Gen
        ///
        /// <param name="isQuery">If true, this uses the variant that has an EntityQuery as a parameter.
        /// If false, expects a ComponentSystem as a parameter.</param>
        /// <param name="usesEntity">If true, the Entity and Index will be passed to the Execute.</param>
        public MethodDefinition GenOuterMethod(string methodName,
            AssemblyDefinition asm,
            TypeDefinition jobStruct,
            MethodDefinition generatedExecute,
            List<ParameterDefinition> executeParams,
            bool isQuery,
            bool usesEntity)
        {
            var zeroJobsAssembly = m_assemblies.First(a => a.Name.Name == "Unity.ZeroJobs");
            var jobHandleDef = zeroJobsAssembly.MainModule.Types.First(i => i.FullName == "Unity.Jobs.JobHandle");
            var jobHandleRef = asm.MainModule.ImportReference(jobHandleDef);

            var entityDef = m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.Entity");
            var entityManagerDef =
                m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.EntityManager");
            var worldDef = m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.World");
            var getActiveWorldFnDef = worldDef.Methods.First(i => i.Name == "get_Active");
            var getEntityManagerFnDef = worldDef.Methods.First(i => i.Name == "get_EntityManager");
            var getManagerACCCTypeFnRef = asm.MainModule.ImportReference(entityManagerDef.Methods.First(i => i.Name == "GetArchetypeChunkComponentType"));

            MethodDefinition getManagerArchetypeChunkEntityTypeFnDef =
                entityManagerDef.Methods.First(i => i.Name == "GetArchetypeChunkEntityType");
            MethodReference getManagerArchetypeChunkEntityTypeFnRef =
                asm.MainModule.ImportReference(getManagerArchetypeChunkEntityTypeFnDef);

            var componentTypeDef =
                m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.ComponentType");
            var componentTypeRef = asm.MainModule.ImportReference(componentTypeDef);

            var openReadWriteRef = asm.MainModule.ImportReference(
                componentTypeDef.Methods.First(i => i.Name == "ReadWrite" && i.GenericParameters.Count == 1));

            var openReadOnlyRef = asm.MainModule.ImportReference(
                componentTypeDef.Methods.First(i => i.Name == "ReadOnly" && i.GenericParameters.Count == 1));

            var openExcludeRef = asm.MainModule.ImportReference(
                componentTypeDef.Methods.First(i => i.Name == "Exclude" && i.GenericParameters.Count == 1));

            var componentSystemDef =
                m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.ComponentSystem");
            var componentSystemRef = asm.MainModule.ImportReference(componentSystemDef);

            var entityQueryDef =
                m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.EntityQuery");
            var entityQueryRef = asm.MainModule.ImportReference(entityQueryDef);

            // Unity.Entities.ComponentSystemBase.GetArchetypeChunkComponentType
            var componentSystemBaseDef =
                m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.ComponentSystemBase");
            var componentSystemBaseRef = asm.MainModule.ImportReference(componentSystemBaseDef);
            var getSystemACCCTypeFnRef = asm.MainModule.ImportReference(componentSystemBaseDef.Methods.First(i => i.Name == "GetArchetypeChunkComponentType"));

            // Unity.Entities.ComponentSystemBase.GetArchetypeChunkEntityType
            MethodDefinition getSystemArchetypeChunkEntityTypeFnDef =
                componentSystemBaseDef.Methods.First(i => i.Name == "GetArchetypeChunkEntityType");
            MethodReference getSystemArchetypeChunkEntityTypeFnRef =
                asm.MainModule.ImportReference(getSystemArchetypeChunkEntityTypeFnDef);

            var archetypeChunkEntityTypeDef =
                m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.ArchetypeChunkEntityType");
            var archetypeChunkEntityTypeRef = asm.MainModule.ImportReference(archetypeChunkEntityTypeDef);

            // Unity.Entities.ComponentSystemBase.GetEntityQueryInternal(ComponentType* componentTypes, int count)
            var getEntityQueryInternalFnDef = componentSystemBaseDef.Methods.First(i =>
                i.Name == "GetEntityQueryInternal"
                && i.Parameters.Count == 2);
            getEntityQueryInternalFnDef.IsPublic = true;

            var archetypeChunkDef =
                m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.ArchetypeChunk");

            // Unity.Jobs.JobHandle.Complete
            MethodDefinition completeFnDef = jobHandleDef.Methods.First(n => n.FullName == "System.Void Unity.Jobs.JobHandle::Complete()");

            // Unity.Entities.ArchetypeChunk.GetNativeArray
            var getNativeArrayFnRef = asm.MainModule.ImportReference(archetypeChunkDef.Methods.First(m => m.Name == "GetNativeArray" && m.HasGenericParameters));

            MethodDefinition getNativeEntityArrayFnDef =
                archetypeChunkDef.Methods.First(m => m.Name == "GetNativeArray" && !m.HasGenericParameters);
            var getNativeEntityArrayFnRef = asm.MainModule.ImportReference(getNativeEntityArrayFnDef);

            var nativeArrayUnsafeUtilityDef = m_zeroJobsAssembly.MainModule.Types.First(i =>
                i.FullName == "Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility");

            var unsafePtrFnRef = asm.MainModule.ImportReference(nativeArrayUnsafeUtilityDef.Methods.First(i => i.Name == "GetUnsafePtr"));
            var unsafeReadOnlyPtrFnRef = asm.MainModule.ImportReference(nativeArrayUnsafeUtilityDef.Methods.First(i => i.Name == "GetUnsafeReadOnlyPtr"));

            MethodDefinition getUnsafeReadOnlyPtrFnDef =
                nativeArrayUnsafeUtilityDef.Methods.First(i => i.Name == "GetUnsafeReadOnlyPtr");

            // Unity.Entities.EntityQuery.CreateArchetypeChunkArray
            var createArchetypeChunkArrayFnDef =
                entityQueryDef.Methods.First(i => i.Name == "CreateArchetypeChunkArray" && i.Parameters.Count == 1);

            TypeDefinition openNativeArrayDef =
                m_zeroJobsAssembly.MainModule.Types.First(i => i.FullName == "Unity.Collections.NativeArray`1");

            MethodDefinition scheduleMethodDef = jobStruct.Methods.First(n => n.Name == InterfaceGen.PrepareJobAtScheduleTimeFn);
            MethodDefinition executeMethodDef = jobStruct.Methods.First(n => n.Name == InterfaceGen.PrepareJobAtExecuteTimeFn);
            MethodDefinition cleanupMethodDef = jobStruct.Methods.First(n => n.Name == InterfaceGen.CleanupJobFn);

            // Unity.Collections.NativeArray.Dispose
            MethodDefinition disposeFnDef = openNativeArrayDef.Methods.First(m => m.Name == "Dispose");

            var getLengthFnDef = openNativeArrayDef.Methods.First(m => m.Name == "get_Length");
            var getLengthFnRef =
                asm.MainModule.ImportReference(getLengthFnDef.MakeHostInstanceGeneric(archetypeChunkDef));

            var getItemFnDef = openNativeArrayDef.Methods.First(m => m.Name == "get_Item");
            var getItemFnRef = asm.MainModule.ImportReference(getItemFnDef.MakeHostInstanceGeneric(archetypeChunkDef));

            var getCountFnDef = archetypeChunkDef.Methods.First(m => m.Name == "get_Count");
            var getCountFnRef = asm.MainModule.ImportReference(getCountFnDef);

            var method = new MethodDefinition(methodName,
                MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig, jobHandleRef);

            // -------- Parameters ---------
            var paramJob = new ParameterDefinition("job", ParameterAttributes.None,
                asm.MainModule.ImportReference(jobStruct));
            method.Parameters.Add(paramJob);

            ParameterDefinition paramSystem = null;
            ParameterDefinition paramQuery = null;

            if (isQuery)
            {
                paramQuery = new ParameterDefinition("query", ParameterAttributes.None, entityQueryRef);
                method.Parameters.Add(paramQuery);
            }
            else
            {
                paramSystem = new ParameterDefinition("system", ParameterAttributes.None, componentSystemBaseRef);
                method.Parameters.Add(paramSystem);
            }

            var paramJobHandle = new ParameterDefinition("dependsOn",
                ParameterAttributes.Optional | ParameterAttributes.HasDefault, jobHandleRef);
            method.Parameters.Add(paramJobHandle);
            paramJobHandle.Constant = new ParameterDefinition(asm.MainModule.ImportReference(typeof(Nullable)));

            // -------- Variables -----------
            VariableDefinition vEntityManager = null;
            if (isQuery)
            {
                vEntityManager = new VariableDefinition(asm.MainModule.ImportReference(entityManagerDef));
                method.Body.Variables.Add(vEntityManager);
            }

            var vDelegateTypePtr =
                new VariableDefinition(asm.MainModule.ImportReference(componentTypeDef.MakePointerType()));
            if (!isQuery) method.Body.Variables.Add(vDelegateTypePtr);

            var vEntityQuery = new VariableDefinition(entityQueryRef);
            method.Body.Variables.Add(vEntityQuery);

            VariableDefinition vEntityType = null;
            if (usesEntity)
            {
                vEntityType = new VariableDefinition(archetypeChunkEntityTypeRef);
                method.Body.Variables.Add(vEntityType);
            }

            var openArchetypeCCTRef =
                asm.MainModule.ImportReference(m_entityAssembly.MainModule.Types.First(i =>
                    i.FullName == "Unity.Entities.ArchetypeChunkComponentType`1"));

            List<VariableDefinition> vChunkComponentType = new List<VariableDefinition>();
            for (int i = 0; i < executeParams.Count; ++i)
            {
                var param = executeParams[i].ParameterType.GetElementType();
                if (param.Module != asm.MainModule)
                    asm.MainModule.ImportReference(param);
                var special = openArchetypeCCTRef.MakeGenericInstanceType(param);
                var vd = new VariableDefinition(special);
                vChunkComponentType.Add(vd);
                method.Body.Variables.Add(vd);
            }

            var archetypeNativeArray = openNativeArrayDef.MakeGenericInstanceType(archetypeChunkDef);
            TypeReference archetypeNativeArrayRef = asm.MainModule.ImportReference(archetypeNativeArray);
            var vChunks = new VariableDefinition(archetypeNativeArrayRef);
            method.Body.Variables.Add(vChunks);

            var vJ = new VariableDefinition(asm.MainModule.ImportReference(typeof(int)));
            method.Body.Variables.Add(vJ);
            var vChunkCount = new VariableDefinition(asm.MainModule.ImportReference(typeof(int)));
            method.Body.Variables.Add(vChunkCount);
            var vChunk = new VariableDefinition(asm.MainModule.ImportReference(archetypeChunkDef));
            method.Body.Variables.Add(vChunk);

            VariableDefinition vEntityArray = null;
            if (usesEntity)
            {
                vEntityArray = new VariableDefinition(asm.MainModule.ImportReference(entityDef.MakePointerType()));
                method.Body.Variables.Add(vEntityArray);
            }

            List<VariableDefinition> vArray = new List<VariableDefinition>();
            for (int i = 0; i < executeParams.Count; ++i)
            {
                var vd = new VariableDefinition(asm.MainModule.ImportReference(typeof(void*)));
                vArray.Add(vd);
                method.Body.Variables.Add(vd);
            }

            var vJobHandle = new VariableDefinition(jobHandleRef);
            method.Body.Variables.Add(vJobHandle);

            method.Body.InitLocals = true;
            var bc = method.Body.Instructions;

            // -------- Body -----------
            // dependsOn.Complete(); // Until proper jobs are supported, use this to make sure we've flushed out previous work.
            bc.Add(Instruction.Create(OpCodes.Ldarga, paramJobHandle));
            bc.Add(Instruction.Create(OpCodes.Call, asm.MainModule.ImportReference(completeFnDef)));

            // ScheduleJob_Gen()
            bc.Add(Instruction.Create(OpCodes.Ldarga, paramJob));
            bc.Add(Instruction.Create(OpCodes.Conv_U));
            bc.Add(Instruction.Create(OpCodes.Call, asm.MainModule.ImportReference(scheduleMethodDef)));
            bc.Add(Instruction.Create(OpCodes.Pop));    // The return code does nothing here. In other jobs code,
                                                             // the expected return code is used to validate the generated
                                                             // code was called and is working correctly.

            // ExecuteJob_Gen(0)
            bc.Add(Instruction.Create(OpCodes.Ldarga, paramJob));
            bc.Add(Instruction.Create(OpCodes.Conv_U));
            bc.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            bc.Add(Instruction.Create(OpCodes.Call, asm.MainModule.ImportReference(executeMethodDef)));

            if (isQuery)
            {
                // entityQuery = _query
                bc.Add(Instruction.Create(OpCodes.Ldarg, paramQuery));
                bc.Add(Instruction.Create(OpCodes.Stloc, vEntityQuery));

                // EntityManager entityManager = World.Active.EntityManager;
                bc.Add(Instruction.Create(OpCodes.Call, asm.MainModule.ImportReference(getActiveWorldFnDef)));
                bc.Add(Instruction.Create(OpCodes.Callvirt, asm.MainModule.ImportReference(getEntityManagerFnDef)));
                bc.Add(Instruction.Create(OpCodes.Stloc, vEntityManager));
            }
            else
            {
                // ---------------------------------------------------
                // var delegateTypes = stackalloc ComponentType[1]
                // {
                //    ComponentType.ReadWrite<Position>(),
                // };

                List<Component> comps = FindComponents(jobStruct, executeParams);

                // Allocate nParams ComponentTypes
                bc.Add(Instruction.Create(OpCodes.Ldc_I4, comps.Count));
                bc.Add(Instruction.Create(OpCodes.Conv_U));
                bc.Add(Instruction.Create(OpCodes.Sizeof, componentTypeRef));
                bc.Add(Instruction.Create(OpCodes.Mul_Ovf_Un));
                bc.Add(Instruction.Create(OpCodes.Localloc));

                // Initialize them.
                for (int i = 0; i < comps.Count; ++i)
                {
                    MethodReference methodRef = null;
                    if (comps[i].access == Access.ReadOnly) methodRef = openReadOnlyRef;
                    else if (comps[i].access == Access.ReadWrite) methodRef = openReadWriteRef;
                    else if (comps[i].access == Access.Exclude) methodRef = openExcludeRef;

                    var genericArg = comps[i].type;
                    if (genericArg.Module != asm.MainModule)
                        asm.MainModule.ImportReference(comps[i].type);

                    MethodReference special = TypeRegGen.MakeGenericMethodSpecialization(methodRef, genericArg);

                    bc.Add(Instruction.Create(OpCodes.Dup)); // duplicate the address we will add to.
                    bc.Add(Instruction.Create(OpCodes.Ldc_I4, i));
                    bc.Add(Instruction.Create(OpCodes.Sizeof, componentTypeRef));
                    bc.Add(Instruction.Create(OpCodes.Mul));
                    bc.Add(Instruction.Create(OpCodes.Add));
                    bc.Add(Instruction.Create(OpCodes.Call, special));
                    bc.Add(Instruction.Create(OpCodes.Stobj, componentTypeRef));
                }

                bc.Add(Instruction.Create(OpCodes.Stloc, vDelegateTypePtr));

                // ---------------------------------------------------
                // EntityQuery query = system.GetEntityQueryInternal(delegateTypes, 1);
                bc.Add(Instruction.Create(OpCodes.Ldarg, paramSystem));
                bc.Add(Instruction.Create(OpCodes.Ldloc, vDelegateTypePtr));
                bc.Add(Instruction.Create(OpCodes.Ldc_I4, comps.Count));
                bc.Add(Instruction.Create(OpCodes.Callvirt,
                    asm.MainModule.ImportReference(getEntityQueryInternalFnDef)));
                bc.Add(Instruction.Create(OpCodes.Stloc, vEntityQuery));
            }

            // ArchetypeChunkEntityType archetypeChunkEntityType = system.GetArchetypeChunkEntityType();
            if (usesEntity)
            {
                if (isQuery)
                {
                    bc.Add(Instruction.Create(OpCodes.Ldloc, vEntityManager));
                    bc.Add(Instruction.Create(OpCodes.Call, getManagerArchetypeChunkEntityTypeFnRef));
                    bc.Add(Instruction.Create(OpCodes.Stloc, vEntityType));
                }
                else
                {
                    bc.Add(Instruction.Create(OpCodes.Ldarg, paramSystem));
                    bc.Add(Instruction.Create(OpCodes.Callvirt, getSystemArchetypeChunkEntityTypeFnRef));
                    bc.Add(Instruction.Create(OpCodes.Stloc, vEntityType));
                }
            }

            // Query:    var chunkComponentType0 = entityManager.GetArchetypeChunkComponentType<Position>(false);
            // System:   var chunkComponentType0 = system.GetArchetypeChunkComponentType<Position>(false);
            for (int i = 0; i < executeParams.Count; ++i)
            {
                if (isQuery)
                    bc.Add(Instruction.Create(OpCodes.Ldloc, vEntityManager));
                else
                    bc.Add(Instruction.Create(OpCodes.Ldarg, paramSystem));

                bc.Add(Instruction.Create(ParamIsReadOnly(executeParams[i]) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0));

                var genericDefinitionType = isQuery ? getManagerACCCTypeFnRef : getSystemACCCTypeFnRef;
                var parameterType = executeParams[i].ParameterType.GetElementType();
                if (parameterType.Module != asm.MainModule)
                    parameterType = asm.MainModule.ImportReference(parameterType);

                var specialGetACCCTypeMethodRef = TypeRegGen.MakeGenericMethodSpecialization(genericDefinitionType, parameterType);
                bc.Add(Instruction.Create(OpCodes.Callvirt, specialGetACCCTypeMethodRef));
                bc.Add(Instruction.Create(OpCodes.Stloc, vChunkComponentType[i]));
            }

            // var chunks = query.CreateArchetypeChunkArray(Allocator.TempJob);
            bc.Add(Instruction.Create(OpCodes.Ldloc, vEntityQuery));
            bc.Add(Instruction.Create(OpCodes.Ldc_I4_3)); // Allocator.TempJob = 3
            bc.Add(Instruction.Create(OpCodes.Callvirt,
                asm.MainModule.ImportReference(createArchetypeChunkArrayFnDef)));
            bc.Add(Instruction.Create(OpCodes.Stloc, vChunks));

            // for(int j=0, chunkCount = chunks.Length; j<chunkCount; ++j)
            bc.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            bc.Add(Instruction.Create(OpCodes.Stloc, vJ));

            bc.Add(Instruction.Create(OpCodes.Ldloca, vChunks));
            bc.Add(Instruction.Create(OpCodes.Call, getLengthFnRef));
            bc.Add(Instruction.Create(OpCodes.Stloc, vChunkCount));

            var jPrefix = bc.Count;
            bc.Add(Instruction.Create(OpCodes.Nop)); // will become branch
            var jLoopStart = Instruction.Create(OpCodes.Nop);
            bc.Add(jLoopStart);

            {
                // var chunk = chunks[j];
                bc.Add(Instruction.Create(OpCodes.Ldloca, vChunks));
                bc.Add(Instruction.Create(OpCodes.Ldloc, vJ));
                bc.Add(Instruction.Create(OpCodes.Call, getItemFnRef));
                bc.Add(Instruction.Create(OpCodes.Stloc, vChunk));

                if (usesEntity)
                {
                    // Entity* entityArray = (Entity*)chunk.GetNativeArray(entityType).GetUnsafeReadOnlyPtr();
                    bc.Add(Instruction.Create(OpCodes.Ldloca, vChunk));
                    bc.Add(Instruction.Create(OpCodes.Ldloc, vEntityType));
                    bc.Add(Instruction.Create(OpCodes.Call, getNativeEntityArrayFnRef));

                    var special = TypeRegGen.MakeGenericMethodSpecialization(getUnsafeReadOnlyPtrFnDef, entityDef);
                    bc.Add(Instruction.Create(OpCodes.Call, asm.MainModule.ImportReference(special)));
                    bc.Add(Instruction.Create(OpCodes.Stloc, vEntityArray));
                }

                // var array0 = chunk.GetNativeArray(chunkComponentType0).GetUnsafePtr();
                for (int i = 0; i < executeParams.Count; ++i)
                {
                    bc.Add(Instruction.Create(OpCodes.Ldloca, vChunk));
                    bc.Add(Instruction.Create(OpCodes.Ldloc, vChunkComponentType[i]));

                    var element = executeParams[i].ParameterType.GetElementType();
                    if (element.Module != asm.MainModule)
                        element = asm.MainModule.ImportReference(element);

                    var specialGetNativeArrayFnRef =
                        TypeRegGen.MakeGenericMethodSpecialization(getNativeArrayFnRef, element);

                    bc.Add(Instruction.Create(OpCodes.Call, specialGetNativeArrayFnRef));

                    MethodReference special = null;
                    if (ParamIsReadOnly(executeParams[i]))
                    {
                        special = TypeRegGen.MakeGenericMethodSpecialization(unsafeReadOnlyPtrFnRef, element);
                    }
                    else
                    {
                        special = TypeRegGen.MakeGenericMethodSpecialization(unsafePtrFnRef, element);
                    }

                    bc.Add(Instruction.Create(OpCodes.Call, special));

                    bc.Add(Instruction.Create(OpCodes.Stloc, vArray[i]));
                }

                // Execute_Gen(&job, chunk.Count, (Position*) array0);
                bc.Add(Instruction.Create(OpCodes.Ldarga, paramJob));
                bc.Add(Instruction.Create(OpCodes.Conv_U));
                bc.Add(Instruction.Create(OpCodes.Ldloca, vChunk));
                bc.Add(Instruction.Create(OpCodes.Call, getCountFnRef));
                if (usesEntity)
                {
                    bc.Add(Instruction.Create(OpCodes.Ldloc, vEntityArray));
                }

                for (int i = 0; i < executeParams.Count; i++)
                {
                    bc.Add(Instruction.Create(OpCodes.Ldloc, vArray[i]));
                }

                bc.Add(Instruction.Create(OpCodes.Call, generatedExecute));

                // Tail of j loop
                var jLoopTail = Instruction.Create(OpCodes.Ldloc, vJ);
                bc.Add(jLoopTail);
                bc.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                bc.Add(Instruction.Create(OpCodes.Add));
                bc.Add(Instruction.Create(OpCodes.Stloc, vJ));
            }
            // Compare
            var jCompare = Instruction.Create(OpCodes.Ldloc, vJ);
            bc.Add(jCompare);
            bc.Add(Instruction.Create(OpCodes.Ldloc, vChunkCount));
            bc.Add(Instruction.Create(OpCodes.Clt));
            bc.Add(Instruction.Create(OpCodes.Brtrue, jLoopStart));

            bc[jPrefix] = Instruction.Create(OpCodes.Br, jCompare);

            // Dispose
            bc.Add(Instruction.Create(OpCodes.Ldloca, vChunks));
            bc.Add(Instruction.Create(OpCodes.Call,
                asm.MainModule.ImportReference(disposeFnDef.MakeHostInstanceGeneric(archetypeChunkDef))));

            // CleanupJob()
            bc.Add(Instruction.Create(OpCodes.Ldarga, paramJob));
            bc.Add(Instruction.Create(OpCodes.Conv_U));
            // This calls the CleanupMethod, but disables the "wrapper cleanup" which makes no sense in this version
            // of IJobForEach. Correct fix coming in a future PR, when IJobForEach is moved back to the
            // normal Job code with the other jobs.
            bc.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            bc.Add(Instruction.Create(OpCodes.Conv_U));
            bc.Add(Instruction.Create(OpCodes.Call, asm.MainModule.ImportReference(cleanupMethodDef)));

            // Return a jobHandle
            bc.Add(Instruction.Create(OpCodes.Ldloca, vJobHandle));
            bc.Add(Instruction.Create(OpCodes.Initobj, jobHandleRef));
            bc.Add(Instruction.Create(OpCodes.Ldloc, vJobHandle));
            bc.Add(Instruction.Create(OpCodes.Ret));

            method.Body.Optimize();
            jobStruct.Methods.Add(method);
            return method;
        }

        public MethodDefinition GenExecuteMethod(string methodName,
            AssemblyDefinition asm,
            TypeDefinition jobStruct,
            MethodDefinition userExecuteMethod,
            List<ParameterDefinition> executeParams,
            bool usesEntity)
        {
            var method = new MethodDefinition(methodName,
                MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
                asm.MainModule.ImportReference(typeof(void)));

            var burstClass = m_burstAssembly.MainModule.Types.First(n => n.Name == "BurstCompileAttribute");
            var burstClassCtor = burstClass.Methods.First(n => n.Name == ".ctor");
            method.CustomAttributes.Add(new CustomAttribute(asm.MainModule.ImportReference(burstClassCtor)));

            var paramJob = new ParameterDefinition("job", ParameterAttributes.None,
#if USE_VOID_PTR
                asm.MainModule.ImportReference(typeof(void*)));
#else
                asm.MainModule.ImportReference(jobStruct.Resolve()).MakePointerType());
#endif
            method.Parameters.Add(paramJob);

            var paramCount = new ParameterDefinition("count", ParameterAttributes.None,
                asm.MainModule.ImportReference(typeof(int)));
            method.Parameters.Add(paramCount);

            ParameterDefinition paramEntityPtr = null;
            var entityDef = m_entityAssembly.MainModule.Types.First(i => i.FullName == "Unity.Entities.Entity");
            var entityIndexDef = entityDef.Fields.First(i => i.Name == "Index");

            if (usesEntity)
            {
                paramEntityPtr = new ParameterDefinition("entity", ParameterAttributes.None,
#if USE_VOID_PTR
                    asm.MainModule.ImportReference(typeof(void*)));
#else
                    asm.MainModule.ImportReference(entityDef.MakePointerType()));
#endif
                method.Parameters.Add(paramEntityPtr);
            }

            List<TypeReference> paramArgsTypes = new List<TypeReference>();
            List<ParameterDefinition> paramArgs = new List<ParameterDefinition>();
            for (int count = 0; count < executeParams.Count; ++count)
            {
#if USE_VOID_PTR
                var tr = asm.MainModule.ImportReference(typeof(void*));
#else
                var tr = asm.MainModule.ImportReference(
                    executeParams[count].ParameterType.GetElementType().MakePointerType());
#endif
                var pd = new ParameterDefinition("arg" + count, ParameterAttributes.None, tr);
                paramArgsTypes.Add(tr);
                paramArgs.Add(pd);
                method.Parameters.Add(pd);
            }

            method.Body.InitLocals = true;

            var iVarRef = new VariableDefinition(asm.MainModule.ImportReference(typeof(int)));
            method.Body.Variables.Add(iVarRef);

            var bc = method.Body.Instructions;

            // for (int i = 0; i < count; ++i, ++arg0)
            bc.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            bc.Add(Instruction.Create(OpCodes.Stloc, iVarRef));

            var iPrefix = bc.Count;
            bc.Add(Instruction.Create(OpCodes.Nop));
            var iLoopStart = Instruction.Create(OpCodes.Nop);
            bc.Add(iLoopStart);

            {
                // job.Execute(ref *arg0, ref *arg1);
                //   or
                // job.Execute(*entity, entity->Index, ref *arg0);
                //
                bc.Add(Instruction.Create(OpCodes.Ldarg, paramJob));
                if (usesEntity)
                {
                    // Load the entity pointer.
                    bc.Add(Instruction.Create(OpCodes.Ldarg, paramEntityPtr));

                    // Load the index
                    bc.Add(Instruction.Create(OpCodes.Ldobj, asm.MainModule.ImportReference(entityDef)));
                    bc.Add(Instruction.Create(OpCodes.Ldarg, paramEntityPtr));
                    bc.Add(Instruction.Create(OpCodes.Ldfld, asm.MainModule.ImportReference(entityIndexDef)));
                }

                for (int i = 0; i < executeParams.Count; ++i)
                {
                    bc.Add(Instruction.Create(OpCodes.Ldarg, paramArgs[i]));
                }

                bc.Add(Instruction.Create(OpCodes.Call, asm.MainModule.ImportReference(userExecuteMethod)));

                // Increment i
                bc.Add(Instruction.Create(OpCodes.Ldloc, iVarRef));
                bc.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                bc.Add(Instruction.Create(OpCodes.Add));
                bc.Add(Instruction.Create(OpCodes.Stloc, iVarRef));

                if (usesEntity)
                {
                    bc.Add(Instruction.Create(OpCodes.Ldarg, paramEntityPtr));
                    bc.Add(Instruction.Create(OpCodes.Sizeof, asm.MainModule.ImportReference(entityDef)));
                    bc.Add(Instruction.Create(OpCodes.Add));
                    bc.Add(Instruction.Create(OpCodes.Starg, paramEntityPtr));
                }

                // Increment params
                for (int i = 0; i < executeParams.Count; ++i)
                {
                    bc.Add(Instruction.Create(OpCodes.Ldarg, paramArgs[i]));
                    bc.Add(Instruction.Create(OpCodes.Sizeof,
                        asm.MainModule.ImportReference(executeParams[i].ParameterType.GetElementType())));
                    bc.Add(Instruction.Create(OpCodes.Add));
                    bc.Add(Instruction.Create(OpCodes.Starg, paramArgs[i]));
                }
            }

            // tail i loop
            var iLoopTail = Instruction.Create(OpCodes.Ldloc, iVarRef);
            bc.Add(iLoopTail);
            bc.Add(Instruction.Create(OpCodes.Ldarg, paramCount));
            bc.Add(Instruction.Create(OpCodes.Clt));
            bc.Add(Instruction.Create(OpCodes.Brtrue, iLoopStart));

            // patch to start with the check:
            bc[iPrefix] = Instruction.Create(OpCodes.Br, iLoopTail);

            bc.Add(Instruction.Create(OpCodes.Ret));

            method.Body.Optimize();
            jobStruct.Methods.Add(method);
            return method;
        }

        List<Component> FindComponents(TypeDefinition jobStruct, List<ParameterDefinition> executeParams)
        {
            List<Component> comps = new List<Component>();
            List<TypeDefinition> require = new List<TypeDefinition>();
            List<TypeDefinition> exclude = new List<TypeDefinition>();

            if (jobStruct.Interfaces.First(i =>
                    i.InterfaceType.FullName == "Unity.Entities.JobForEachExtensions/IBaseJobForEach") != null)
            {
                foreach (var attr in jobStruct.CustomAttributes)
                {
                    bool hasExclude = attr.AttributeType.FullName == "Unity.Entities.ExcludeComponentAttribute";
                    bool hasRequire = attr.AttributeType.FullName == "Unity.Entities.RequireComponentTagAttribute";

                    if (!hasExclude && !hasRequire) continue;

                    if (attr.HasConstructorArguments)
                    {
                        foreach (var arg in attr.ConstructorArguments)
                        {
                            var cArr = arg.Value as CustomAttributeArgument[];
                            if (cArr != null)
                            {
                                CustomAttributeArgument[] caa = cArr;
                                for (int i = 0; i < caa.Length; ++i)
                                {
                                    if (caa[i].Value is TypeDefinition)
                                    {
                                        TypeDefinition td = (TypeDefinition) caa[i].Value;
                                        if (hasExclude) exclude.Add(td);
                                        if (hasRequire) require.Add(td);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < executeParams.Count; ++i)
            {
                comps.Add(new Component()
                {
                    type = executeParams[i].ParameterType.GetElementType(),
                    access = ParamIsReadOnly(executeParams[i]) ? Access.ReadOnly : Access.ReadWrite
                });
            }

            for (int i = 0; i < require.Count; ++i)
            {
                comps.Add(new Component()
                {
                    type = require[i],
                    access = Access.ReadOnly
                });
            }

            for (int i = 0; i < exclude.Count; ++i)
            {
                comps.Add(new Component()
                {
                    type = exclude[i],
                    access = Access.Exclude
                });
            }

            return comps;
        }

        // A game developer would call IJobForEachExtensions.Schedule<T>(), or a similar function.
        //
        // This method will  change the call to Schedule (or Run or ScheduleSingle) to call
        // instead a generated method in the Job structure.
        //
        // Will change the call to Schedule (or Run or ScheduleSingle) to call a generated method in the
        // Job structure.
        //
        // Code gen will generate 2 or 4 functions depending on use.
        // They can be viewed with a decompiler to see how the IL code  works, or to debug the code.
        //
        // Execute_Gen is the inner method variant that uses a System, and calls Execute. It is Burstable.
        // Execute_Query_Gen is the inner method variant that uses an EntityQuery, and calls Execute. It is Burstable.
        // Outer_Gen is the outer wrapper when a System is used. It is not Burstable.
        // Outer_Query_gen is the outer wrapper when an EntityQuery is used. It is not Burstable.
        //
        public void PatchScheduleCallsAndGenerateIL(TypeDefinition type, AssemblyDefinition asm,
            MethodDefinition method,
            IEnumerable<Instruction> outerList)
        {
            var giList = outerList.Where(i =>
                (i.Operand is MethodReference)
                && (i.Operand as MethodReference).ContainsGenericParameter
                && ((i.Operand as MethodReference).FullName.Contains(
                        "Unity.Entities.JobForEachExtensions::Schedule<") ||
                    (i.Operand as MethodReference).FullName.Contains(
                        "Unity.Entities.JobForEachExtensions::Run<") ||
                    (i.Operand as MethodReference).FullName.Contains(
                        "Unity.Entities.JobForEachExtensions::ScheduleSingle<"))

            );
            foreach (var g in giList)
            {
                var gMethod = g.Operand as GenericInstanceMethod;
                if (gMethod == null)
                    throw new InvalidOperationException($"{gMethod.FullName} was expected to be a generic method!");

                var jobStruct = gMethod.GenericArguments[0].Resolve();
                var jobName = jobStruct.FullName;

                try
                {

                    // We have found a call to Schedule. But what types do we use to specialize the
                    // generic? The Job is required to have an Execute method - with the correct
                    // parameters. It's a handy place to grab them, so find that method and use it.
                    MethodDefinition executeMethod = jobStruct.Methods.First(f => f.Name == "Execute");

                    // There are (so far) 2 flavors of Schedule.
                    //     Schedule<T>(this T jobData, ComponentSystemBase system, JobHandle dependsOn = default(JobHandle))
                    //     Schedule<T>(this T jobData, EntityQuery query, JobHandle dependsOn = default(JobHandle))
                    // The only difference being the 2nd parameter.

                    bool isQuery = gMethod.Parameters.Count >= 2 &&
                                   gMethod.Parameters[1].ParameterType.FullName == "Unity.Entities.EntityQuery";

                    if (!isQuery)
                    {
                        // Check to make sure the parameters are as expected.
                        if (gMethod.Parameters.Count < 2 &&
                            gMethod.Parameters[1].ParameterType.FullName != "Unity.Entities.ComponentSystemBase")
                        {
                            throw new InvalidOperationException(
                                "Schedule<T> form isn't recognized. Support needs to be added in code-gen.");
                        }
                    }

                    // Extract the ComponentData used by the Execute
                    bool usesEntity = false;
                    if (executeMethod.Parameters[0].ParameterType.FullName == "Unity.Entities.Entity")
                    {
                        usesEntity = true;
                        if (executeMethod.Parameters[1].ParameterType.FullName != "System.Int32")
                        {
                            throw new InvalidOperationException(
                                "FindScheduleMethod: Execute method specifies an Entity, but not an int index");
                        }
                    }

                    // Incredibly confusing to extract the parameters later, so extract them here and re-use.
                    List<ParameterDefinition> executeParams = new List<ParameterDefinition>();
                    for (int i = usesEntity ? 2 : 0; i < executeMethod.Parameters.Count; ++i)
                    {
                        executeParams.Add(executeMethod.Parameters[i]);
                    }

                    // There are two variants of the Outer method, but the Execute is unique (and
                    // called by the Outer_ method.)
                    var outerName = "Outer_" + (isQuery ? "Query_" : "System_") + "Gen";

                    var existingOuterMethod = jobStruct.Methods.FirstOrDefault(m => m.Name == outerName);
                    var existingExecuteMethod = jobStruct.Methods.FirstOrDefault(m => m.Name == ExecuteFnName);

                    if (existingOuterMethod == null)
                    {
                        // An Execute_Gen can be shared between Outer_Query_Gen and Outer_System_Gen
                        if (existingExecuteMethod == null)
                        {
                            existingExecuteMethod = GenExecuteMethod(ExecuteFnName, asm, jobStruct, executeMethod,
                                executeParams, usesEntity);
                        }

                        existingOuterMethod = GenOuterMethod(outerName, asm, jobStruct,
                            existingExecuteMethod,
                            executeParams,
                            isQuery, usesEntity);

                        g.Operand = asm.MainModule.ImportReference(existingOuterMethod);
                    }
                    else
                    {
                        // TODO: should we check parameters just to be sure?
                        // TODO: also could / should confirm existence of both execute and schedule
                        g.Operand = asm.MainModule.ImportReference(existingOuterMethod);
                    }
                }
                catch (Exception)
                {
                    string msg = $"Error patching IJobForEach '{jobName}' in method '{method.FullName}'";
                    throw new InvalidOperationException(msg);
                }
            }
        }
    }
}

