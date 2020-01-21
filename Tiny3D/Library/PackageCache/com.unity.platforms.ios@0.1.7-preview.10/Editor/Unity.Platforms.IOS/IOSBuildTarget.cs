using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace Unity.Platforms.iOS
{
    public class iOSBuildTarget : BuildTarget
    {
        public override bool CanBuild => UnityEngine.Application.platform == UnityEngine.RuntimePlatform.OSXEditor;
        public override string DisplayName => "iOS";
        public override string BeeTargetName => "ios";
        public override string ExecutableExtension => "";
        public override string UnityPlatformName => nameof(UnityEditor.BuildTarget.iOS);

        public override bool Run(FileInfo buildTarget)
        {
			UnityEditor.EditorUtility.RevealInFinder(buildTarget.FullName);
            return true;
		}

        public override ShellProcessOutput RunTestMode(string exeName, string workingDirPath, int timeout)
        {
            return new ShellProcessOutput
            {
                Succeeded = false,
                ExitCode = 0,
                FullOutput = "Test mode is not supported for iOS yet"
            };
        }
    }
}
