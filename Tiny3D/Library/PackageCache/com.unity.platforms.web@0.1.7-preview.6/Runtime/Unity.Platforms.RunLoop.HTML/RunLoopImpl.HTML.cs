#if (UNITY_WEBGL)
using System;
using System.Runtime.InteropServices;

namespace Unity.Platforms
{
    public class RunLoopImpl
    {
        internal class MonoPInvokeCallbackAttribute : Attribute
        {
        }

        [MonoPInvokeCallbackAttribute]
        static bool ManagedRAFCallback()
        {
            return staticM();
        }

        private static RunLoop.RunLoopDelegate staticM;
        private static RunLoop.RunLoopDelegate staticManagedDelegate;

        public static void EnterMainLoop(RunLoop.RunLoopDelegate runLoopDelegate)
        {
            // From `Marshal.GetFunctionPointerForDelegate` documentation:
            //   (https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.getfunctionpointerfordelegate)
            // You must manually keep the delegate from being collected by the garbage collector from managed code.
            // The garbage collector does not track references to unmanaged code.
            staticManagedDelegate = (RunLoop.RunLoopDelegate)ManagedRAFCallback;
            staticM = runLoopDelegate;
            HTMLNativeCalls.set_animation_frame_callback(Marshal.GetFunctionPointerForDelegate(staticManagedDelegate));
            Console.WriteLine("HTML Main loop exiting.");
        }
    }

    static class HTMLNativeCalls
    {
        // calls to HTMLWrapper.cpp
        [DllImport("lib_unity_tiny_html", EntryPoint = "rafcallbackinit_html")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool set_animation_frame_callback(IntPtr func);
    }
}
#endif