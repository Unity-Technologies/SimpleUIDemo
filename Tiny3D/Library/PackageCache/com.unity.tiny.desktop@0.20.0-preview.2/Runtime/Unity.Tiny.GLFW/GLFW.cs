using System;
using Unity.Entities;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Tiny;

namespace Unity.Tiny.GLFW
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class GLFWWindowSystem : WindowSystem
    {
        protected bool initialized;
        protected double frameTime;
        protected bool windowOpen;

        public GLFWWindowSystem()
        {
            initialized = false;
            windowOpen = false;
        }

        public override IntPtr GetPlatformWindowHandle()
        {
            if (!windowOpen)
                return IntPtr.Zero;
            return GLFWNativeCalls.getPlatformWindowHandle();
        }

        public override void DebugReadbackImage(out int w, out int h, out NativeArray<byte> pixels)
        {
            throw new InvalidOperationException("Can no longer read-back from window use BGFX instead.");
        }

        protected override void OnCreate()
        {
            try
            {
                initialized = GLFWNativeCalls.init();
            }
            catch (Exception)
            {
                Debug.LogWarning("GLFW support unable to initialize; likely missing lib_unity_tiny_glfw.dll");
            }
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (!initialized)
                throw new InvalidOperationException("GLFW wasn't initialized");

            // setup window
            var env = World.TinyEnvironment();
            var config = env.GetConfigData<DisplayInfo>();

            if (config.width <= 0 || config.height <= 0)
            {
                Debug.LogError($"GLFW: configuration entity DisplayInfo has width or height <= 0! ({config.width} {config.height}).  Is it being created properly?");
                throw new InvalidOperationException("Bad DisplayInfo, window can't be opened");
            }

            // no-op if the window is already created
            var ok = GLFWNativeCalls.create_window(config.width, config.height);
            if (!ok)
                throw new InvalidOperationException("Failed to Open GLFW Window!");
            GLFWNativeCalls.show_window(1);

            GLFWNativeCalls.getWindowSize(out int winw, out int winh);
            GLFWNativeCalls.getScreenSize(out int sw, out int sh);
            config.focused = true;
            config.visible = true;
            config.orientation = winw >= winh ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;
            config.frameWidth = winw;
            config.frameHeight = winh;
            config.screenWidth = sw;
            config.screenHeight = sh;
            config.width = winw;
            config.height = winh;
            config.framebufferWidth = winw;
            config.framebufferHeight = winh;
            env.SetConfigData(config);

            frameTime = GLFWNativeCalls.time();

            windowOpen = true;
        }

        protected override void OnDestroy()
        {
            // close window
            if (windowOpen)
            {
#if UNITY_EDITOR
                GLFWNativeCalls.show_window(0);
#else
                GLFWNativeCalls.destroy_window();
#endif
                windowOpen = false;
            }

#if UNITY_DOTSPLAYER
            if (initialized)
            {
                GLFWNativeCalls.shutdown(0);
                initialized = false;
            }
#endif
        }

        protected override void OnUpdate()
        {
            if (!windowOpen)
                return;

#if UNITY_DOTSPLAYER
            Unity.Profiling.Profiler.FrameEnd();
            Unity.Profiling.Profiler.FrameBegin();
#endif

            var env = World.TinyEnvironment();
            var config = env.GetConfigData<DisplayInfo>();
            GLFWNativeCalls.getWindowSize(out int winw, out int winh);
            if (winw != config.width || winh != config.height)
            {
                if (config.autoSizeToFrame)
                {
                    config.width = winw;
                    config.height = winh;
                    config.frameWidth = winw;
                    config.frameHeight = winh;
                    config.framebufferWidth = winw;
                    config.framebufferHeight = winh;
                    env.SetConfigData(config);
                }
                else
                {
                    GLFWNativeCalls.resize(config.width, config.height);
                }
            }
            if (!GLFWNativeCalls.messagePump())
            {
#if UNITY_DOTSPLAYER
                World.QuitUpdate = true;
#endif
                return;
            }
            double newFrameTime = GLFWNativeCalls.time();
            var timeData = env.StepWallRealtimeFrame(newFrameTime - frameTime);
            World.SetTime(timeData);
            frameTime = newFrameTime;
        }
    }

    public static class GLFWNativeCalls
    {
        [DllImport("lib_unity_tiny_glfw", EntryPoint = "init_glfw")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool init();

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "create_window_glfw")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool create_window(int width, int height);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "show_window_glfw")]
        public static extern void show_window(int show);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "destroy_window_glfw")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool destroy_window();

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "getWindowSize_glfw")]
        public static extern void getWindowSize(out int width, out int height);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "getScreenSize_glfw")]
        public static extern void getScreenSize(out int width, out int height);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "getWindowFrameSize_glfw")]
        public static extern void getWindowFrameSize(out int left, out int top, out int right, out int bottom);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "shutdown_glfw")]
        public static extern void shutdown(int exitCode);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "resize_glfw")]
        public static extern void resize(int width, int height);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "messagePump_glfw")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool messagePump();

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "time_glfw")]
        public static extern double time();

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "getwindow_glfw")]
        public static extern unsafe void *getWindow();

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "get_key_stream_glfw_input")]
        public static extern unsafe int * getKeyStream(ref int len);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "get_mouse_pos_stream_glfw_input")]
        public static extern unsafe int * getMousePosStream(ref int len);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "get_mouse_button_stream_glfw_input")]
        public static extern unsafe int * getMouseButtonStream(ref int len);

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "init_glfw_input")]
        public static extern bool init_input();

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "reset_glfw_input")]
        public static extern void resetStreams();

        [DllImport("lib_unity_tiny_glfw", EntryPoint = "glfw_get_platform_window_handle")]
        public static extern IntPtr getPlatformWindowHandle();
    }

}
