using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Unity.Jobs.LowLevel.Unsafe
{
    // Code gen depends on these constants.
    public enum JobType
    {
        Single,
        ParallelFor
    }

    // NOTE: This doesn't match (big) Unity's JobRanges because JobsUtility.GetWorkStealingRange isn't fully implemented
    public struct JobRanges
    {
        public int ArrayLength;
        public int IndicesPerPhase;
    }

    // The internally used header for JobData
    // The code-gen relies on explicit layout.
    // Trivial with one entry, but declared here as a reminder.
    // Also, to preserve alignment, code-gen is using a size=16
    // for this structure.
    [StructLayout(LayoutKind.Explicit)]
    struct JobMetaData
    {
        [FieldOffset(0)]
        public JobRanges JobRanges;

        // Make structure 16-byte aligned.
        [FieldOffset(8)]
        long _padding;
    }

    public enum ScheduleMode
    {
        Run,
        Batched
    }

    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class JobProducerTypeAttribute : Attribute
    {
        public JobProducerTypeAttribute(Type producerType) => throw new NotImplementedException();
        public Type ProducerType => throw new NotImplementedException();
    }

    public static class JobsUtility
    {
#if UNITY_SINGLETHREADED_JOBS
        public static int JobWorkerCount => 0;
#else
        public static int JobWorkerCount => Environment.ProcessorCount;
#endif
        public const int MaxJobThreadCount = 128;
        public const int CacheLineSize = 64;

        public static bool JobCompilerEnabled => false;
        public static bool JobDebuggerEnabled => false;

        [StructLayout(LayoutKind.Sequential)]
        public struct JobScheduleParameters
        {
            public JobHandle    Dependency;
            public int          ScheduleMode;
            public IntPtr       ReflectionData;
            public IntPtr       JobDataPtr;

            public unsafe JobScheduleParameters(void* jobData, IntPtr reflectionData, JobHandle jobDependency,
                ScheduleMode scheduleMode, int jobDataSize = 0, int schedule = 3)
            {
                // Default is 0; code-gen should set to a correct size.
                if (jobDataSize == 0)
                    throw new InvalidOperationException("JobScheduleParameters (size) should be set by code gen.");
                // Default is 1; however, the function created by code gen will always return 2.
                if (schedule != 2)
                    throw new InvalidOperationException(
                        "JobScheduleParamaters (rc of ScheduleJob_Gen) should be set by code gen.");

                Assert.IsTrue(sizeof(JobMetaData) % 16 == 0);
                int headerSize = sizeof(JobMetaData);
                int size = headerSize + jobDataSize;

                void* mem = UnsafeUtility.Malloc(size, 16, Allocator.TempJob);
                UnsafeUtility.MemClear(mem, size);
                UnsafeUtility.MemCpy(((byte*)mem + headerSize), jobData, jobDataSize);
                UnsafeUtility.AssertHeap(mem);

                Dependency = jobDependency;
                JobDataPtr = (IntPtr) mem;
                ReflectionData = reflectionData;
                ScheduleMode = (int) scheduleMode;
            }
        }

        class ReflectionDataStore
        {
            // Dotnet throws an exception if the function pointers aren't pinned by a delegate.
            // Error checking? The pointers certainly can't change.
            // This class registers the function pointers with the GC.
            // TODO a more elegant solution, or switch to calli and avoid this.
            public ReflectionDataStore(Delegate delegate0, Delegate staticDelegate1, Delegate staticDelegate2)
            {
                Delegate0 = delegate0;
                Delegate0Handle = GCHandle.Alloc(Delegate0);

                if (staticDelegate1 != null)
                {
                    Delegate1 = staticDelegate1;
                    Delegate1Handle = GCHandle.Alloc(Delegate1);
                    Delegate1FunctionPtr = Marshal.GetFunctionPointerForDelegate(staticDelegate1);
                }

                if (staticDelegate2 != null)
                {
                    Delegate2 = staticDelegate2;
                    Delegate2Handle = GCHandle.Alloc(Delegate2);
                    Delegate2FunctionPtr = Marshal.GetFunctionPointerForDelegate(staticDelegate2);
                }
            }

            internal ReflectionDataStore next;

            public Delegate Delegate0;
            public GCHandle Delegate0Handle;

            public Delegate Delegate1;
            public GCHandle Delegate1Handle;
            public IntPtr Delegate1FunctionPtr;

            public Delegate Delegate2;
            public GCHandle Delegate2Handle;
            public IntPtr Delegate2FunctionPtr;

        }

        [StructLayout(LayoutKind.Sequential)]
        struct ReflectionDataProxy
        {
            public JobType JobType;
            public IntPtr GenExecuteFunctionPtr;
            public IntPtr GenCleanupFunctionPtr;

        }

        public unsafe delegate void ManagedJobDelegate(void* ptr);
        public unsafe delegate void ManagedJobForEachDelegate(void* ptr, int jobIndex);

        static private ReflectionDataStore reflectionDataStoreRoot = null;

#if (UNITY_IOS)
        const string nativejobslib = "__Internal";
#else
        const string nativejobslib = "nativejobs";
#endif        

#if !UNITY_SINGLETHREADED_JOBS
        internal static IntPtr JobQueue
        {
            get
            {
                if (s_JobQueue == IntPtr.Zero)
                    s_JobQueue = CreateJobQueue("job-queue", "worker-bee", JobWorkerCount);

                return s_JobQueue;
            }
        }
        internal static IntPtr BatchScheduler
        {
            get
            {
                if (s_BatchScheduler == IntPtr.Zero)
                {
                    Assert.IsTrue(JobQueue != IntPtr.Zero);
                    s_BatchScheduler = CreateJobBatchScheduler();
                }

                return s_BatchScheduler;
            }
        }

        static IntPtr s_JobQueue;
        static IntPtr s_BatchScheduler;

        public static JobHandle ScheduleJob(IntPtr jobFuncPtr, IntPtr jobDataPtr, JobHandle dependsOn)
        {
            Assert.IsTrue(JobQueue != IntPtr.Zero);
            return ScheduleJobBatch(BatchScheduler, jobFuncPtr, jobDataPtr, dependsOn);
        }

        public static JobHandle ScheduleJobParallelFor(IntPtr jobFuncPtr, IntPtr jobCompletionFuncPtr, IntPtr jobDataPtr, int arrayLength, int innerloopBatchCount, JobHandle dependsOn)
        {
            Assert.IsTrue(JobQueue != IntPtr.Zero && BatchScheduler != IntPtr.Zero);
            return ScheduleJobBatchParallelFor(BatchScheduler, jobFuncPtr, jobDataPtr, arrayLength, innerloopBatchCount, jobCompletionFuncPtr, dependsOn);
        }

        // TODO: Need to find a good place to shut down jobs on application quit/exit
        public static void Shutdown()
        {
            if (s_BatchScheduler != IntPtr.Zero)
            {
                DestroyJobBatchScheduler(s_BatchScheduler);
                s_BatchScheduler = IntPtr.Zero;
            }

            if (s_JobQueue != IntPtr.Zero)
            {
                DestroyJobQueue();
                s_JobQueue = IntPtr.Zero;
            }
        }

        [DllImport(nativejobslib)]
        static extern IntPtr CreateJobQueue(string queueName, string workerName, int numJobWorkerThreads);

        [DllImport(nativejobslib)]
        static extern void DestroyJobQueue();

        [DllImport(nativejobslib)]
        static extern IntPtr CreateJobBatchScheduler();

        [DllImport(nativejobslib)]
        static extern void DestroyJobBatchScheduler(IntPtr scheduler);

        [DllImport(nativejobslib)]
        static extern JobHandle ScheduleJobBatch(IntPtr scheduler, IntPtr func, IntPtr userData, JobHandle dependency);

        [DllImport(nativejobslib)]
        static extern JobHandle ScheduleJobBatchParallelFor(IntPtr scheduler, IntPtr func, IntPtr userData, int arrayLength, int innerloopBatchCount, IntPtr completionFunc, JobHandle dependency);

        [DllImport(nativejobslib)]
        internal static extern void ScheduleMultiDependencyJob(ref JobHandle fence, IntPtr dispatch, IntPtr dependencies, int fenceCount);

        [DllImport(nativejobslib)]
        internal static extern void ScheduleBatchedJobs(IntPtr scheduler);

        [DllImport(nativejobslib)]
        internal static extern void Complete(IntPtr scheduler, ref JobHandle jobHandle);

        [DllImport(nativejobslib)]
        internal static extern int IsCompleted(IntPtr scheduler, ref JobHandle jobHandle);
#endif
        // The following are needed regardless if we are in single or multi-threaded environment
        [DllImport(nativejobslib, EntryPoint = "IsExecutingJob")]
        internal static extern int IsExecutingJobInternal();

        public static bool IsExecutingJob() { return IsExecutingJobInternal() != 0; }

        public static unsafe IntPtr CreateJobReflectionData(Type type, Type _, JobType jobType,
            Delegate executeDelegate,
            Delegate cleanupDelegate = null,
            ManagedJobForEachDelegate codegenExecuteDelegate = null,       // Note that the 2 param form is used for both Normal and ParallelFor
            ManagedJobDelegate codegenCleanupDelegate = null)
        {
            // Tiny doesn't use this on any codepath currently; may need future support for custom jobs.
            if (cleanupDelegate != null) throw new ArgumentException("Runtime needs support for cleanup delegates in jobs.");

            if (codegenExecuteDelegate == null) throw new CodegenShouldReplaceException("Code gen should have supplied an execute wrapper.");
            if (jobType == JobType.ParallelFor)
            {
                if (codegenCleanupDelegate == null) throw new CodegenShouldReplaceException("For ParallelFor jobs, code gen should have supplied a cleanup wrapper.");
            }

            var reflectionDataPtr = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ReflectionDataProxy>(),
                UnsafeUtility.AlignOf<ReflectionDataProxy>(), Allocator.Persistent);

            var reflectionData = new ReflectionDataProxy();
            reflectionData.JobType = jobType;

            // Protect against garbage collector relocating delegate
            ReflectionDataStore store = new ReflectionDataStore(executeDelegate, codegenCleanupDelegate, codegenExecuteDelegate);
            store.next = reflectionDataStoreRoot;
            reflectionDataStoreRoot = store;

            reflectionData.GenExecuteFunctionPtr = Marshal.GetFunctionPointerForDelegate(codegenExecuteDelegate);
            if (codegenCleanupDelegate != null)
                reflectionData.GenCleanupFunctionPtr = Marshal.GetFunctionPointerForDelegate(codegenCleanupDelegate);

            UnsafeUtility.CopyStructureToPtr(ref reflectionData, reflectionDataPtr);

            return new IntPtr(reflectionDataPtr);
        }

#if UNITY_SINGLETHREADED_JOBS
        public static int GetDefaultIndicesPerPhase(int arrayLength)
        {
            return Math.Max(arrayLength, 1);
        }
#else
        public static int GetDefaultIndicesPerPhase(int arrayLength)
        {
            return Math.Max((arrayLength + (JobWorkerCount - 1)) / JobWorkerCount, 1);
        }
#endif

        // TODO: Currently, the actual work stealing code sits in (big) Unity's native code w/ some dependencies
        //     For now, let's simply split the work for each thread over the number of job threads
        public static bool GetWorkStealingRange(ref JobRanges ranges, int jobIndex, out int begin, out int end)
        {
#if UNITY_SINGLETHREADED_JOBS
            // IndicesPerPhase is used as a "done" flag.
            bool done = ranges.IndicesPerPhase == 0;
            begin = 0;
            end = ranges.ArrayLength;
            ranges.IndicesPerPhase = 0;
            return !done;
#else
            begin = jobIndex * ranges.IndicesPerPhase;
            end = Math.Min(begin + ranges.IndicesPerPhase, ranges.ArrayLength);
            ranges.IndicesPerPhase = 0;
            return begin < end;
#endif
        }

        public static unsafe JobHandle ScheduleParallelFor(ref JobScheduleParameters parameters, int arrayLength, int innerloopBatchCount)
        {
            UnsafeUtility.AssertHeap(parameters.JobDataPtr.ToPointer());
            UnsafeUtility.AssertHeap(parameters.ReflectionData.ToPointer());
            ReflectionDataProxy jobReflectionData = UnsafeUtility.AsRef<ReflectionDataProxy>(parameters.ReflectionData.ToPointer());

            Assert.IsFalse(jobReflectionData.GenExecuteFunctionPtr.ToPointer() == null);
            Assert.IsFalse(jobReflectionData.GenCleanupFunctionPtr.ToPointer() == null);

            void* jobMetaPtr = parameters.JobDataPtr.ToPointer();
            JobMetaData jobMetaData = default;
            jobMetaData.JobRanges.ArrayLength = arrayLength;
            jobMetaData.JobRanges.IndicesPerPhase = GetDefaultIndicesPerPhase(arrayLength);
            UnsafeUtility.CopyStructureToPtr(ref jobMetaData, jobMetaPtr);

#if UNITY_SINGLETHREADED_JOBS
            // In the single threaded case, this is synchronous execution.
            UnsafeUtility.CallFunctionPtr_pi(jobReflectionData.GenExecuteFunctionPtr.ToPointer(), jobMetaPtr, 0);
            UnsafeUtility.CallFunctionPtr_p(jobReflectionData.GenCleanupFunctionPtr.ToPointer(), jobMetaPtr);

            // This checks that the generated code was actually called; the last responsibility of
            // the generated code is to clean up the memory. Unfortunately only works in single threaded mode,
            Assert.IsTrue(UnsafeUtility.GetLastFreePtr() == jobMetaPtr);
            return new JobHandle();
#else
            return ScheduleJobParallelFor(jobReflectionData.GenExecuteFunctionPtr, jobReflectionData.GenCleanupFunctionPtr,
                parameters.JobDataPtr, arrayLength, innerloopBatchCount, parameters.Dependency);
#endif
        }

        static unsafe int CountFromDeferredData(void* deferredCountData)
        {
            // The initial count (which is what tiny only uses) is the `int` past the first `void*`.
            int count = *((int*) ((byte*) deferredCountData + sizeof(void*)));
            return count;
        }

        public static unsafe JobHandle ScheduleParallelForDeferArraySize(ref JobScheduleParameters parameters,
            int innerloopBatchCount, void* getInternalListDataPtrUnchecked, void* atomicSafetyHandlePtr)
        {

            return ScheduleParallelFor(ref parameters, CountFromDeferredData(getInternalListDataPtrUnchecked), innerloopBatchCount);
        }

        public static unsafe JobHandle Schedule(ref JobScheduleParameters parameters)
        {
            // Heap memory must be passed to schedule, so that Cleanup can free() it.
            UnsafeUtility.AssertHeap(parameters.JobDataPtr.ToPointer());
            UnsafeUtility.AssertHeap(parameters.ReflectionData.ToPointer());
            ReflectionDataProxy jobReflectionData = UnsafeUtility.AsRef<ReflectionDataProxy>(parameters.ReflectionData.ToPointer());

            Assert.IsFalse(jobReflectionData.GenExecuteFunctionPtr.ToPointer() == null);
            Assert.IsTrue(jobReflectionData.GenCleanupFunctionPtr.ToPointer() == null);

            void* jobMetaPtr = parameters.JobDataPtr.ToPointer();

#if UNITY_SINGLETHREADED_JOBS
            // In the single threaded case, this is synchronous execution.
            UnsafeUtility.CallFunctionPtr_pi(jobReflectionData.GenExecuteFunctionPtr.ToPointer(), jobMetaPtr, 0);

            // This checks that the generated code was actually called; the last responsibility of
            // the generated code is to clean up the memory. Unfortunately only works in single threaded mode,
            Assert.IsTrue(UnsafeUtility.GetLastFreePtr() == jobMetaPtr);
            return new JobHandle();
#else
            return ScheduleJob(jobReflectionData.GenExecuteFunctionPtr, parameters.JobDataPtr, parameters.Dependency);
#endif
        }

        public static unsafe void PatchBufferMinMaxRanges(IntPtr bufferRangePatchData, void* jobdata, int startIndex,
            int rangeSize)
        {
            // TODO https://unity3d.atlassian.net/browse/DOTSR-282
        }
    }


    public static class JobHandleUnsafeUtility
    {
        public static unsafe JobHandle CombineDependencies(JobHandle* jobs, int count)
        {
#if UNITY_SINGLETHREADED_JOBS
            return default(JobHandle);
#else
            var fence = new JobHandle();
            JobsUtility.ScheduleMultiDependencyJob(ref fence, JobsUtility.BatchScheduler, new IntPtr(jobs), count);
            return fence;
#endif
        }
    }
}

