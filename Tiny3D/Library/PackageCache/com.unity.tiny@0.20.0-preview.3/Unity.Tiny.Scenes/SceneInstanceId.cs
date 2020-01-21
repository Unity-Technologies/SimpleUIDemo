using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    //[NonSerializedForPersistence]
    //[HideInInspector, NonExported]
    public struct SceneInstanceId : ISharedComponentData
    {
        public uint InstanceId;
    }
}
