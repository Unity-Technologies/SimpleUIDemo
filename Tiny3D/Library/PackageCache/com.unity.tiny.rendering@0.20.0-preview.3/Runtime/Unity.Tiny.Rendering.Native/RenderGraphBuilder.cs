#define SAFARI_WEBGL_WORKAROUND

using System;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Tiny;
using Unity.Tiny.Assertions;
using Unity.Tiny.Rendering;
using Unity.Transforms;
using Bgfx;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Tiny.Scenes;

namespace Unity.Tiny.Rendering
{
    // This system creates a "default" render graph 
    // it is intended for debugging / quick setup only! 
    // Graphs should be built at export time!
    // Initialzing the render graph at runtime init will move a 
    // ton of archetypes around and will not scale! 

    // various tags that are used as hints to the builder

    public struct CameraMask : IComponentData
    {
        // add renderer to that camera if mask on camera AND mask on renderer != 0
        // note that if the mask component is missing it implies a mask with all bits set
        public ulong mask;
    }

    public struct ShadowMask : IComponentData
    {
        // cast shadows with regards to a light: if the light mask AND rendered mask !=0 
        // note that if the mask component is missing it implies a mask with all bits set
        public ulong mask;
    }

    public struct MainViewNodeTag : IComponentData
    {
        // tag main vie node for auto building
    }

    // SpriteRenderer is a bit different, as it needs to batch and strict sort
    // need to buffer up entities once 
    public struct SortSpritesEntry : IBufferElementData, IComparable<SortSpritesEntry>
    {
        // do not put extra stuff in here, it's shuffled around during sorting
        public ulong key;
        public Entity e;

