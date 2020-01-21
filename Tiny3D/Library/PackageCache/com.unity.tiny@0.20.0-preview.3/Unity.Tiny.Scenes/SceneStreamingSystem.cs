using System;
using Unity.Entities.Runtime.Hashing;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Tiny;
using Unity.Tiny.Codec;
using Unity.Tiny.IO;
using Unity.Tiny.Assertions;
using static Unity.Tiny.IO.AsyncOp;

namespace Unity.Tiny.Scenes
{
    public enum SceneStatus
    {
        NotYetProcessed = 0,
        Loading,
        Loaded,
        FailedToLoad
    }

    /// <summary>
    /// Provides information for a requested scene. This component will be updated as a load request progresses.
    /// </summary>
    //[HideInInspector]
    public struct SceneData : IComponentData
    {
        public Scene Scene;
        public SceneStatus Status;
    }

    //[HideInInspector]
    internal struct SceneLoadRequest : IComponentData
    {
        internal AsyncOp SceneOpHandle;
    }

    /// <summary>
    ///  Provides common scene helper functions
    /// </summary>
    public class SceneService
    {
        static internal Entity LoadConfigAsync(World world)
        {
            if (world.TinyEnvironment().configEntity != Entity.Null)
                throw new Exception("Configuration already loaded");

            return LoadSceneAsync(world, new SceneGuid() { Guid = ConfigurationScene.Guid });
        }

        /// <summary>
        /// Creates a request to load the scene provided by the passed `SceneReference` argument.
        /// </summary>
        /// <param name="sceneReference"></param>
        /// <returns>A new Entity with a `SceneData` component which should be stored for use in `GetSceneStatus`</returns>
        static public Entity LoadSceneAsync(World world, SceneReference sceneReference)
        {
            return LoadSceneAsync(world, new SceneGuid() { Guid = sceneReference.SceneGuid });
        }

        static internal Entity LoadSceneAsync(World world, SceneGuid sceneGuid)
        {
            var em = world.EntityManager;
            var newScene = SceneManager.Create(sceneGuid.Guid);

            var eScene = em.CreateEntity();
            em.AddComponentData(eScene, new SceneData() { Scene = newScene, Status = SceneStatus.NotYetProcessed });
            em.AddComponentData(eScene, new RequestSceneLoaded());
            em.AddSharedComponentData(eScene, newScene.SceneGuid);
            em.AddSharedComponentData(eScene, newScene.SceneInstanceId);

            return eScene;
        }

        /// <summary>
        /// Unloads the scene instance for the provided entity. As such, the entity passed in must belong
        /// to a scene otherwise this function will throw.
        /// </summary>
        /// <param name="sceneEntity"></param>
        static public void UnloadSceneInstance(World world, Entity sceneEntity)
        {
            var sceneGuid = world.EntityManager.GetSharedComponentData<SceneGuid>(sceneEntity);
            var sceneInstance = world.EntityManager.GetSharedComponentData<SceneInstanceId>(sceneEntity);
            var scene = new Scene(sceneGuid, sceneInstance);
            world.EntityManager.DestroyEntity(scene.GetSceneEntityQueryRO(world.EntityManager));
        }

        /// <summary>
        /// Unloads all scene instances of the same type as the `SceneReference` passed in.
        /// </summary>
        /// <param name="sceneReference"></param>
        static public void UnloadAllSceneInstances(World world, SceneReference sceneReference)
        {
            UnloadAllSceneInstances(world, new SceneGuid() { Guid = sceneReference.SceneGuid });
        }

        /// <summary>
        /// Unloads all scene instances of the same type as the scene the passed in entity belongs to.
        /// </summary>
        /// <param name="scene"></param>
        static public void UnloadAllSceneInstances(World world, Entity scene)
        {
            var sceneGuid = world.EntityManager.GetSharedComponentData<SceneGuid>(scene);
            UnloadAllSceneInstances(world, sceneGuid);
        }

