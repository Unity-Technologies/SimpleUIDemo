using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    /// <summary>
    /// Stores a list of scenes.
    /// </summary>
    //[HideInInspector]
    public struct Scenes : IBufferElementData
    {
        public SceneReference SceneReference;
    }
}
