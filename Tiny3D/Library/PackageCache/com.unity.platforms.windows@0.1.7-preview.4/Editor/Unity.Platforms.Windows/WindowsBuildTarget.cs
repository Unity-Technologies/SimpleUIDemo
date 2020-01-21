using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace Unity.Platforms.Windows
{
    public abstract class WindowsBuildTarget : BuildTarget
    {
        public override bool CanBuild => UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor;
        public override string ExecutableExtension => ".exe";
        public override string UnityPlatformName => nameof(UnityEditor.BuildTarget.StandaloneWindows64);

        public override bool Run(FileInfo buildTarget)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = buildTarget.FullName;
            startInfo.WorkingDirectory = buildTarget.Directory.FullName;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            var process = new Process();
            process.StartInfo = startInfo;
            process.OutputDataReceived += (_, args) =>
            {
                if(args.Data != null)
                    Debug.Log(args.Data);
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if(args.Data != null)
                    Debug.LogError(args.Data);
            };

            var success = process.Start();
            if (!success)
                return false;

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return true;
        }

        public override ShellProcessOutput RunTestMode(string exeName, string workingDirPath, int timeout)
        {
            var args = new string[] { };
            var workingDir = new DirectoryInfo(workingDirPath);
            var executable = $"{workingDirPath}/{exeName}.exe";

            var shellArgs = new ShellProcessArgs
            {
                Executable = executable,
                Arguments = args,
                WorkingDirectory = workingDir,
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

    class DotNetWindowsBuildTarget : WindowsBuildTarget
    {
#if UNITY_EDITOR_WIN
        protected override bool IsDefaultBuildTarget => true;
#endif

        public override string DisplayName => "Windows .NET";
        public override string BeeTargetName => "windows-dotnet";
    }

    class IL2CPPWindowsBuildTarget : WindowsBuildTarget
    {
        public override string DisplayName => "Windows IL2CPP";
        public override string BeeTargetName => "windows-il2cpp";
    }
}
