using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Tiny;
using Unity.Tiny.Assertions;
using Unity.Tiny.Rendering;
using Bgfx;

namespace Unity.Tiny.Rendering
{

    public struct RenderNodeRef : IBufferElementData
    {
        public Entity e; // next to a RenderNode, this node depends on those other nodes 
    }

    public struct RenderPassRef : IBufferElementData
    {
        public Entity e; // next to RenderNode, list of passes in this node
    }

    [System.Serializable]
    public struct RenderToPasses : ISharedComponentData
    {
        public Entity e; // shared on every renderer, points to an entity that has RenderToPassesEntry[] buffer
    }

    public struct RenderGroup : IComponentData 
    {   // tag for a render group (optional)
        // next to it: optional object/world bounds 
        // next to it: DynamicArray<RenderToPassesEntry>
    }

    public struct RenderToPassesEntry : IBufferElementData
    {
        public Entity e; // list of entities that have a RenderPass component, where the renderer will render to
    }

    public struct RenderNode : IComponentData
    {
        public bool alreadyAdded;
        // next to it, required: DynamicArray<RenderNodeRef>, dependencies
        // next to it, required: DynamicArray<RenderPassRef>, list of passes in node 
    }

    public struct RenderNodePrimarySurface : IComponentData
    {
        // place next to a RenderNode, to mark it as a sink: recursively starts evaluating render graphs from here 
    }

    public struct RenderNodeTexture : IComponentData
    {
        public Entity colorTexture;
        public Entity depthTexture;
        public RenderPassRect rect;
    }

    public struct RenderNodeCubemap : IComponentData
    {
        public Entity target;
        public int side;
    }

    public struct RenderNodeShadowMap : IComponentData
    {
        public Entity lightsource;
    }
    
    public struct RenderPassUpdateFromCamera : IComponentData
    {
        // frustum and transforms will auto update from a camera entity 
        public Entity camera; // must have Camera component
    }

    public struct RenderPassUpdateFromLight : IComponentData
    {
        // frustum and transforms will auto update from a camera entity 
        public Entity light; // must have Light component
    }

    public struct RenderPassAutoSizeToNode : IComponentData
    {
        // convenience, place next to a RenderPass so it updates it's size to match the node's size 
        // the node must be either primary or have a target texture of some sort
    }

    [Flags]
    public enum RenderPassClear : ushort
    {
        Color = bgfx.ClearFlags.Color,
        Depth = bgfx.ClearFlags.Depth,
        Stencil = bgfx.ClearFlags.Stencil
    }

    public enum RenderPassSort: ushort
    {
        Unsorted = bgfx.ViewMode.Default,
        SortZLess = bgfx.ViewMode.DepthDescending,
        SortZGreater = bgfx.ViewMode.DepthAscending,
        Sorted = bgfx.ViewMode.Sequential
    }

    [Flags]
    public enum RenderPassType : uint
    {
        ZOnly = 1,
        Opaque = 2,
        Transparent = 4,
        UI = 8,
        FullscreenQuad = 16,
        ShadowMap = 32,
        Sprites = 64
    }

    public struct RenderPassRect 
    {
        public ushort x, y, w, h;
    }

