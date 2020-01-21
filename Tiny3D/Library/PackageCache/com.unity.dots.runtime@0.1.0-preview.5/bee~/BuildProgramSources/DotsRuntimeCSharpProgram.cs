using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using Bee;
using Bee.Core;
using Bee.CSharpSupport;
using Bee.DotNet;
using Bee.NativeProgramSupport.Building;
using Bee.Toolchain.Emscripten;
using Bee.Toolchain.Xcode;
using Bee.VisualStudioSolution;
using NiceIO;
using Unity.BuildSystem.CSharpSupport;
using Unity.BuildSystem.NativeProgramSupport;
using Unity.BuildTools;

public enum DotsConfiguration
{
    Debug,
    Develop,
    Release,
}

/// <summary>
/// DotsRuntimeCSharpProgram is a csharp program that targets dots-runtime. It follows a particular file structure. It always has a folder
/// that folder can have *.cs files, which will be part of the csharp program. The folder can also have a .cpp~ and .js~ folder.  If any
/// of those are present, DotsRuntimeCSharpProgram will build a NativeProgram with those .cpp files and .js libraries side by side. The common
/// usecase for this is for the c# code to [DllImport] pinvoke into the c++ code.
///
/// A DotsRuntimeCSharpProgram does not know about asmdefs (e.g. Unity.LowLevel)
/// </summary>
public class DotsRuntimeCSharpProgram : CSharpProgram
{
    private bool _doneEnsureNativeProgramLinksToReferences;
    
    public NPath MainSourcePath { get; }
    public List<NPath> ExtraSourcePaths { get; }
    private IEnumerable<NPath> AllSourcePaths => new[] {MainSourcePath}.Concat(ExtraSourcePaths);
    public NativeProgram NativeProgram { get; set; }
    public Platform[] IncludePlatforms { get; set; }
    public Platform[] ExcludePlatforms { get; set; }

    public virtual bool IsSupportedFor(CSharpProgramConfiguration config)
    {
        if (config is DotsRuntimeCSharpProgramConfiguration dotsConfig)
        {
            if (IncludePlatforms?.Any(p => p.GetType().IsInstanceOfType(dotsConfig.Platform)) ?? false)
                return true;

            if (IncludePlatforms?.Any() ?? false)
                return false;
            
            if (!ExcludePlatforms?.Any() ?? true)
                return true;

            return !ExcludePlatforms?.Any(p => p.GetType().IsInstanceOfType(dotsConfig.Platform)) ?? true;
        }

        return false;
    }

    public DotsRuntimeCSharpProgram(NPath mainSourcePath,
        IEnumerable<NPath> extraSourcePaths = null,
        string name = null,
        bool isExe = false,
        bool deferConstruction = false)
    {
        MainSourcePath = mainSourcePath;
        ExtraSourcePaths = extraSourcePaths?.ToList() ?? new List<NPath>();
        name = name ?? MainSourcePath.FileName;
        
        if (!deferConstruction)
            Construct(name, isExe);

        //ProjectFile.ExplicitConfigurationsToUse = new CSharpProgramConfiguration[] {DotsConfigs.ProjectFileConfig};

        ProjectFile.IntermediateOutputPath.Set(config => Configuration.RootArtifactsPath.Combine(ArtifactsGroup ?? "Bee.CSharpSupport").Combine("MSBuildIntermediateOutputPath", config.Identifier));
    }

