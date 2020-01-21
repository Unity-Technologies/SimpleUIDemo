using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Bee.NativeProgramSupport.Building;
using Bee.Core;
using Bee.DotNet;
using Bee.Stevedore;
using Bee.Toolchain.Xcode;
using Bee.Toolchain.Extension;
using Bee.BuildTools;
using Newtonsoft.Json.Linq;
using NiceIO;
using Unity.BuildSystem.NativeProgramSupport;
using Unity.BuildTools;
using UnityEditor.iOS.Xcode.Custom;
using UnityEditor.iOS.Xcode.Custom.Extensions;

namespace Bee.Toolchain.IOS
{
    internal class UserIOSSdkLocator : IOSSdkLocator
    {
        public UserIOSSdkLocator() : base(Architecture.Arm64) {}

        public IOSSdk UserIOSSdk(NPath path)
        {
            return path != null ? DefaultSdkFromXcodeApp(path) : IOSSdk.LocatorArm64.FindSdkInDownloadsOrSystem(new Version(10, 1));
        }
    }

    internal class IOSAppToolchain : IOSToolchain
    {
        public static ToolChain ToolChain_IOSAppArm64 { get; } = new IOSAppToolchain();

        public override NativeProgramFormat ExecutableFormat { get; }

        private static NPath _XcodePath = null;

        private static NPath XcodePath
        {
            get
            {
                if (_XcodePath == null)
                {
                    string error = "";

                    try
                    {
                        if (HostPlatform.IsOSX)
                        {
                            var start = new ProcessStartInfo("xcode-select", "-p")
                            {
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                RedirectStandardInput = true,
                                UseShellExecute = false,
                            };
                            string output = "";
                            using (var process = Process.Start(start))
                            {
                                process.OutputDataReceived += (sender, e) => { output += e.Data; };
                                process.ErrorDataReceived += (sender, e) => { error += e.Data; };
                                process.BeginOutputReadLine();
                                process.BeginErrorReadLine();
                                process.WaitForExit(); //? doesn't work correctly if time interval is set
                            }

                            _XcodePath = error == "" ? output : "";
                            if (_XcodePath != "" && _XcodePath.DirectoryExists())
                            {
                                _XcodePath = XcodePath.Parent.Parent;
                            }
                            else
                            {
                                throw new InvalidOperationException("Failed to find Xcode, xcode-select did not return a valid path");
                            }
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        Console.WriteLine(
                            $"xcode-select did not return a valid path. Error message, if any, was: {error}. " +
                            $"Often this can be fixed by making sure you have Xcode command line tools" +
                            $" installed correctly, and then running `sudo xcode-select -r`");
                        throw e;
                    }
                }

                return _XcodePath;
            }
        }

        public IOSAppToolchain() : base((new UserIOSSdkLocator()).UserIOSSdk(XcodePath))
        {
            ExecutableFormat = new IOSAppMainModuleFormat(this);
        }
    }

    internal sealed class IOSAppMainModuleFormat : NativeProgramFormat
    {
        public override string Extension { get; } = "";

        internal IOSAppMainModuleFormat(XcodeToolchain toolchain) : base(
            new IOSAppMainModuleLinker(toolchain))
        {
        }
    }

    //TODO: should be inherited from XcodeStaticLinker, but it is sealed
    internal class IOSAppMainModuleLinker : StaticLinker
    {
        private NPath ChangeMainModuleName(NPath target)
        {
            // need to rename to make it start with "lib", otherwise Android have problems with loading native library
            return target.ChangeExtension("a");
        }

        protected override bool SupportsResponseFile => false; // libtool does not support response files

        public IOSAppMainModuleLinker(ToolChain toolChain) : base(toolChain) {}

        protected override IEnumerable<string> CommandLineFlagsForLibrary(PrecompiledLibrary library, CodeGen codegen)
        {
            if (BundleStaticLibraryDependencies && library.Static)
                yield return library.InQuotes();
        }

        protected override IEnumerable<string> CommandLineFlagsFor(NPath destination, CodeGen codegen, IEnumerable<NPath> objectFiles)
        {
            if (Toolchain.Architecture is ARMv7Architecture)
            {
                yield return "-arch_only";
                yield return "armv7";
            }

            if (Toolchain.Architecture is Arm64Architecture)
            {
                yield return "-arch_only";
                yield return "arm64";
            }

            yield return "-static";

            foreach (var objectFile in objectFiles)
                yield return objectFile.InQuotes();

            yield return "-o";
            yield return ChangeMainModuleName(destination).InQuotes();
        }

