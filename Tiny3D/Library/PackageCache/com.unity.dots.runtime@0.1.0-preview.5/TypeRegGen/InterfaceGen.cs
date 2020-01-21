using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Entities.BuildUtils;

namespace Unity.ZeroPlayer
{
    class InterfaceGen
    {
        // Duplicated from Unity.Jobs.LowLevel
        public enum JobType
        {
            Single,
            ParallelFor
        }

        // Note that these can use nameof(Type.Func) when we switch to the ILPP approach.
        // Placed on the IJobBase instance:
        internal const string PrepareJobAtScheduleTimeFn = "PrepareJobAtScheduleTimeFn_Gen";
        internal const string PrepareJobAtExecuteTimeFn = "PrepareJobAtExecuteTimeFn_Gen";
        internal const string CleanupJobFn = "CleanupJobFn_Gen";
        internal const string GetExecuteMethodFn = "GetExecuteMethod_Gen";

        // Placed on the JobProducer
        internal const string ProducerExecuteFn = "ProducerExecuteFn_Gen";
        internal const string ProducerCleanupFn = "ProducerCleanupFn_Gen"; // Only for IJobParallel for.

        const int EXECUTE_JOB_PARAM = 0;
        const int EXECUTE_JOB_INDEX_PARAM = 4;

        List<AssemblyDefinition> m_Assemblies;
        AssemblyDefinition m_SystemAssembly;
        AssemblyDefinition m_ZeroJobsAssembly;
        AssemblyDefinition m_LowLevelAssembly;
        TypeDefinition m_AtomicDef; // if null, then a release build (no safety handles)
        MethodDefinition m_ScheduleJob;
        MethodDefinition m_ExecuteJob;
        MethodDefinition m_CleanupJob;
        TypeDefinition m_IJobBase;
        TypeDefinition m_UnsafeUtilityDef;
        TypeDefinition m_JobUtilityDef;
        TypeDefinition m_PinvokeCallbackAttribute;
        TypeDefinition m_JobMetaDataDef;
        int m_ArchBits;

        // TODO the JobDataField is truly strange, and I'd like to pull it out.
        // But there's working code that uses it, so it isn't trivial.
        // A better generic resolution system might fix it.

        public class JobDesc
        {
            public TypeReference JobProducer; // Type of the producer: CustomJobProcess.
            public TypeDefinition JobProducerDef; // just the Resolve() of above; use all the time.
            public TypeReference JobInterface; // Type of the job: ICustomJob
            public TypeReference JobData;   // Type of the JobData, which is the first parameter of
                                            // the Execute: CustomJobData<T>
                                            // (Where T, remember, is an ICustomJob)
            public JobType JobType;
            public FieldDefinition JobDataField; // If the jobs wraps an inner definition, it is here. (Or null if not.)

            // Ex: jobStruct.JobData.Execute(); // JobData is the JobDataField
            public bool IsParallel()
            {
                return JobType == JobType.ParallelFor;
            }
        }

        public List<JobDesc> jobList = new List<JobDesc>();
        public List<TypeDefinition> typesToMakePublic = new List<TypeDefinition>();

        // Performs the many assorted tasks to allow Jobs (Custom, Unity, etc.)
        // to run without reflection. The name refers to creating the IJobBase
        // interface (and code-gen of the appropriate methods) for all Jobs.
        public InterfaceGen(List<AssemblyDefinition> assemblies, int archBits)
        {
            m_Assemblies = assemblies;
            m_ArchBits = archBits;
            m_SystemAssembly = assemblies.First(asm => asm.Name.Name == "mscorlib");
            m_ZeroJobsAssembly = assemblies.First(asm => asm.Name.Name == "Unity.ZeroJobs");
            m_LowLevelAssembly = assemblies.First(asm => asm.Name.Name == "Unity.LowLevel");

            // Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle
            m_AtomicDef = m_ZeroJobsAssembly.MainModule.Types.FirstOrDefault(i =>
                i.FullName == "Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle");
            m_UnsafeUtilityDef = m_LowLevelAssembly.MainModule.GetAllTypes().First(i =>
                i.FullName == "Unity.Collections.LowLevel.Unsafe.UnsafeUtility");

            m_JobUtilityDef = m_ZeroJobsAssembly.MainModule.Types.First(i =>
                i.FullName == "Unity.Jobs.LowLevel.Unsafe.JobsUtility");
            m_PinvokeCallbackAttribute = m_ZeroJobsAssembly.MainModule.Types.First(i =>
                i.FullName == "Unity.Jobs.MonoPInvokeCallbackAttribute");
            m_JobMetaDataDef = m_ZeroJobsAssembly.MainModule.Types.First(i =>
                i.FullName == "Unity.Jobs.LowLevel.Unsafe.JobMetaData");

            FindAllJobProducers();
        }

        JobType FindJobType(TypeDefinition producer)
        {
            foreach (var m in producer.Methods)
            {
                if (m.HasBody)
                {
                    var bc = m.Body.Instructions;
                    for (int i = 0; i < bc.Count; i++)
                    {
                        if (bc[i].OpCode == OpCodes.Call && ((MethodReference)bc[i].Operand).Name ==
                            "CreateJobReflectionData")
                        {
                            // Found the call to CreateJobReflection data. Now look at the constant
                            // on the stack to get the Single (0) or Parallel (1)
                            int j = i - 1;
                            while (j > 0)
                            {
                                if (bc[j].OpCode == OpCodes.Ldc_I4_0) return JobType.Single;
                                if (bc[j].OpCode == OpCodes.Ldc_I4_1) return JobType.ParallelFor;
                                --j;
                            }

                            throw new Exception($"The CreateJobReflectionData in method '{m.Name}' on '{producer.Name}' does not specify a constant value for JobType.");
                        }
                    }
                }
            }

            throw new Exception($"Can not find the CreateJobReflectionData call on '{producer.Name}'");
        }

        // Scans all the JobProducers and fills in the JobDesc that gives information about them.
        void FindAllJobProducers()
        {
            foreach (var asm in m_Assemblies)
            {
                foreach (TypeDefinition type in asm.MainModule.GetAllTypes())
                {
                    if (!type.IsInterface || !type.HasCustomAttributes)
                        continue;

                    CustomAttribute ca = GetProducerAttributeIfExists(type);

                    if (ca == null)
                        continue;

                    TypeReference producer = (TypeReference)ca.ConstructorArguments[0].Value;

                    var executeMethod = producer.Resolve().Methods.FirstOrDefault(n => n.Name == "Execute");

                    // There can be multiple Execute methods; simple check to find the required one.
                    // The required form:
                    //  public delegate void ExecuteJobFunction(ref JobStruct<T> jobStruct, System.IntPtr additionalPtr, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
                    if (executeMethod != null
                        && executeMethod.Parameters.Count == 5
                        && executeMethod.Parameters[1].ParameterType.Name == "IntPtr"
                        && executeMethod.Parameters[2].ParameterType.Name == "IntPtr"
                        && executeMethod.Parameters[3].ParameterType.Name == "JobRanges&")
                    {
                        JobDesc jobDesc = new JobDesc();
                        jobDesc.JobProducer = producer;
                        jobDesc.JobProducerDef = producer.Resolve();
                        jobDesc.JobInterface = type;

                        var jobData = executeMethod.Parameters[EXECUTE_JOB_PARAM].ParameterType.GetElementType();
                        jobDesc.JobData = jobData;
                        jobDesc.JobDataField = FindJobData(jobDesc.JobData.Resolve());
                        if (jobDesc.JobDataField == null)
                        {
                            jobDesc.JobData = producer;
                        }
                        else
                        {
                            typesToMakePublic.Add(jobDesc.JobDataField.DeclaringType);
                        }

                        jobDesc.JobType = FindJobType(producer.Resolve());
                        jobList.Add(jobDesc);
                    }
                }
            }

            // Need to restore logging:
            // https://unity3d.atlassian.net/browse/DOTSR-336
        }

