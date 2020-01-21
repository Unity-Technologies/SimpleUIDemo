using System.Runtime.InteropServices;
using Unity.Burst;
using UnityEngine;

namespace Unity.Collections
{
    public enum NativeLeakDetectionMode
    {
        Enabled = 0,
        Disabled = 1,
        EnabledWithStackTrace = 3,
    }

    public static class NativeLeakDetection
    {
        // For performance reasons no assignment operator (static initializer cost in il2cpp)
        // and flipped enabled / disabled enum value
        static int s_NativeLeakDetectionMode;

        public static NativeLeakDetectionMode Mode {
            get {
                return (NativeLeakDetectionMode)s_NativeLeakDetectionMode;
            }
            set {
                s_NativeLeakDetectionMode = (int)value;
            }
        }
    }
}


#if ENABLE_UNITY_COLLECTIONS_CHECKS
namespace Unity.Collections.LowLevel.Unsafe
{
    // DisposeSentinel is wrapped here to prevent the managed DisposeSentinelInner type from relocating inside any type with a DisposeSentinel field.
    // In mono, relocation of managed fields in a type does not occur but in Net Core this does, and can cause managed and unmanaged type layouts to 
    // differ resulting in memory corruption issues when using these types in Burst Jobs.
    [StructLayout(LayoutKind.Sequential)]
    public struct DisposeSentinel
    {
        internal DisposeSentinelInternal m_DisposeSentinel;

        public static void Dispose(ref AtomicSafetyHandle safety, ref DisposeSentinel sentinel) => DisposeSentinelInternal.Dispose(ref safety, ref sentinel);
        public static void Create(out AtomicSafetyHandle safety, out DisposeSentinel sentinel, int callSiteStackDepth, Allocator allocator) => DisposeSentinelInternal.Create(out safety, out sentinel, callSiteStackDepth, allocator);
        [Unity.Burst.BurstDiscard]
        public static void Clear(ref DisposeSentinel sentinel) => DisposeSentinelInternal.Clear(ref sentinel);

        // Note: These overloads are simply here to swallow any existing code paths where a DisposeSentinel var was being set to null.
        // Now that we wrap the original managed DisposeSentinel with a struct, we would break these code paths, but instead we
        // construct the null as if it were a string and just set the inner value to null instead to workaround this compatibility issue
        public DisposeSentinel(string o) {m_DisposeSentinel = null; }
        public static implicit operator DisposeSentinel(string o) { return new DisposeSentinel { m_DisposeSentinel = null }; }
        public static bool operator ==(DisposeSentinel ds1, DisposeSentinel ds2) { return ds1.m_DisposeSentinel == ds2.m_DisposeSentinel; }
        public static bool operator !=(DisposeSentinel ds1, DisposeSentinel ds2) { return ds1.m_DisposeSentinel != ds2.m_DisposeSentinel; }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class DisposeSentinelInternal
    {
        int        m_IsCreated;
        string     m_Stack;

        private DisposeSentinelInternal()
        {
        }

        public static void Dispose(ref AtomicSafetyHandle safety, ref DisposeSentinel sentinel)
        {
            AtomicSafetyHandle.CheckDeallocateAndThrow(safety);
            // If the safety handle is for a temp allocation, create a new safety handle for this instance which can be marked as invalid
            // Setting it to new AtomicSafetyHandle is not enough since the handle needs a valid node pointer in order to give the correct errors
            if (AtomicSafetyHandle.IsTempMemoryHandle(safety))
                safety = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.Release(safety);
            Clear(ref sentinel);
        }

        [BurstDiscard]
        private static string CaptureStack()
        {
            if (NativeLeakDetection.Mode == NativeLeakDetectionMode.EnabledWithStackTrace)
                return System.Environment.StackTrace;
            return "Set NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace to enable traces.";
        }

        public static void Create(out AtomicSafetyHandle safety, out DisposeSentinel sentinel, int callSiteStackDepth, Allocator allocator)
        {
            safety = (allocator == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();

            CreateInternal(allocator, out sentinel);

        }

        [BurstDiscard]
        private static void CreateInternal(Allocator allocator, out DisposeSentinel sentinel)
        {
            if (NativeLeakDetection.Mode != NativeLeakDetectionMode.Disabled && allocator != Allocator.Temp)
            {
                if (Unity.Jobs.LowLevel.Unsafe.JobsUtility.IsExecutingJob())
                    throw new System.InvalidOperationException("Jobs can only create Temp memory");

                sentinel = new DisposeSentinel
                {
                    m_DisposeSentinel = new DisposeSentinelInternal
                    {
                        m_IsCreated = 1,
                        m_Stack = CaptureStack()
                    }
                };
            }
            else
            {
                sentinel.m_DisposeSentinel = null;
            }
        }

        ~DisposeSentinelInternal()
        {
            if (m_IsCreated != 0)
            {
                Debug.Log("A Native Collection has not been disposed, resulting in a memory leak. Trace:");
                Debug.LogError(m_Stack);
            }
        }

        [Unity.Burst.BurstDiscard]
        public static void Clear(ref DisposeSentinel sentinel)
        {
            if (sentinel.m_DisposeSentinel != null)
            {
                sentinel.m_DisposeSentinel.m_IsCreated = 0;
                sentinel.m_DisposeSentinel = null;
            }
        }
    }
}
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
