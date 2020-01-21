using System;
using Unity.Collections;
using Unity.Entities;
using System.Diagnostics;

namespace Unity.Tiny.GenericAssetLoading
{
    public enum LoadResult
    {
        stillWorking = 0,
        success = 1,
        failed = 2
    };

    public interface IGenericAssetLoader<T, TN, TS, L>
        where T : struct, IComponentData
        where TN : struct, IComponentData, ISystemStateComponentData
        where TS : struct, IComponentData
        where L : struct, IComponentData, ISystemStateComponentData
    {
        void StartLoad(EntityManager man, Entity e, ref T thing, ref TN native, ref TS source, ref L loading);
        LoadResult CheckLoading(IntPtr cppwrapper,EntityManager man, Entity e, ref T thing, ref TN native, ref TS source, ref L loading);
        void FreeNative(EntityManager man, Entity e, ref TN native);
        void FinishLoading(EntityManager man, Entity e, ref T thing, ref TN native, ref L loading);
    }

    // T = the thing to load
    // TN = native component of the thing to load
    // TS = source component for loading
    // L = extra loading data, component is added while loading is in flight
    public class GenericAssetLoader<T, TN, TS, L> : ComponentSystem
        where T : struct, IComponentData
        where TN : struct, IComponentData, ISystemStateComponentData
        where TS : struct, IComponentData
        where L : struct, IComponentData, ISystemStateComponentData
    {
        protected IGenericAssetLoader<T, TN, TS, L> c;
        protected IntPtr wrapper;
        // TODO: need to dispose groups?

        protected override void OnUpdate()
        {
            var mgr = EntityManager;

            // cleanup native and internal components in case the thing component was removed (this also covers entity deletion)
            // remove Native and Loading if Thing is gone
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities
                .WithNone<T>()
                .ForEach((Entity e, ref TN n) =>
                {
                    c.FreeNative(mgr, e, ref n);
                    ecb.RemoveComponent<TN>(e);
                    if (mgr.HasComponent<L>(e))
                        ecb.RemoveComponent<L>(e);
                });
            ecb.Playback(mgr);
            ecb.Dispose();

            // add the Native component for Things that want to load and do not have one yet
            ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities
                .WithNone<TN>()
                .WithAll<TS>()
                .ForEach((Entity e, ref T n) =>
                {
                    ecb.AddComponent(e, default(TN)); // +TN
                });
            ecb.Playback(mgr);
            ecb.Dispose();

            // start loading!
            ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities
                .WithNone<L>()
                .ForEach((Entity e, ref T t, ref TN n, ref TS s) =>
                {
                    L l = default(L);
                    c.StartLoad(mgr, e, ref t, ref n, ref s, ref l);
                    ecb.AddComponent(e, l);
                });
            ecb.Playback(mgr);
            ecb.Dispose();

            // check on all things that are in flight, and finish when done
            ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities.ForEach((Entity e, ref T t, ref TN n, ref TS s, ref L l) =>
            {
                LoadResult lr = c.CheckLoading(wrapper, mgr, e, ref t, ref n, ref s, ref l);
                if (lr == LoadResult.stillWorking)
                    return;
                // remove load state
                ecb.RemoveComponent<L>(e);
                ecb.RemoveComponent<TS>(e);
                if (lr == LoadResult.failed)
                {
                    c.FreeNative(mgr, e, ref n);
                    // should we remove native here?
                    return;
                }
                // success!
                c.FinishLoading(mgr, e, ref t, ref n, ref l);
            });
            ecb.Playback(mgr);
            ecb.Dispose();
        }
    }
}