        private static CustomAttribute GetProducerAttributeIfExists(TypeDefinition type)
        {
            return type.CustomAttributes.FirstOrDefault(a =>
                a.AttributeType.FullName == "Unity.Jobs.LowLevel.Unsafe.JobProducerTypeAttribute");
        }

        // TODO should probably put the jobData in the JobDesc
        FieldDefinition FindJobData(TypeDefinition tr)
        {
            if (tr == null)
                return null;

            // internal struct JobStruct<T> where T : struct, IJob
            // {
            //    static IntPtr JobReflectionData;
            //    internal T JobData;                    <---- looking for this. Has the same name as the first generic.
            //
            // But some (many) jobs don't have the inner JobData; the job itself is the type.
            // So need to handle that fallback.

            return tr.Fields.FirstOrDefault(f => f.FieldType.Name == tr.GenericParameters[0].Name);
        }

        // Generates the ProducerExecuteFn which wraps the user Execute, to
        // pass down job data structure.
        void GenerateProducerExecuteFn(ModuleDefinition module, JobDesc jobDesc)
        {
            var jobRanges = m_ZeroJobsAssembly.MainModule.Types.First(i => i.FullName == "Unity.Jobs.LowLevel.Unsafe.JobRanges");
            var intPtr = m_SystemAssembly.MainModule.Types.First(i => i.FullName == "System.IntPtr");
            var intPtrCtor = intPtr.GetConstructors().First(c => c.Parameters.Count == 1 && c.Parameters[0].ParameterType.FullName == "System.Int32");
            var freeRef = m_UnsafeUtilityDef.Methods.First(n => n.Name == "Free");
            /*
             * types from other assemblies need to be able to reach into this type and grab its generated execute method
             * via GetExecuteMethod_Gen(), so it has to be public. 
             */
            jobDesc.JobProducerDef.IsPublic = true;
            if (jobDesc.JobProducerDef.IsNested)
                jobDesc.JobProducerDef.IsNestedPublic = true;

            MethodDefinition executeMethod = jobDesc.JobProducerDef.Methods.First(m => m.Name == "Execute");
            MethodDefinition executeGen = new MethodDefinition(ProducerExecuteFn,
                MethodAttributes.Public | MethodAttributes.Static,
                module.ImportReference(typeof(void)));

            var pInvokeCctor = m_PinvokeCallbackAttribute.GetConstructors();
            executeGen.CustomAttributes.Add(new CustomAttribute(module.ImportReference(pInvokeCctor.First())));

            var metaPtrParam = new ParameterDefinition("jobMetaPtr", ParameterAttributes.None,
                module.ImportReference(typeof(void*)));
            executeGen.Parameters.Add(metaPtrParam);

            var jobIndexParam = new ParameterDefinition("jobIndex", ParameterAttributes.None,
                module.ImportReference(typeof(int)));
            executeGen.Parameters.Add(jobIndexParam);

            // TODO: is it safe to not copy the structure in MT??
            // var genericJobDataRef = jobDesc.JobData.MakeGenericInstanceType(jobDesc.JobData.GenericParameters.ToArray());
            // var jobData = new VariableDefinition(module.ImportReference(genericJobDataRef));
            // executeRT.Body.Variables.Add(jobData);

            var jobDataPtr = new VariableDefinition(module.ImportReference(typeof(void*)));
            executeGen.Body.Variables.Add(jobDataPtr);

            executeGen.Body.InitLocals = true;
            var bc = executeGen.Body.Instructions;

            // TODO: is it safe to not copy the structure in MT??
            // CustomJobData<T> jobData = *ptr;
            // bc.Add(Instruction.Create(OpCodes.Ldarg, ptrParam));
            // bc.Add(Instruction.Create(OpCodes.Ldobj, module.ImportReference(genericJobDataRef)));
            // bc.Add(Instruction.Create(OpCodes.Stloc, jobData));

            // void* jobDataPtr = jobMetaPtr + sizeof(JobMetaData);
            bc.Add(Instruction.Create(OpCodes.Ldarg, metaPtrParam));
            bc.Add(Instruction.Create(OpCodes.Sizeof, module.ImportReference(m_JobMetaDataDef)));
            bc.Add(Instruction.Create(OpCodes.Add));
            bc.Add(Instruction.Create(OpCodes.Stloc, jobDataPtr));

            // Execute(ref jobData, new IntPtr(0), new IntPtr(0), ref ranges, 0);
            bc.Add(Instruction.Create(OpCodes.Ldloc, jobDataPtr));
            bc.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            bc.Add(Instruction.Create(OpCodes.Newobj, module.ImportReference(intPtrCtor)));
            bc.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            bc.Add(Instruction.Create(OpCodes.Newobj, module.ImportReference(intPtrCtor)));

            bc.Add(Instruction.Create(OpCodes.Ldarg, metaPtrParam)); // TODO assumes the metaPtr *is* the JobRanges
            bc.Add(Instruction.Create(OpCodes.Ldarg, jobIndexParam));
            bc.Add(Instruction.Create(OpCodes.Call,
                module.ImportReference(
                    executeMethod.MakeHostInstanceGeneric(jobDesc.JobData.GenericParameters.ToArray()))));

            if (!jobDesc.IsParallel())
            {
                // UnsafeUtility.Free(structPtr, Allocator.TempJob);
                bc.Add(Instruction.Create(OpCodes.Ldarg, metaPtrParam));
                bc.Add(Instruction.Create(OpCodes.Ldc_I4_3)); // literal value of Allocator.TempJob
                bc.Add(Instruction.Create(OpCodes.Call, module.ImportReference(freeRef)));
            }

            bc.Add(Instruction.Create(OpCodes.Ret));
            jobDesc.JobProducerDef.Methods.Add(executeGen);
        }