    protected void Construct(string name, bool isExe)
    {
        FileName = name + (isExe ? ".exe" : ".dll");

        Framework.Add(c=> ShouldTargetTinyCorlib(c, this),Bee.DotNet.Framework.FrameworkNone);
        References.Add(c=>ShouldTargetTinyCorlib(c, this),Il2Cpp.TinyCorlib);
        
        Framework.Add(c=>!ShouldTargetTinyCorlib(c, this),Bee.DotNet.Framework.Framework471);
        References.Add(c=>!ShouldTargetTinyCorlib(c, this), new SystemReference("System"));
        
        ProjectFile.Path = DeterminePathForProjectFile();

        ProjectFile.ReferenceModeCallback = arg =>
        {
            // Most projects are AsmDefCSharpProgram. For everything else we'll look up their
            // packagestatus by the fact that we know it's in the same package as Unity.Entities.CPlusPlus
            // XXX This is not true any more!
            //var asmdefDotsProgram = (arg as AsmDefCSharpProgram)?.AsmDefDescription ?? AsmDefConfigFile.AsmDefDescriptionFor("Unity.Entities.CPlusPlus");
            return ProjectFile.ReferenceMode.ByCSProj;
        };
        
        LanguageVersion = "7.3";
        Defines.Add(
            "UNITY_DOTSPLAYER",
            "NET_DOTS",
            // TODO -- figure out what's gated with this, and if it should have a UNITY_DOTSPLAYER || ...
            "UNITY_2018_3_OR_NEWER",
            // And these might not make sense for DOTS Runtime anyway beacuse they are UnityEngine/UnityEditor APIs.
            // They break the build if we add them.
            //"UNITY_2019_1_OR_NEWER",
            //"UNITY_2019_2_OR_NEWER",
            //"UNITY_2019_3_OR_NEWER",
            // TODO -- removing this breaks Burst, it should be using DOTSPLAYER!
            "UNITY_ZEROPLAYER"
        );
        
        Defines.Add(c => (c as DotsRuntimeCSharpProgramConfiguration)?.Platform is WebGLPlatform, "UNITY_WEBGL");
        Defines.Add(c =>(c as DotsRuntimeCSharpProgramConfiguration)?.Platform is WindowsPlatform, "UNITY_WINDOWS");
        Defines.Add(c =>(c as DotsRuntimeCSharpProgramConfiguration)?.Platform is MacOSXPlatform, "UNITY_MACOSX");
        Defines.Add(c => (c as DotsRuntimeCSharpProgramConfiguration)?.Platform is LinuxPlatform, "UNITY_LINUX");
        Defines.Add(c =>(c as DotsRuntimeCSharpProgramConfiguration)?.Platform is IosPlatform, "UNITY_IOS");
        Defines.Add(c => (c as DotsRuntimeCSharpProgramConfiguration)?.Platform is AndroidPlatform, "UNITY_ANDROID");
        Defines.Add(c => !((DotsRuntimeCSharpProgramConfiguration) c).MultiThreadedJobs, "UNITY_SINGLETHREADED_JOBS");

        CopyReferencesNextToTarget = false;

        WarningsAsErrors = false;
        //hack, fix this in unity.mathematics

        foreach (var sourcePath in AllSourcePaths)
        {
            if (sourcePath.FileName == "Unity.Mathematics")
                Sources.Add(sourcePath.Files("*.cs", true)
                    .Where(f => f.FileName != "math_unity_conversion.cs" && f.FileName != "PropertyAttributes.cs"));
            else
            {
                Sources.Add(new CustomProvideFiles(sourcePath));
            }
        }
        foreach (var sourcePath in AllSourcePaths)
        {
            var cppFolder = sourcePath.Combine("cpp~");
            var prejsFolder = sourcePath.Combine("prejs~");
            var jsFolder = sourcePath.Combine("js~");
            var postjsFolder = sourcePath.Combine("postjs~");
            var beeFolder = sourcePath.Combine("bee~");
            var includeFolder = cppFolder.Combine("include");

            NPath[] cppFiles = Array.Empty<NPath>();
            if (cppFolder.DirectoryExists())
            {
                cppFiles = cppFolder.Files("*.c*", true);
                ProjectFile.AdditionalFiles.AddRange(cppFolder.Files(true));
                GetOrMakeNativeProgram().Sources.Add(cppFiles);
            }

            if (prejsFolder.DirectoryExists())
            {
                var jsFiles = prejsFolder.Files("*.js", true);
                ProjectFile.AdditionalFiles.AddRange(prejsFolder.Files(true));
                GetOrMakeNativeProgram()
                    .Libraries.Add(c => c.Platform is WebGLPlatform,
                        jsFiles.Select(jsFile => new PreJsLibrary(jsFile)));
            }

            //todo: get rid of having both a regular js and a prejs folder
            if (jsFolder.DirectoryExists())
            {
                var jsFiles = jsFolder.Files("*.js", true);
                ProjectFile.AdditionalFiles.AddRange(jsFolder.Files(true));
                GetOrMakeNativeProgram()
                    .Libraries.Add(c => c.Platform is WebGLPlatform,
                        jsFiles.Select(jsFile => new JavascriptLibrary(jsFile)));
            }

            if (postjsFolder.DirectoryExists())
            {
                var jsFiles = postjsFolder.Files("*.js", true);
                ProjectFile.AdditionalFiles.AddRange(postjsFolder.Files(true));
                GetOrMakeNativeProgram()
                    .Libraries.Add(c => c.Platform is WebGLPlatform,
                        jsFiles.Select(jsFile => new PostJsLibrary(jsFile)));
            }

            if (beeFolder.DirectoryExists())
                ProjectFile.AdditionalFiles.AddRange(beeFolder.Files("*.cs"));

            if (includeFolder.DirectoryExists())
                GetOrMakeNativeProgram().PublicIncludeDirectories.Add(includeFolder);
        }

        SupportFiles.Add(AllSourcePaths.SelectMany(p =>
            p.Files()
                .Where(f => f.HasExtension("jpg", "png", "wav", "mp3", "jpeg", "mp4", "webm", "ogg", "ttf", "json"))));
        
        Defines.Add(c => c.CodeGen == CSharpCodeGen.Debug || (c as DotsRuntimeCSharpProgramConfiguration)?.DotsConfiguration < DotsConfiguration.Release, "DEBUG");

        Defines.Add(c => ((DotsRuntimeCSharpProgramConfiguration) c).EnableUnityCollectionsChecks, "ENABLE_UNITY_COLLECTIONS_CHECKS");

        Defines.Add(
            c => (c as DotsRuntimeCSharpProgramConfiguration)?.ScriptingBackend == ScriptingBackend.TinyIl2cpp,
            "UNITY_DOTSPLAYER_IL2CPP");
        Defines.Add(c => (c as DotsRuntimeCSharpProgramConfiguration)?.ScriptingBackend == ScriptingBackend.Dotnet, "UNITY_DOTSPLAYER_DOTNET");
        Defines.Add(c => (c as DotsRuntimeCSharpProgramConfiguration)?.Defines ?? new List<string>());

        ProjectFile.RedirectMSBuildBuildTargetToBee = true;
        ProjectFile.AddCustomLinkRoot(MainSourcePath, ".");
        ProjectFile.RootNameSpace = "";
        
        DotsRuntimeCSharpProgramCustomizer.RunAllCustomizersOn(this);
    }