        static internal void UnloadAllSceneInstances(World world, SceneGuid sceneGuid)
        {
            var em = world.EntityManager;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<SceneLoadRequest>(), ComponentType.ReadOnly<SceneGuid>()))
            {
                query.SetSharedComponentFilter(sceneGuid);
                using (var issuedRequests = query.ToEntityArray(Allocator.Temp))
                {
                    foreach (var entity in issuedRequests)
                    {
                        var request = em.GetComponentData<SceneLoadRequest>(entity);

                        // Disabled for web builds until https://github.com/emscripten-core/emscripten/issues/8234 is resolved
                        #if !UNITY_WEBGL
                        // Cancels the request if not already complete
                        request.SceneOpHandle.Dispose();
                        #endif
                        ecb.DestroyEntity(entity);
                    }
                }
            }

            using (var query = em.CreateEntityQuery(new EntityQueryDesc()
            {
                All = new[] { ComponentType.ReadOnly<SceneGuid>() },
                None = new[] { ComponentType.ReadOnly<SceneLoadRequest>() }
            }))
            {
                query.SetSharedComponentFilter(sceneGuid);
                ecb.DestroyEntity(query);
            }
            ecb.Playback(em);
            ecb.Dispose();
        }

        /// <summary>
        /// Retrieves the status of a scene load request.
        /// </summary>
        /// <param name="scene">Pass in the entity returned from `LoadSceneAsync`</param>
        /// <returns></returns>
        static public SceneStatus GetSceneStatus(World world, Entity scene)
        {
            var sceneData = world.EntityManager.GetComponentData<SceneData>(scene);
            return sceneData.Status;
        }

        /// <summary>
        /// Check that all startup scenes have been loaded
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        static public bool AreStartupScenesLoaded(World world)
        {
             using (var startupScenes = world.TinyEnvironment().GetConfigBufferData<StartupScenes>().ToNativeArray(Allocator.Temp))
             {
                bool bAllLoaded = true;
                var em = world.EntityManager;
                for (var i = 0; i < startupScenes.Length; ++i)
                {
                    using (var query = em.CreateEntityQuery(ComponentType.ReadOnly<SceneData>(), ComponentType.ReadOnly<SceneGuid>()))
                    {
                        query.SetSharedComponentFilter(new SceneGuid() { Guid = startupScenes[i].SceneReference.SceneGuid });
                        var entities = query.ToEntityArray(Allocator.TempJob);
                        foreach (var e in entities)
                        {
                            SceneData sceneData = em.GetComponentData<SceneData>(e);
                            if (sceneData.Status != SceneStatus.Loaded)
                                bAllLoaded &= false;
                        }
                        entities.Dispose();
                    }
                }
                return bAllLoaded;
             }
        }   
    }

    /// <summary>
    /// System for handling scene load requests, and instantiating the requested scene entities into `World.Active`
    /// </summary>
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class SceneStreamingSystem : ComponentSystem
    {
        static public readonly string DataRootPath = "Data/";

        private World m_LoadingWorld;
        private EntityQuery m_PendingRequestsQuery;

        protected override void OnCreate()
        {
            m_LoadingWorld = new World("Loading World");
            m_PendingRequestsQuery = GetEntityQuery(
                ComponentType.ReadWrite<SceneData>(),
                ComponentType.ReadWrite<SceneLoadRequest>());

            // Ensure these systems are created
            EntityManager.World.GetOrCreateSystem<EntityReferenceRemapSystem>();
            EntityManager.World.GetOrCreateSystem<RemoveRemapInformationSystem>();
        }

        protected override void OnDestroy()
        {
            m_LoadingWorld.Dispose();
        }

        internal static string DebugSceneGuid(SceneData sd) => sd.Scene.SceneGuid.Guid.ToString("N");
        internal static string DebugSceneGuid(Scene sc) => sc.SceneGuid.Guid.ToString("N");

        protected override unsafe void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            Entities
                .WithAll<SceneData, RequestSceneLoaded>()
                .WithNone<SceneLoadRequest>()
                .ForEach((Entity e) =>
                {
                    var sceneData = EntityManager.GetComponentData<SceneData>(e);
                    var sceneGuid = sceneData.Scene.SceneGuid;
                    var path = DataRootPath + sceneGuid.Guid.ToString("N");
#if IO_ENABLE_TRACE
                    Debug.Log($"SceneStreamingSystem: starting load for {DebugSceneGuid(sceneData)} from {path}");
#endif

                    // Fire async reads for scene data
                    SceneLoadRequest request = new SceneLoadRequest();
                    request.SceneOpHandle = IOService.RequestAsyncRead(path);
                    ecb.AddComponent(e, request);
                    ecb.RemoveComponent<RequestSceneLoaded>(e);

                    sceneData.Status = SceneStatus.Loading;
                    ecb.SetComponent(e, sceneData);
                });
            ecb.Playback(EntityManager);
            ecb.Dispose();

            var pendingRequests = m_PendingRequestsQuery.ToEntityArray(Allocator.TempJob);
            ecb = new EntityCommandBuffer(Allocator.TempJob);
            foreach (var requestEntity in pendingRequests)
            {
                SceneData sceneData = EntityManager.GetComponentData<SceneData>(requestEntity);
                SceneLoadRequest request = EntityManager.GetComponentData<SceneLoadRequest>(requestEntity);

                var opStatus = request.SceneOpHandle.GetStatus();

                if (opStatus <= Status.InProgress)
                {
                    continue;
                }

                if (opStatus == Status.Failure)
                {
                    request.SceneOpHandle.Dispose();
                    ecb.RemoveComponent<SceneLoadRequest>(requestEntity);

                    Debug.Log($"SceneStreamingSystem: Failed to load {DebugSceneGuid(sceneData)}");

                    sceneData.Status = SceneStatus.FailedToLoad;
                    ecb.SetComponent(requestEntity, sceneData);
                    continue;
                }
                Assert.IsTrue(opStatus == Status.Success);

                request.SceneOpHandle.GetData(out var data, out var sceneDataSize);
                SceneHeader header = *(SceneHeader*)data;
                int headerSize = UnsafeUtility.SizeOf<SceneHeader>();
                if (header.Version != SceneHeader.CurrentVersion)
                {
                    throw new Exception($"Scene serialization version mismatch in {DebugSceneGuid(sceneData)}. Reading version '{header.Version}', expected '{SceneHeader.CurrentVersion}'");
                }

                byte* decompressedScene = data + headerSize;
                if (header.Codec != Codec.Codec.None)
                {
                    decompressedScene = (byte*)UnsafeUtility.Malloc(header.DecompressedSize, 16, Allocator.Temp);

                    if (!CodecService.Decompress(header.Codec, data + headerSize, sceneDataSize - headerSize, decompressedScene, header.DecompressedSize))
                    {
                        throw new Exception($"Failed to decompress compressed scene {DebugSceneGuid(sceneData)} using codec '{header.Codec}'");
                    }
                }

                using (var sceneReader = new MemoryBinaryReader(decompressedScene))
                {
                    var loadingEM = m_LoadingWorld.EntityManager;
                    var transaction = loadingEM.BeginExclusiveEntityTransaction();
                    SerializeUtility.DeserializeWorld(transaction, sceneReader);
                    loadingEM.EndExclusiveEntityTransaction();
                }

                var scene = sceneData.Scene;
                var activeEM = EntityManager;
                activeEM.MoveEntitiesFrom(out var movedEntities, m_LoadingWorld.EntityManager);
                foreach (var e in movedEntities)
                {
                    ecb.AddSharedComponent(e, scene.SceneGuid);
                    ecb.AddSharedComponent(e, scene.SceneInstanceId);
                }

                // Fixup Entity references now that the entities have moved
                EntityManager.World.GetExistingSystem<EntityReferenceRemapSystem>().Update();
                EntityManager.World.GetExistingSystem<RemoveRemapInformationSystem>().Update();

                if (header.Codec != Codec.Codec.None)
                {
                    UnsafeUtility.Free(decompressedScene, Allocator.Temp);
                }
#if IO_ENABLE_TRACE
                Debug.Log($"SceneStreamingSystem: Loaded scene {DebugSceneGuid(sceneData)}");
#endif
                sceneData.Status = SceneStatus.Loaded;
                ecb.SetComponent(requestEntity, sceneData);

                request.SceneOpHandle.Dispose();
                ecb.RemoveComponent<SceneLoadRequest>(requestEntity);

                m_LoadingWorld.EntityManager.PrepareForDeserialize();
                movedEntities.Dispose();
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
            pendingRequests.Dispose();
        }
    }
}
