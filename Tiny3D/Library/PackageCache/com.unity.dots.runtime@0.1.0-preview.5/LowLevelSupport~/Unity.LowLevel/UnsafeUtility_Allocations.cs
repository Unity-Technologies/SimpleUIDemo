using System;
using System.Runtime.InteropServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public partial class UnsafeUtility
    {
        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_malloc")]
        public static extern unsafe void* Malloc(long totalSize, int alignOf, Allocator allocator);

        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_memcpy")]
        public static extern unsafe void MemCpy(void* dst, void* src, long n);

        // Debugging. Checks the heap guards on the requested memory.
        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_assertheap")]
        public static extern unsafe void AssertHeap(void* dst);

        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_free")]
        public static extern unsafe void Free(void* mBuffer, Allocator mAllocatorLabel);

        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_memset")]
        public static extern unsafe void MemSet(void* destination, byte value, long size);

        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_memclear")]
        public static extern unsafe void MemClear(void* mBuffer, long lize);

        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_memcpystride")]
        public static extern unsafe void MemCpyStride(void* destination, int destinationStride, void* source, int sourceStride, int elementSize, long count);

        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_memcmp")]
        public static extern unsafe int MemCmp(void* ptr1, void* ptr2, long size);

        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_memcpyreplicate")]
        public static extern unsafe void MemCpyReplicate(void* destination, void* source, int size, int count);

        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_memmove")]
        public static extern unsafe void MemMove(void* destination, void* source, long size);

        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_freetemp")]
        public static extern unsafe void FreeTempMemory();

        // TODO This will be deleted and deprecated with the changes that allow NativeJobs to run in single-threaded mode.
        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_call_p")]
        public static extern unsafe void CallFunctionPtr_p(void* fnc, void* data);

        // TODO This will be deleted and deprecated with the changes that allow NativeJobs to run in single-threaded mode.
        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_call_pi")]
        public static extern unsafe void CallFunctionPtr_pi(void* fnc, void* data, int param0);

        // Debugging / testing. Useful to check if the memory deletion that was expected actually did happen.
        // Only reliable & useful when running or testing single threaded.
        [DllImport("lib_unity_lowlevel", EntryPoint = "unsafeutility_get_last_free_ptr")]
        public static extern unsafe void* GetLastFreePtr();
        
        public static bool IsValidAllocator(Allocator allocator) { return allocator > Allocator.None; }
    }
}
