using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    /// <summary>
    /// A scene represents a grouped collection of entities within the world.
    /// </summary>
    public readonly struct Scene : IEquatable<Scene>
    {
        public static readonly Scene Null = new Scene();

        public readonly SceneGuid SceneGuid;
        public readonly SceneInstanceId SceneInstanceId;

        public Scene(SceneGuid sceneGuid, SceneInstanceId sceneInstanceId)
        {
            SceneGuid = sceneGuid;
            SceneInstanceId = sceneInstanceId;
        }

        public Scene(Guid sceneGuid, uint sceneInstanceId)
        {
            SceneGuid = new SceneGuid { Guid = sceneGuid };
            SceneInstanceId = new SceneInstanceId { InstanceId = sceneInstanceId };
        }

        /// <summary>
        /// Returns the set of all entities for the scene.
        /// </summary>
        public NativeArray<Entity> ToEntityArray(EntityManager entityManager, Allocator allocator)
        {
            return GetSceneEntityQueryRO(entityManager).ToEntityArray(allocator);
        }

        /// <summary>
        /// Sets the given entity to be part of the scene.
        /// </summary>
        /// <param name="entityManager">The entity manager to operate on.</param>
        /// <param name="entity">The entity to add.</param>
        public void AddEntityReference(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<SceneGuid>(entity))
            {
                entityManager.SetSharedComponentData(entity, SceneGuid);
            }
            else
            {
                entityManager.AddSharedComponentData(entity, SceneGuid);
            }

            if (entityManager.HasComponent<SceneInstanceId>(entity))
            {
                entityManager.SetSharedComponentData(entity, SceneInstanceId);
            }
            else
            {
                entityManager.AddSharedComponentData(entity, SceneInstanceId);
            }
        }

        /// <summary>
        /// Returns the number of entities in the scene.
        /// </summary>
        public int EntityCount(EntityManager entityManager)
        {
            return GetSceneEntityQueryRO(entityManager).CalculateEntityCount();
        }

        internal EntityQuery GetSceneEntityQueryRO(EntityManager entityManager)
        {
            var query = entityManager.CreateEntityQuery
                (
                    new EntityQueryDesc
                    {
                        All = new[] {ComponentType.ReadOnly<SceneGuid>(), ComponentType.ReadOnly<SceneInstanceId>()},
                        Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabled
                    }
                );

            query.SetSharedComponentFilter(SceneGuid, SceneInstanceId);
            return query;
        }

        public bool Equals(Scene other)
        {
            return Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Scene other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SceneGuid.GetHashCode() * 397) ^ SceneInstanceId.GetHashCode();
            }
        }

        public static bool operator==(Scene left, Scene right)
        {
            return Equals(left, right);
        }

        public static bool operator!=(Scene left, Scene right)
        {
            return !Equals(left, right);
        }

        private static unsafe bool Equals(Scene lhs, Scene rhs)
        {
            return UnsafeUtility.MemCmp(UnsafeUtility.AddressOf(ref lhs), UnsafeUtility.AddressOf(ref rhs), UnsafeUtility.SizeOf<Scene>()) == 0;
        }
    }
}