        protected override BuiltNativeProgram BuiltNativeProgramFor(NPath destination, IEnumerable<PrecompiledLibrary> allLibraries)
        {
            var resultingLibs = BundleStaticLibraryDependencies ? allLibraries.Where(a => !a.Static) : allLibraries;

            return (BuiltNativeProgram)new IOSAppMainStaticLibrary(ChangeMainModuleName(destination), Toolchain as IOSAppToolchain, resultingLibs.ToArray());
        }
    }

    internal class IOSAppMainStaticLibrary : StaticLibrary, IPackagedAppExtension
    {
        private const string TinyProjectName = "Tiny-iPhone";

        private IOSAppToolchain m_iosAppToolchain;
        private String m_gameName;
        private CodeGen m_codeGen;
        private IEnumerable<IDeployable> m_supportFiles;

        public IOSAppMainStaticLibrary(NPath path, IOSAppToolchain toolchain, params PrecompiledLibrary[] libraryDependencies) : base(path, libraryDependencies)
        {
            m_iosAppToolchain = toolchain;
        }

        public void SetAppPackagingParameters(String gameName, CodeGen codeGen, IEnumerable<IDeployable> supportFiles)
        {
            m_gameName = gameName.Replace(".","-");
            m_codeGen = codeGen;
            m_supportFiles = supportFiles;
        }

        public override BuiltNativeProgram DeployTo(NPath targetDirectory, Dictionary<IDeployable, IDeployable> alreadyDeployed = null)
        {
            var libDirectory = Path.Parent.Combine(TinyProjectName);
            var result = base.DeployTo(libDirectory, alreadyDeployed);
            return new Executable(PackageApp(targetDirectory, result.Path));
        }

        public NPath PackageApp(NPath buildPath, NPath mainLibPath)
        {
            if (m_iosAppToolchain == null)
            {
                Console.WriteLine("Error: not IOS App toolchain");
                return mainLibPath;
            }

            var iosPlatformPath = AsmDefConfigFile.AsmDefDescriptionFor("Unity.Platforms.iOS").Path.Parent;
            var xcodeProjectPath = mainLibPath.Parent;
            var xcodeSrcPath = iosPlatformPath.Combine(TinyProjectName+"~");
            var xcodeprojPath = xcodeProjectPath.Combine($"{TinyProjectName}.xcodeproj");

            // copy and patch pbxproj file
            var pbxPath = xcodeprojPath.Combine("project.pbxproj");
            var pbxTemplatePath = xcodeSrcPath.Combine($"{TinyProjectName}.xcodeproj", "project.pbxproj");
            var exportManifestPath = new NPath(m_gameName).Combine("export.manifest");
            var result = SetupXCodeProject(pbxTemplatePath, exportManifestPath.FileExists());
            Backend.Current.AddWriteTextAction(pbxPath, result);
            Backend.Current.AddDependency(pbxPath, mainLibPath);

            // copy and patch Info.plist file
            var plistPath = xcodeProjectPath.Combine("Sources", "Info.plist");
            var plistTemplatePath = xcodeSrcPath.Combine("Sources", "Info.plist");
            result = SetupInfoPlist(plistTemplatePath);
            Backend.Current.AddWriteTextAction(plistPath, result);
            Backend.Current.AddDependency(pbxPath, plistPath);

            // copy xcodeproj files
            foreach (var r in xcodeSrcPath.Files(true))
            {
                if (r.Extension != "pbxproj" && r.FileName != "Info.plist")
                {
                    var destPath = xcodeProjectPath.Combine(r.RelativeTo(xcodeSrcPath));
                    destPath = CopyTool.Instance().Setup(destPath, r);
                    Backend.Current.AddDependency(pbxPath, destPath);
                }
            }

            foreach (var r in m_supportFiles)
            {
                if (r.Path.FileName == "testconfig.json")
                {
                    Backend.Current.AddDependency(pbxPath, CopyTool.Instance().Setup(buildPath.Combine(r.Path.FileName), r.Path));
                    break;
                }
            }

            // TODO probably it is required to keep previous project since it can be modified by user
            var outputPath = buildPath.Combine($"{m_gameName}");
            Console.WriteLine($"Move project to {outputPath}");
            Backend.Current.AddAction(
                actionName: "Open XCode project folder",
                targetFiles: new[] { outputPath },
                inputs: new[] { pbxPath },
                executableStringFor: $"rm -rf {outputPath} && mv {xcodeProjectPath} {outputPath} && open {outputPath}",
                commandLineArguments: Array.Empty<string>(),
                allowUnexpectedOutput: true
            );

            return outputPath;
        }

