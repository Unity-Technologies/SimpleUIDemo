using System;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;

namespace Unity.Tiny
{
    public struct EntityName : IBufferElementData
    {
        public char c;
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class TinyEnvironment : ComponentSystem
    {
        private const uint m_EntityCacheSize = 4096;
        private Entity[] m_EntityNameCache;
        private Entity[] m_EntityGuidCache;

        public Entity configEntity;
        public Entity whiteSpriteEntity;

        public bool fixedFrameRateEnabled;
        public float fixedFrameDeltaTime = 1.0f / 60.0f;

        internal TimeData m_timeData;

        // must called from windowing systems once a frame
        public TimeData StepWallRealtimeFrame(double deltaTimeDouble)
        {
            // do the cast in here so that if we need to, we can detect
            // precision loss in a single place
            float deltaTime = (float) deltaTimeDouble;
            // update
            float frameDeltaTime;
            if (fixedFrameRateEnabled)
                frameDeltaTime = fixedFrameDeltaTime;
            else
                frameDeltaTime = deltaTime;
            if (frameDeltaTime >= .5f) // max 1/2 second
                frameDeltaTime = .5f;
            if (frameDeltaTime <= 0.0) // no negative steps
                return m_timeData;

            m_timeData = new TimeData(
                elapsedTime: m_timeData.ElapsedTime + frameDeltaTime,
                deltaTime: frameDeltaTime);

            return m_timeData;
        }

        private void AddEntityToNameCache(Entity entity, string name)
        {
            if (!EntityManager.Exists(entity) || string.IsNullOrEmpty(name))
            {
                return;
            }

            if (m_EntityNameCache == null)
            {
                m_EntityNameCache = new Entity[m_EntityCacheSize];
            }

            var hash = (uint)name.GetHashCode();
            var index = hash % m_EntityCacheSize;
            m_EntityNameCache[index] = entity;
        }

        private Entity GetEntityFromNameCache(string name)
        {
            if (m_EntityNameCache == null || string.IsNullOrEmpty(name))
            {
                return Entity.Null;
            }

            var hash = (uint)name.GetHashCode();
            var index = hash % m_EntityCacheSize;
            var entity = m_EntityNameCache[index];

            if (GetEntityName(entity) != name)
            {
                m_EntityNameCache[index] = Entity.Null;
                return Entity.Null;
            }

            return entity;
        }

        private void AddEntityToGuidCache(Entity entity, EntityGuid guid)
        {
            if (!EntityManager.Exists(entity) || guid == EntityGuid.Null)
            {
                return;
            }

            if (m_EntityGuidCache == null)
            {
                m_EntityGuidCache = new Entity[m_EntityCacheSize];
            }

            var hash = (uint)guid.GetHashCode();
            var index = hash % m_EntityCacheSize;
            m_EntityGuidCache[index] = entity;
        }

        private Entity GetEntityFromGuidCache(EntityGuid guid)
        {
            if (m_EntityGuidCache == null || guid == EntityGuid.Null)
            {
                return Entity.Null;
            }

            var hash = (uint)guid.GetHashCode();
            var index = hash % m_EntityCacheSize;
            var entity = m_EntityGuidCache[index];

            if (GetEntityGuid(entity) != guid)
            {
                m_EntityGuidCache[index] = Entity.Null;
                return Entity.Null;
            }

            return entity;
        }

        internal Entity TryGetSingletonEntity<T>()
        {
            using (var query = EntityManager.CreateEntityQuery(typeof(T)))
            {
                int num = query.CalculateEntityCount();
                if (num == 0)
                    return Entity.Null;
                return query.GetSingletonEntity();
            }
        }

        protected override void OnCreate()
        {
            configEntity = TryGetSingletonEntity<ConfigurationTag>();
            whiteSpriteEntity = EntityManager.CreateEntity();
        }

        protected override void OnDestroy()
        {
            configEntity = Entity.Null;

            if (EntityManager.Exists(whiteSpriteEntity))
                EntityManager.DestroyEntity(whiteSpriteEntity);
            whiteSpriteEntity = Entity.Null;
        }

        public Entity GetEntityByGuid(EntityGuid guid)
        {
            var entity = GetEntityFromGuidCache(guid);
            if (entity != Entity.Null)
            {
                return entity;
            }

            var query = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(EntityGuid) },
                Options = EntityQueryOptions.IncludeDisabled
            };

