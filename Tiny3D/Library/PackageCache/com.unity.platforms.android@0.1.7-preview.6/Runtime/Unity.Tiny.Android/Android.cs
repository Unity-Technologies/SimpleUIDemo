using System;
using System.Diagnostics;
using Unity.Entities;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Tiny.Rendering;

namespace Unity.Tiny.Android
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class AndroidWindowSystem : WindowSystem
    {
        private static AndroidWindowSystem sWindowSystem;
        public AndroidWindowSystem()
        {
            initialized = false;
            sWindowSystem = this;
        }

        public override IntPtr GetPlatformWindowHandle()
        {
            return (IntPtr)AndroidNativeCalls.getNativeWindow();
        }

        internal class MonoPInvokeCallbackAttribute : Attribute
        {
        }

        public delegate void OnPauseDelegate(int pause);

        [MonoPInvokeCallbackAttribute]
        static void ManagedOnPauseCallback(int pause)
        {
            var renderSystem = sWindowSystem.World.GetExistingSystem<RendererBGFXSystem>();
            if (renderSystem != null)
            {
                renderSystem.Pause(pause != 0);
            }
        }

        public void SetOnPauseCallback()
        {
            AndroidNativeCalls.set_pause_callback(Marshal.GetFunctionPointerForDelegate((OnPauseDelegate)ManagedOnPauseCallback));
        }

        /*TODO how we can inform RunLoop about system events pause/resume/destroy?
        private static OnPauseDelegate onPauseM;

        [MonoPInvokeCallbackAttribute]
        static void ManagedOnPauseCallback(int pause)
        {
            onPauseM(pause);
        }

        public void SetOnPauseCallback(OnPauseDelegate m)
        {
            onPauseM = m;
            AndroidNativeCalls.set_pause_callback(Marshal.GetFunctionPointerForDelegate((OnPauseDelegate)ManagedOnPauseCallback));
        }

        private static Action onDestroyM;

        [MonoPInvokeCallbackAttribute]
        static void ManagedOnDestroyCallback()
        {
            onDestroyM();
        }

        public void SetOnDestroyCallback(Action m)
        {
            onDestroyM = m;
            AndroidNativeCalls.set_destroy_callback(Marshal.GetFunctionPointerForDelegate((Action)ManagedOnDestroyCallback));
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
            Console.WriteLine("Android Window init.");

            var env = World.GetExistingSystem<TinyEnvironment>();
            var config = env.GetConfigData<DisplayInfo>();

            try
            {
                initialized = AndroidNativeCalls.init();
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

            SetOnPauseCallback();

            int winw = 0, winh = 0;
            AndroidNativeCalls.getWindowSize(ref winw, ref winh);
            config.focused = true;
            config.visible = true;
            config.orientation = winw >= winh ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;
            config.frameWidth = winw;
            config.frameHeight = winh;
            int sw = 0, sh = 0;
            AndroidNativeCalls.getScreenSize(ref sw, ref sh);
            config.screenWidth = sw;
            config.screenHeight = sh;
            config.width = winw;
            config.height = winh;
            int fbw = 0, fbh = 0;
            AndroidNativeCalls.getFramebufferSize(ref fbw, ref fbh);
            config.framebufferWidth = fbw;
            config.framebufferHeight = fbh;
            env.SetConfigData(config);

            frameTime = AndroidNativeCalls.time();
        }

        protected override void OnDestroy()
        {
            // close window
            if (initialized)
            {
                Console.WriteLine("Android Window shutdown.");
                AndroidNativeCalls.shutdown(0);
                initialized = false;
            }
        }

        protected override void OnUpdate()
        {
            if (!initialized)
                return;

#if UNITY_DOTSPLAYER
            Unity.Profiling.Profiler.FrameEnd();
            Unity.Profiling.Profiler.FrameBegin();
#endif

            var env = World.GetExistingSystem<TinyEnvironment>();
            var config = env.GetConfigData<DisplayInfo>();
            int winw = 0, winh = 0;
            AndroidNativeCalls.getWindowSize(ref winw, ref winh);
            if (winw != config.width || winh != config.height)
            {
                if (config.autoSizeToFrame)
                {
                    Console.WriteLine("Android Window update size.");
                    config.orientation = winw >= winh ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;
                    config.width = winw;
                    config.height = winh;
                    config.frameWidth = winw;
                    config.frameHeight = winh;
                    int fbw = 0, fbh = 0;
                    AndroidNativeCalls.getFramebufferSize(ref fbw, ref fbh);
                    config.framebufferWidth = fbw;
                    config.framebufferHeight = fbh;
                    env.SetConfigData(config);
                }
                else
                {
                    AndroidNativeCalls.resize(config.width, config.height);
                }
            }
            if (!AndroidNativeCalls.messagePump())
            {
                Console.WriteLine("Android message pump exit.");
                AndroidNativeCalls.shutdown(1);
                World.QuitUpdate = true;
                initialized = false;
                return;
            }
            double newFrameTime = AndroidNativeCalls.time();
            var timeData = env.StepWallRealtimeFrame(newFrameTime - frameTime);
            World.SetTime(timeData);
            frameTime = newFrameTime;
        }

        private bool initialized;
        private double frameTime;
    }

    public static class AndroidNativeCalls
    {
        [DllImport("lib_unity_tiny_android", EntryPoint = "init_android")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool init();

        [DllImport("lib_unity_tiny_android", EntryPoint = "getWindowSize_android")]
        public static extern void getWindowSize(ref int w, ref int h);

        [DllImport("lib_unity_tiny_android", EntryPoint = "getScreenSize_android")]
        public static extern void getScreenSize(ref int w, ref int h);

        [DllImport("lib_unity_tiny_android", EntryPoint = "getFramebufferSize_android")]
        public static extern void getFramebufferSize(ref int w, ref int h);

        [DllImport("lib_unity_tiny_android", EntryPoint = "getWindowFrameSize_android")]
        public static extern void getWindowFrameSize(ref int left, ref int top, ref int right, ref int bottom);

        [DllImport("lib_unity_tiny_android", EntryPoint = "shutdown_android")]
        public static extern void shutdown(int exitCode);

        [DllImport("lib_unity_tiny_android", EntryPoint = "resize_android")]
        public static extern void resize(int width, int height);

        [DllImport("lib_unity_tiny_android", EntryPoint = "messagePump_android")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool messagePump();

        [DllImport("lib_unity_tiny_android", EntryPoint = "time_android")]
        public static extern double time();

        [DllImport("lib_unity_tiny_android", EntryPoint = "pausecallbacksinit_android")]
        public static extern bool set_pause_callback(IntPtr func);

        [DllImport("lib_unity_tiny_android", EntryPoint = "destroycallbacksinit_android")]
        public static extern bool set_destroy_callback(IntPtr func);

        [DllImport("lib_unity_tiny_android", EntryPoint = "get_touch_info_stream_android")]
        public static extern unsafe int * getTouchInfoStream(ref int len);

        [DllImport("lib_unity_tiny_android", EntryPoint = "get_key_stream_android")]
        public static extern unsafe int * getKeyStream(ref int len);

        [DllImport("lib_unity_tiny_android", EntryPoint = "get_native_window_android")]
        public static extern long getNativeWindow();

        [DllImport("lib_unity_tiny_android", EntryPoint = "reset_android_input")]
        public static extern void resetStreams();
    }

}