        private void ProcessLibs(BuiltNativeProgram p, HashSet<NPath> xCodeLibs)
        {
            if (p.Path.Extension == "dylib" || p.Path.Extension == "a")
            {
                xCodeLibs.Add(p.Path);
            }
            foreach (var d in p.Deployables)
            {
                if (d is BuiltNativeProgram)
                {
                    ProcessLibs(d as BuiltNativeProgram, xCodeLibs);
                }
            }
        }

        private string SetupXCodeProject(NPath pbxTemplatePath, bool dataExists)
        {
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromFile(pbxTemplatePath.ToString());
            var target = pbxProject.TargetGuidByName(TinyProjectName);
            var targets = new string[] { target };

            // preparing list of libs and adding them to project
            HashSet<NPath> xCodeLibs = new HashSet<NPath>();
            ProcessLibs(this, xCodeLibs);
            foreach (var lib in xCodeLibs)
            {
                var fileGuid = pbxProject.AddFile(lib.FileName, lib.FileName);
                pbxProject.AddFileToBuild(target, fileGuid);
                if (lib.Extension == "dylib")
                {
                    PBXProjectExtensions.AddFileToEmbedFrameworks(pbxProject, target, fileGuid);
                }
            }

            foreach (var r in m_supportFiles)
            {
                // skipping all subdirectories
                // TODO: subdirectories require special processing (see processing Data below)
                if (r.Path.RelativeTo(Path).Depth == 0 && r.Path.FileName != "testconfig.json")
                {
                    var fileGuid = pbxProject.AddFile(r.Path.FileName, r.Path.FileName);
                    pbxProject.AddFileToBuild(target, fileGuid);
                }
            }
            // adding Data folder
            if (dataExists)
            {
                var fileGuid = pbxProject.AddFile("Data", "Data");
                pbxProject.AddFileToBuild(target, fileGuid);
            }

            pbxProject.SetBuildProperty(targets, "PRODUCT_BUNDLE_IDENTIFIER", $"com.unity.{m_gameName.ToLower()}");
            pbxProject.SetBuildProperty(targets, "CODE_SIGN_STYLE", "Automatic");
            pbxProject.SetBuildProperty(targets, "PROVISIONING_PROFILE", "");

            /* TODO pass signing config from build settings or from env vars
            string appleDeveloperTeamID = null;
            string manualProvisioningProfileName = null;
            string manualProvisioningProfileUUID = null;
            string codeSignIdentity = null;

            appleDeveloperTeamID = Environment.GetEnvironmentVariable("TEAM_ID");
            manualProvisioningProfileName = Environment.GetEnvironmentVariable("UNITY_IOSPROVISIONINGNAME");
            manualProvisioningProfileUUID = Environment.GetEnvironmentVariable("UNITY_IOSPROVISIONINGUUID");
            codeSignIdentity = Environment.GetEnvironmentVariable("UNITY_APPLECERTIFICATENAME");

            if (!string.IsNullOrEmpty(appleDeveloperTeamID))
            {
                pbxProject.SetBuildProperty(targets, "DEVELOPMENT_TEAM", appleDeveloperTeamID);
            }
            if (string.IsNullOrEmpty(manualProvisioningProfileUUID) && string.IsNullOrEmpty(manualProvisioningProfileName))
            {
                pbxProject.SetBuildProperty(targets, "CODE_SIGN_STYLE", "Automatic");
                pbxProject.SetBuildProperty(targets, "PROVISIONING_PROFILE", "Automatic");
            }
            else
            {
                pbxProject.SetBuildProperty(targets, "CODE_SIGN_STYLE", "Manual");
                pbxProject.SetBuildProperty(targets, "PROVISIONING_PROFILE", !string.IsNullOrEmpty(manualProvisioningProfileUUID) ? manualProvisioningProfileUUID : manualProvisioningProfileName);
                pbxProject.SetBuildProperty(targets, "CODE_SIGN_IDENTITY[sdk=iphoneos*]", codeSignIdentity == null ? "iPhone Developer" : codeSignIdentity);
            }
            */
            return pbxProject.WriteToString();
        }

        private string SetupInfoPlist(NPath plistTemplatePath)
        {
            var text = plistTemplatePath.ReadAllText();
            var doc = new PlistDocument();
            doc.ReadFromString(text);
            var root = doc.root;
            root.SetString("CFBundleIdentifier", $"com.unity.{m_gameName.ToLower()}");
            root.SetString("CFBundleDisplayName", m_gameName);
            return doc.WriteToString();
        }
    }
}