    protected virtual NPath DeterminePathForProjectFile()
    {
        return new NPath(FileName).ChangeExtension(".csproj");
    }

    public static bool DoesPackageSourceIndicateUserHasControlOverSource(string packageSource)
    {
        switch (packageSource)
        {
            case "NoPackage":
            case "Local":
            case "Embedded":
                return true;
            default:
                return false;
        }
    }

    internal NativeProgram GetOrMakeNativeProgram()
    {
        if (NativeProgram != null)
            return NativeProgram;

        var libname = "lib_"+new NPath(FileName).FileNameWithoutExtension.ToLower().Replace(".","_");
        NativeProgram = new NativeProgram(libname);
        
        NativeProgram.DynamicLinkerSettingsForMac().Add(c => c.WithInstallName(libname + ".dylib"));
        NativeProgram.DynamicLinkerSettingsForIos().Add(c => c.WithInstallName("@executable_path/Frameworks/" + libname + ".dylib"));
        NativeProgram.IncludeDirectories.Add(BuildProgram.BeeRootValue.Combine("cppsupport/include"));

        //lets always add a dummy cpp file, in case this nativeprogram is only used to carry other libraries
        NativeProgram.Sources.Add(BuildProgram.BeeRootValue.Combine("cppsupport/dummy.cpp"));

        NativeProgram.Defines.Add(c => c.Platform is WebGLPlatform, "UNITY_WEBGL=1");
        NativeProgram.Defines.Add(c => c.Platform is WindowsPlatform, "UNITY_WINDOWS=1");
        NativeProgram.Defines.Add(c => c.Platform is MacOSXPlatform, "UNITY_MACOSX=1");
        NativeProgram.Defines.Add(c => c.Platform is LinuxPlatform, "UNITY_LINUX=1");
        NativeProgram.Defines.Add(c => c.Platform is IosPlatform, "UNITY_IOS=1");
        NativeProgram.Defines.Add(c => c.Platform is AndroidPlatform, "UNITY_ANDROID=1");

        // sigh
        NativeProgram.Defines.Add("BUILD_" + MainSourcePath.FileName.ToUpper().Replace(".", "_") + "=1");

        NativeProgram.Defines.Add(c => c.CodeGen == CodeGen.Debug, "DEBUG=1");
        
        NativeProgram.Defines.Add("BINDGEM_DOTS=1");

        return NativeProgram;
    }

