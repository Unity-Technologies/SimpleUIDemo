using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Unity.Build;
using Unity.Properties;

namespace Unity.Platforms.Web
{
    public abstract class WebBuildTarget : BuildTarget
    {
        public override bool CanBuild => true;
        public override string UnityPlatformName => "WebGL";
        public override string ExecutableExtension => ".html";

        public override bool Run(FileInfo buildTarget)
        {
			UnityEditor.EditorUtility.RevealInFinder(buildTarget.FullName);
            return true;

			// Currently we don't have a server to run
			//return HTTPServer.Instance.HostAndOpen(
            //    buildTarget.Directory.FullName,
            //    buildTarget.Name,
            //    19050);
        }

        public override ShellProcessOutput RunTestMode(string exeName, string workingDirPath, int timeout)
        {
            return new ShellProcessOutput
            {
                Succeeded = false,
                ExitCode = 0,
                FullOutput = "Test mode is not supported for web yet"
            };
        }
    }

    class AsmJSBuildTarget : WebBuildTarget
    {
        public override string DisplayName => "Web (AsmJS)";
        public override string BeeTargetName => "asmjs";
    }

    class WasmBuildTarget : WebBuildTarget
    {
        public override string DisplayName => "Web (Wasm)";
        public override string BeeTargetName => "wasm";
    }

    public class EmscriptenSettings : IBuildSettingsComponent, IDotsRuntimeBuildModifier
    {
        [Property] public List<string> EmccArgs = new List<string>();
        public void Modify(JObject settingsJObject)
        {
            var dict = new JObject();
            foreach (var arg in EmccArgs)
            {
                var separated = arg.Split('=');
                dict[separated[0]] = separated[1];
            }

            settingsJObject["EmscriptenSettings"] = dict;
        }
    }
}
