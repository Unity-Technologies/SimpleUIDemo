using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace Unity.Platforms.MacOS
{
    public abstract class MacOSBuildTarget : BuildTarget
    {
        public override bool CanBuild => UnityEngine.Application.platform == UnityEngine.RuntimePlatform.OSXEditor;
        public override string UnityPlatformName => nameof(UnityEditor.BuildTarget.StandaloneOSX);
    }

    class DotNetMacOSBuildTarget : MacOSBuildTarget
    {
#if UNITY_EDITOR_OSX
        protected override bool IsDefaultBuildTarget => true;
#endif

        public override string DisplayName => "macOS .NET";
        public override string BeeTargetName => "macos-dotnet";
        public override string ExecutableExtension => ".exe";

        public override bool Run(FileInfo buildTarget)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.Arguments = $"\"{buildTarget.FullName.Trim('\"')}\"";
            startInfo.FileName = Path.GetFullPath(Path.Combine(UnityEditor.EditorApplication.applicationContentsPath, "MonoBleedingEdge", "bin", "mono"));
            startInfo.WorkingDirectory = buildTarget.Directory.FullName;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (_, args) => Debug.Log(args.Data);
            process.ErrorDataReceived += (_, args) => Debug.LogError(args.Data);
            
            var success = process.Start();
            if (!success)
                return false;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return true;
        }

        public override ShellProcessOutput RunTestMode(string exeName, string workingDirPath, int timeout)
        {
            var shellArgs = new ShellProcessArgs
            {
                Executable = Path.GetFullPath(Path.Combine(UnityEditor.EditorApplication.applicationContentsPath, "MonoBleedingEdge", "bin", "mono")),
                Arguments = new [] { $"\"{workingDirPath}/{exeName}{ExecutableExtension}\"" },
                WorkingDirectory = new DirectoryInfo(workingDirPath),
                ThrowOnError = false
            };

            // samples should be killed on timeout
            if (timeout > 0)
            {
                shellArgs.MaxIdleTimeInMilliseconds = timeout;
                shellArgs.MaxIdleKillIsAnError = false;
            }

            return Shell.Run(shellArgs);
        }

    }

    class IL2CPPMacOSBuildTarget : MacOSBuildTarget
    {
        public override string DisplayName => "macOS IL2CPP";
        public override string BeeTargetName => "macos-il2cpp";
        public override string ExecutableExtension => string.Empty;

        public override bool Run(FileInfo buildTarget)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = buildTarget.FullName.Trim('.');
            startInfo.WorkingDirectory = buildTarget.Directory.FullName;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (_, args) => Debug.Log(args.Data);
            process.ErrorDataReceived += (_, args) => Debug.LogError(args.Data);
            
            var success = process.Start();
            if (!success)
                return false;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return true;
        }

        public override ShellProcessOutput RunTestMode(string exeName, string workingDirPath, int timeout)
        {
            var shellArgs = new ShellProcessArgs
            {
                Executable = $"{workingDirPath}/{exeName}{ExecutableExtension}",
                Arguments = new string [] {},
                WorkingDirectory = new DirectoryInfo(workingDirPath),
                ThrowOnError = false
            };

            // samples should be killed on timeout
            if (timeout > 0)
            {
                shellArgs.MaxIdleTimeInMilliseconds = timeout;
                shellArgs.MaxIdleKillIsAnError = false;
            }

            return Shell.Run(shellArgs);
        }
    }
}
