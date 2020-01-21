using DotsBuildTargets;
using Newtonsoft.Json.Linq;
using NiceIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bee.Core;
using Unity.BuildSystem.CSharpSupport;
using Unity.BuildSystem.NativeProgramSupport;

public static class DotsConfigs
{
    private static Dictionary<string, List<DotsRuntimeCSharpProgramConfiguration>> PerConfigBuildSettings =
        new Dictionary<string, List<DotsRuntimeCSharpProgramConfiguration>>();

    public static Dictionary<string, List<DotsRuntimeCSharpProgramConfiguration>> MakeConfigs()
    {
        var platformList = DotsBuildSystemTargets;

        var settingsDir = new NPath("settings");
        
        if (settingsDir.Exists())
        {
            foreach (var settingsRelative in settingsDir.Files("*.json"))
            {
                var settingsFile = settingsRelative.MakeAbsolute();
                if (settingsFile.Exists())
                {
                    Backend.Current.RegisterFileInfluencingGraph(settingsFile);
                    var settingsObject = new FriendlyJObject {Content = JObject.Parse(settingsFile.ReadAllText())};

                    var id = settingsObject.GetString("PlatformTargetIdentifier");

                    var target = platformList.Single(t => t.Identifier == id);

                    if (!target.ToolChain.CanBuild)
                        continue;
                    
                    var targetShouldUseBurst = settingsObject.GetBool("UseBurst");

                    var dotsCfg = DotsConfigForSettings(settingsObject, out var codegen);
                    var enableUnityCollectionsChecks = dotsCfg != DotsConfiguration.Release;

                    /* dotnet reorders struct layout when there are managed components in the job struct,
                     * most notably DisposeSentinel. Mono does not. So disable burst on windows dotnet when
                     * collections checks are enabled. 
                     */
                    var canUseBurst = target.CanUseBurst &&
                                      !(target is DotsWindowsDotNetTarget &&
                                        target.ScriptingBackend == ScriptingBackend.Dotnet &&
                                        enableUnityCollectionsChecks);
                    if (!canUseBurst && targetShouldUseBurst)
                    {
                        Console.WriteLine(
                            "Warning: UseBurst specified, but target does not support burst yet. Not using burst.");
                        targetShouldUseBurst = false;
                    }

                    var mdb = settingsObject.GetBool("EnableManagedDebugging");

                    var rootAssembly = settingsObject.GetString("RootAssembly");
                    string finalOutputDir = null;
                    if (settingsObject.Content.TryGetValue("FinalOutputDirectory", out var finalOutputToken))
                        finalOutputDir = finalOutputToken.Value<string>();

                    var multithreading = settingsObject.GetBool("EnableMultiThreading");
                    var defines = new List<string>();

                    if (settingsObject.Content.TryGetValue("ScriptingDefines", out var definesJToken))
                        defines = ((JArray) definesJToken).Select(token => token.Value<string>()).ToList();

                    if (!PerConfigBuildSettings.ContainsKey(rootAssembly))
                        PerConfigBuildSettings[rootAssembly] = new List<DotsRuntimeCSharpProgramConfiguration>();

                    PerConfigBuildSettings[rootAssembly]
                        .Add(
                            new DotsRuntimeCSharpProgramConfiguration(codegen,
                                codegen == CSharpCodeGen.Debug ? CodeGen.Debug : CodeGen.Release,
                                target.ToolChain,
                                target.ScriptingBackend,
                                settingsFile.FileNameWithoutExtension,
                                enableUnityCollectionsChecks,
                                mdb,
                                multithreading,
                                dotsCfg,
                                targetShouldUseBurst,
                                target.CustomizeExecutableForSettings(settingsObject),
                                defines,
                                finalOutputDir));
                }
            }
        }

        return PerConfigBuildSettings;
    }

    public static DotsConfiguration DotsConfigForSettings(FriendlyJObject settingsObject, out CSharpCodeGen codegen)
    {
        DotsConfiguration dotsCfg;
        var codegenString = settingsObject.GetString("DotsConfig");
        switch (codegenString)
        {
            case "Debug":
                codegen = CSharpCodeGen.Debug;
                dotsCfg = DotsConfiguration.Debug;
                break;
            case "Develop":
                codegen = CSharpCodeGen.Release;
                dotsCfg = DotsConfiguration.Develop;
                break;
            case "Release":
                codegen = CSharpCodeGen.Release;
                dotsCfg = DotsConfiguration.Release;
                break;
            default:
                throw new ArgumentException(
                    $"Error: Unrecognized codegen {codegenString} in build json file. This is a bug.");
        }

        return dotsCfg;
    }


    private static List<DotsBuildSystemTarget> _dotsBuildSystemTargets;
    
    private static List<DotsBuildSystemTarget> DotsBuildSystemTargets
    {
        get
        {
            if (_dotsBuildSystemTargets != null)
                return _dotsBuildSystemTargets;

            var platformList = new List<DotsBuildSystemTarget>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract)
                        continue;

                    if (!type.IsSubclassOf(typeof(DotsBuildSystemTarget)))
                        continue;

                    platformList.Add((DotsBuildSystemTarget) Activator.CreateInstance(type));
                }
            }

            _dotsBuildSystemTargets = platformList;

            return _dotsBuildSystemTargets;
        }
    }

    private static Lazy<DotsRuntimeCSharpProgramConfiguration> _multiThreadedJobsTestConfig =
        new Lazy<DotsRuntimeCSharpProgramConfiguration>(() =>
            HostDotnet.WithMultiThreadedJobs(true).WithIdentifier(HostDotnet.Identifier + "-mt"));

    public static DotsRuntimeCSharpProgramConfiguration MultithreadedJobsTestConfig =>
        _multiThreadedJobsTestConfig.Value;

    public static DotsRuntimeCSharpProgramConfiguration HostDotnet
    {
        get
        {
            var target = DotsBuildSystemTargets.First(c =>
                c.ScriptingBackend == ScriptingBackend.Dotnet &&
                c.ToolChain.Platform.GetType() == Platform.HostPlatform.GetType());
            return new DotsRuntimeCSharpProgramConfiguration(CSharpCodeGen.Release,
                CodeGen.Release,
                target.ToolChain,
                ScriptingBackend.Dotnet,
                "HostDotNet",
                true,
                false,
                false,
                DotsConfiguration.Develop,
                true);
        }
    }
}