        public int CompareTo(SortSpritesEntry other)
        {
            if (key != other.key)
                return key < other.key ? -1 : 1;
            else
                return e.Index - other.e.Index;
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public unsafe class RenderGraphBuilder : ComponentSystem
    {
        private struct SortedCamera : IComparable<SortedCamera>
        {
            public float depth;
            public Entity e;

            public int CompareTo(SortedCamera other)
            {
                if (depth == other.depth)
                    return e.Index - other.e.Index;
                return depth < other.depth ? -1 : 1;
            }
        }

        public Entity CreateFrontBufferRenderNode(int w, int h, bool primary)
        {
            Entity eNode = EntityManager.CreateEntity();
            EntityManager.AddComponentData(eNode, new RenderNode { });
            if (primary)
                EntityManager.AddComponentData(eNode, new RenderNodePrimarySurface { });

            Entity ePassBlit = EntityManager.CreateEntity();
            EntityManager.AddComponentData(ePassBlit, new RenderPass
            {
                inNode = eNode,
                sorting = RenderPassSort.Unsorted,
                projectionTransform = float4x4.identity,
                viewTransform = float4x4.identity,
                passType = RenderPassType.FullscreenQuad,
                viewId = 0xffff,
                scissor = new RenderPassRect(),
                viewport = new RenderPassRect { x = 0, y = 0, w = (ushort)w, h = (ushort)h },
                clearFlags = RenderPassClear.Color,
                clearRGBA = RendererBGFXSystem.PackColorBGFX(new Color(0, 0, 0, 0)),
                clearDepth = 1.0f,
                clearStencil = 0
            });
            EntityManager.AddComponent<RenderPassAutoSizeToNode>(ePassBlit);
            EntityManager.AddComponent<Frustum>(ePassBlit);

            DynamicBuffer<RenderNodeRef> nodeRefs = EntityManager.AddBuffer<RenderNodeRef>(eNode);
            DynamicBuffer<RenderPassRef> passRefs = EntityManager.AddBuffer<RenderPassRef>(eNode);
            passRefs.Add(new RenderPassRef { e = ePassBlit });

            return eNode;
        }

        private void SetPassComponents(Entity ePass, CameraMask cameraMask, Entity eCam)
        {
            EntityManager.AddComponent<RenderPassAutoSizeToNode>(ePass);
            EntityManager.AddComponent<Frustum>(ePass);
            EntityManager.AddComponentData<CameraMask>(ePass, cameraMask);
            EntityManager.AddComponentData(ePass, new RenderPassUpdateFromCamera
            {
                camera = eCam
            });
        }

        public void CreateAllPasses(int w, int h, Entity eCam, Entity eNode)
        {
            Camera cam = EntityManager.GetComponentData<Camera>(eCam);
            CameraMask cameraMask = new CameraMask { mask = ulong.MaxValue };
            if (EntityManager.HasComponent<CameraMask>(eCam))
                cameraMask = EntityManager.GetComponentData<CameraMask>(eCam);

            Entity ePassOpaque = EntityManager.CreateEntity();
            EntityManager.AddComponentData(ePassOpaque, new RenderPass
            {
                inNode = eNode,
                sorting = RenderPassSort.Unsorted,
                projectionTransform = float4x4.identity,
                viewTransform = float4x4.identity,
                passType = RenderPassType.Opaque,
                viewId = 0xffff,
                scissor = new RenderPassRect(),
                viewport = new RenderPassRect { x = 0, y = 0, w = (ushort)w, h = (ushort)h },
                clearFlags = RenderPassClear.Depth | (cam.clearFlags == CameraClearFlags.SolidColor ? RenderPassClear.Color : 0),
                clearRGBA = RendererBGFXSystem.PackColorBGFX(cam.backgroundColor),
                clearDepth = 1.0f,
                clearStencil = 0
            });
            SetPassComponents(ePassOpaque, cameraMask, eCam);

            Entity ePassTransparent = EntityManager.CreateEntity();
            EntityManager.AddComponentData(ePassTransparent, new RenderPass
            {
                inNode = eNode,
                sorting = RenderPassSort.SortZLess,
                projectionTransform = float4x4.identity,
                viewTransform = float4x4.identity,
                passType = RenderPassType.Transparent,
                viewId = 0xffff,
                scissor = new RenderPassRect(),
                viewport = new RenderPassRect { x = 0, y = 0, w = (ushort)w, h = (ushort)h },
                clearFlags = 0,
                clearRGBA = 0,
                clearDepth = 1.0f,
                clearStencil = 0
            });
            SetPassComponents(ePassTransparent, cameraMask, eCam);

            Entity ePassSprites = EntityManager.CreateEntity();
            EntityManager.AddComponentData(ePassSprites, new RenderPass
            {
                inNode = eNode,
                sorting = RenderPassSort.Sorted,
                projectionTransform = float4x4.identity,
                viewTransform = float4x4.identity,
                passType = RenderPassType.Sprites,
                viewId = 0xffff,
                scissor = new RenderPassRect(),
                viewport = new RenderPassRect { x = 0, y = 0, w = (ushort)w, h = (ushort)h },
                clearFlags = 0,
                clearRGBA = 0,
                clearDepth = 1.0f,
                clearStencil = 0
            });
            SetPassComponents(ePassSprites, cameraMask, eCam);
            EntityManager.AddBuffer<SortSpritesEntry>(ePassSprites);

            Entity ePassUI = EntityManager.CreateEntity();
            EntityManager.AddComponentData(ePassUI, new RenderPass
            {
                inNode = eNode,
                sorting = RenderPassSort.Sorted,
                projectionTransform = float4x4.identity,
                viewTransform = float4x4.identity,
                passType = RenderPassType.UI,
                viewId = 0xffff,
                scissor = new RenderPassRect(),
                viewport = new RenderPassRect { x = 0, y = 0, w = (ushort)w, h = (ushort)h },
                clearFlags = 0,
                clearRGBA = 0,
                clearDepth = 1.0f,
                clearStencil = 0
            });
            SetPassComponents(ePassUI, cameraMask, eCam);

            // add passes to node, in order
            DynamicBuffer<RenderPassRef> passRefs = EntityManager.GetBuffer<RenderPassRef>(eNode);
            passRefs.Add(new RenderPassRef { e = ePassOpaque });
            passRefs.Add(new RenderPassRef { e = ePassTransparent });
            passRefs.Add(new RenderPassRef { e = ePassSprites });
            passRefs.Add(new RenderPassRef { e = ePassUI });
        }

        public void AddRenderToShadowMapForNode(Entity eNode, int w, int h)
        {
            Entity eTexDepth = EntityManager.CreateEntity();
            EntityManager.AddComponentData(eTexDepth, new Image2DRenderToTexture { format = RenderToTextureFormat.ShadowMap });
            EntityManager.AddComponentData(eTexDepth, new Image2D
            {
                imagePixelWidth = w,
                imagePixelHeight = h, 
                status = ImageStatus.Loaded,
                flags = TextureFlags.UVClamp | TextureFlags.Nearest
            });
            Entity eTexColor = Entity.Null;
#if SAFARI_WEBGL_WORKAROUND
            // Safari webgl can not render to depth only. 
            // need to investigate more if this is caused by emscripten, bgfx, or Safari. 
            // for now, this workaround does the job altough we are wasting a bunch of memory
            eTexColor = EntityManager.CreateEntity();
            EntityManager.AddComponentData(eTexColor, new Image2DRenderToTexture { format = RenderToTextureFormat.RGBA });
            EntityManager.AddComponentData(eTexColor, new Image2D
            {
                imagePixelWidth = w,
                imagePixelHeight = h, 
                status = ImageStatus.Loaded,
                flags = TextureFlags.UVClamp | TextureFlags.Nearest
            });
#endif
            EntityManager.AddComponentData(eNode, new RenderNodeTexture
            {
                colorTexture = eTexColor,
                depthTexture = eTexDepth,
                rect = new RenderPassRect { x = 0, y = 0, w = (ushort)w, h = (ushort)h }
            });
        }

        public void AddRenderToTextureForNode(Entity eNode, int w, int h, bool color, bool depth)
        {
            Entity eTex = Entity.Null;
            if (color)
            {
                var di = GetSingleton<DisplayInfo>();
                eTex = EntityManager.CreateEntity();
                EntityManager.AddComponentData(eTex, new Image2DRenderToTexture { format = RenderToTextureFormat.RGBA });
                TextureFlags tf = TextureFlags.Linear | TextureFlags.UVClamp;
                if (!di.disableSRGB)
                    tf |= TextureFlags.Srgb;
                EntityManager.AddComponentData(eTex, new Image2D
                {
                    imagePixelWidth = w,
                    imagePixelHeight = h,
                    status = ImageStatus.Loaded,
                    flags = tf
                }); ;
            }
            Entity eTexDepth = Entity.Null;
            if (depth)
            {
                eTexDepth = EntityManager.CreateEntity();
                EntityManager.AddComponentData(eTexDepth, new Image2DRenderToTexture { format = RenderToTextureFormat.Depth });
                EntityManager.AddComponentData(eTexDepth, new Image2D
                {
                    imagePixelWidth = w,
                    imagePixelHeight = h,
                    status = ImageStatus.Loaded,
                    flags =  TextureFlags.Linear | TextureFlags.UVClamp
                });
            }
            EntityManager.AddComponentData(eNode, new RenderNodeTexture
            {
                colorTexture = eTex,
                depthTexture = eTexDepth,
                rect = new RenderPassRect { x = 0, y = 0, w = (ushort)w, h = (ushort)h }
            });
        }

        public Entity CreateRenderNodeFromCamera(Entity eCam, int w, int h, bool primary)
        {
            Entity eNode = EntityManager.CreateEntity();
            EntityManager.AddComponentData(eNode, new RenderNode { });
            if (primary)
            {
                EntityManager.AddComponentData(eNode, new RenderNodePrimarySurface { });
            }
            else
            {
                AddRenderToTextureForNode(eNode, w, h, true, true);
            }

            // prepare node 
            EntityManager.AddBuffer<RenderNodeRef>(eNode);
            EntityManager.AddBuffer<RenderPassRef>(eNode);

            CreateAllPasses(w, h, eCam, eNode);

            return eNode;
        }

        public void LinkNodes(Entity eThisNode, Entity eDependsOnThis)
        {
            if (!EntityManager.HasComponent<RenderNodeRef>(eThisNode))
                EntityManager.AddBuffer<RenderNodeRef>(eThisNode);
            DynamicBuffer<RenderNodeRef> nodeRefs = EntityManager.GetBuffer<RenderNodeRef>(eThisNode);
            nodeRefs.Add(new RenderNodeRef { e = eDependsOnThis });
        }

        public Entity FindPassOnNode(Entity node, RenderPassType pt)
        {
            var passes = EntityManager.GetBuffer<RenderPassRef>(node);
            for (int i = 0; i < passes.Length; i++)
            {
                var p = EntityManager.GetComponentData<RenderPass>(passes[i].e);
                if (p.passType == pt)
                    return passes[i].e;
            }
            return Entity.Null;
        }

        protected Entity CreateNodeEntity()
        {
            var e = EntityManager.CreateEntity();
            EntityManager.AddComponent<RenderNode>(e);
            EntityManager.AddBuffer<RenderNodeRef>(e);
            EntityManager.AddBuffer<RenderPassRef>(e);
            return e;
        }

        protected Entity BuildDefaultRenderGraph(int w, int h)
        {
            var eMainViewNode = CreateNodeEntity();
            World.TinyEnvironment().SetEntityName(eMainViewNode, "Auto generated: Main view node");
            EntityManager.AddComponent<MainViewNodeTag>(eMainViewNode);

            // gather & sort camera 
            NativeList<SortedCamera> cameras = new NativeList<SortedCamera>(Allocator.TempJob);
            Entities.ForEach((Entity e, ref Camera c) =>
            {
                cameras.Add(new SortedCamera { depth = c.depth, e = e });
            });
            cameras.Sort();

            // every camera needs passes - we don't really know here which one though, so just add everything for every camera. later we can remove unused passes 
            for (int i = 0; i < cameras.Length; i++)
            {
                CreateAllPasses(w, h, cameras[i].e, eMainViewNode);
            }
            cameras.Dispose();

            // add a target texture for main view 
            AddRenderToTextureForNode(eMainViewNode, w, h, true, true);

            // blit the main view node 
            var eFrontBufferNode = CreateFrontBufferRenderNode(w, h, true);
            World.TinyEnvironment().SetEntityName(eFrontBufferNode, "Auto generated: Front buffer node");
            LinkNodes(eFrontBufferNode, eMainViewNode);

            // build a blit renderer 
            AddBlitter(eMainViewNode, eFrontBufferNode);

            return eMainViewNode;
        }

        public Entity FindNodeWithComponent<T>()
        {
            EntityQuery q = GetEntityQuery(ComponentType.ReadOnly<T>(), ComponentType.ReadOnly<RenderNode>());
            using (var r = q.ToEntityArray(Allocator.TempJob))
            {
                Assert.IsTrue(r.Length == 1);
                return r[0];
            }
        }

        public Entity FindNodeColorOutput(Entity eNode)
        {
            if (!EntityManager.HasComponent<RenderNodeTexture>(eNode))
                return Entity.Null;
            var rnt = EntityManager.GetComponentData<RenderNodeTexture>(eNode);
            return rnt.colorTexture;
        }

        public void AddBlitter(Entity eSourceNode, Entity eTargetNode)
        {
            // blitter
            Entity erendBlit = EntityManager.CreateEntity();
            EntityManager.AddComponentData(erendBlit, new BlitRenderer
            {
                texture = FindNodeColorOutput(eSourceNode),
                color = new float4(1),
                preserveAspect = true
            });

            Entity eToBlitPasses = EntityManager.CreateEntity();
            var bToPasses = EntityManager.AddBuffer<RenderToPassesEntry>(eToBlitPasses);
            bToPasses.Add(new RenderToPassesEntry { e = FindPassOnNode(eTargetNode, RenderPassType.FullscreenQuad) });
            EntityManager.AddSharedComponentData(erendBlit, new RenderToPasses { e = eToBlitPasses });
        }

        protected void BuildAllLightNodes(Entity eNodeOutput)
        {
            Assert.IsTrue(EntityManager.HasComponent<RenderNode>(eNodeOutput));
            // go through all lights and create nodes 
            Entities.ForEach((Entity eLight, ref Light l, ref ShadowmappedLight sl) =>
            {
                if (sl.shadowMapRenderNode == Entity.Null)
                {
                    // need a node, with a pass
                    Entity eNode = CreateNodeEntity();
                    EntityManager.AddComponent<RenderNode>(eNode);
                    Entity ePass = eNode; //why not stick everything on the same entity! EntityManager.CreateEntity();
                    EntityManager.AddComponentData(ePass, new RenderPass
                    {
                        inNode = eNode,
                        sorting = RenderPassSort.Unsorted,
                        projectionTransform = float4x4.identity,
                        viewTransform = float4x4.identity,
                        passType = RenderPassType.ShadowMap,
                        viewId = 0xffff,
                        scissor = new RenderPassRect { x = 0, y = 0, w = 0, h = 0 },
                        viewport = new RenderPassRect { x = 0, y = 0, w = (ushort)sl.shadowMapResolution, h = (ushort)sl.shadowMapResolution },
#if SAFARI_WEBGL_WORKAROUND
                        clearFlags = RenderPassClear.Depth | RenderPassClear.Color,
#else
                        clearFlags = RenderPassClear.Depth,
#endif
                        clearRGBA = 0,
                        clearDepth = 1,
                        clearStencil = 0
                    });
                    EntityManager.AddComponentData(ePass, new RenderPassUpdateFromLight
                    {
                        light = eLight
                    });
                    EntityManager.AddComponentData(eNode, new RenderNodeShadowMap
                    {
                        lightsource = eLight,
                    });
                    sl.shadowMapRenderNode = eNode;
                    EntityManager.GetBuffer<RenderPassRef>(eNode).Add(new RenderPassRef { e = ePass });

                    LinkNodes(eNodeOutput, eNode);
                    RenderDebug.LogFormat("Build shadow map node {0}*{0}, input to {1}", sl.shadowMapResolution, eNodeOutput);
                    World.TinyEnvironment().SetEntityName(eNode, "Auto generated: Shadow Map");
                }
                if (sl.shadowMap == Entity.Null)
                {
                    AddRenderToShadowMapForNode(sl.shadowMapRenderNode, sl.shadowMapResolution, sl.shadowMapResolution);
                    var rtt = EntityManager.GetComponentData<RenderNodeTexture>(sl.shadowMapRenderNode);
                    sl.shadowMap = rtt.depthTexture;
                    Assert.IsTrue(sl.shadowMap != Entity.Null);
                }
            });
        }

        protected Entity FindMainViewNode()
        {
            var q = GetEntityQuery(ComponentType.ReadOnly<MainViewNodeTag>());
            var a = q.ToEntityArray(Allocator.TempJob);
            Entity r = Entity.Null;
            if (a.Length > 0)
                r = a[0];
            a.Dispose();
            return r;
        }

        protected int GetNumEntities()
        {
            var cs = EntityManager.GetAllChunks(Allocator.TempJob);
            int r = 0;
            foreach (var c in cs) r += c.Count;
            cs.Dispose();
            return r;
        }

        protected override void OnUpdate()
        {
            if (!SceneService.AreStartupScenesLoaded(World))
                return;
#if DEBUG
            var countEntsStart = GetNumEntities();
#endif
            Entity eMainView = FindMainViewNode();
            if (eMainView == Entity.Null)
            {
                // we only build a default graph if there are no existing nodes - otherwise assume they are already built
                RenderDebug.Log("Auto building default render graph");
                eMainView = BuildDefaultRenderGraph(1920, 1080);
            }

            // build light nodes for lights that have no node associated 
            BuildAllLightNodes(eMainView);

#if DEBUG
            var countEntsEnd = GetNumEntities();
            if (countEntsEnd != countEntsStart)
                RenderDebug.LogFormatAlways("Render graph builder added entities (was {0}, now {1})", countEntsStart, countEntsEnd);
#endif
            //Disable Render graph builder system. We need to create it only once
            Enabled = false;
        }
    }
    
    public struct BuildGroup : IComponentData, IEquatable<BuildGroup>
    {
        public RenderPassType passTypes;
        public CameraMask cameraMask;
        public ShadowMask shadowMask;

        override public int GetHashCode()
        {
            return (int)passTypes +
                   (int)cameraMask.mask + (int)(cameraMask.mask >> 32) +
                   (int)shadowMask.mask + (int)(shadowMask.mask >> 32);
        }

        public bool Equals(BuildGroup other)
        {
            return passTypes == other.passTypes &&
                   cameraMask.mask == other.cameraMask.mask &&
                   shadowMask.mask == other.shadowMask.mask;
        }
    }    

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(RenderGraphBuilder))]
    public unsafe class AssignRenderGroups : ComponentSystem
    {
        NativeHashMap<BuildGroup, Entity> m_buildGroups;
        NativeArray<Entity> m_allPasses;

