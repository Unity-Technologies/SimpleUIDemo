using System;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Tiny;
using Unity.Tiny.Assertions;
using Unity.Tiny.Rendering;
using Bgfx;
using System.Runtime.InteropServices;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;

namespace Unity.Tiny.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UpdateWorldBoundsSystem))]
    [UpdateBefore(typeof(SubmitFrameSystem))]
    [UpdateAfter(typeof(RendererBGFXSystem))]
    [UpdateAfter(typeof(PreparePassesSystem))]
    public class SubmitSystemGroup : ComponentSystemGroup
    {
    }

    public struct MappedLightBGFX
    {
        public bgfx.TextureHandle shadowMap;
        public float4x4 projection;
        public float4 color_invrangesqr;
        public float4 worldPosOrDir;
        public float4 mask;

        public void Set(float4x4 m, float3 color, float4 _worldPosOrDir, float _range, float4 _mask, bgfx.TextureHandle _shadowMap)
        {
            projection = m;
            color_invrangesqr = new float4(color, LightingBGFX.InverseSquare(_range) * _worldPosOrDir.w);
            worldPosOrDir = _worldPosOrDir;
            mask = _mask;
            shadowMap = _shadowMap;
        }
    }

    public unsafe struct LightingViewSpaceBGFX
    {
        public fixed float podl_positionOrDirViewSpace[LightingBGFX.maxPointOrDirLights * 4];
        public float4 mappedLight0_viewPosOrDir;
        public float4 mappedLight1_viewPosOrDir;
        public int cacheTag; // must init and invalidate as -1
    }

    public unsafe struct LightingBGFX : IComponentData
    {
        static public float InverseSquare(float x)
        {
            if (x <= 0.0f)
                return 0.0f;
            x = 1.0f / x;
            return x * x;
        }

        public const int maxMappedLights = 2;
        public int numMappedLights;
        public MappedLightBGFX mappedLight0;
        public MappedLightBGFX mappedLight1;
        public float4 mappedLight01sis;

        public void SetMappedLight(int idx, float4x4 m, float3 color, float4 worldPosOrDir, float range, float4 mask, bgfx.TextureHandle shadowMap, int shadowMapSize)
        {
            switch (idx) {
                case 0: 
                    mappedLight0.Set(m, color, worldPosOrDir, range, mask, shadowMap);
                    mappedLight01sis.x = (float)shadowMapSize;
                    mappedLight01sis.y = 1.0f / (float)shadowMapSize;
                    break;
                case 1: 
                    mappedLight1.Set(m, color, worldPosOrDir, range, mask, shadowMap);
                    mappedLight01sis.z = (float)shadowMapSize;
                    mappedLight01sis.w = 1.0f / (float)shadowMapSize;
                    break;
                default: throw new IndexOutOfRangeException();
            };
        }

        public const int maxPointOrDirLights = 8;
        public int numPointOrDirLights;
        public fixed float podl_positionOrDir[maxPointOrDirLights * 4];
        public fixed float podl_colorIVR[maxPointOrDirLights * 4];

        public void TransformToViewSpace(ref float4x4 viewTx, ref LightingViewSpaceBGFX dest, ushort viewId)
        {
            if (dest.cacheTag == viewId)
                return;
            // simple lights 
            fixed (float* pDest = dest.podl_positionOrDirViewSpace, pSrc = podl_positionOrDir) {
                for (int i = 0; i < numPointOrDirLights; i++)
                    *(float4*)(pDest + (i << 2)) = math.mul(viewTx, *(float4*)(pSrc + (i << 2)));
            }
            // mapped lights 
            dest.mappedLight0_viewPosOrDir = math.mul(viewTx, mappedLight0.worldPosOrDir);
            dest.mappedLight1_viewPosOrDir = math.mul(viewTx, mappedLight1.worldPosOrDir);
            dest.cacheTag = viewId;
        }

        public void SetPointLight(int idx, float3 pos, float range, float3 color)
        {
            Assert.IsTrue(idx >= 0 && idx < maxPointOrDirLights);
            Assert.IsTrue(math.lengthsq(color) > 0.0f);
            Assert.IsTrue(range > 0.0f);
            idx <<= 2;
            podl_positionOrDir[idx] = pos.x;
            podl_positionOrDir[idx + 1] = pos.y;
            podl_positionOrDir[idx + 2] = pos.z;
            podl_positionOrDir[idx + 3] = 1.0f;
            podl_colorIVR[idx] = color.x;
            podl_colorIVR[idx + 1] = color.y;
            podl_colorIVR[idx + 2] = color.z;
            podl_colorIVR[idx + 3] = InverseSquare(range);
        }

        public void SetDirLight(int idx, float3 dirWorldSpace, float3 color)
        {
            Assert.IsTrue(idx >= 0 && idx < maxPointOrDirLights);
            idx <<= 2;
            Assert.IsTrue(math.lengthsq(color) > 0.0f);
            Assert.IsTrue(math.lengthsq(dirWorldSpace) > 0.0f);
            podl_positionOrDir[idx] = -dirWorldSpace.x;
            podl_positionOrDir[idx + 1] = -dirWorldSpace.y;
            podl_positionOrDir[idx + 2] = -dirWorldSpace.z;
            podl_positionOrDir[idx + 3] = 0.0f;
            podl_colorIVR[idx] = color.x;
            podl_colorIVR[idx + 1] = color.y;
            podl_colorIVR[idx + 2] = color.z;
            podl_colorIVR[idx + 3] = 0.0f;
        }

        public float4 ambient;
    }

    [System.Serializable]
    public struct LightingRef : ISharedComponentData
    {
        public Entity e;
    }

    [UpdateInGroup(typeof(SubmitSystemGroup))]
    public class SubmitBlitters : ComponentSystem
    {
        protected override void OnUpdate()
        {
            var sys = World.GetExistingSystem<RendererBGFXSystem>();
            if (!sys.Initialized)
                return;

            Entities.ForEach((Entity e, ref BlitRenderer br) =>
            {
                if (!EntityManager.HasComponent<RenderToPasses>(e))
                    return;

                RenderToPasses toPassesRef = EntityManager.GetSharedComponentData<RenderToPasses>(e);
                DynamicBuffer<RenderToPassesEntry> toPasses = EntityManager.GetBufferRO<RenderToPassesEntry>(toPassesRef.e);
                var tex = EntityManager.GetComponentData<TextureBGFX>(br.texture);
                var im2d = EntityManager.GetComponentData<Image2D>(br.texture);
                float srcAspect = (float)im2d.imagePixelWidth / (float)im2d.imagePixelHeight;
                float4x4 m = float4x4.identity;
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    if (br.preserveAspect) {
                        float destAspect = (float)pass.viewport.w / (float)pass.viewport.h;
                        if (destAspect <= srcAspect) { // flip comparison to zoom in instead of black bars 
                            m.c0.x = 1.0f; m.c1.y = destAspect / srcAspect;
                        } else {
                            m.c0.x = srcAspect / destAspect; m.c1.y = 1.0f;
                        }
                    }
                    if (sys.BlitPrimarySRGB) {
                        // need to convert linear to srgb if we are not rendering to a texture in linear workflow
                        bool toPrimaryWithSRGB = EntityManager.HasComponent<RenderNodePrimarySurface>(pass.inNode) && sys.AllowSRGBTextures;
                        if (!toPrimaryWithSRGB)
                            SubmitHelper.SubmitBlitDirectFast(sys, pass.viewId, ref m, br.color, tex.handle);
                        else
                            SubmitHelper.SubmitBlitDirectExtended(sys, pass.viewId, ref m, tex.handle,
                                false, true, 0.0f, new float4(1.0f), new float4(0.0f), false);
                    } else {
                        SubmitHelper.SubmitBlitDirectFast(sys, pass.viewId, ref m, br.color, tex.handle);
                    }
                }
            });
        }
    }

    [UpdateInGroup(typeof(SubmitSystemGroup))]
    public class SubmitSimpleMesh : ComponentSystem
    {
        protected override void OnUpdate()
        {
            var sys = World.GetExistingSystem<RendererBGFXSystem>();
            if (!sys.Initialized)
                return;
            // get all MeshRenderer, cull them, and add them to graph nodes that need them 
            // any mesh renderer MUST have a shared component data that has a list of passes to render to
            // this list is usually very shared - all opaque meshes will render to all ZOnly and Opaque passes
            // this shared data is not dynamically updated - other systems are responsible to update them if needed
            // simple
            Entities.ForEach((Entity e, ref MeshRenderer mr, ref SimpleMeshReference meshRef, ref LocalToWorld tx, ref WorldBounds wb, ref WorldBoundingSphere wbs) =>
            {
                if (!EntityManager.HasComponent<RenderToPasses>(e))
                    return;

                RenderToPasses toPassesRef = EntityManager.GetSharedComponentData<RenderToPasses>(e);
                DynamicBuffer<RenderToPassesEntry> toPasses = EntityManager.GetBufferRO<RenderToPassesEntry>(toPassesRef.e);
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    if (EntityManager.HasComponent<Frustum>(ePass)) {
                        var frustum = EntityManager.GetComponentData<Frustum>(ePass);
                        if (Culling.Cull(ref wbs, ref frustum) == Culling.CullingResult.Outside)
                            continue;
                        // double cull as example only
                        if (Culling.IsCulled(ref wb, ref frustum))
                            continue;
                    }
                    var mesh = EntityManager.GetComponentData<SimpleMeshBGFX>(meshRef.mesh);
                    switch (pass.passType) {
                        case RenderPassType.ZOnly:
                            SubmitHelper.SubmitZOnlyDirect(sys, pass.viewId, ref mesh, ref tx.Value, mr.startIndex, mr.indexCount, pass.flipCulling);
                            break;
                        case RenderPassType.ShadowMap:
                            SubmitHelper.SubmitZOnlyDirect(sys, pass.viewId, ref mesh, ref tx.Value, mr.startIndex, mr.indexCount, (byte)(pass.flipCulling ^ 0x3));
                            break;
                        case RenderPassType.Transparent:
                        case RenderPassType.Opaque:
                            var material = EntityManager.GetComponentData<SimpleMaterialBGFX>(mr.material);
                            SubmitHelper.SubmitSimpleDirect(sys, pass.viewId, ref mesh, ref tx.Value, ref material, mr.startIndex, mr.indexCount, pass.flipCulling);
                            break;
                        default:
                            Assert.IsTrue(false);
                            break;
                    }
                }
            });
        }
        // other renderable "things" need to do something similar- a problem is that "things" need to be aware of all target nodes 
    }

    [UpdateInGroup(typeof(SubmitSystemGroup))]
    public class SubmitSimpleLitMeshChunked : JobComponentSystem
    {
        unsafe struct SubmitSimpleLitMeshJob : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<LocalToWorld> LocalToWorldType;
            [ReadOnly] public ArchetypeChunkComponentType<MeshRenderer> MeshRendererType;
            [ReadOnly] public ArchetypeChunkComponentType<LitMeshReference> LitMeshType;
            [ReadOnly] public ArchetypeChunkComponentType<WorldBounds> WorldBoundsType;
            [ReadOnly] public ArchetypeChunkComponentType<WorldBoundingSphere> WorldBoundingSphereType;
            [ReadOnly] public ArchetypeChunkComponentType<ChunkWorldBoundingSphere> ChunkWorldBoundingSphereType;
            [ReadOnly] public ArchetypeChunkComponentType<ChunkWorldBounds> ChunkWorldBoundsType;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> SharedRenderToPass;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> SharedLightingRef;
            [ReadOnly] public BufferFromEntity<RenderToPassesEntry> BufferRenderToPassesEntry;
            [ReadOnly] public ComponentDataFromEntity<SimpleMeshBGFX> ComponentSimpleMeshBGFX;
            [ReadOnly] public ComponentDataFromEntity<RenderPass> ComponentRenderPass;
            [ReadOnly] public ComponentDataFromEntity<LitMaterialBGFX> ComponentLitMaterialBGFX;
            [ReadOnly] public ComponentDataFromEntity<LightingBGFX> ComponentLightingBGFX;
            [ReadOnly] public ComponentDataFromEntity<Frustum> ComponentFrustum;
            [ReadOnly] public ComponentDataFromEntity<LitMeshRenderData> ComponentMeshRenderData;
#pragma warning disable 0649
            [NativeSetThreadIndex] internal int ThreadIndex;
#pragma warning restore 0649
            [ReadOnly] public PerThreadDataBGFX* PerThreadData;
            [ReadOnly] public int MaxPerThreadData;
            [ReadOnly] public RendererBGFXSystem BGFXSystem;

            public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var chunkLocalToWorld = chunk.GetNativeArray(LocalToWorldType);
                var chunkMeshRenderer = chunk.GetNativeArray(MeshRendererType);
                var chunkMeshReference = chunk.GetNativeArray(LitMeshType);
                var chunkWorldBoundingSphere = chunk.GetNativeArray(WorldBoundingSphereType);
                var boundingSphere = chunk.GetChunkComponentData(ChunkWorldBoundingSphereType).Value;
                var bounds = chunk.GetChunkComponentData(ChunkWorldBoundsType).Value;

                Entity lighte = SharedLightingRef[chunkIndex];
                var lighting = ComponentLightingBGFX[lighte];
                Entity rtpe = SharedRenderToPass[chunkIndex];

                Assert.IsTrue(ThreadIndex >= 0 && ThreadIndex < MaxPerThreadData);
                bgfx.Encoder* encoder = PerThreadData[ThreadIndex].encoder;
                if (encoder == null) {
                    encoder = bgfx.encoder_begin(true);
                    Assert.IsTrue(encoder != null);
                    PerThreadData[ThreadIndex].encoder = encoder;
                }
                DynamicBuffer<RenderToPassesEntry> toPasses = BufferRenderToPassesEntry[rtpe];

                // we can do this loop either way, passes first or renderers first. 
                // TODO: profile what is better!
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    Frustum frustum = default;
                    if (ComponentFrustum.Exists(ePass)) {
                        frustum = ComponentFrustum[ePass]; // TODO, just make frustum a member of pass?
                        if (Culling.Cull(ref boundingSphere, ref frustum) == Culling.CullingResult.Outside)
                            continue; // nothing to do for this pass
                    }
                    Assert.IsTrue(encoder != null);
                    var pass = ComponentRenderPass[ePass];
                    for (int j = 0; j < chunk.Count; j++) {
                        var wbs = chunkWorldBoundingSphere[j];
                        var meshRenderer = chunkMeshRenderer[j];
                        var meshRef = chunkMeshReference[j];
                        var mesh = ComponentSimpleMeshBGFX[meshRef.mesh];
                        var tx = chunkLocalToWorld[j].Value;
                        if (Culling.Cull(ref wbs, ref frustum) == Culling.CullingResult.Outside) // TODO: fine cull only if rough culling was !Inside
                            continue;
                        switch (pass.passType) { // TODO: we can hoist this out of the loop 
                            case RenderPassType.ZOnly:
                                SubmitHelper.EncodeZOnly(BGFXSystem, encoder, pass.viewId, ref mesh, ref tx, meshRenderer.startIndex, meshRenderer.indexCount, pass.flipCulling);
                                break;
                            case RenderPassType.ShadowMap:
                                float4 bias = new float4(0);
                                SubmitHelper.EncodeShadowMap(BGFXSystem, encoder, pass.viewId, ref mesh, ref tx, meshRenderer.startIndex, meshRenderer.indexCount, (byte)(pass.flipCulling ^ 0x3), bias);
                                break;
                            case RenderPassType.Transparent:
                            case RenderPassType.Opaque:
                                var material = ComponentLitMaterialBGFX[meshRenderer.material];
                                SubmitHelper.EncodeLit(BGFXSystem, encoder, pass.viewId, ref mesh, ref tx, ref material, ref lighting, ref pass.viewTransform, meshRenderer.startIndex, meshRenderer.indexCount, pass.flipCulling, ref PerThreadData[ThreadIndex].viewSpaceLightCache);
                                break;
                            default:
                                Assert.IsTrue(false);
                                break;
                        }
                    }
                }
            }
        }

        EntityQuery m_query;

        protected override void OnCreate()
        {
            m_query = GetEntityQuery(
                ComponentType.ReadOnly<MeshRenderer>(),
                ComponentType.ReadOnly<LitMeshReference>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<WorldBounds>(),
                ComponentType.ReadOnly<WorldBoundingSphere>(),
                ComponentType.ChunkComponentReadOnly<ChunkWorldBoundingSphere>(),
                ComponentType.ChunkComponentReadOnly<ChunkWorldBounds>(),
                ComponentType.ReadOnly<RenderToPasses>()
            );
        }

        protected override void OnDestroy()
        {
        }

        protected unsafe override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var sys = World.GetExistingSystem<RendererBGFXSystem>();
            if (!sys.Initialized)
                return inputDeps;

            var chunks = m_query.CreateArchetypeChunkArray(Allocator.Temp);
            NativeArray<Entity> sharedRenderToPass = new NativeArray<Entity>(chunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<Entity> sharedLightingRef = new NativeArray<Entity>(chunks.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            ArchetypeChunkSharedComponentType<RenderToPasses> renderToPassesType = GetArchetypeChunkSharedComponentType<RenderToPasses>();
            ArchetypeChunkSharedComponentType<LightingRef> lightingRefType = GetArchetypeChunkSharedComponentType<LightingRef>();

            // it really sucks we can't get shared components in the job itself
            for (int i = 0; i < chunks.Length; i++) {
                sharedRenderToPass[i] = chunks[i].GetSharedComponentData<RenderToPasses>(renderToPassesType, EntityManager).e;
                sharedLightingRef[i] = chunks[i].GetSharedComponentData<LightingRef>(lightingRefType, EntityManager).e;
            }
            chunks.Dispose();

            var encodejob = new SubmitSimpleLitMeshJob {
                LocalToWorldType = GetArchetypeChunkComponentType<LocalToWorld>(true),
                MeshRendererType = GetArchetypeChunkComponentType<MeshRenderer>(true),
                LitMeshType = GetArchetypeChunkComponentType<LitMeshReference>(true),
                WorldBoundsType = GetArchetypeChunkComponentType<WorldBounds>(true),
                WorldBoundingSphereType = GetArchetypeChunkComponentType<WorldBoundingSphere>(true),
                ChunkWorldBoundingSphereType = GetArchetypeChunkComponentType<ChunkWorldBoundingSphere>(true),
                ChunkWorldBoundsType = GetArchetypeChunkComponentType<ChunkWorldBounds>(true),
                SharedRenderToPass = sharedRenderToPass,
                SharedLightingRef = sharedLightingRef,
                BufferRenderToPassesEntry = GetBufferFromEntity<RenderToPassesEntry>(true),
                ComponentSimpleMeshBGFX = GetComponentDataFromEntity<SimpleMeshBGFX>(true),
                ComponentMeshRenderData = GetComponentDataFromEntity<LitMeshRenderData>(true),
                ComponentRenderPass = GetComponentDataFromEntity<RenderPass>(true),
                ComponentLitMaterialBGFX = GetComponentDataFromEntity<LitMaterialBGFX>(true),
                ComponentLightingBGFX = GetComponentDataFromEntity<LightingBGFX>(true),
                ComponentFrustum = GetComponentDataFromEntity<Frustum>(true),
                PerThreadData = sys.m_perThreadDataPtr,
                MaxPerThreadData = sys.m_maxPerThreadData,
                BGFXSystem = World.GetExistingSystem<RendererBGFXSystem>()
            };

            var encodejobHandle = encodejob.Schedule(m_query, inputDeps);
            return encodejobHandle;
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UpdateLightMatricesSystem))]
    [UpdateAfter(typeof(RendererBGFXSystem))]
    [UpdateBefore(typeof(SubmitSystemGroup))]
    public class UpdateBGFXLightSetups : ComponentSystem
    {
        private void AddMappedLight(ref LightingBGFX r, ref ShadowmappedLight sml, ref Light l, ref float4x4 tx, ref LightMatrices txCache, RendererBGFXSystem sys, bool isSpot)
        {
            if (r.numMappedLights >= LightingBGFX.maxMappedLights)
                throw new InvalidOperationException("Too many mapped lights");
            bgfx.TextureHandle texShadowMap = sys.NoShadowTexture;
            int shadowMapSize = 1;
            if (sml.shadowMap != Entity.Null && EntityManager.HasComponent<TextureBGFX>(sml.shadowMap))
            {
                var imShadowMap = EntityManager.GetComponentData<Image2D>(sml.shadowMap);
                Assert.IsTrue(imShadowMap.imagePixelWidth == imShadowMap.imagePixelHeight);
                shadowMapSize = imShadowMap.imagePixelHeight;
                texShadowMap = EntityManager.GetComponentData<TextureBGFX>(sml.shadowMap).handle;
            }
            float4 mask = isSpot ? new float4(1.0f, 1.0f, 1.0f, 0.0f) : new float4(0.0f, 0.0f, 0.0f, 1.0f);
            float4 worldPosOrDir = isSpot? new float4(tx.c3.xyz, 1.0f) : new float4(-tx.c2.xyz, 0.0f);
            r.SetMappedLight(r.numMappedLights, txCache.mvp, l.color * l.intensity, worldPosOrDir, l.clipZFar, mask, texShadowMap, shadowMapSize);
            r.numMappedLights++;
        }

        protected override void OnUpdate()
        {
            var sys = World.GetExistingSystem<RendererBGFXSystem>();

            // reset lighting 
            Entities.ForEach((ref LightingBGFX r) =>
            {
                r = default;
                r.mappedLight0.shadowMap = sys.NoShadowTexture;
                r.mappedLight1.shadowMap = sys.NoShadowTexture;
            });

            Entities.ForEach((DynamicBuffer<LightToBGFXLightingSetup> dest, ref Light l, ref AmbientLight al) =>
            {
                for (int i = 0; i < dest.Length; i++)
                {
                    LightingBGFX r = EntityManager.GetComponentData<LightingBGFX>(dest[i].e);
                    r.ambient.xyz += l.color * l.intensity;
                    EntityManager.SetComponentData<LightingBGFX>(dest[i].e, r);
                }
            });

            Entities.WithNone<ShadowmappedLight, AmbientLight>().ForEach((Entity e, DynamicBuffer<LightToBGFXLightingSetup> dest, ref LocalToWorld tx, ref Light l, ref DirectionalLight dl) =>
            {
                for (int i = 0; i < dest.Length; i++) {
                    LightingBGFX r = EntityManager.GetComponentData<LightingBGFX>(dest[i].e);
                    if (r.numPointOrDirLights >= LightingBGFX.maxPointOrDirLights)
                        throw new InvalidOperationException("Too many directional lights");
                    r.SetDirLight(r.numPointOrDirLights, math.normalize(tx.Value.c2.xyz), l.color * l.intensity);
                    r.numPointOrDirLights++;
                    EntityManager.SetComponentData<LightingBGFX>(dest[i].e, r);
                }
            });

            Entities.WithNone<ShadowmappedLight, DirectionalLight, AmbientLight>().ForEach((Entity e, DynamicBuffer<LightToBGFXLightingSetup> dest, ref LocalToWorld tx, ref Light l) =>
            {
                for (int i = 0; i < dest.Length; i++) {
                    LightingBGFX r = EntityManager.GetComponentData<LightingBGFX>(dest[i].e);
                    if (r.numPointOrDirLights >= LightingBGFX.maxPointOrDirLights)
                        throw new InvalidOperationException("Too many point lights");
                    r.SetPointLight(r.numPointOrDirLights, tx.Value.c3.xyz, l.clipZFar, l.color * l.intensity);
                    r.numPointOrDirLights++;
                    EntityManager.SetComponentData<LightingBGFX>(dest[i].e, r);
                }
            });

            Entities.ForEach((Entity e, DynamicBuffer<LightToBGFXLightingSetup> dest, ref LocalToWorld tx, ref LightMatrices txCache, ref Light l, ref SpotLight sl, ref ShadowmappedLight sml) =>
            {
                // matrix for now: world -> light projection
                for (int i = 0; i < dest.Length; i++) {
                    LightingBGFX r = EntityManager.GetComponentData<LightingBGFX>(dest[i].e);
                    AddMappedLight(ref r, ref sml, ref l, ref tx.Value, ref txCache, sys, true);
                    EntityManager.SetComponentData<LightingBGFX>(dest[i].e, r);
                }
            });

            Entities.ForEach((Entity e, DynamicBuffer<LightToBGFXLightingSetup> dest, ref LocalToWorld tx, ref LightMatrices txCache, ref Light l, ref DirectionalLight dl, ref ShadowmappedLight sml) =>
            {
                // matrix: world -> light projection
                for (int i = 0; i < dest.Length; i++) {
                    LightingBGFX r = EntityManager.GetComponentData<LightingBGFX>(dest[i].e);
                    AddMappedLight(ref r, ref sml, ref l, ref tx.Value, ref txCache, sys, false);
                    EntityManager.SetComponentData<LightingBGFX>(dest[i].e, r);
                }
            });

            Entities.ForEach((Entity e, ref LightingBGFX r) =>
            {
                if (r.numPointOrDirLights + r.numMappedLights <= 0)
                    RenderDebug.LogFormat("Warning: No lights found for lighting setup {0}.", e);
            });
        }
    }
}
