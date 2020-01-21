using System;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    public static class SceneManager
    {
        // @TODO Move to ECS storage to properly support live-linking
        private static uint s_NextSceneInstanceId = 1;

        internal static void SetNextSceneInstanceId(uint nextSceneInstanceId) => s_NextSceneInstanceId = nextSceneInstanceId;

        /// <summary>
        /// Converts the given set of entities to be part of the same scene.
        /// </summary>
        /// <param name="entityManager">The entity manager to operate on.</param>
        /// <param name="entities">The set of entities to convert.</param>
        /// <param name="guid">The unique identifier of the scene.</param>
        /// <returns>A scene view of entities.</returns>
        public static Scene Create(EntityManager entityManager, NativeArray<Entity> entities, Guid guid)
        {
            var sceneGuid = new SceneGuid {Guid = guid};
            var sceneInstanceId = new SceneInstanceId {InstanceId = s_NextSceneInstanceId++};

            for (var i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];

                if (!entityManager.HasComponent<SceneGuid>(entity))
                {
                    entityManager.AddSharedComponentData(entities[i], sceneGuid);
                }
                else
                {
                    entityManager.SetSharedComponentData(entities[i], sceneGuid);
                }

                if (!entityManager.HasComponent<SceneInstanceId>(entity))
                {
                    entityManager.AddSharedComponentData(entities[i], sceneInstanceId);
                }
                else
                {
                    entityManager.SetSharedComponentData(entities[i], sceneInstanceId);
                }
            }

            return new Scene(sceneGuid, sceneInstanceId);
        }

        public static Scene Create(Guid guid)
        {
            var sceneGuid = new SceneGuid {Guid = guid};
            var sceneInstanceId = new SceneInstanceId {InstanceId = s_NextSceneInstanceId++};

            return new Scene(sceneGuid, sceneInstanceId);
        }

        public static void Destroy(EntityManager entityManager, Scene scene)
        {
            using (var entities = scene.ToEntityArray(entityManager, Allocator.TempJob))
            {
                // @TODO jobify
                foreach (var entity in entities)
                {
                    entityManager.DestroyEntity(entity);
                }
            }
        }
    }
}