        private Entity FindOrCreateRenderGroup(BuildGroup key)
        {
            Entity e = Entity.Null;
            if (m_buildGroups.TryGetValue(key, out e)) {
                return e;
            } else {
                e = EntityManager.CreateEntity();
                EntityManager.AddComponent<RenderGroup>(e);
                EntityManager.AddComponentData<BuildGroup>(e, key);
                var groupTargetPasses = EntityManager.AddBuffer<RenderToPassesEntry>(e);
                m_buildGroups.TryAdd(key, e);
                for (int i = 0; i < m_allPasses.Length; i++) {
                    var ePass = m_allPasses[i];
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    if (((uint)pass.passType & (uint)key.passTypes) == 0)
                        continue;
                    groupTargetPasses.Add(new RenderToPassesEntry { e = ePass });
                }
                return e;
            }
        }

        protected void OptionalSetSharedComponent<T>(Entity e, T value) where T : unmanaged, ISharedComponentData
        {
            if (EntityManager.HasComponent<T>(e)) {
                T oldValue = EntityManager.GetSharedComponentData<T>(e);
                if (UnsafeUtility.MemCmp(&oldValue, &value, sizeof(T)) != 0)
                    EntityManager.SetSharedComponentData<T>(e, value);
            } else {
                EntityManager.AddSharedComponentData<T>(e, value);
            }
        }

