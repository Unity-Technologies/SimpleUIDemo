using System;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Tiny
{
    public abstract class WindowSystem : ComponentSystem
    {
        public abstract void DebugReadbackImage(out int w, out int h, out NativeArray<byte> pixels);
        public abstract IntPtr GetPlatformWindowHandle();
    }

}
