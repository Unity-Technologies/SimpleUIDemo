using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Tiny
{
    public static class DynamicBufferExtensions
    {
        public static unsafe string AsString(this DynamicBuffer<char> buffer)
        {
            return new string((char*)buffer.GetUnsafePtr(), 0, buffer.Length);
        }

        public static unsafe void FromString(this DynamicBuffer<char> buffer, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                buffer.Clear();
                return;
            }

            fixed (char* ptr = name)
            {
                var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<char>(ptr, name.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());
#endif
                buffer.CopyFrom(array);
            }
        }
    }
}
