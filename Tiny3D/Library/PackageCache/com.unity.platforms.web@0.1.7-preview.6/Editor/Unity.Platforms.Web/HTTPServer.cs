using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;

namespace Unity.Platforms.Web
{
    internal class HTTPServer : BasicServer
    {
        public static HTTPServer Instance { get; private set; }
        protected override string[] ShellArgs =>
            new []
            {
                $"-p {Port}",
                $"-w {Process.GetCurrentProcess().Id}",
                $"-c \"{ContentDir.Trim('\"')}\"",
                $"-i \"{IndexFile.Trim('\"')}\"",
                $"-t \"{Path.GetFullPath(".").Trim('\"')}\""
            };

        public override Uri URL => new UriBuilder("http", LocalIP, Port).Uri;

        public Uri LocalURL => Listening ? new UriBuilder("http", "localhost", Port).Uri : new Uri(Path.Combine(ContentDir, "index.html"));
        public string BuildTimeStamp
        {
            get => EditorPrefs.GetString($"Unity.Tiny.{Name}.BuildTimeStamp", null);
            set => EditorPrefs.SetString($"Unity.Tiny.{Name}.BuildTimeStamp", value);
        }
        private string ContentDir { get; set; }
        private string IndexFile { get; set; }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
#if false
            Instance = new HTTPServer();

            var applicationType = Assembly.Load("Unity.Editor").GetType("Unity.Editor.Application");
            var add_EndAuthoringProjectMethod = applicationType.GetMethod("add_EndAuthoringProject", BindingFlags.Static | BindingFlags.Public);
            Action<Project> onEndAuthoringProject = (project) => { Instance.Close(); };
            add_EndAuthoringProjectMethod.Invoke(null, new object[] { onEndAuthoringProject });
#endif
        }

        private HTTPServer() : base("HTTPServer")
        {
        }

        private void Host(string contentDir, string indexFile, int port)
        {
            Close();

            ContentDir = contentDir;
            IndexFile = indexFile;
            BuildTimeStamp = DateTime.Now.ToString("d MMM yyyy HH:mm:ss");
            if (Listen(port))
            {
                UnityEngine.Debug.Log($"DOTS project content hosted at {URL.AbsoluteUri}");
            }
        }

        public bool HostAndOpen(string contentDir, string indexFile, int port)
        {
            if (port == 0 || string.IsNullOrEmpty(contentDir) || !Directory.Exists(contentDir))
            {
                return false;
            }

            var progressBarScopeType = Assembly.Load("Unity.Editor").GetType("Unity.Editor.Utilities.ProgressBarScope");
            using ((IDisposable)Activator.CreateInstance(progressBarScopeType, new object[] { "HTTP Server", "Starting...", float.MinValue }))
            {
                // Get hosted URL from content directory
                Host(contentDir, indexFile, port);
                UnityEngine.Application.OpenURL(LocalURL.AbsoluteUri);
            }

            return true;
        }
    }
}
