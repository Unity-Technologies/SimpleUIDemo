using System;
using System.Linq;
using Bee.Core;
using Bee.Stevedore;
using Bee.Toolchain.VisualStudio;
using JetBrains.Annotations;
using Unity.BuildSystem.NativeProgramSupport;

[UsedImplicitly]
class CustomizerForZeroJobs : DotsRuntimeCSharpProgramCustomizer
{
    public override void Customize(DotsRuntimeCSharpProgram program)
    {
        if (program.MainSourcePath.FileName != "Unity.ZeroJobs")
            return;

        program.NativeProgram.Libraries.Add(c => c.Platform is LinuxPlatform, new SystemLibrary("rt"));
        program.NativeProgram.Libraries.Add(c => c.Platform is WindowsPlatform, new SystemLibrary("ws2_32.lib"));

        NativeJobsPrebuiltLibrary.Add(program.NativeProgram);
    }

    public static class NativeJobsPrebuiltLibrary
    {
        public static string BaselibArchitectureName(NativeProgramConfiguration npc)
        {
            switch (npc.Platform)
            {
                case MacOSXPlatform _:
                    if (npc.ToolChain.Architecture.IsX64) return "mac64";
                    break;
                case WindowsPlatform _:
                    if (npc.ToolChain.Architecture.IsX86) return "win32";
                    if (npc.ToolChain.Architecture.IsX64) return "win64";
                    break;
                default:
                    if (npc.ToolChain.Architecture.IsX86) return "x86";
                    if (npc.ToolChain.Architecture.IsX64) return "x64";
                    if (npc.ToolChain.Architecture.IsArmv7) return "arm32";
                    if (npc.ToolChain.Architecture.IsArm64) return "arm64";
                    if (npc.ToolChain.Architecture is WasmArchitecture) return "wasm";
                    if (npc.ToolChain.Architecture is AsmJsArchitecture) return "asmjs";
                    //if (npc.ToolChain.Architecture is WasmArchitecture && HAS_THREADING) return "wasm_withthreads";
                    break;
            }

            throw new InvalidProgramException($"Unknown toolchain and architecture for baselib: {npc.ToolChain.LegacyPlatformIdentifier} {npc.ToolChain.Architecture.Name}");
        }

        public static void Add(NativeProgram np)
        {
            var allPlatforms = new []
            {
                "Android",
                "Linux",
                "Windows",
                "OSX",
                "IOS",
                "WebGL"
            };

            var staticPlatforms = new[]
            {
                "IOS",
                "WebGL",
            };

            var allArtifact = new StevedoreArtifact("nativejobs-all-public");
            Backend.Current.Register(allArtifact);
            np.PublicIncludeDirectories.Add(allArtifact.Path.Combine("Include"));

            DotsConfiguration DotsConfig(NativeProgramConfiguration npc) => ((DotsRuntimeNativeProgramConfiguration)npc).CSharpConfig.DotsConfiguration;

            foreach (var platform in allPlatforms)
            {
                var platformIncludes = new StevedoreArtifact($"nativejobs-{platform}-public");
                var prebuiltLibName = $"nativejobs-{platform}" + (staticPlatforms.Contains(platform) ? "-s" : "-d");
                var prebuiltLib = new StevedoreArtifact(prebuiltLibName);
                Backend.Current.Register(platformIncludes);
                Backend.Current.Register(prebuiltLib);

                np.PublicDefines.Add(c => c.Platform.Name == platform, "BASELIB_USE_DYNAMICLIBRARY=1");
                np.PublicIncludeDirectories.Add(c => c.Platform.Name == platform, platformIncludes.Path.Combine("Platforms", platform, "Include"));

                switch (platform)
                {
                    case "Windows":
                        np.Libraries.Add(c => c.Platform.Name == platform,
                            c => new[] { new MsvcDynamicLibrary(prebuiltLib.Path.Combine("lib", platform.ToLower(), BaselibArchitectureName(c), DotsConfig(c).ToString().ToLower(), "nativejobs.dll")) });
                        break;
                    case "Linux":
                    case "Android":
                        np.Libraries.Add(c => c.Platform.Name == platform,
                            c => new[] { new DynamicLibrary(prebuiltLib.Path.Combine("lib", platform.ToLower(), BaselibArchitectureName(c), DotsConfig(c).ToString().ToLower(), "libnativejobs.so")) });
                        break;
                    case "OSX":
                        np.Libraries.Add(c => c.Platform.Name == platform,
                            c => new[] { new DynamicLibrary(prebuiltLib.Path.Combine("lib", platform.ToLower(), BaselibArchitectureName(c), DotsConfig(c).ToString().ToLower(), "libnativejobs.dylib")) });
                        break;
                    case "IOS":
                        // this is ugly solution, but I don't see any other way to add static librray to Deployables
                        np.Libraries.Add(c => c.Platform.Name == platform,
                            c => new[] { new DynamicLibrary(prebuiltLib.Path.Combine("lib", platform.ToLower(), BaselibArchitectureName(c), DotsConfig(c).ToString().ToLower(), "libnativejobs.a")) });
                        break;
                    case "WebGL":
                        np.Libraries.Add(c => c.Platform.Name == platform,
                            c => new[] { new StaticLibrary(prebuiltLib.Path.Combine("lib", platform.ToLower(), BaselibArchitectureName(c), DotsConfig(c).ToString().ToLower(), "libnativejobs.bc")) });
                        break;
                }
            }
        }
    }
}
