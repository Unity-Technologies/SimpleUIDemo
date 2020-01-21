using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    /// <summary>
    /// Stores a list of scenes that will be automatically loaded at boot time.
    /// </summary>
    //[HideInInspector]
    public struct StartupScenes : IBufferElementData
    {
        public SceneReference SceneReference;
    }
}
