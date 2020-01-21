using System;
using System.Diagnostics;
using Unity.Entities;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Tiny.iOS
{
    public class iOSWindowSystem : WindowSystem
    {
        private static iOSWindowSystem sWindowSystem;
        public iOSWindowSystem()
        {
            initialized = false;
            sWindowSystem = this;
        }

        public override IntPtr GetPlatformWindowHandle()
        {
            unsafe {
                return (IntPtr)iOSNativeCalls.getNativeWindow();
            }
        }

        /*TODO how we can inform RunLoop about system events pause/resume/destroy?
        private static Action<int> onPauseM;

        [MonoPInvokeCallbackAttribute]
        static void ManagedOnPauseCallback(int pause)
        {
            onPauseM(pause);
        }

        private static Action onDestroyM;

        [MonoPInvokeCallbackAttribute]
        static void ManagedOnDestroyCallback()
        {
            onDestroyM();
        }

        internal class MonoPInvokeCallbackAttribute : Attribute
        {
        }

        public override void InfiniteMainLoop(MainLoopDelegate m)
        {
            staticM = m;
            IOSNativeCalls.set_animation_frame_callback(Marshal.GetFunctionPointerForDelegate((MainLoopDelegate)ManagedRAFCallback));
        }

        public void SetOnPauseCallback(Action<int> m)
        {
            onPauseM = m;
            IOSNativeCalls.set_pause_callback(Marshal.GetFunctionPointerForDelegate((Action<int>)ManagedOnPauseCallback));
        }

        public void SetOnDestroyCallback(Action m)
        {
            onDestroyM = m;
            IOSNativeCalls.set_destroy_callback(Marshal.GetFunctionPointerForDelegate((Action)ManagedOnDestroyCallback));
        }
        */

        public override void DebugReadbackImage(out int w, out int h, out NativeArray<byte> pixels)
        {
            throw new InvalidOperationException("Can no longer read-back from window use BGFX instead.");
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            // setup window
            Console.WriteLine("IOS Window init.");

            var env = World.GetExistingSystem<TinyEnvironment>();
            var config = env.GetConfigData<DisplayInfo>();

            try
            {
                initialized = iOSNativeCalls.init();
            } catch
            {
                Console.WriteLine("  Exception during initialization.");
                initialized = false;
            }
            if (!initialized)
            {
                Console.WriteLine("  Failed.");
                World.QuitUpdate = true;
                return;
            }
            int winw = 0, winh = 0;
            iOSNativeCalls.getWindowSize(ref winw, ref winh);
            config.focused = true;
            config.visible = true;
            config.orientation = winw >= winh ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;
            config.frameWidth = winw;
            config.frameHeight = winh;
            int sw = 0, sh = 0;
            iOSNativeCalls.getScreenSize(ref sw, ref sh);
            config.screenWidth = sw;
            config.screenHeight = sh;
            config.width = winw;
            config.height = winh;
            int fbw = 0, fbh = 0;
            iOSNativeCalls.getFramebufferSize(ref fbw, ref fbh);
            config.framebufferWidth = fbw;
            config.framebufferHeight = fbh;
            env.SetConfigData(config);

            frameTime = iOSNativeCalls.time();
        }

        protected override void OnDestroy()
        {
            // close window
            if (initialized)
            {
                Console.WriteLine("iOS Window shutdown.");
                iOSNativeCalls.shutdown(0);
                initialized = false;
            }
        }

        protected override void OnUpdate()
        {
            if (!initialized)
                return;

            var env = World.GetExistingSystem<TinyEnvironment>();
            var config = env.GetConfigData<DisplayInfo>();
            int winw = 0, winh = 0;
            iOSNativeCalls.getWindowSize(ref winw, ref winh);
            if (winw != config.width || winh != config.height)
            {
                if (config.autoSizeToFrame)
                {
                    Console.WriteLine("IOS Window update size.");
                    config.orientation = winw >= winh ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;
                    config.width = winw;
                    config.height = winh;
                    config.frameWidth = winw;
                    config.frameHeight = winh;
                    int fbw = 0, fbh = 0;
                    iOSNativeCalls.getFramebufferSize(ref fbw, ref fbh);
                    config.framebufferWidth = fbw;
                    config.framebufferHeight = fbh;
                    env.SetConfigData(config);
                }
                else
                {
                    iOSNativeCalls.resize(config.width, config.height);
                }
            }
            if (!iOSNativeCalls.messagePump())
            {
                Console.WriteLine("iOS message pump exit.");
                iOSNativeCalls.shutdown(1);
                World.QuitUpdate = true;
                initialized = false;
                return;
            }
            double newFrameTime = iOSNativeCalls.time();
            var timeData = env.StepWallRealtimeFrame(newFrameTime - frameTime);
            World.SetTime(timeData);
            frameTime = newFrameTime;
        }

        private bool initialized;
        private double frameTime;
    }

    public static class iOSNativeCalls
    {
        [DllImport("lib_unity_tiny_ios", EntryPoint = "init_ios")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool init();

        [DllImport("lib_unity_tiny_ios", EntryPoint = "getWindowSize_ios")]
        public static extern void getWindowSize(ref int w, ref int h);

        [DllImport("lib_unity_tiny_ios", EntryPoint = "getScreenSize_ios")]
        public static extern void getScreenSize(ref int w, ref int h);

        [DllImport("lib_unity_tiny_ios", EntryPoint = "getFramebufferSize_ios")]
        public static extern void getFramebufferSize(ref int w, ref int h);

        [DllImport("lib_unity_tiny_ios", EntryPoint = "getWindowFrameSize_ios")]
        public static extern void getWindowFrameSize(ref int left, ref int top, ref int right, ref int bottom);

        [DllImport("lib_unity_tiny_ios", EntryPoint = "shutdown_ios")]
        public static extern void shutdown(int exitCode);

        [DllImport("lib_unity_tiny_ios", EntryPoint = "resize_ios")]
        public static extern void resize(int width, int height);

        [DllImport("lib_unity_tiny_ios", EntryPoint = "messagePump_ios")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool messagePump();

        [DllImport("lib_unity_tiny_ios", EntryPoint = "time_ios")]
        public static extern double time();

        [DllImport("lib_unity_tiny_ios", EntryPoint = "pausecallbacksinit_ios")]
        public static extern bool set_pause_callback(IntPtr func);

        [DllImport("lib_unity_tiny_ios", EntryPoint = "destroycallbacksinit_ios")]
        public static extern bool set_destroy_callback(IntPtr func);

        [DllImport("lib_unity_tiny_ios", EntryPoint = "get_touch_info_stream_ios")]
        public static extern unsafe int * getTouchInfoStream(ref int len);

        [DllImport("lib_unity_tiny_ios", EntryPoint = "get_native_window_ios")]
        public static extern unsafe void * getNativeWindow();

        [DllImport("lib_unity_tiny_ios", EntryPoint = "reset_ios_input")]
        public static extern void resetStreams();
    }

}

