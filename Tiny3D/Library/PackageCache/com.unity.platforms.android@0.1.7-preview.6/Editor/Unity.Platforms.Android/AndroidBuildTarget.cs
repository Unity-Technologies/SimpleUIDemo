using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Unity.Build.Android;

namespace Unity.Platforms.Android
{
    public class AndroidBuildTarget : BuildTarget
    {
        public override bool CanBuild => true;
        public override string DisplayName => "Android";
        public override string BeeTargetName => "android_armv7";
        public override string ExecutableExtension => ".apk";
        public override string UnityPlatformName => nameof(UnityEditor.BuildTarget.Android);

        private ShellProcessOutput InstallApp(string adbPath, string apkName, string buildDir)
        {
            return Shell.Run(new ShellProcessArgs()
            {
                ThrowOnError = false,
                Executable = adbPath,
                Arguments = new string[] { "install", "-r", apkName },
                WorkingDirectory = new DirectoryInfo(buildDir)
            });
        }

        private ShellProcessOutput LaunchApp(string adbPath, string name, string buildDir)
        {
            return Shell.Run(new ShellProcessArgs()
            {
                ThrowOnError = false,
                Executable = adbPath,
                Arguments = new string[] {
                        "shell", "am", "start",
                        "-a", "android.intent.action.MAIN",
                        "-c", "android.intent.category.LAUNCHER",
                        "-f", "0x10200000",
                        "-S",
                        "-n", $"com.unity3d.{name}/com.unity3d.tinyplayer.UnityTinyActivity"
                },
                WorkingDirectory = new DirectoryInfo(buildDir)
            });
        }

        public override bool Run(FileInfo buildTarget)
        {
            var buildDir = buildTarget.Directory.FullName;
            var adbPath = AndroidTools.AdbPath;
            var result = InstallApp(adbPath, buildTarget.FullName, buildDir);
            if (!result.FullOutput.Contains("Success"))
            {
                throw new Exception($"Cannot install APK : {result.FullOutput}");
            }
            var name = Path.GetFileNameWithoutExtension(buildTarget.Name).ToLower();
            result = LaunchApp(adbPath, name, buildDir);
            if (result.Succeeded)
            {
                return true;
            }
            else
            {
                throw new Exception($"Cannot launch APK : {result.FullOutput}");
            }
        }

        public override ShellProcessOutput RunTestMode(string exeName, string workingDirPath, int timeout)
        {
            ShellProcessOutput output;
            var adbPath = AndroidTools.AdbPath;

            var executable = $"{workingDirPath}/{exeName}{ExecutableExtension}";
            output = InstallApp(adbPath, executable, workingDirPath);
            if (!output.FullOutput.Contains("Success"))
            {
                return output;
            }

            // clear logcat
            Shell.Run(new ShellProcessArgs()
            {
                ThrowOnError = false,
                Executable = adbPath,
                Arguments = new string[] {
                        "logcat", "-c"
                },
                WorkingDirectory = new DirectoryInfo(workingDirPath)
            });

            var name = exeName.ToLower();
            output = LaunchApp(adbPath, name, workingDirPath);

            System.Threading.Thread.Sleep(timeout == 0 ? 2000 : timeout); // to kill process anyway,
                                                                          // should be rewritten to support tests which quits after execution

            // killing on timeout
            Shell.Run(new ShellProcessArgs()
            {
                ThrowOnError = false,
                Executable = adbPath,
                Arguments = new string[] {
                        "shell", "am", "force-stop",
                        $"com.unity3d.{name}"
                },
                WorkingDirectory = new DirectoryInfo(workingDirPath)
            });

            // get logcat
            output = Shell.Run(new ShellProcessArgs()
            {
                ThrowOnError = false,
                Executable = adbPath,
                Arguments = new string[] {
                        "logcat", "-d"
                },
                WorkingDirectory = new DirectoryInfo(workingDirPath)
            });
            if (timeout == 0) // non-sample test, TODO invent something better
            {
                output.Succeeded = output.FullOutput.Contains("Test suite: SUCCESS");
            }
            return output;
        }

        private struct AndroidConfig
        {
            public string JavaPath;
            public string SdkPath;
            public string NdkPath;
            public string GradlePath;
        }

        public override void WriteBeeConfigFile(string path)
        {
            if (string.IsNullOrEmpty(AndroidTools.SdkRootPath))
            {
                throw new Exception("Couldn't find Android SDK. Please set Android SDK path in editor preferences window.");
            }
            if (string.IsNullOrEmpty(AndroidTools.JdkRootPath))
            {
                throw new Exception("Couldn't find JDK. Please set JDK path in editor preferences window.");
            }
            if (string.IsNullOrEmpty(AndroidTools.GradlePath))
            {
                throw new Exception( "Couldn't find Gradle. Please set Gradle path in editor preferences window.");
            }
            if (string.IsNullOrEmpty(AndroidTools.NdkRootPath))
            {
                throw new Exception( "Couldn't find Android NDK. Please set Android NDK path in editor preferences window.");
            }
            File.WriteAllText(Path.Combine(path, "androidsettings.json"),
                EditorJsonUtility.ToJson(new AndroidConfig()
                {
                    JavaPath = AndroidTools.JdkRootPath,
                    SdkPath = AndroidTools.SdkRootPath,
                    NdkPath = AndroidTools.NdkRootPath,
                    GradlePath = AndroidTools.GradlePath
                })
            );
        }
    }
}
