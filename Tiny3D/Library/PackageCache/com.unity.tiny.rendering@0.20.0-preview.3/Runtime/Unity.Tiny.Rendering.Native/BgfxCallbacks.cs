using System;
using System.Runtime.InteropServices;

namespace Bgfx
{
    public static partial class bgfx
    {
        public enum CallbackType : int
        {
            Fatal = 0,
            Trace = 1,
            ProfilerBegin = 2,
            ProfilerBeginLiteral = 3,
            ProfilerEnd = 4,
            ScreenShot = 5,
            ScreenShotFilename = 6,
            ScreenShotDesc = 7
        }

        public struct CallbackEntry
        {
            public ulong time;
            public CallbackType callbacktype;
            public int additionalAllocatedDataStart;
            public int additionalAllocatedDataSize;
        }

        public struct ScreenShotDesc
        {
            public int width;
            public int height;
            public int pitch;
            public int size;
            public int yflip;
        }

        [DllImport("lib_unity_tiny_rendering_native.dll", EntryPoint = "BGFXCB_Init", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe IntPtr CallbacksInit();

        [DllImport("lib_unity_tiny_rendering_native.dll", EntryPoint = "BGFXCB_DeInit", CallingConvention = CallingConvention.StdCall)]
        public static extern void CallbacksDeInit();

        [DllImport("lib_unity_tiny_rendering_native.dll", EntryPoint = "BGFXCB_Lock", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe int CallbacksLock(byte** destMem, CallbackEntry** destLog);

        [DllImport("lib_unity_tiny_rendering_native.dll", EntryPoint = "BGFXCB_UnlockAndClear", CallingConvention = CallingConvention.StdCall)]
        public static extern void CallbacksUnlockAndClear();
    }
}
