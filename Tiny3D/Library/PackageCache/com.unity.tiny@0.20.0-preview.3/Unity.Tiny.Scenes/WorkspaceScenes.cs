using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    /// <summary>
    /// Stores a list of scenes that are currently open in the hierarchy.
    /// </summary>
    //[HideInInspector, NonExported, NonSerializedForPersistence]
    public struct WorkspaceScenes : IBufferElementData
    {
        public Scene Scene;
        public int ChangeVersion;
    }
}
