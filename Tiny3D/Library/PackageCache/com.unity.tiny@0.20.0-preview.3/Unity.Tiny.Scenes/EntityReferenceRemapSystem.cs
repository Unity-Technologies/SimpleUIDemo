using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Tiny.Scenes
{
    [DisableAutoCreation]
    public class EntityReferenceRemapSystem : ComponentSystem
    {
        [BurstCompile]
        private struct BuildEntityGuidHashMapJob : IJobChunk
        {
            [Unity.Collections.ReadOnly] public ArchetypeChunkEntityType Entity;
            [Unity.Collections.ReadOnly] public ArchetypeChunkComponentType<EntityGuid> EntityGuidType;
            [Unity.Collections.WriteOnly] public NativeHashMap<EntityGuid, Entity>.ParallelWriter HashMap;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entities = chunk.GetNativeArray(Entity);
                var entityGuids = chunk.GetNativeArray(EntityGuidType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    HashMap.TryAdd(entityGuids[entityIndex], entities[entityIndex]);
                }
            }
        }

        private EntityQuery m_EntityGuidQuery;
        private EntityQuery m_EntityReferenceRemapQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EntityGuidQuery = EntityManager.CreateEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new[] {ComponentType.ReadOnly<EntityGuid>()},
                    Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabled
                }
            );

            m_EntityReferenceRemapQuery = EntityManager.CreateEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new[] {ComponentType.ReadOnly<EntityReferenceRemap>()},
                    Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabled
                }
            );
        }

        protected override unsafe void OnUpdate()
        {
            using (var entityGuidHashMap = new NativeHashMap<EntityGuid, Entity>(m_EntityGuidQuery.CalculateEntityCount(), Allocator.TempJob))
            {
                var entityType = EntityManager.GetArchetypeChunkEntityType();

                new BuildEntityGuidHashMapJob
                {
                    Entity = entityType,
                    EntityGuidType = GetArchetypeChunkComponentType<EntityGuid>(true),
                    HashMap = entityGuidHashMap.AsParallelWriter()
                }.Schedule(m_EntityGuidQuery).Complete();

                using (var chunks = m_EntityReferenceRemapQuery.CreateArchetypeChunkArray(Allocator.TempJob))
                {
                    var entityReferenceRemapType = GetArchetypeChunkBufferType<EntityReferenceRemap>(true);

                    // Run through all chunks
                    for (var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                    {
                        var entities = chunks[chunkIndex].GetNativeArray(entityType);
                        var entityReferenceRemaps = chunks[chunkIndex].GetBufferAccessor(entityReferenceRemapType);

                        // Run through each entity of the chunk
                        for (var entityIndex = 0; entityIndex < entityReferenceRemaps.Length; entityIndex++)
                        {
                            var entity = entities[entityIndex];
                            var entityReferenceRemap = entityReferenceRemaps[entityIndex];

                            // Run through each remap for the entity
                            for (var remapIndex = 0; remapIndex < entityReferenceRemap.Length; remapIndex++)
                            {
                                var remap = entityReferenceRemap[remapIndex];

                                // Find the live entity which matches this guid
                                if (!entityGuidHashMap.TryGetValue(remap.Guid, out var target))
                                {
                                    continue;
                                }

                                // Resolve the type
                                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(remap.TypeHash);

                                if (TypeManager.IsSharedComponent(typeIndex))
                                {
                                    // @TODO get this working in the NET_DOTS runtime
                                    /*
                                    var type = TypeManager.GetType(typeIndex);

                                    // ASSUMPTION: Shared component data is blittable
                                    if (!UnsafeUtility.IsBlittable(type))
                                    {
                                        throw new Exception($"Trying to remap entity reference for a non blittable shared component Type=[{type.FullName}]");
                                    }

                                    // Patch shared component reference
                                    var sharedComponent = EntityManager.GetSharedComponentData(entity, typeIndex);
                                    *(Entity*) ((byte*) Unsafe.AsPointer(ref sharedComponent) + remap.Offset) = target;
                                    EntityManager.SetSharedComponentDataBoxed(entity, typeIndex, sharedComponent);
                                    */
                                    continue;
                                }

                                if (TypeManager.IsBuffer(typeIndex))
                                {
                                    // Patch buffer component reference
                                    var ptr = (BufferHeader*) EntityManager.GetComponentDataRawRW(entity, typeIndex);
                                    *(Entity*) (BufferHeader.GetElementPointer(ptr) + remap.Offset) = target;
                                    continue;
                                }

                                // Patch standard component reference
                                *(Entity*) ((byte*) EntityManager.GetComponentDataRawRW(entity, typeIndex) + remap.Offset) = target;
                            }
                        }
                    }
                }
            }
        }
    }

    [DisableAutoCreation]
    public class ClearRemappedEntityReferenceSystem : ComponentSystem
    {
        private EntityQuery m_EntityReferenceRemapQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EntityReferenceRemapQuery = EntityManager.CreateEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new[] {ComponentType.ReadOnly<EntityReferenceRemap>()},
                    Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabled
                }
            );
        }

        protected override unsafe void OnUpdate()
        {
            using (var chunks = m_EntityReferenceRemapQuery.CreateArchetypeChunkArray(Allocator.TempJob))
            {
                var entityType = EntityManager.GetArchetypeChunkEntityType();
                var entityReferenceRemapType = GetArchetypeChunkBufferType<EntityReferenceRemap>();

                // Run through all chunks
                for (var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    var entities = chunks[chunkIndex].GetNativeArray(entityType);
                    var entityReferenceRemaps = chunks[chunkIndex].GetBufferAccessor(entityReferenceRemapType);

                    // Run through each entity of the chunk
                    for (var entityIndex = 0; entityIndex < entityReferenceRemaps.Length; entityIndex++)
                    {
                        var entity = entities[entityIndex];
                        var entityReferenceRemap = entityReferenceRemaps[entityIndex];

                        // Run through each remap for the entity
                        for (var remapIndex = 0; remapIndex < entityReferenceRemap.Length; remapIndex++)
                        {
                            var remap = entityReferenceRemap[remapIndex];

                            // Resolve the type
                            var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(remap.TypeHash);

                            if (TypeManager.IsSharedComponent(typeIndex))
                            {
                                // @TODO get this working in the NET_DOTS runtime
                                /*
                                var type = TypeManager.GetType(typeIndex);

                                // ASSUMPTION: Shared component data is blittable
                                if (!UnsafeUtility.IsBlittable(type))
                                {
                                    throw new Exception($"Trying to remap entity reference for a non blittable shared component Type=[{type.FullName}]");
                                }

                                // Patch shared component reference
                                var sharedComponent = EntityManager.GetSharedComponentData(entity, typeIndex);
                                *(Entity*) ((byte*) Unsafe.AsPointer(ref sharedComponent) + remap.Offset) = Entity.Null;
                                EntityManager.SetSharedComponentDataBoxed(entity, typeIndex, sharedComponent);
                                */
                                continue;
                            }

                            if (TypeManager.IsBuffer(typeIndex))
                            {
                                // Patch buffer component reference
                                var ptr = (BufferHeader*) EntityManager.GetComponentDataRawRW(entity, typeIndex);
                                *(Entity*) (BufferHeader.GetElementPointer(ptr) + remap.Offset) = Entity.Null;
                                continue;
                            }

                            // Patch standard component reference
                            *(Entity*) ((byte*) EntityManager.GetComponentDataRawRW(entity, typeIndex) + remap.Offset) = Entity.Null;
                        }
                    }
                }
            }
        }
    }

    [DisableAutoCreation]
    [UpdateAfter(typeof(EntityReferenceRemapSystem))]
    public class RemoveRemapInformationSystem : ComponentSystem
    {
        private EntityQuery m_EntityReferenceRemapQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EntityReferenceRemapQuery = EntityManager.CreateEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new[] {ComponentType.ReadWrite<EntityReferenceRemap>()},
                    Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabled
                }
            );
        }

        protected override void OnUpdate()
        {
            using (var entities = m_EntityReferenceRemapQuery.ToEntityArray(Allocator.TempJob))
            {
                foreach (var entity in entities)
                {
                    PostUpdateCommands.RemoveComponent<EntityReferenceRemap>(entity);
                }
            }
        }
    }
}
