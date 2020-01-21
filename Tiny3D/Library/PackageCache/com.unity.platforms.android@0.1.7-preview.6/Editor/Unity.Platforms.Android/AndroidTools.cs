using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Unity.Build.Android
{
    internal class AndroidTools
    {
        private static Type AndroidExternalToolsSettings { get; set; }

        static AndroidTools()
        {
            AndroidExternalToolsSettings =
                (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                 from type in assembly.GetTypes()
                 where type.Name == "AndroidExternalToolsSettings"
                 select type).FirstOrDefault();
        }

        private static string GetProperty(string name)
        {
            if (AndroidExternalToolsSettings == default(Type))
            {
                return null;
            }
            var property = AndroidExternalToolsSettings.GetProperty(name);
            return property?.GetValue(null) as string;
        }

        private static string AdbName
        {
            get
            {
#if UNITY_EDITOR_WIN
                return "adb.exe";
#elif UNITY_EDITOR_OSX
                return "adb";
#else
                return "adb";
#endif
            }
        }

        public static string SdkRootPath => GetProperty("sdkRootPath");
        public static string NdkRootPath => GetProperty("ndkRootPath");
        public static string JdkRootPath => GetProperty("jdkRootPath");
        public static string GradlePath => GetProperty("gradlePath");
        
        public static string AdbPath
        { 
            get 
            {
                if (string.IsNullOrEmpty(SdkRootPath))
                {
                    throw new Exception("ADB is not found");
                }
                return Path.Combine(SdkRootPath, "platform-tools", AdbName);
            }
        }
    }
}
