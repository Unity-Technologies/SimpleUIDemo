using System;
using System.IO;
using Unity.Build;

namespace Unity.Entities.Runtime.Build
{
    internal class DotsRuntimeRunStep : RunStep
    {
        public override bool CanRun(BuildSettings settings, out string reason)
        {
            var artifact = BuildArtifacts.GetBuildArtifact<DotsRuntimeBuildArtifact>(settings);
            if (artifact == null)
            {
                reason = $"Could not retrieve build artifact '{nameof(DotsRuntimeBuildArtifact)}'.";
                return false;
            }

            if (artifact.OutputTargetFile == null)
            {
                reason = $"{nameof(DotsRuntimeBuildArtifact.OutputTargetFile)} is null.";
                return false;
            }

            if (!File.Exists(artifact.OutputTargetFile.FullName))
            {
                reason = $"Output target file '{artifact.OutputTargetFile.FullName}' not found.";
                return false;
            }

            if (!settings.TryGetComponent<DotsRuntimeBuildProfile>(out var profile))
            {
                reason = $"Could not retrieve component '{nameof(DotsRuntimeBuildProfile)}'.";
                return false;
            }

            if (profile.Target == null)
            {
                reason = $"{nameof(DotsRuntimeBuildProfile)} target is null.";
                return false;
            }

            reason = null;
            return true;
        }

        public override RunStepResult Start(BuildSettings settings)
        {
            var artifact = BuildArtifacts.GetBuildArtifact<DotsRuntimeBuildArtifact>(settings);
            var profile = settings.GetComponent<DotsRuntimeBuildProfile>();

            if (!profile.Target.Run(artifact.OutputTargetFile))
            {
                return RunStepResult.Failure(settings, this, $"Failed to start build target {profile.Target.DisplayName} at '{artifact.OutputTargetFile.FullName}'.");
            }

            //@TODO: BuildTarget.Run should return the process, so we can store it in DotsRuntimeRunInstance
            return RunStepResult.Success(settings, this, new DotsRuntimeRunInstance());
        }
    }
}
