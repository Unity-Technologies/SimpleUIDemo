using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Build;
using Unity.Platforms;

namespace Unity.Entities.Runtime.Build
{
    internal static class BeeTools
    {
        // Group 1: progress numerator
        // Group 2: progress denominator
        // Group 3: progress description
        static readonly Regex BeeProgressRegex = new Regex(@"\[(?:(\s*\d+)/(\s*\d+)|\s*\w*)\s*(?:\w*)\]\s*(.*)", RegexOptions.Compiled);

        struct BeeProgressInfo
        {
            public float Progress;
            public string Info;
            public string FullInfo;
            public bool IsDone;
            public int ExitCode;
            public Process Process;
        }

        static IEnumerator<BeeProgressInfo> Run(string arguments, StringBuilder command, StringBuilder output, DirectoryInfo workingDirectory = null)
        {
            var beeExe = Path.GetFullPath($"{Constants.DotsRuntimePackagePath}/bee~/bee.exe");
            var executable = beeExe;
            arguments = "--no-colors " + arguments;

#if !UNITY_EDITOR_WIN
            arguments = "\"" + executable + "\" " + arguments;
            executable = Path.Combine(UnityEditor.EditorApplication.applicationContentsPath,
                "MonoBleedingEdge/bin/mono");
#endif

            command.Append(executable);
            command.Append(" ");
            command.Append(arguments);

            var progressInfo = new BeeProgressInfo()
            {
                Progress = 0.0f,
                Info = null
            };

            void ProgressHandler(object sender, DataReceivedEventArgs args)
            {
                if (args.Data != null)
                {
                    lock (output)
                    {
                        output.AppendLine(args.Data);
                    }
                }

                var msg = args.Data;
                if (string.IsNullOrWhiteSpace(msg))
                {
                    return;
                }

                progressInfo.FullInfo = msg;

                var match = BeeProgressRegex.Match(msg);
                if (match.Success)
                {
                    var num = match.Groups[1].Value;
                    var den = match.Groups[2].Value;
                    if (int.TryParse(num, out var numInt) && int.TryParse(den, out var denInt))
                    {
                        progressInfo.Progress = (float)numInt / denInt;
                    }
                    progressInfo.Info = match.Groups[3].Value;
                }
                else
                {
                    progressInfo.Progress = float.MinValue;
                    progressInfo.Info = null;
                }
            }

            var config = new ShellProcessArgs()
            {
                Executable = executable,
                Arguments = new[] { arguments },
                WorkingDirectory = workingDirectory,
#if !UNITY_EDITOR_WIN
                // bee requires external programs to perform build actions
                EnvironmentVariables = new Dictionary<string, string>() { {"PATH", string.Join(":",
                    Path.Combine(UnityEditor.EditorApplication.applicationContentsPath,
                        "MonoBleedingEdge/bin"),
                    "/bin",
                    "/usr/bin",
                    "/usr/local/bin")} },
#else
                EnvironmentVariables = null,
#endif
                OutputDataReceived = ProgressHandler,
                ErrorDataReceived = ProgressHandler
            };

            var bee = Shell.RunAsync(config);
            progressInfo.Process = bee;

            yield return progressInfo;

            const int maxBuildTimeInMs = 30 * 60 * 1000; // 30 minutes

            var statusEnum = Shell.WaitForProcess(bee, maxBuildTimeInMs, config.MaxIdleKillIsAnError);
            while (statusEnum.MoveNext())
            {
                yield return progressInfo;
            }

            progressInfo.Progress = 1.0f;
            progressInfo.IsDone = true;
            progressInfo.ExitCode = bee.ExitCode;
            progressInfo.Info = "Build completed";
            yield return progressInfo;
        }

        public class BeeRunResult
        {
            public int ExitCode { get; }
            public bool Succeeded => ExitCode == 0;
            public bool Failed => !Succeeded;
            public string Command { get; }
            public string Output { get; }
            public string Error => Failed ? Output.TrimStart("##### Output").TrimStart('\n', '\r') : string.Empty;

            public BeeRunResult(int exitCode, string command, string output)
            {
                ExitCode = exitCode;
                Command = command;
                Output = output;
            }
        }

        public static BeeRunResult Run(string arguments, DirectoryInfo workingDirectory, BuildProgress progress = null)
        {
            var command = new StringBuilder();
            var output = new StringBuilder();

            var beeProgressInfo = Run(arguments, command, output, workingDirectory);
            while (beeProgressInfo.MoveNext())
            {
                if (progress?.Update(beeProgressInfo.Current.Info, beeProgressInfo.Current.Progress) ?? false)
                {
                    beeProgressInfo.Current.Process.Kill();
                    break;
                }
            }

            return new BeeRunResult(beeProgressInfo.Current.ExitCode, command.ToString(), output.ToString());
        }

        static string TrimStart(this string str, string value)
        {
            var index = str.IndexOf(value);
            return index >= 0 ? str.Substring(index + value.Length) : str;
        }
    }
}