    protected virtual bool ShouldTargetTinyCorlib(CSharpProgramConfiguration config, DotsRuntimeCSharpProgram program)
    {
        return true;
    }
    
    public override DotNetAssembly SetupSpecificConfiguration(CSharpProgramConfiguration config)
    {
        EnsureNativeProgramLinksToReferences();
        
        var result = base.SetupSpecificConfiguration(config);

        return SetupNativeProgram(config, result);
    }

    protected virtual DotNetAssembly SetupNativeProgram(CSharpProgramConfiguration config, DotNetAssembly result)
    {
        var dotsConfig = (DotsRuntimeCSharpProgramConfiguration) config;

        var npc = dotsConfig.NativeProgramConfiguration;
        if (NativeProgram != null && NativeProgram.Sources.ForAny().Any())
        {
            BuiltNativeProgram setupSpecificConfiguration = NativeProgram.SetupSpecificConfiguration(npc,
                npc.ToolChain.DynamicLibraryFormat ?? npc.ToolChain.StaticLibraryFormat);
            result = result.WithDeployables(setupSpecificConfiguration);
        }

        return result;
    }

    private void EnsureNativeProgramLinksToReferences()
    {
        //todo: find a more elegant way than this..
        if (_doneEnsureNativeProgramLinksToReferences)
            return;
        _doneEnsureNativeProgramLinksToReferences = true;
        
        NativeProgram?.Libraries.Add(npc =>
        {
            var csharpConfig = ((DotsRuntimeNativeProgramConfiguration) npc).CSharpConfig;
            var dotsRuntimeCSharpPrograms = References.For(csharpConfig)
                .OfType<DotsRuntimeCSharpProgram>()
                .Where(p => p.IsSupportedFor(csharpConfig))
                .ToArray();
            return dotsRuntimeCSharpPrograms.Select(dcp => dcp.NativeProgram).Where(np => np != null)
                .Select(np => new NativeProgramAsLibrary(np) { BuildMode = NativeProgramLibraryBuildMode.Dynamic});
        });
    }


    protected class CustomProvideFiles : OneOrMoreFiles
    {
        public NPath SourcePath { get; }

        public CustomProvideFiles(NPath sourcePath) => SourcePath = sourcePath;

        public override IEnumerable<NPath> GetFiles()
        {
            var files = SourcePath.Files("*.cs",recurse:true);
            var beeDirs = SourcePath.Directories(true).Where(d => d.FileName == "bee~").ToList();
            var ignoreDirectories = SourcePath.Files("*.asm?ef", recurse: true)
                .Where(f => f.Parent != SourcePath)
                .Select(asmdef => asmdef.Parent)
                .Concat(beeDirs)
                .ToList();
            return files.Where(f => f.HasExtension("cs") && !ignoreDirectories.Any(f.IsChildOf));
        }

        public override IEnumerable<XElement> CustomMSBuildElements(NPath projectFileParentPath)
        {
            if (SourcePath != projectFileParentPath && !SourcePath.IsChildOf(projectFileParentPath)) 
                return null;

            var relative = SourcePath.RelativeTo(projectFileParentPath).ToString(SlashMode.Native);

            var prefix = relative == "." ? "" : $"{relative}\\";
            var ns = ProjectFileContentBuilder.DefaultNamespace;
            return new[]
            {
                new XElement(ns + "Compile", new XAttribute("Include", $@"{prefix}**\*.cs"),
                    new XAttribute("Exclude", $"{prefix}bee?\\**\\*.*"))
            };

        }
    }
}

