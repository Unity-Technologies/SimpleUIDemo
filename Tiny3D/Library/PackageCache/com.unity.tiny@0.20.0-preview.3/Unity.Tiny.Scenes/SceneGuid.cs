using System;
using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    //[NonSerializedForPersistence]
    //[HideInInspector, NonExported]
    public struct SceneGuid : ISharedComponentData, IEquatable<SceneGuid>
    {
        public Guid Guid;

        public bool Equals(SceneGuid other)
        {
            return Guid.Equals(other.Guid);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SceneGuid other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public static bool operator ==(SceneGuid left, SceneGuid right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SceneGuid left, SceneGuid right)
        {
            return !left.Equals(right);
        }
    }
}
