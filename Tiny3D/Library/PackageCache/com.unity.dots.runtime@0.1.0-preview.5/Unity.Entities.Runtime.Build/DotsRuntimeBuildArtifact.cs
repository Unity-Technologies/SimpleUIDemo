using System.IO;
using Unity.Build;
using Unity.Properties;

namespace Unity.Entities.Runtime.Build
{
    internal class DotsRuntimeBuildArtifact : IBuildArtifact
    {
        [Property] public FileInfo OutputTargetFile { get; set; }
    }
}
