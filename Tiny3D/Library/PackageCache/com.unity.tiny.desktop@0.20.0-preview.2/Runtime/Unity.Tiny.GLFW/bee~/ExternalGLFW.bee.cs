using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Bee.Core;
using Bee.Stevedore;
using Bee.Toolchain.Xcode;
using NiceIO;
using Unity.BuildSystem.NativeProgramSupport;
using Bee.Toolchain.GNU;

partial class External
{
    public static NativeProgram SetupGLFW()
    {
        var glfwArtifacts = new StevedoreArtifact("glfw");
        Backend.Current.Register(glfwArtifacts);
        var glfwRoot = glfwArtifacts.Path;

        var glfwLib = new NativeProgram("libglfw")
        {
            //RootDirectory = glfwRoot,
            IncludeDirectories =
            {
                glfwRoot.Combine("include"),
                glfwRoot.Combine("src"),
            },
            PublicIncludeDirectories =
            {
                glfwRoot.Combine("include")
            },
            Sources =
            {
            },
        };
        glfwLib.Defines.Add(c => c.CodeGen != CodeGen.Debug, "NDEBUG");

        glfwLib.Sources.Add(glfwRoot.Combine("src").CombineMany(new string[]
        {
            "context.c", "init.c", "input.c", "monitor.c", "vulkan.c", "window.c",
        }));

        //
        // Windows
        //
        glfwLib.Defines.Add(c => c.Platform is WindowsPlatform, "_GLFW_WIN32", "_GLFW_BUILD_DLL");
        glfwLib.Sources.Add(c => c.Platform is WindowsPlatform, glfwRoot.Combine("src").CombineMany(new string[]
        {
            "win32_init.c",
            "win32_joystick.c",
            "win32_monitor.c",
            "win32_time.c",
            "win32_thread.c",
            "win32_window.c",
            "wgl_context.c",
            "egl_context.c",
            "osmesa_context.c"
        }));

        glfwLib.Libraries.Add(c => c.Platform is WindowsPlatform,
            new List<string>
                {
                    "winmm",
                    "gdi32",
                    "opengl32",
                    "user32",
                    "winspool",
                    "shell32",
                    "uuid",
                    "comdlg32",
                    "advapi32"
                }
                .ConvertAll(s => new SystemLibrary(s + ".lib"))
        );

        //
        // Linux
        //
        glfwLib.Defines.Add(c => c.Platform is LinuxPlatform, new string[]
            {"_GLFW_X11", "IL_NO_UTX"});


        glfwLib.Sources.Add(c => c.Platform is LinuxPlatform, glfwRoot.Combine("src").CombineMany(new string[]
        {
            "input.c",
            "linux_joystick.c",
            "x11_init.c",
            "x11_monitor.c",
            "x11_window.c",
            "xkb_unicode.c",
            "egl_context.c",
            "glx_context.c",
            "osmesa_context.c",
            "posix_thread.c",
            "posix_time.c"
        }));
        glfwLib.Libraries.Add(c => c.Platform is LinuxPlatform,
            new List<string> { "GL", "X11", "udev", "Xrandr", "dl" , "rt" }
                .ConvertAll(s => new SystemLibrary(s)));
        //
        // Mac/iOS
        //
        glfwLib.Defines.Add(c => c.Platform is MacOSXPlatform, new string[]
            {"_GLFW_COCOA"});

        glfwLib.Sources.Add(c => c.Platform is MacOSXPlatform, glfwRoot.Combine("src").CombineMany(new string[]
        {
            "cocoa_init.m", "cocoa_joystick.m", "cocoa_monitor.m",
            "cocoa_window.m", "cocoa_time.c", "posix_thread.c",
            "nsgl_context.m", "egl_context.c", "osmesa_context.c"
        }));

        glfwLib.Libraries.Add(c => c.Platform is MacOSXPlatform,
            new List<string> {"OpenGL", "CoreFoundation", "IOKit", "Cocoa", "CoreData", "CoreVideo" }
                .ConvertAll(s => new SystemFramework(s)));
        return glfwLib;
    }

    public static NativeProgram GLFWStaticLibrary;

}

