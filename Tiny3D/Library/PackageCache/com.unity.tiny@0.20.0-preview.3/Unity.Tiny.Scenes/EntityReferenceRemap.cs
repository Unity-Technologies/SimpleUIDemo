using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    //[NonSerializedForPersistence]
    //[HideInInspector]
    public struct EntityReferenceRemap : IBufferElementData
    {
        public EntityGuid Guid;
        public ulong TypeHash;
        public int Offset;
    }
}