    public struct RenderPass : IComponentData
    {
        public Entity inNode;
        public RenderPassSort sorting;
        public float4x4 projectionTransform;
        public float4x4 viewTransform;
        public RenderPassType passType;
        public ushort viewId;
        public RenderPassRect scissor;
        public RenderPassRect viewport;
        public RenderPassClear clearFlags; // matches bgfx
        public byte flipCulling; // 3 if culling direction needs to be flipped, 0 otherwise
        public uint clearRGBA; // matches bgfx
        public float clearDepth; // matches bgfx
        public byte clearStencil; // matches bgfx
        // next to it, optional, Frustum for late stage culling
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(RendererBGFXSystem))]
    [UpdateAfter(typeof(UpdateCameraMatricesSystem))]
    [UpdateAfter(typeof(UpdateLightMatricesSystem))]
    [UpdateBefore(typeof(SubmitFrameSystem))]
    public class PreparePassesSystem : ComponentSystem
    {
        private void RecAddPasses(Entity eNode, ref ushort nextViewId)
        {
            // check already added 
            RenderNode node = EntityManager.GetComponentData<RenderNode>(eNode);
            if (node.alreadyAdded)
                return;
            node.alreadyAdded = true;
            // recurse dependencies
            if (EntityManager.HasComponent<RenderNodeRef>(eNode)) {
                DynamicBuffer<RenderNodeRef> deps = EntityManager.GetBuffer<RenderNodeRef>(eNode);
                for (int i = 0; i < deps.Length; i++)
                    RecAddPasses(deps[i].e, ref nextViewId);
            }
            // now add own passes
            if (EntityManager.HasComponent<RenderPassRef>(eNode)) {
                DynamicBuffer<RenderPassRef> passes = EntityManager.GetBuffer<RenderPassRef>(eNode);
                //RenderDebug.LogFormat("Adding passes to graph for {0}: {1} passes.", eNode, passes.Length);
                for (int i = 0; i < passes.Length; i++ ) {
                    var p = EntityManager.GetComponentData<RenderPass>(passes[i].e);
                    p.viewId = nextViewId++;
                    EntityManager.SetComponentData<RenderPass>(passes[i].e, p);
                }
            }
        }

        protected override void OnUpdate() {
            var bgfxsys = World.GetExistingSystem<RendererBGFXSystem>();
            if (!bgfxsys.Initialized)
                return;

            // make sure passes have viewid, transform, scissor rect and view rect set 

            // reset alreadyAdded state
            // we expect < 100 or so passes, so the below code does not need to be crazy great 
            Entities.ForEach((ref RenderNode rnode) => { rnode.alreadyAdded = false; });
            Entities.ForEach((ref RenderPass pass) => { pass.viewId = 0xffff; }); // there SHOULD not be any passes around that are not referenced by the graph... 

            // get all nodes, sort (bgfx issues in-order per view. a better api could use the render graph to issue without gpu
            // barriers where possible)
            // sort into eval order, assign pass viewId 
            ushort nextViewId = 0;
            Entities.WithAll<RenderNodePrimarySurface>().ForEach((Entity eNode) => { RecAddPasses(eNode, ref nextViewId); });

            Entities.WithAll<RenderPassAutoSizeToNode>().ForEach((Entity e, ref RenderPass pass) =>
            {
                if (EntityManager.HasComponent<RenderNodePrimarySurface>(pass.inNode)) {
                    var di = World.TinyEnvironment().GetConfigData<DisplayInfo>();
                    pass.viewport.x = 0;
                    pass.viewport.y = 0;
                    pass.viewport.w = (ushort)di.width;
                    pass.viewport.h = (ushort)di.height;
                    return;
                } 
                if (EntityManager.HasComponent<RenderNodeTexture>(pass.inNode)) {
                    var texRef = EntityManager.GetComponentData<RenderNodeTexture>(pass.inNode);
                    pass.viewport = texRef.rect;
                }
                // TODO: add others like cubemap
            });

            // auto update passes that are matched with a camera 
            Entities.ForEach((Entity e, ref RenderPass pass, ref RenderPassUpdateFromCamera fromCam) =>
            {
                Entity eCam = fromCam.camera;
                CameraMatrices camData = EntityManager.GetComponentData<CameraMatrices>(eCam);
                pass.viewTransform = camData.view;
                pass.projectionTransform = camData.projection;
                if (EntityManager.HasComponent<Frustum>(eCam)) {
                    if (EntityManager.HasComponent<Frustum>(e)) {
                        EntityManager.SetComponentData(e, EntityManager.GetComponentData<Frustum>(eCam));
                    }
                } else {
                    if (EntityManager.HasComponent<Frustum>(e)) {
                        EntityManager.SetComponentData(e, new Frustum());
                    }
                }
            });

            // auto update passes that are matched with a light 
            Entities.ForEach((Entity e, ref RenderPass pass, ref RenderPassUpdateFromLight fromLight) =>
            {
                Entity eLight = fromLight.light;
                LightMatrices lightData = EntityManager.GetComponentData<LightMatrices>(eLight);
                pass.viewTransform = lightData.view;
                pass.projectionTransform = lightData.projection;
                if (EntityManager.HasComponent<Frustum>(eLight)) {
                    if (EntityManager.HasComponent<Frustum>(e)) {
                        EntityManager.SetComponentData(e, EntityManager.GetComponentData<Frustum>(eLight));
                    }
                } else {
                    if (EntityManager.HasComponent<Frustum>(e)) {
                        EntityManager.SetComponentData(e, new Frustum());
                    }
                }
            });

            // set up extra pass data 
            Entities.ForEach((Entity e, ref RenderPass pass) =>
            {
                if (pass.viewId == 0xffff) {
                    RenderDebug.LogFormat("Render pass entity {0} on render node entity {1} is not referenced by the render graph. It should be deleted.", e, pass.inNode);
                    Assert.IsTrue(false);
                    return;
                }
                bool rtt = EntityManager.HasComponent<FramebufferBGFX>(pass.inNode);
                // those could be more shared ... (that is, do all passes really need a copy of view & projection?)
                unsafe { fixed (float4x4* viewp = &pass.viewTransform, projp = &pass.projectionTransform) {
                    if ( bgfxsys.m_homogeneousDepth && bgfxsys.m_originBottomLeft ) { // gl style
                        bgfx.set_view_transform(pass.viewId, viewp, projp);
                        pass.flipCulling = 0;
                    } else { // dx style
                        bool yflip = !bgfxsys.m_originBottomLeft && rtt;
                        float4x4 adjustedProjection = RendererBGFXSystem.AdjustProjection(ref pass.projectionTransform, !bgfxsys.m_homogeneousDepth, yflip);
                        bgfx.set_view_transform(pass.viewId, viewp, &adjustedProjection);
                        pass.flipCulling = yflip ? (byte)3 : (byte)0;
                    }
                }}
                bgfx.set_view_mode(pass.viewId, (bgfx.ViewMode)pass.sorting);
                bgfx.set_view_rect(pass.viewId, pass.viewport.x, pass.viewport.y, pass.viewport.w, pass.viewport.h);
                bgfx.set_view_scissor(pass.viewId, pass.scissor.x, pass.scissor.y, pass.scissor.w, pass.scissor.h);
                bgfx.set_view_clear(pass.viewId, (ushort)pass.clearFlags, pass.clearRGBA, pass.clearDepth, pass.clearStencil);
                if ( rtt ) { 
                    var rttbgfx = EntityManager.GetComponentData<FramebufferBGFX>(pass.inNode);
                    bgfx.set_view_frame_buffer(pass.viewId, rttbgfx.handle );
                } else {
                    bgfx.set_view_frame_buffer(pass.viewId, new bgfx.FrameBufferHandle { idx=0xffff });
                }
                // touch it? needed?
                bgfx.touch(pass.viewId);
            });
        }
    }


}