public enum ScriptingBackend
{
    TinyIl2cpp,
    Dotnet
}

public sealed class DotsRuntimeCSharpProgramConfiguration : CSharpProgramConfiguration
{
    public DotsRuntimeNativeProgramConfiguration NativeProgramConfiguration { get; set; }

    public ScriptingBackend ScriptingBackend { get; }

    public DotsConfiguration DotsConfiguration { get; }

    public Platform Platform => NativeProgramConfiguration.ToolChain.Platform;
    
    public bool MultiThreadedJobs { get; private set; }
    
    public bool UseBurst { get; }

    private string _identifier { get; set; }

    public DotsRuntimeCSharpProgramConfiguration(
        CSharpCodeGen csharpCodegen,
        CodeGen cppCodegen,
        //The stevedore global manifest will override DownloadableCsc.Csc72 artifacts and use Csc73
        ToolChain nativeToolchain,
        ScriptingBackend scriptingBackend,
        string identifier,
        bool enableUnityCollectionsChecks,
        bool enableManagedDebugging,
        bool multiThreadedJobs,
        DotsConfiguration dotsConfiguration,
        bool useBurst,
        NativeProgramFormat executableFormat = null,
        IEnumerable<string> defines = null, 
        NPath finalOutputDirectory = null)
        : base(
            csharpCodegen,
            DownloadableCsc.Csc72,
            DebugFormat.PortablePdb,
            nativeToolchain.Architecture is IntelArchitecture ? nativeToolchain.Architecture : null)
    {
        NativeProgramConfiguration = new DotsRuntimeNativeProgramConfiguration(
            cppCodegen,
            nativeToolchain,
            identifier,
            this,
            executableFormat: executableFormat);
        _identifier = identifier;
        EnableUnityCollectionsChecks = enableUnityCollectionsChecks;
        DotsConfiguration = dotsConfiguration;
        MultiThreadedJobs = multiThreadedJobs;
        UseBurst = useBurst;
        EnableManagedDebugging = enableManagedDebugging;
        ScriptingBackend = scriptingBackend;
        Defines = defines?.ToList();
        FinalOutputDirectory = finalOutputDirectory;
    }


    public override string Identifier => _identifier;
    public bool EnableUnityCollectionsChecks { get; }
    public bool EnableManagedDebugging { get; }

    public DotsRuntimeCSharpProgramConfiguration WithMultiThreadedJobs(bool value) => MultiThreadedJobs == value ? this : With(c=>c.MultiThreadedJobs = value);
    public DotsRuntimeCSharpProgramConfiguration WithIdentifier(string value) => Identifier == value ? this : With(c=>c._identifier = value);

    private DotsRuntimeCSharpProgramConfiguration With(Action<DotsRuntimeCSharpProgramConfiguration> modifyCallback)
    {
        var copy = (DotsRuntimeCSharpProgramConfiguration) MemberwiseClone();
        modifyCallback(copy);
        return copy;
    }
    
    public List<string> Defines { get; set; }
    
    public NPath FinalOutputDirectory { get; set; }
}

public class DotsRuntimeNativeProgramConfiguration : NativeProgramConfiguration
{
    private NativeProgramFormat _executableFormat;
    public DotsRuntimeCSharpProgramConfiguration CSharpConfig { get; }
    public DotsRuntimeNativeProgramConfiguration(CodeGen codeGen, ToolChain toolChain, string identifier, DotsRuntimeCSharpProgramConfiguration cSharpConfig, NativeProgramFormat executableFormat = null) : base(codeGen, toolChain, false)
    {
        Identifier = identifier;
        CSharpConfig = cSharpConfig;
        _executableFormat = executableFormat;
    }

    public NativeProgramFormat ExecutableFormat => _executableFormat ?? base.ToolChain.ExecutableFormat;
    
    public override string Identifier { get; }
}