        // Generates the method to cleanup memory and call the IJobBase.CleanupJobFn_Gen()
        void GenerateProducerCleanupFn(ModuleDefinition module, JobDesc jobDesc)
        {
            MethodDefinition cleanupFn = new MethodDefinition(ProducerCleanupFn,
                MethodAttributes.Public | MethodAttributes.Static,
                module.ImportReference(typeof(void)));

            var pInvokeCctor = m_PinvokeCallbackAttribute.GetConstructors();
            cleanupFn.CustomAttributes.Add(new CustomAttribute(module.ImportReference(pInvokeCctor.First())));

            var freeDef = m_UnsafeUtilityDef.Methods.First(n => n.Name == "Free");

            var metaPtrParam = new ParameterDefinition("jobMetaPtr", ParameterAttributes.None,
                module.ImportReference(typeof(void*)));
            cleanupFn.Parameters.Add(metaPtrParam);

            var jobDataPtr = new VariableDefinition(module.ImportReference(typeof(void*)));
            cleanupFn.Body.Variables.Add(jobDataPtr);

            cleanupFn.Body.InitLocals = true;
            var bc = cleanupFn.Body.Instructions;

            // void* ptr = jobMetaPtr + sizeof(JobMetaData);
            bc.Add(Instruction.Create(OpCodes.Ldarg, metaPtrParam));
            bc.Add(Instruction.Create(OpCodes.Sizeof, module.ImportReference(m_JobMetaDataDef)));
            bc.Add(Instruction.Create(OpCodes.Add));
            bc.Add(Instruction.Create(OpCodes.Stloc, jobDataPtr));

            // The UserJobData case is tricky.
            // jobData.UserJobData.CleanupTasksFn_Gen()
            // OR
            // jobData.CleanupTasksFn_Gen()
            if (jobDesc.JobDataField != null)
            {
                var genericJobDataRef = jobDesc.JobData.MakeGenericInstanceType(jobDesc.JobData.GenericParameters.ToArray());
                var jobDataVar = new VariableDefinition(module.ImportReference(genericJobDataRef));
                cleanupFn.Body.Variables.Add(jobDataVar);

                // CustomJobData<T> jobData = *ptr;
                bc.Add(Instruction.Create(OpCodes.Ldloc, jobDataPtr));
                bc.Add(Instruction.Create(OpCodes.Ldobj, module.ImportReference(genericJobDataRef)));
                bc.Add(Instruction.Create(OpCodes.Stloc, jobDataVar));

                // jobData.UserJobData.CleanupTasksFn_Gen(ptr)
                bc.Add(Instruction.Create(OpCodes.Ldloca, jobDataVar));
                bc.Add(Instruction.Create(OpCodes.Ldflda,
                    module.ImportReference(TypeRegGen.MakeGenericFieldSpecialization(jobDesc.JobDataField, jobDesc.JobData.GenericParameters.ToArray()))));

                bc.Add(Instruction.Create(OpCodes.Ldloc, jobDataPtr));
                bc.Add(Instruction.Create(OpCodes.Constrained, jobDesc.JobProducerDef.GenericParameters[0]));
            }
            else
            {
                var jobDataRef = module.ImportReference(jobDesc.JobData.GenericParameters[0]);
                var jobDataVar = new VariableDefinition(jobDataRef);
                cleanupFn.Body.Variables.Add(jobDataVar);

                // T jobData = *ptr;
                bc.Add(Instruction.Create(OpCodes.Ldloc, jobDataPtr));
                bc.Add(Instruction.Create(OpCodes.Ldobj, jobDataRef));
                bc.Add(Instruction.Create(OpCodes.Stloc, jobDataVar));

                // jobData.CleanupTasksFn_Gen(null)
                // There is no wrapping data structure; so the parameter can be null.
                bc.Add(Instruction.Create(OpCodes.Ldloca, jobDataVar));

                bc.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                bc.Add(Instruction.Create(OpCodes.Conv_U));
                bc.Add(Instruction.Create(OpCodes.Constrained, jobDataRef));
            }

            // The first generic parameter is always the user Job, which is where the IJobBase has been attached.
            bc.Add(Instruction.Create(OpCodes.Callvirt, module.ImportReference(m_CleanupJob)));

            // UnsafeUtility.Free(metaPtrParam, Allocator.TempJob);
            bc.Add(Instruction.Create(OpCodes.Ldarg, metaPtrParam));
            bc.Add(Instruction.Create(OpCodes.Ldc_I4_3)); // literal value of Allocator.TempJob
            bc.Add(Instruction.Create(OpCodes.Call, module.ImportReference(freeDef)));

            bc.Add(Instruction.Create(OpCodes.Ret));

            jobDesc.JobProducerDef.Methods.Add(cleanupFn);
        }

        // Adds the prefix and postfix calls to the Execute method.
        // For a super-simple Execute:
        // public static void Execute(...
        // {
        //      jobData.UserJobData.PrepareJobAtExecuteTimeFn_Gen(jobIndex);  <-- generated here
        //      jobData.UserJobData.Execute(ref jobData.abData);
        //      jobData.UserJobData.CleanupJobFn_Gen(&jobData);               <-- generated here
        // }
        void PatchProducerExecute(ModuleDefinition module, JobDesc jobDesc)
        {
            MethodDefinition executeMethod = jobDesc.JobProducerDef.Methods.First(m => m.Name == "Execute");

            var bc = executeMethod.Body.Instructions;

            var il = executeMethod.Body.GetILProcessor();
            var first = bc[0];
            var last = bc[bc.Count - 1];

            il.InsertBefore(first, Instruction.Create(OpCodes.Ldarg_0));
            if (jobDesc.JobDataField != null)
                il.InsertBefore(first, Instruction.Create(OpCodes.Ldflda,
                    module.ImportReference(TypeRegGen.MakeGenericFieldSpecialization(jobDesc.JobDataField,
                        jobDesc.JobData.GenericParameters.ToArray()))));
            il.InsertBefore(first, Instruction.Create(OpCodes.Ldarg, executeMethod.Parameters[EXECUTE_JOB_INDEX_PARAM]));

            // The first generic parameter is always the user Job, which is where the IJobBase has been attached.
            il.InsertBefore(first, Instruction.Create(OpCodes.Constrained, jobDesc.JobProducerDef.GenericParameters[0]));
            il.InsertBefore(first, Instruction.Create(OpCodes.Callvirt, module.ImportReference(m_ExecuteJob)));

            if (!jobDesc.IsParallel())
            {
                il.InsertBefore(last, Instruction.Create(OpCodes.Ldarg_0));
                if (jobDesc.JobDataField != null)
                    il.InsertBefore(last, Instruction.Create(OpCodes.Ldflda,
                        module.ImportReference(
                            TypeRegGen.MakeGenericFieldSpecialization(jobDesc.JobDataField, jobDesc.JobData.GenericParameters.ToArray()))));

                if (jobDesc.JobDataField != null)
                {
                    // pass in the wrapper.
                    il.InsertBefore(last, Instruction.Create(OpCodes.Ldarg_0));
                }
                else
                {
                    // pass in null
                    il.InsertBefore(last, Instruction.Create(OpCodes.Ldc_I4_0));
                    il.InsertBefore(last ,Instruction.Create(OpCodes.Conv_U));
                }

                // The first generic parameter is always the user Job, which is where the IJobBase has been attached.
                il.InsertBefore(last, Instruction.Create(OpCodes.Constrained, jobDesc.JobProducerDef.GenericParameters[0]));
                il.InsertBefore(last, Instruction.Create(OpCodes.Callvirt, module.ImportReference(m_CleanupJob)));
            }
        }