        protected int GetNumEntities()
        {
            var cs = EntityManager.GetAllChunks(Allocator.TempJob);
            int r = 0;
            foreach (var c in cs) r += c.Count;
            cs.Dispose();
            return r;
        }

        protected override void OnUpdate()
        {
            if (!SceneService.AreStartupScenesLoaded(World))
                return;
#if DEBUG
            var countEntsStart = GetNumEntities();
#endif
            // gather all passes
            var q = GetEntityQuery(ComponentType.ReadOnly<RenderPass>());
            m_allPasses = q.ToEntityArray(Allocator.TempJob);

            // go through lit renderers and assign lighting setup to them 
            // create light
            // TEMP HACK 
            var q2 = GetEntityQuery(ComponentType.ReadOnly<LightingBGFX>());
            Entity eLighting = Entity.Null;
            if (q2.CalculateEntityCount() == 0) {
                eLighting = EntityManager.CreateEntity();
                EntityManager.AddComponentData(eLighting, new LightingBGFX());
                // add both ways lookup lighting setup <-> light 
                var b1 = EntityManager.AddBuffer<LightToBGFXLightingSetup>(eLighting);
                Entities.WithAll<Light>().ForEach((Entity e) =>
                {
                    b1.Add(new LightToBGFXLightingSetup{ e = e });
                });
                Entities.WithAll<Light>().ForEach((Entity e) =>
                {
                    var b = EntityManager.AddBuffer<LightToBGFXLightingSetup>(e);
                    b.Add(new LightToBGFXLightingSetup{ e = eLighting });
                });
            } else {
                var a = q2.ToEntityArray(Allocator.TempJob);
                Assert.IsTrue(a.Length == 1);
                eLighting = a[0];
                a.Dispose();
            }
            Entities.WithNone<LightingRef>().ForEach((Entity e, ref MeshRenderer rlmr, ref LitMeshReference meshRef) =>
            {
                OptionalSetSharedComponent(e, new LightingRef { e = eLighting }); 
            });
            // EOH

            m_buildGroups = new NativeHashMap<BuildGroup, Entity>(256, Allocator.TempJob);

            // find existing groups and add them to builder 
            Entities.WithAll<RenderGroup>().ForEach((Entity e, ref BuildGroup bg) =>
            {
                m_buildGroups.Add(bg, e);
            });

            // go through all known render types, and assign groups to them
            // assign groups to all renderers 
            Entities.WithNone<RenderToPasses>().ForEach((Entity e, ref MeshRenderer rlmr) =>
            {
                bool isTransparent = false;
                if (EntityManager.HasComponent<SimpleMeshReference>(e))
                {
                    if(EntityManager.HasComponent<SimpleMaterial>(rlmr.material))
                        isTransparent = EntityManager.GetComponentData<SimpleMaterial>(rlmr.material).transparent;
                    else if(EntityManager.HasComponent<SimpleMaterialBGFX>(rlmr.material))
                    {
                        var matBGFX = EntityManager.GetComponentData<SimpleMaterialBGFX>(rlmr.material);
                        isTransparent = (matBGFX.state & (ulong)bgfx.StateFlags.WriteZ) != (ulong)bgfx.StateFlags.WriteZ;
                    }
                }
                else if (EntityManager.HasComponent<LitMeshReference>(e))
                {
                    if (EntityManager.HasComponent<LitMaterial>(rlmr.material))
                        isTransparent = EntityManager.GetComponentData<LitMaterial>(rlmr.material).transparent;
                    else if(EntityManager.HasComponent<LitMaterialBGFX>(rlmr.material))
                    {
                        var matBGFX = EntityManager.GetComponentData<LitMaterialBGFX>(rlmr.material);
                        isTransparent = (matBGFX.state & (ulong)bgfx.StateFlags.WriteZ) != (ulong)bgfx.StateFlags.WriteZ;
                    }
                }

                CameraMask cameraMask = new CameraMask { mask = ulong.MaxValue };
                if (EntityManager.HasComponent<CameraMask>(e))
                    cameraMask = EntityManager.GetComponentData<CameraMask>(e);
                ShadowMask shadowMask = new ShadowMask { mask = ulong.MaxValue };
                if (EntityManager.HasComponent<ShadowMask>(e))
                    shadowMask = EntityManager.GetComponentData<ShadowMask>(e);
                Entity eGroup;
                if (isTransparent)
                    eGroup = FindOrCreateRenderGroup(new BuildGroup { passTypes = RenderPassType.Transparent, cameraMask = cameraMask, shadowMask = shadowMask });
                else
                    eGroup = FindOrCreateRenderGroup(new BuildGroup { passTypes = RenderPassType.Opaque | RenderPassType.ZOnly | RenderPassType.ShadowMap, cameraMask = cameraMask, shadowMask = shadowMask });

                OptionalSetSharedComponent(e, new RenderToPasses { e = eGroup });
            });

            Entities.WithNone<RenderToPasses>().ForEach((Entity e, ref GizmoLight rlgmr) =>
            {
                ShadowMask shadowMask = new ShadowMask { mask = ulong.MaxValue };
                CameraMask cameraMask = new CameraMask { mask = 0 };
                Entity eGroup = FindOrCreateRenderGroup(new BuildGroup { passTypes = RenderPassType.Transparent, cameraMask = cameraMask, shadowMask = shadowMask });
                OptionalSetSharedComponent(e, new RenderToPasses { e = eGroup });
            });


            m_buildGroups.Dispose();
            m_allPasses.Dispose();

            // TODO: remove any passes that are not referenced by anything 

#if DEBUG
            var countEntsEnd = GetNumEntities();
            if (countEntsEnd != countEntsStart)
                RenderDebug.LogFormatAlways("Render graph builder added entities (was {0}, now {1})", countEntsStart, countEntsEnd);
#endif
        }
    }
}
