using System;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Tiny.Assertions;

namespace Unity.Tiny
{
    public static class EntityManagerExtensions
    {
        public static void AddBufferFromString<T>(this EntityManager manager, Entity entity, string value)
            where T : struct, IBufferElementData
        {
            if (!manager.Exists(entity) || manager.HasComponent<T>(entity))
            {
                return;
            }
            manager.AddBuffer<T>(entity).Reinterpret<char>().FromString(value);
        }

        public static string GetBufferAsString<T>(this EntityManager manager, Entity entity)
            where T : struct, IBufferElementData
        {
            if (!manager.Exists(entity) || !manager.HasComponent<T>(entity))
            {
                return string.Empty;
            }
            return manager.GetBufferRO<T>(entity).Reinterpret<char>().AsString();
        }

        public static void SetBufferFromString<T>(this EntityManager manager, Entity entity, string value)
            where T : struct, IBufferElementData
        {
            if (!manager.Exists(entity) || !manager.HasComponent<T>(entity))
            {
                return;
            }
            manager.GetBuffer<T>(entity).Reinterpret<char>().FromString(value);
        }

        public static unsafe DynamicBuffer<T> GetBufferRO<T>(this EntityManager manager, Entity entity)
            where T : struct, IBufferElementData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            manager.EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            if (!TypeManager.IsBuffer(typeIndex))
                throw new ArgumentException(
                    $"GetBuffer<{typeof(T)}> may not be IComponentData or ISharedComponentData; currently {TypeManager.GetTypeInfo<T>().Category}");
#endif

            manager.DependencyManager->CompleteReadAndWriteDependency(typeIndex);

            BufferHeader* header = (BufferHeader*) manager.EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);
            int internalCapacity = TypeManager.GetTypeInfo(typeIndex).BufferCapacity;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var isReadOnly = false; // @TODO FIXME! we need DynamicBuffer<T>.GetUnsafeReadOnlyPtr();
            return new DynamicBuffer<T>(header, manager.SafetyHandles->GetSafetyHandle(typeIndex, isReadOnly), manager.SafetyHandles->GetBufferSafetyHandle(typeIndex), isReadOnly, false, 0, internalCapacity);
#else
            return new DynamicBuffer<T>(header, internalCapacity);
#endif
        }
    }

    internal static class TinyInternalEntityManagerExtensions
    {
        internal static unsafe byte* GetComponentDataWithTypeRW(this EntityManager manager, Entity entity, int typeIndex)
        {
            var ptr = manager.GetComponentDataRawRW(entity, typeIndex);
            return (byte*)ptr;
        }

        internal static unsafe byte* GetComponentDataWithTypeRO(this EntityManager manager, Entity entity, int typeIndex)
        {
            var ptr = manager.GetComponentDataRawRO(entity, typeIndex);
            return (byte*)ptr;
        }

        internal static bool HasComponentRaw(this EntityManager manager, Entity entity, int typeIndex)
        {
            return manager.HasComponentRaw(entity, typeIndex);
        }
    }
}