        static TypeReference[] MakeGenericArgsArray(ModuleDefinition module, IGenericParameterProvider forType, IEnumerable<GenericParameter> gp)
        {
            List<TypeReference> lst = new List<TypeReference>();
            foreach (var g in gp)
            {
                TypeReference t = module.ImportReference(g);
                lst.Add(t);

                // We may have more generic parameters than we need. For example,
                // the schedule may take more parameters than needed by the job.
                if (lst.Count == forType.GenericParameters.Count)
                    break;
            }

            return lst.ToArray();
        }

        // Patches the Schedule method to add the size, and call the IJobBase.PrepareJobAtScheduleTimeFn_Gen
        void PatchJobSchedule(ModuleDefinition module, JobDesc jobDesc)
        {
            TypeDefinition parent = jobDesc.JobProducerDef.DeclaringType ?? jobDesc.JobProducerDef;

            foreach (var method in parent.Methods)
            {
                if (method.Body?.Instructions != null)
                {
                    var bc = method.Body.Instructions;
                    for (int i = 0; i < bc.Count; ++i)
                    {
                        if (bc[i].OpCode == OpCodes.Call)
                        {
                            if (((MethodReference)bc[i].Operand).FullName.Contains(
                                "Unity.Jobs.LowLevel.Unsafe.JobsUtility/JobScheduleParameters"))
                            {
                                // Need generic Parameters (keeps as generic form)
                                {
                                    if (bc[i - 2].OpCode != OpCodes.Ldc_I4_0)
                                        throw new Exception(
                                            $"Expected to find default 0 value for size in JobScheduleParameters when processing '{method.FullName}'");

                                    if (jobDesc.JobDataField != null)
                                    {
                                        var arr = MakeGenericArgsArray(module, jobDesc.JobData, method.GenericParameters);
                                        TypeReference td = module.ImportReference(jobDesc.JobData.MakeGenericInstanceType(arr));
                                        bc[i - 2] = Instruction.Create(OpCodes.Sizeof, module.ImportReference(td));
                                    }
                                    else
                                    {
                                        bc[i - 2] = Instruction.Create(OpCodes.Sizeof, method.Parameters[0].ParameterType);
                                    }
                                }
                                {
                                    // the 3 here is a magic flag value from the default parameter to help find the byte code.
                                    if (bc[i - 1].OpCode != OpCodes.Ldc_I4_3)
                                        throw new Exception(
                                            $"Unexpected default value in '{method.FullName}'");

                                    // The first generic parameter is always the user Job, which is where the IJobBase has been attached.
                                    TypeReference t = module.ImportReference(method.GenericParameters[0]);

                                    // The parameter to AddressOf() is the parameter or local we want to load.
                                    // Go find that.
                                    int j;
                                    for (j = i - 1; j > 0; --j)
                                    {
                                        if (bc[j].OpCode == OpCodes.Call &&
                                            ((MethodReference)bc[j].Operand).Name == "AddressOf")
                                            break;
                                    }

                                    if (j == 0)
                                        throw new ArgumentException($"Expected to find AddressOf call in JobSchedule parameters while looking at `{method.FullName}'");

                                    // data.UserJobData.PrepareJobAtScheduleTimeFn_Gen()
                                    // OR
                                    // data.PrepareJobAtScheduleTimeFn_Gen()
                                    if (jobDesc.JobDataField != null)
                                    {
                                        if (bc[j - 1].OpCode == OpCodes.Ldarga || bc[j - 1].OpCode == OpCodes.Ldarga_S)
                                        {
                                            var pd = (ParameterDefinition)bc[j - 1].Operand;
                                            bc[i - 1] =
                                                Instruction.Create(OpCodes.Ldarga, pd);
                                        }
                                        else
                                        {
                                            var vd = (VariableDefinition)bc[j - 1].Operand;
                                            bc[i - 1] =
                                                Instruction.Create(OpCodes.Ldloca, vd);
                                        }

                                        TypeDefinition userDataFD = jobDesc.JobDataField.DeclaringType;
                                        var arr = MakeGenericArgsArray(module, userDataFD, method.GenericParameters);

                                        bc.Insert(i + 0,
                                            Instruction.Create(OpCodes.Ldflda,
                                                module.ImportReference(
                                                    TypeRegGen.MakeGenericFieldSpecialization(jobDesc.JobDataField, arr))));
                                    }
                                    else
                                    {
                                        bc[i - 1] = Instruction.Create(OpCodes.Nop);
                                        bc.Insert(i + 0, Instruction.Create(OpCodes.Ldarga, method.Parameters[0]));
                                    }

                                    bc.Insert(i + 1,
                                        Instruction.Create(OpCodes.Constrained, t));

                                    bc.Insert(i + 2,
                                        Instruction.Create(OpCodes.Callvirt, module.ImportReference(m_ScheduleJob)));
                                }

                                // TODO more than one Schedule call in the same method won't be found. But can't keep iterating with fixing the 'i' index.
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Patch CreateJobReflectionData to pass in the ProducerExecuteFn_Gen and ProducerCleanupFn_Gen methods.
        void PatchCreateJobReflection(ModuleDefinition module, JobDesc jobDesc)
        {
            var managedJobDelegate = m_JobUtilityDef.NestedTypes.First(i => i.Name == "ManagedJobDelegate");
            var managedJobDelegateCtor = managedJobDelegate.Methods[0];
            var managedForEachJobDelegate = m_JobUtilityDef.NestedTypes.First(i => i.Name == "ManagedJobForEachDelegate");
            var managedForEachJobDelegateCtor = managedForEachJobDelegate.Methods[0];

            var genExecuteMethodMethodToCall = module.ImportReference(m_IJobBase.Methods.First(m =>
                m.Name == GetExecuteMethodFn));
            // Patch the CreateJobReflectionData to pass in ExecuteRT_Gen
            foreach (var method in jobDesc.JobProducerDef.Methods)
            {
                var bc = method.Body.Instructions;
                for (int i = 0; i < bc.Count; ++i)
                {
                    if (bc[i].OpCode == OpCodes.Call && bc[i].Operand is MethodReference
                        && (bc[i].Operand as MethodReference).FullName.StartsWith(
                            "System.IntPtr Unity.Jobs.LowLevel.Unsafe.JobsUtility::CreateJobReflectionData")
                    )
                    {
                        var typeOfUserJobStruct = jobDesc.JobProducerDef.GenericParameters[0];
                        typeOfUserJobStruct.Constraints.Add(new GenericParameterConstraint(module.ImportReference(m_IJobBase)));
                        
                        var userJobStructLocal = new VariableDefinition(typeOfUserJobStruct);
                        method.Body.Variables.Add(userJobStructLocal);
                        // Required to have an Execute_RT
                        MethodDefinition executeRTMDef =
                            jobDesc.JobProducerDef.Methods.First(m => m.Name == ProducerExecuteFn);

                        MethodDefinition postRTMDef = null;
                        if (jobDesc.JobType == JobType.ParallelFor)
                        {
                            postRTMDef = jobDesc.JobProducerDef.Methods.First(m => m.Name == ProducerCleanupFn);
                        }

                        // Instruction before should be 2 ldnull
                        if (bc[i - 1].OpCode != OpCodes.Ldnull)
                            throw new InvalidOperationException($"Expected ldnull opcode (at position -1) in '{method.FullName}'");
                        if (bc[i - 2].OpCode != OpCodes.Ldnull)
                            throw new InvalidOperationException($"Expected ldnull opcode (at position -2) in '{method.FullName}'");

                        // Wipe out the ldnull, then replace with new parameters.
                        bc[i - 1] = Instruction.Create(OpCodes.Nop);
                        bc[i - 2] = Instruction.Create(OpCodes.Nop);

                        var il = method.Body.GetILProcessor();
                        var func = bc[i];

                        List<TypeReference> lst = new List<TypeReference>();
                        foreach (var g in jobDesc.JobProducerDef.GenericParameters)
                        {
                            TypeReference t = module.ImportReference(g);
                            lst.Add(t);
                        }

                        var executeRTMRef = executeRTMDef.MakeHostInstanceGeneric(lst.ToArray());
                        
                        il.InsertBefore(func, Instruction.Create(OpCodes.Ldloca, userJobStructLocal));
                        il.InsertBefore(func, Instruction.Create(OpCodes.Initobj, typeOfUserJobStruct));
                        il.InsertBefore(func, Instruction.Create(OpCodes.Ldloca, userJobStructLocal));
                        il.InsertBefore(func, Instruction.Create(OpCodes.Constrained, typeOfUserJobStruct));

                        il.InsertBefore(func,
                            Instruction.Create(OpCodes.Callvirt,
                                genExecuteMethodMethodToCall));

                        // ManagedJobForEachDelegate codegenCleanupDelegate
                        // Tiny doesn't currently support this not being null. (No intrinsic reason,
                        // just the code isn't plumbed in.)
                        il.InsertBefore(func, Instruction.Create(OpCodes.Ldnull));

                        if (jobDesc.JobType == JobType.ParallelFor)
                        {
                            var postRTMRef = postRTMDef.MakeHostInstanceGeneric(lst.ToArray());
                            il.InsertBefore(func, Instruction.Create(OpCodes.Ldftn,
                                module.ImportReference(postRTMRef)));
                            il.InsertBefore(func, Instruction.Create(OpCodes.Newobj,
                                module.ImportReference(managedJobDelegateCtor)));
                        }

                        break;
                    }
                }
            }
        }

        public void PatchJobsCode()
        {
            foreach (JobDesc jobDesc in jobList)
            {
                var module = jobDesc.JobInterface.Module;

                GenerateProducerExecuteFn(module, jobDesc);
                if (jobDesc.IsParallel())
                {
                    GenerateProducerCleanupFn(module, jobDesc);
                }

                PatchProducerExecute(module, jobDesc);
                PatchJobSchedule(module, jobDesc);
                PatchCreateJobReflection(module, jobDesc);
            }
        }

        bool TypeHasIJobBase(TypeDefinition td)
        {
            if (td.HasInterfaces && td.Interfaces.Any(i =>
                i.InterfaceType.FullName == "Unity.Jobs.IJobBase"))
                return true;
            return false;
        }

        bool TypeHasIJobBaseMethods(TypeDefinition td)
        {
            if (td.HasMethods && td.Methods.FirstOrDefault(m => m.Name == PrepareJobAtExecuteTimeFn) != null)
                return true;
            return false;
        }

        public void AddGenExecuteMethodMethods()
        {
            foreach (var asm in m_Assemblies)
            {
                var allTypes = asm.MainModule.GetAllTypes();
                foreach (var type in allTypes)
                {
                    if (type.IsValueType && type.HasInterfaces)
                    {
                        var foreachJobInterface = type.Interfaces.FirstOrDefault(i =>
                            i.InterfaceType.FullName == "Unity.Entities.JobForEachExtensions/IBaseJobForEach");
                        if (foreachJobInterface != null)
                        {
                            /*
                             * it makes no sense to have one of these for ijobforeach jobs, but they inherit from ijobbase,
                             * which has a getexecutemethod on it, and
                             * the clr will complain if we try to load a type with an unimplemented method on it, so generate one that
                             * loads a nullptr.
                             */
                            type.Methods.Add(GenGetExecuteMethodMethod(asm,
                                type,
                                null));
                        }
                        else
                        {
                            for (int i = 0; i < type.Interfaces.Count; i++)
                            {
                                foreach (JobDesc job in jobList)
                                {
                                    if (type.Interfaces[i].InterfaceType.FullName == job.JobInterface.FullName)
                                    {
                                        type.Methods.Add(GenGetExecuteMethodMethod(asm,
                                            type,
                                            job.JobProducerDef.Methods.First(m => m.Name == ProducerExecuteFn)));
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddMethods()
        {
            // Find IJobBase
            //     Add SetupJob(int)
            // Patch Unity.Jobs.LowLevel.Unsafe.JobsUtility.SetupJob/Cleanup
            // Find all implementers of IJobBase
            //    Add SetupJob(int) { return }

            m_IJobBase = m_ZeroJobsAssembly.MainModule.GetAllTypes().First(i => i.FullName == "Unity.Jobs.IJobBase");
            m_IJobBase.IsPublic = true;

            m_ScheduleJob = m_IJobBase.Methods.First(m => m.Name == PrepareJobAtScheduleTimeFn);
            m_ExecuteJob = m_IJobBase.Methods.First(m => m.Name == PrepareJobAtExecuteTimeFn);
            m_CleanupJob = m_IJobBase.Methods.First(m => m.Name == CleanupJobFn);
            
            // Add the IJobBase interface to the custom job interface
            foreach (JobDesc job in jobList)
            {
                TypeDefinition type = ((TypeDefinition)job.JobInterface);
                type.Interfaces.Add(new InterfaceImplementation(type.Module.ImportReference(m_IJobBase)));
            }

            // Go through each type, and if it is targeted by a JobProducer, add the IJobBase interface,
            // as well as the IJobBase Setup/Cleanup methods.
            // Also handle the special case of IJobForEach.
            foreach (var asm in m_Assemblies)
            {
                var allTypes = asm.MainModule.GetAllTypes();
                foreach (var type in allTypes)
                {
                    if (type.IsValueType && type.HasInterfaces)
                    {
                        bool isIJobForEach = type.Interfaces.FirstOrDefault(i =>
                                i.InterfaceType.FullName == "Unity.Entities.JobForEachExtensions/IBaseJobForEach") != null;
                        JobDesc jobDesc = null;
                        if (!isIJobForEach)
                        {
                            for (int i = 0; i < type.Interfaces.Count; i++)
                            {
                                foreach (JobDesc job in jobList)
                                {
                                    if (type.Interfaces[i].InterfaceType.FullName == job.JobInterface.FullName)
                                    {
                                        jobDesc = job;
                                        break;
                                    }
                                }
                            }
                        }
                        if (isIJobForEach)
                        {
                            // Special case (for now) for IJobForEach
                            if (!TypeHasIJobBase(type))
                            {
                                type.Interfaces.Add(
                                    new InterfaceImplementation(type.Module.ImportReference(m_IJobBase)));
                            }

                            if (!TypeHasIJobBaseMethods(type))
                            {
                                type.Methods.Add(GenScheduleMethod(asm, type));
                                type.Methods.Add(GenExecuteMethod(asm, type));
                                type.Methods.Add(GenCleanupMethod(asm, type, null));
                            }
                        }
                        else if (jobDesc != null)
                        {
                            if (!TypeHasIJobBase(type))
                            {
                                type.Interfaces.Add(
                                    new InterfaceImplementation(type.Module.ImportReference(m_IJobBase)));
                            }

                            if (!TypeHasIJobBaseMethods(type))
                            {
                                type.Methods.Add(GenScheduleMethod(asm, type));
                                type.Methods.Add(GenExecuteMethod(asm, type));
                                type.Methods.Add(GenCleanupMethod(asm, type, jobDesc));
                            }
                        }
                    }
                }
            }
        }

        private MethodDefinition GenGetExecuteMethodMethod(AssemblyDefinition asm,
            TypeDefinition type,
            MethodDefinition genExecuteMethod)
        {
            var module = asm.MainModule;
            var managedJobDelegate = module.ImportReference(m_JobUtilityDef.NestedTypes.First(i => i.Name == "ManagedJobForEachDelegate").Resolve());
            var managedJobDelegateCtor = module.ImportReference(managedJobDelegate.Resolve().Methods[0]);
            var method = new MethodDefinition(GetExecuteMethodFn,
                MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.NewSlot |
                MethodAttributes.Virtual,
                managedJobDelegate);
            method.Body.InitLocals = true;
            var bc = method.Body.Instructions;
            
            /*
             * return "wrapper job struct, e.g. IJobExtensions.JobStruct<YourSpecificJobType>".ProducerExecuteFn_Gen;
             */
            
            bc.Add(Instruction.Create(OpCodes.Ldnull));
            /*
             * the clr will complain if we try to load a type with an unimplemented method on it, so generate one that
             * throws an exception.
             *
             * (il2cpp will explode if we try to just return one with null in it)
             */
            if (genExecuteMethod == null)
            {
                bc.Add(Instruction.Create(OpCodes.Ldnull));
                bc.Add(Instruction.Create(OpCodes.Throw));
            }
            else
            {
                TypeReference job = type;
                if (job.HasGenericParameters)
                    job = job.MakeGenericInstanceType(job.GenericParameters.Select(p => job.Module.ImportReference(p)).ToArray());
                bc.Add(Instruction.Create(OpCodes.Ldftn,
                    module.ImportReference(genExecuteMethod).MakeHostInstanceGeneric(job)));
                bc.Add(Instruction.Create(OpCodes.Newobj,
                    managedJobDelegateCtor));
                bc.Add(Instruction.Create(OpCodes.Ret));
            }

            method.Body.Optimize();
            return method;
        }

        MethodDefinition GenScheduleMethod(
            AssemblyDefinition asm,
            TypeDefinition jobTypeDef)
        {
            var method = new MethodDefinition(PrepareJobAtScheduleTimeFn,
                MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.NewSlot |
                MethodAttributes.Virtual,
                asm.MainModule.ImportReference(typeof(int)));

            // -------- Parameters ---------
            method.Body.InitLocals = true;
            var bc = method.Body.Instructions;

            if (m_AtomicDef != null)
                AddSafetyIL(method, asm, jobTypeDef);

            // Magic number "2" is returned so that we can check (at run time) that code-gen actually occured.
            bc.Add(Instruction.Create(OpCodes.Ldc_I4_2));
            bc.Add(Instruction.Create(OpCodes.Ret));
            method.Body.Optimize();
            return method;
        }

        MethodDefinition FindDispose(TypeDefinition td)
        {
            // Make sure we find the Dispose() and not Dispose(JobHandle).
            var disposeFnDef = td.Methods.FirstOrDefault(m => m.Name == "Dispose" && m.Parameters.Count == 0);
            return disposeFnDef;
        }

        MethodDefinition GenExecuteMethod(
            AssemblyDefinition asm,
            TypeDefinition jobTypeDef)
        {
            var method = new MethodDefinition(PrepareJobAtExecuteTimeFn,
                MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.NewSlot |
                MethodAttributes.Virtual,
                asm.MainModule.ImportReference(typeof(void)));

            // -------- Parameters ---------
            var paramJobIndex = new ParameterDefinition("jobIndex", ParameterAttributes.None,
                asm.MainModule.ImportReference(typeof(int)));
            method.Parameters.Add(paramJobIndex);

            method.Body.InitLocals = true;
            var bc = method.Body.Instructions;

            AddThreadIndexIL(method, asm, jobTypeDef);

            bc.Add(Instruction.Create(OpCodes.Ret));
            method.Body.Optimize();
            return method;
        }

        void GenDisposeIL(
            MethodDefinition method,
            AssemblyDefinition asm,
            TypeDefinition jobTypeDef)
        {
            var bc = method.Body.Instructions;

            foreach (var field in jobTypeDef.Fields)
            {
                var deallocateOnJobCompletionAttr = field.CustomAttributes.FirstOrDefault(ca =>
                    ca.Constructor.DeclaringType.Name == "DeallocateOnJobCompletionAttribute");
                if (deallocateOnJobCompletionAttr != null)
                {
                    var supportsAttribute = field.FieldType.Resolve().CustomAttributes.FirstOrDefault(ca =>
                        ca.Constructor.DeclaringType.Name == "NativeContainerSupportsDeallocateOnJobCompletionAttribute");
                    if (supportsAttribute == null)
                        throw new ArgumentException(
                            $"DeallocateOnJobCompletion for {field.FullName} is invalid without NativeContainerSupportsDeallocateOnJobCompletion on {field.FieldType.FullName}");

                    bc.Add(Instruction.Create(OpCodes.Ldarg_0));
                    bc.Add(Instruction.Create(OpCodes.Ldflda, asm.MainModule.ImportReference(field)));

                    var disposeFnDef = FindDispose(field.FieldType.Resolve());
                    disposeFnDef.IsPublic = true;
                    var disposeFnRef = asm.MainModule.ImportReference(disposeFnDef);
                    if (field.FieldType is GenericInstanceType)
                    {
                        GenericInstanceType git = (GenericInstanceType)field.FieldType;
                        List<TypeReference> genericArgs = new List<TypeReference>();
                        foreach (var specializationType in git.GenericArguments)
                        {
                            genericArgs.Add(asm.MainModule.ImportReference(specializationType));
                        }

                        disposeFnRef =
                            asm.MainModule.ImportReference(disposeFnDef.MakeHostInstanceGeneric(genericArgs.ToArray()));
                    }

                    if (disposeFnRef == null)
                        throw new Exception(
                            $"{jobTypeDef.Name}::{field.Name} is missing a {field.FieldType.Name}::Dispose() implementation");

                    bc.Add(Instruction.Create(OpCodes.Call, disposeFnRef));
                }
            }
        }

        void GenWrapperDisposeIL(
            MethodDefinition method,
            AssemblyDefinition asm,
            TypeDefinition jobTypeDef,
            TypeReference wrapperTypeRef)
        {
            var bc = method.Body.Instructions;
            TypeReference closedWrapperTypeRef;
            TypeReference jobTypeRef = jobTypeDef;
            if (jobTypeDef.HasGenericParameters)
            {
                var genericJobTypeDef = new GenericInstanceType(jobTypeDef);
                foreach (var gp in jobTypeDef.GenericParameters)
                {
                    genericJobTypeDef.GenericArguments.Add(asm.MainModule.ImportReference(gp));
                }
                jobTypeRef = asm.MainModule.ImportReference(genericJobTypeDef);
                closedWrapperTypeRef = TypeRegGen.MakeGenericTypeSpecialization(asm.MainModule.ImportReference(wrapperTypeRef), jobTypeRef);
            }
            else
            {
                closedWrapperTypeRef = TypeRegGen.MakeGenericTypeSpecialization(asm.MainModule.ImportReference(wrapperTypeRef), jobTypeRef);
            }

            ParameterDefinition ptrParam = method.Parameters[0];

            bool first = true;
            var wrapperVar = new VariableDefinition(asm.MainModule.ImportReference(closedWrapperTypeRef));

            foreach (FieldDefinition field in wrapperTypeRef.Resolve().Fields)
            {
                if (first)
                {
                    first = false;

                    method.Body.Variables.Add(wrapperVar);

                    // There are cases where a null ptr gets passed in - all IJobForEach for example. Check and return!

                    var target = Instruction.Create(OpCodes.Nop);
                    var branch = Instruction.Create(OpCodes.Brfalse, target);

                    bc.Add(Instruction.Create(OpCodes.Ldarg, ptrParam));
                    bc.Add(Instruction.Create(OpCodes.Ldc_I4_0));
                    bc.Add(Instruction.Create(OpCodes.Conv_U));
                    bc.Add(Instruction.Create(OpCodes.Ceq));

                    bc.Add(branch);
                    bc.Add(Instruction.Create(OpCodes.Ret));
                    bc.Add(target);

                    // ICustomJobExtensions.CustomJobData<CustomJob1> output = *ptr;
                    bc.Add(Instruction.Create(OpCodes.Ldarg, ptrParam));
                    bc.Add(Instruction.Create(OpCodes.Ldobj, asm.MainModule.ImportReference(closedWrapperTypeRef)));
                    bc.Add(Instruction.Create(OpCodes.Stloc, wrapperVar));
                }

                var deallocateOnJobCompletionAttr = field.CustomAttributes.FirstOrDefault(ca =>
                    ca.Constructor.DeclaringType.Name == "DeallocateOnJobCompletionAttribute");
                if (deallocateOnJobCompletionAttr != null)
                {
                    var supportsAttribute = field.FieldType.Resolve().CustomAttributes.FirstOrDefault(ca =>
                        ca.Constructor.DeclaringType.Name == "NativeContainerSupportsDeallocateOnJobCompletionAttribute");
                    if (supportsAttribute == null)
                        throw new ArgumentException(
                            $"DeallocateOnJobCompletion for {field.FullName} is invalid without NativeContainerSupportsDeallocateOnJobCompletion on {field.FieldType.FullName}");

                    bc.Add(Instruction.Create(OpCodes.Ldloca, wrapperVar));

                    FieldReference closedField = TypeRegGen.MakeGenericFieldSpecialization(field, jobTypeRef);
                    bc.Add(Instruction.Create(OpCodes.Ldflda, asm.MainModule.ImportReference(closedField, jobTypeRef)));

                    MethodDefinition disposeFnDef = FindDispose(field.FieldType.Resolve());
                    disposeFnDef.IsPublic = true;
                    var disposeFnRef = asm.MainModule.ImportReference(disposeFnDef);
                    if (field.FieldType is GenericInstanceType)
                    {
                        GenericInstanceType git = (GenericInstanceType)field.FieldType;
                        List<TypeReference> genericArgs = new List<TypeReference>();
                        foreach (var specializationType in git.GenericArguments)
                        {
                            genericArgs.Add(asm.MainModule.ImportReference(specializationType));
                        }

                        disposeFnRef =
                            asm.MainModule.ImportReference(disposeFnDef.MakeHostInstanceGeneric(genericArgs.ToArray()));
                    }

                    if (disposeFnRef == null)
                        throw new Exception(
                            $"{jobTypeRef.Name}::{field.Name} is missing a {field.FieldType.Name}::Dispose() implementation");

                    bc.Add(Instruction.Create(OpCodes.Call, asm.MainModule.ImportReference(disposeFnRef)));
                }
            }
        }

        MethodDefinition GenCleanupMethod(
            AssemblyDefinition asm,
            TypeDefinition jobTypeDef,
            JobDesc jobDesc)
        {
            var method = new MethodDefinition(CleanupJobFn,
                MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.NewSlot |
                MethodAttributes.Virtual,
                asm.MainModule.ImportReference(typeof(void)));

            method.Parameters.Add(new ParameterDefinition("ptr", ParameterAttributes.None, asm.MainModule.ImportReference(typeof(void*))));

            method.Body.InitLocals = true;
            var bc = method.Body.Instructions;

            GenDisposeIL(method, asm, jobTypeDef);
            if (jobDesc != null && jobDesc.JobDataField != null)
                GenWrapperDisposeIL(method, asm, jobTypeDef, jobDesc.JobDataField.DeclaringType);

            bc.Add(Instruction.Create(OpCodes.Ret));
            return method;
        }

        void WalkFieldsRec(TypeDefinition type, List<List<FieldReference>> paths, Func<FieldDefinition, bool> match)
        {
            if (type == null || !type.IsStructValueType())
                return;

            foreach (FieldDefinition f in type.Fields)
            {
                if (f.IsStatic || f.FieldType.IsPointer)
                    continue;

                paths[paths.Count - 1].Add(f);
                var fType = f.FieldType.Resolve();
                if (fType != null)
                {
                    if (match(f))
                    {
                        var endPath = paths[paths.Count - 1];

                        // Duplicate the stack:
                        paths.Add(new List<FieldReference>(endPath));
                    }

                    WalkFieldsRec(fType, paths, match);
                }

                var lastPath = paths[paths.Count - 1];
                lastPath.RemoveAt(lastPath.Count - 1);
            }
        }

        List<List<FieldReference>> WalkFields(TypeDefinition type, Func<FieldDefinition, bool> match)
        {
            List<List<FieldReference>> paths = new List<List<FieldReference>>();
            paths.Add(new List<FieldReference>());
            WalkFieldsRec(type, paths, match);
            if (paths[paths.Count - 1].Count == 0)
                paths.RemoveAt(paths.Count - 1);
            return paths;
        }

        FieldReference SpecializeFieldIfPossible(ModuleDefinition module, FieldReference target, TypeReference srcGenerics)
        {
            if (srcGenerics is GenericInstanceType)
            {
                GenericInstanceType git = (GenericInstanceType)srcGenerics;
                List<TypeReference> genericArgs = new List<TypeReference>();
                foreach (TypeReference specializationType in git.GenericArguments)
                {
                    var imp = module.ImportReference(specializationType);
                    genericArgs.Add(imp);
                }

                var closed = TypeRegGen.MakeGenericFieldSpecialization(module.ImportReference(target), genericArgs.ToArray());
                return closed;
            }
            else
            {
                return module.ImportReference(target);
            }
        }

        void AddThreadIndexIL(
            MethodDefinition method,
            AssemblyDefinition asm,
            TypeDefinition jobTypeDef)
        {
            var bc = method.Body.Instructions;
            var paramJobIndex = method.Parameters[0];


            List<List<FieldReference>> paths = WalkFields(jobTypeDef,
                fd =>
                    fd.HasCustomAttributes && fd.CustomAttributes.FirstOrDefault(a =>
                        a.AttributeType.FullName ==
                        "Unity.Collections.LowLevel.Unsafe.NativeSetThreadIndexAttribute") != null);

            if (jobTypeDef.HasGenericParameters && paths.Count > 0)
            {
                throw new ArgumentException($"The job {jobTypeDef.Name} is generic and uses a ThreadIndex; this case cannot be processed.");
            }

            // C# re-orders structures that contain a reference to a class. Since the DisposeSentinel
            // is in almost every job, every job gets re-ordered. So the normal list returned by GetFieldOffsetsOf
            // can't account for re-ordering (since we don't even know what the re-order *is*.) Therefore we
            // need a bunch of ldflda to find them.

            foreach (var path in paths)
            {
                if (path.Count == 0)
                    continue;

                FieldReference targetTR = path[path.Count - 1];
                FieldDefinition targetTD = targetTR.Resolve();
                targetTD.IsPublic = true;

                // This code has several problems, for nearish term fixing.
                // Constraint:
                //    Because the DisposeSentinel is a class, C# will reorder
                //    the struct. This is painful, else we could just use the
                //    list of addresses from GetFieldOffsetsOf.
                // 1. The field walk (contained in the path) is pretty complex and should
                //    be wrapped up into a function better.
                // 2. Said function to wrap that up needs independent tests.
                // 3. Once the generics are found they are propegated to the inner
                //    type. This only works on the simplest case. Container<A, B, C>
                //    might contain ParallelWriter<C, B> which would be totally broken in
                //    this scheme. This code is assuming: Container<A, B> has ParallelWriter<A, B>
                //
                // https://unity3d.atlassian.net/browse/DOTSR-353

                TypeReference genericsSrc = null;
                int srcIndex = 0;
                for (int pi = path.Count - 2; pi >= 0; pi--)
                {
                    if (!path[pi].ContainsGenericParameter)
                    {
                        genericsSrc = path[pi].FieldType;
                        srcIndex = pi;
                        break;
                    }
                }

                bc.Add(Instruction.Create(OpCodes.Ldarg_0));
                for (int i = 0; i < path.Count - 1; ++i)
                {
                    FieldReference fr = path[i];
                    FieldDefinition fd = fr.Resolve();
                    fd.IsPublic = true;
                    if (i > srcIndex)
                        bc.Add(Instruction.Create(OpCodes.Ldflda, SpecializeFieldIfPossible(asm.MainModule, fr, genericsSrc)));
                    else
                        bc.Add(Instruction.Create(OpCodes.Ldflda, asm.MainModule.ImportReference(fr)));
                }

                bc.Add(Instruction.Create(OpCodes.Ldarg, paramJobIndex));
                bc.Add(Instruction.Create(OpCodes.Stfld, SpecializeFieldIfPossible(asm.MainModule, targetTR, genericsSrc)));
            }
        }

        void AddSafetyIL(
            MethodDefinition method,
            AssemblyDefinition asm,
            TypeDefinition jobTypeDef)
        {
            // TODO deal with the generics case.
            // Currently this generates bad IL; much better to not have the safety checks.
            if (!jobTypeDef.HasFields || !jobTypeDef.IsValueType || jobTypeDef.HasGenericParameters)
                return;

            method.Body.InitLocals = true;
            var bc = method.Body.Instructions;

            // TODO this implementation (or at least the call to SetAllowXYZ) isn't correct in MT mode.
            var setAllowWriteOnlyFnDef = m_AtomicDef.Methods.First(i => i.Name == "SetAllowWriteOnly");
            var setAllowReadOnlyFnDef = m_AtomicDef.Methods.First(i => i.Name == "SetAllowReadOnly");

            TypeUtils.IterateFieldsRecurse(
                (FieldReference field, TypeReference fieldType) =>
                {
                    if (field == null || fieldType == null)
                        return;

                    if (fieldType.IsValueType && !fieldType.IsPrimitive)
                    {
                        TypeDefinition td = fieldType.Resolve();
                        FieldDefinition fd = field.Resolve();

                        bool writeOnly = false;
                        bool readOnly = false;

                        if (td.HasCustomAttributes &&
                            td.CustomAttributes.FirstOrDefault(a =>
                                a.AttributeType.FullName ==
                                "Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute") != null
                            && td.CustomAttributes.FirstOrDefault(a =>
                                a.AttributeType.FullName ==
                                "Unity.Collections.NativeContainerIsAtomicWriteOnlyAttribute") != null)
                        {
                            writeOnly = true;
                        }

                        if (fd.HasCustomAttributes &&
                            fd.CustomAttributes.FirstOrDefault(a =>
                                a.AttributeType.FullName == "Unity.Collections.WriteOnlyAttribute") != null)
                        {
                            writeOnly = true;
                        }

                        if (fd.HasCustomAttributes && fd.CustomAttributes.FirstOrDefault(a =>
                            a.AttributeType.FullName == "Unity.Collections.ReadOnlyAttribute") != null)
                        {
                            readOnly = true;
                        }

                        if (writeOnly && readOnly)
                        {
                            throw new ArgumentException(
                                $"[ReadOnly] and [WriteOnly] are both specified on '{fd.FullName}'");
                        }

                        if (readOnly || writeOnly)
                        {
                            // No recursion here - if there are sub-fields which may use atomic safety handles, these
                            // will be treated separately.
                            foreach (var subField in td.Fields)
                            {
                                if (subField.FieldType.FullName == "Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle")
                                {
                                    subField.IsPublic = true;

                                    // AtomicSafetyHandle.SetAllowWriteOnly(result.m_Safety);
                                    // or
                                    // AtomicSafetyHandle.SetAllowReadOnly(result.m_Safety);

                                    bc.Add(Instruction.Create(OpCodes.Ldarg_0));
                                    bc.Add(Instruction.Create(OpCodes.Ldflda,
                                        asm.MainModule.ImportReference(fd)));

                                    var specialSubField = new FieldReference(subField.Name,
                                        asm.MainModule.ImportReference(subField.FieldType),
                                        fd.FieldType);
                                    var importedSpecialSubField = asm.MainModule.ImportReference(specialSubField);
                                    bc.Add(Instruction.Create(OpCodes.Ldflda, importedSpecialSubField));

                                    if (writeOnly)
                                        bc.Add(Instruction.Create(OpCodes.Call,
                                            asm.MainModule.ImportReference(setAllowWriteOnlyFnDef)));
                                    if (readOnly)
                                        bc.Add(Instruction.Create(OpCodes.Call,
                                            asm.MainModule.ImportReference(setAllowReadOnlyFnDef)));
                                }
                            }
                        }
                    }
                }
                , jobTypeDef);
        }
    }
}
