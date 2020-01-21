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
using Bee.Toolchain.Windows;
using JetBrains.Annotations;
using static Unity.BuildSystem.NativeProgramSupport.NativeProgramConfiguration;

[UsedImplicitly]
class CustomizerForBgfx : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Tiny.Rendering.Native";

    public override string[] ImplementationFor => new[] {"Unity.Tiny.Rendering"};

    public override void CustomizeSelf(AsmDefCSharpProgram program)
    {
        // We need to make sure that these two files are identical, but we can't read the stevedore
        // one until the build actually starts.  And we can't just use the stevedore one for our
        // build, because Unity also needs to build the code and it doesn't see the files that
        // we download as part of this build.
        // Also they're not identical because we have to hack in the library name.
#if false
        var ourBgfxCs = program.MainSourcePath.Combine("Bgfx.cs").MakeAbsolute();
        var realBgfxCs = BgfxBuild.Instance.BgfxRoot.Combine("bgfx/bindings/cs/bgfx.cs").MakeAbsolute();

        if (ourBgfxCs.ReadAllText() != realBgfxCs.ReadAllText())
        {
            throw new InvalidProgramException($"Bgfx.cs file mismatch!  The Bgfx.cs that's checked in must match the stevedore version, because both Unity and DOTS Runtime need access to it.  Please copy {realBgfxCs} to {ourBgfxCs}.");
        }
#endif

        program.NativeProgram.CompilerSettings().Add(c => c.WithCppLanguageVersion(CppLanguageVersion.Cpp14));
        program.NativeProgram.CompilerSettingsForMac().Add(c => c.WithObjcArc(false));
        program.NativeProgram.Libraries.Add(new NativeProgramAsLibrary(BgfxBuild.Instance.BxLib){BuildMode = NativeProgramLibraryBuildMode.BagOfObjects});
        program.NativeProgram.Libraries.Add(new NativeProgramAsLibrary(BgfxBuild.Instance.BimgLib){BuildMode = NativeProgramLibraryBuildMode.BagOfObjects});
        program.NativeProgram.Libraries.Add(new NativeProgramAsLibrary(BgfxBuild.Instance.BgfxLib){BuildMode = NativeProgramLibraryBuildMode.BagOfObjects});

        // lib_unity_blah_bgfx.bc bx.bc
        program.NativeProgram.Libraries.Add(IsWindows, new List<string>
                {
                    "gdi32",
                    "user32",
                    "kernel32",
                    "psapi",
                    "dxguid"
                }
                .ConvertAll(s => new SystemLibrary(s + ".lib"))
        );
        program.NativeProgram.Libraries.Add(IsLinux, new SystemLibrary("dl"), new SystemLibrary("pthread"), new SystemLibrary("X11"));
        program.NativeProgram.Libraries.Add(c => c.Platform is MacOSXPlatform, new List<string>
            {
                "Cocoa", "QuartzCore", "OpenGL", "Metal"
            }
            .ConvertAll(s => new SystemFramework(s)));
        program.NativeProgram.Libraries.Add(c => c.Platform is LinuxPlatform, new List<string>
            { "GL", "X11", "udev", "Xrandr", "dl" }
            .ConvertAll(s => new SystemFramework(s)));


        program.NativeProgram.Libraries.Add(c => c.Platform is WebGLPlatform, new List<string>
            {
                "GL", "EGL"
            }.ConvertAll(s => new SystemLibrary(s)));

        program.NativeProgram.Libraries.Add(c => c.Platform is AndroidPlatform, new List<string>
            {
                "android", "log", "GLESv3", "EGL"
            }.ConvertAll(s => new SystemLibrary(s)));
        program.NativeProgram.Libraries.Add(c => c.Platform is IosPlatform, new List<string>
            {
                "Foundation", "UIKit", "QuartzCore", "OpenGLES", "Metal"
            }
            .ConvertAll(s => new SystemFramework(s)));
    }
}

public class BgfxBuild
{
    internal StevedoreArtifact BgfxArtifact;
    internal NPath BgfxRoot;

    private static BgfxBuild _Instance;

    public NativeProgram BgfxLib;
    public NativeProgram BxLib;
    public NativeProgram BimgLib;

    public static BgfxBuild Instance {
        get {
            if (_Instance == null)
                _Instance = new BgfxBuild();
            return _Instance;
        }
    }