            using (var group = EntityManager.CreateEntityQuery(query))
            using (var entities = group.ToEntityArray(Allocator.TempJob))
            using (var guids = group.ToComponentDataArray<EntityGuid>(Allocator.TempJob))
            {
                for (var i = 0; i < group.CalculateEntityCount(); ++i)
                {
                    if (guids[i] == guid)
                    {
                        entity = entities[i];
                        AddEntityToGuidCache(entity, guid);
                        return entity;
                    }
                }
            }

            return Entity.Null;
        }

        public EntityGuid GetEntityGuid(Entity entity)
        {
            if (!EntityManager.Exists(entity) ||
                !EntityManager.HasComponent<EntityGuid>(entity))
            {
                return EntityGuid.Null;
            }
            return EntityManager.GetComponentData<EntityGuid>(entity);
        }

        public void SetEntityGuid(Entity entity, EntityGuid guid)
        {
            if (!EntityManager.Exists(entity))
            {
                return;
            }

            if (!EntityManager.HasComponent<EntityGuid>(entity))
            {
                EntityManager.AddComponentData(entity, guid);
            }
            else
            {
                EntityManager.SetComponentData(entity, guid);
            }

            AddEntityToGuidCache(entity, guid);
        }

        public Entity GetEntityByName(string name)
        {
            var entity = GetEntityFromNameCache(name);
            if (entity != Entity.Null)
            {
                return entity;
            }

            using (var entities = GetAllEntitiesByName(name))
            {
                if (entities.Length == 0)
                {
                    return Entity.Null;
                }
                if (entities.Length > 1)
                    throw new System.ArgumentException("More than one entity with the same name has been found.");
                entity = entities[0];
                AddEntityToNameCache(entity, name);
                return entity;
            }
        }

        public NativeList<Entity> GetAllEntitiesByName(string name, Allocator allocator = Allocator.Temp)
        {
            var result = new NativeList<Entity>(allocator);
            var query = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(EntityName) },
                Options = EntityQueryOptions.IncludeDisabled
            };

            using (var group = EntityManager.CreateEntityQuery(query))
            using (var entities = group.ToEntityArray(Allocator.TempJob))
            {
                foreach (var entity in entities)
                {
                    if (EntityManager.GetBufferAsString<EntityName>(entity) == name)
                    {
                        result.Add(entity);
                    }
                }
            }

            return result;
        }

        public string GetEntityName(Entity entity)
        {
            if (!EntityManager.Exists(entity) ||
                !EntityManager.HasComponent<EntityName>(entity))
            {
                return string.Empty;
            }
            return EntityManager.GetBufferAsString<EntityName>(entity);
        }

        public void SetEntityName(Entity entity, string name)
        {
            if (!EntityManager.Exists(entity))
            {
                return;
            }

            if (!EntityManager.HasComponent<EntityName>(entity))
            {
                EntityManager.AddBufferFromString<EntityName>(entity, name);
            }
            else
            {
                EntityManager.SetBufferFromString<EntityName>(entity, name);
            }

            AddEntityToNameCache(entity, name);
        }

        internal bool EnsureConfigEntity(bool forceCreate = false)
        {
            if (configEntity != Entity.Null)
                return true;

            configEntity = TryGetSingletonEntity<ConfigurationTag>();
            if (forceCreate && configEntity == Entity.Null)
            {
                configEntity = EntityManager.CreateEntity();
                EntityManager.AddComponent<ConfigurationTag>(configEntity);
            }

            return configEntity != Entity.Null;
        }

        public T GetConfigData<T>() where T : struct, IComponentData
        {
            if (!EnsureConfigEntity())
                return default(T);

            if (!EntityManager.HasComponent<T>(configEntity))
            {
                return default(T);
            }

            return EntityManager.GetComponentData<T>(configEntity);
        }

        public void SetConfigData<T>(T data) where T : struct, IComponentData
        {
            EnsureConfigEntity(true);

            if (!EntityManager.HasComponent<T>(configEntity))
            {
                EntityManager.AddComponentData(configEntity, data);
            }
            else
            {
                EntityManager.SetComponentData(configEntity, data);
            }
        }

        public DynamicBuffer<T> GetConfigBufferData<T>() where T : struct, IBufferElementData
        {
            EnsureConfigEntity(true);

            if (!EntityManager.HasComponent<T>(configEntity))
            {
                return EntityManager.AddBuffer<T>(configEntity);
            }
            return EntityManager.GetBuffer<T>(configEntity);
        }

        protected override void OnUpdate()
        {
        }
    }

    public static class TinyEnvironmentExtensions
    {
        public static TinyEnvironment TinyEnvironment(this World world)
        {
            return world.GetOrCreateSystem<TinyEnvironment>();
        }
    }
}
