using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using Unity.Build;
using Unity.Build.Common;
using Unity.Build.Internals;
/*
 * 11/15/2019
 * We are temporarily using Json.NET while we wait for the new com.unity.serialization package release,
 * which will offer similar functionality.
 */
using Unity.Platforms;

namespace Unity.Entities.Runtime.Build
{
    [BuildStep(description = k_Description, category = "DOTS")]
    sealed internal class BuildStepGenerateBeeFiles : BuildStep
    {
        const string k_Description = "Generate Bee Files";

        public override string Description => k_Description;

        public override Type[] RequiredComponents => new[]
        {
            typeof(DotsRuntimeBuildProfile)
        };

        public override Type[] OptionalComponents => new[]
        {
            typeof(OutputBuildDirectory),
            typeof(DotsRuntimeScriptingDefines),
            typeof(IDotsRuntimeBuildModifier)
        };

        public override BuildStepResult RunBuildStep(BuildContext context)
        {
            var manifest = context.BuildManifest;
            var profile = GetRequiredComponent<DotsRuntimeBuildProfile>(context);
            var outputDir = profile.BeeRootDirectory;

            var buildSettingsJObject = new JObject();

            BuildProgramDataFileWriter.WriteAll(outputDir.FullName);

            if (HasOptionalComponent<DotsRuntimeScriptingDefines>(context))
                buildSettingsJObject["ScriptingDefines"] = new JArray(GetOptionalComponent<DotsRuntimeScriptingDefines>(context).ScriptingDefines);

            buildSettingsJObject["PlatformTargetIdentifier"] = profile.Target.BeeTargetName;
            buildSettingsJObject["UseBurst"] = profile.EnableBurst;
            buildSettingsJObject["EnableManagedDebugging"] = profile.EnableManagedDebugging;
            buildSettingsJObject["RootAssembly"] = profile.RootAssembly.name;
            buildSettingsJObject["EnableMultiThreading"] = profile.EnableMultiThreading;
            buildSettingsJObject["FinalOutputDirectory"] = this.GetOutputBuildDirectory(context);
            buildSettingsJObject["DotsConfig"] = profile.Configuration.ToString();

            var buildSettings = BuildContextInternals.GetBuildSettings(context);
            //web is broken until we can get all components that modify a particular interface
            foreach (var component in BuildSettingsInternals.GetComponents<IDotsRuntimeBuildModifier>(buildSettings))
            {
                component.Modify(buildSettingsJObject);
            }

            var settingsDir = new NPath(outputDir.FullName).Combine("settings");
            settingsDir.Combine($"{buildSettings.name}.json")
                .UpdateAllText(buildSettingsJObject.ToString());

            WriteBeeExportManifestFile(profile, manifest);

            profile.Target.WriteBeeConfigFile(profile.BeeRootDirectory.ToString());

            return Success();
        }

        private void WriteBeeExportManifestFile(DotsRuntimeBuildProfile profile, BuildManifest manifest)
        {
            if (!profile.ShouldWriteDataFiles)
            {
                return;
            }

            var file = profile.StagingDirectory.GetFile("export.manifest");
            file.UpdateAllLines(manifest.ExportedFiles.Select(x => x.FullName).ToArray());
        }
    }
}
