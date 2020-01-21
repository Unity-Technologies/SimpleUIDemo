using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    /// <summary>
    /// Stores the currently active scene in the hierarchy
    /// </summary>
    //[HideInInspector, NonExported, NonSerializedForPersistence]
    public struct ActiveScene : IComponentData
    {
        public Scene Scene;
    }
}
