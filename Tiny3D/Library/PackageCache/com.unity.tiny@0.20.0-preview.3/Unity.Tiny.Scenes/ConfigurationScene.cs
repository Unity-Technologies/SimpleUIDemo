using System;
using Unity.Entities.Runtime.Hashing;

namespace Unity.Tiny.Scenes
{
    public static class ConfigurationScene
    {
        public static readonly string Path = "Configuration";
        public static readonly Guid Guid = GuidUtility.NewGuid(Path);
    }
}
