using System.Diagnostics;
using Unity.Build;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.Entities.Runtime.Build
{
    public static class GenerateDotsSolution
    {
        const string k_Title = "Generating DOTS Runtime C# Project";

        [MenuItem("Assets/Open DOTS Runtime C# Project")]
        static void GenerateSolution()
        {
            using (var progress = new BuildProgress(k_Title, "Please wait..."))
            {
                var result = BeeTools.Run("ProjectFiles", new DotsRuntimeBuildProfile().BeeRootDirectory, progress);
                if (!result.Succeeded)
                {
                    UnityEngine.Debug.LogError($"{k_Title} failed.\n{result.Error}");
                    return;
                }

                var scriptEditor = ScriptEditorUtility.GetExternalScriptEditor();
                var projectPath = new NPath(UnityEngine.Application.dataPath).Parent;
                var pi = new ProcessStartInfo();
                pi.FileName = scriptEditor;
                pi.Arguments = $"{projectPath.Combine(projectPath.FileName + "-Dots.sln").InQuotes()}";
                var proc = new Process();
                proc.StartInfo = pi;
                proc.Start();
            }
        }
    }
}
