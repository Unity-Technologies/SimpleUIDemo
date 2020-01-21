using System;
using Unity.Entities.Runtime.Hashing;

namespace Unity.Tiny.Scenes
{
    public static class AssetsScene
    {
        public static readonly string Path = "Assets";
        public static readonly Guid Guid = GuidUtility.NewGuid(Path);
    }
}