    public BgfxBuild()
    {
        var rendererRoot = AsmDefConfigFile.AsmDefDescriptionFor("Unity.Tiny.Rendering.Native").Directory;

        bool useLocalBgfx = false;

        BgfxArtifact = new StevedoreArtifact("bgfx-source");
        Backend.Current.Register(BgfxArtifact);
        BgfxRoot = BgfxArtifact.Path;

        //BgfxRoot = @"C:\Users\sebastianm\gits\bgfxroot";
        //useLocalBgfx = true;

        var bx = BgfxRoot.Combine("bx");
        var bgfx = BgfxRoot.Combine("bgfx");
        var bimg = BgfxRoot.Combine("bimg");

        // Note that these 3 NativePrograms are only linked in as BagOfObjects into the bgfx dll above.
        // They should not have Libraries themselves (e.g. bgfx should not reference the bx or bimg NativePrograms).
        // This means that PublicIncludes won't work, which is why Bimg and Bgfx explicitly add BxLib's PublicIncludes
        // to their own Includes.

        BxLib = new NativeProgram("bx") {
            Exceptions = { false },
            RTTI = { false },
            PublicIncludeDirectories = {
                bx.Combine("include"),
                bx.Combine("3rdparty"),
            },
            Sources = {
                //bx.Combine("src").Files("*.cpp").Except(new[] {bx.Combine("src/amalgamated.cpp"), bx.Combine("src/crtnone.cpp")})
                bx.Combine("src/amalgamated.cpp")
            },
            Defines = { "__STDC_FORMAT_MACROS" },
        };
        BxLib.CompilerSettings().Add(c => c.WithCppLanguageVersion(CppLanguageVersion.Cpp14));
        BxLib.CompilerSettingsForMac().Add(c => c.WithObjcArc(false));
        BxLib.CompilerSettingsForIos().Add(c => c.WithObjcArc(false));
        BxLib.Defines.Add(c => c.Platform is WindowsPlatform, "_CRT_SECURE_NO_WARNINGS");
        BxLib.PublicIncludeDirectories.Add(c => c.ToolChain is WindowsToolchain, bx.Combine("include/compat/msvc"));
        BxLib.PublicIncludeDirectories.Add(c => c.Platform is MacOSXPlatform, bx.Combine("include/compat/osx"));
        BxLib.PublicIncludeDirectories.Add(c => c.Platform is IosPlatform, bx.Combine("include/compat/ios"));

        BimgLib = new NativeProgram("bimg") {
            Exceptions = { false },
            RTTI = { false },
            IncludeDirectories = {
                bimg.Combine("include"),
                bimg.Combine("3rdparty/astc-codec"),
                bimg.Combine("3rdparty/astc-codec/include"),
            },
            Sources = {
                bimg.Combine("src/image.cpp"),
                bimg.Combine("src/image_gnf.cpp"),
                bimg.Combine("3rdparty/astc-codec/src/decoder").CombineMany(new [] {"astc_file.cc","codec.cc","endpoint_codec.cc","footprint.cc","integer_sequence_codec.cc","intermediate_astc_block.cc","logical_astc_block.cc","partition.cc","physical_astc_block.cc","quantization.cc","weight_infill.cc"})
            },
            Defines = { "__STDC_FORMAT_MACROS" },
        };
        BimgLib.CompilerSettings().Add(c => c.WithCppLanguageVersion(CppLanguageVersion.Cpp14));
        BimgLib.CompilerSettingsForMac().Add(c => c.WithObjcArc(false));
        BimgLib.CompilerSettingsForIos().Add(c => c.WithObjcArc(false));
        BimgLib.IncludeDirectories.Add(c => BxLib.PublicIncludeDirectories.For(c));

        BgfxLib = new NativeProgram("bgfx") {
            Exceptions = { false },
            RTTI = { false },
            IncludeDirectories = {
                bimg.Combine("include"),
                bgfx.Combine("include"),
                bgfx.Combine("3rdparty"),
                bgfx.Combine("3rdparty/khronos"),
                rendererRoot.Combine("cpp~/include"),
            },
            Defines = {
                "BGFX_SHARED_LIB_BUILD",
                "__STDC_FORMAT_MACROS"
            },
        };
        BgfxLib.CompilerSettings().Add(c => c.WithCppLanguageVersion(CppLanguageVersion.Cpp14));
        BgfxLib.CompilerSettingsForMac().Add(c => c.WithObjcArc(false));
        BgfxLib.CompilerSettingsForIos().Add(c => c.WithObjcArc(false));
        BgfxLib.IncludeDirectories.Add(c => BxLib.PublicIncludeDirectories.For(c));

        BgfxLib.Defines.Add(c => ((DotsRuntimeNativeProgramConfiguration)c).CSharpConfig.Defines.Contains("RENDERING_ENABLE_TRACE"), "BGFX_CONFIG_DEBUG=1");
        BgfxLib.Defines.Add(c => c.ToolChain is WindowsToolchain, "_CRT_SECURE_NO_WARNINGS");

        if (!useLocalBgfx)
        {
            // when using bgfx from stevedore, this requires pix3.h which we don't distribute
            BgfxLib.Defines.Add(c => c.Platform is WindowsPlatform, "BGFX_CONFIG_DEBUG_ANNOTATION=0");
        }

        BgfxLib.Defines.Add("BGFX_CONFIG_MAX_BONES=4");

        // At some point we need to stop using amalgamated, especially for small-size web builds
        BgfxLib.Sources.Add(c => !(c.Platform is MacOSXPlatform || c.Platform is IosPlatform), bgfx.Combine("src/amalgamated.cpp"));
        BgfxLib.Sources.Add(c => (c.Platform is MacOSXPlatform || c.Platform is IosPlatform), bgfx.Combine("src/amalgamated.mm"));

        // This is a hack that the Khronos eglplatform.h header understands in order to define the EGL types as intptr_t,
        // which is what emscripten wants.  Otherwise we fall into a __unix__ path, which includes X11/Xlib.h, and
        // all hell breaks loose.
        BgfxLib.Defines.Add(c => c.Platform is WebGLPlatform, "USE_OZONE");
    }

}

