using System;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Tiny;
using Unity.Tiny.Assertions;
using Unity.Tiny.Rendering;
using Unity.Transforms;
#if !UNITY_WEBGL
using Unity.Tiny.STB;
#else
using Unity.Tiny.Web;
#endif
using Bgfx;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Burst;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Tiny.RendererExtras")]
[assembly: InternalsVisibleTo("Unity.Tiny.Rendering.Tests")]
[assembly: InternalsVisibleTo("Unity.Tiny.Android")]
[assembly: InternalsVisibleTo("Unity.2D.Tiny")]

namespace Unity.Tiny.Rendering
{
    // use this interface to make bgfx things public to other packages, like android that
    // wants re-init functionality 
    // (except for tests)
    public abstract class RenderingGPUSystem : ComponentSystem
    {
        public abstract void Init();
        public abstract void Shutdown();
        public abstract void ReloadAllImages();
    }

    internal struct TextureBGFX : ISystemStateComponentData
    {
        public bgfx.TextureHandle handle;
        public bool externalOwner;
    }

    internal struct TextureBGFXExternal : IComponentData
    {
        public UIntPtr value;
    }

    internal struct FramebufferBGFX : ISystemStateComponentData
    {
        public bgfx.FrameBufferHandle handle;
    }

    internal struct SimpleMeshBGFX : IComponentData
    {
        // this has the 
        public bgfx.IndexBufferHandle indexBufferHandle;
        public int indexCount;
        public bgfx.VertexBufferHandle vertexBufferHandle;
        public int vertexFirst, vertexCount;
        public AABB bounds;
        public bool externalOwner;
        public bgfx.VertexLayoutHandle vertexDeclHandle;
    }

    internal unsafe struct PerThreadDataBGFX
    {
        public LightingViewSpaceBGFX viewSpaceLightCache;
        public bgfx.Encoder *encoder;
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UpdateWorldBoundsSystem))]
    [AlwaysUpdateSystem]
    internal unsafe class RendererBGFXSystem : RenderingGPUSystem
    {
        private bgfx.VertexLayoutHandle m_simpleVertexBufferDeclHandle;
        public bgfx.VertexLayoutHandle SimpleVertexBufferDeclHandle { get { return m_simpleVertexBufferDeclHandle; } }
        private bgfx.VertexLayout[] m_simpleVertexBufferDecl = new bgfx.VertexLayout[8];
        public bgfx.VertexLayout[] SimpleVertexBufferDecl { get { return m_simpleVertexBufferDecl; } }
        private bgfx.VertexLayoutHandle m_simpleLitVertexBufferDeclHandle;
        public bgfx.VertexLayoutHandle SimpleLitVertexBufferDeclHandle { get { return m_simpleLitVertexBufferDeclHandle; } }
        private bgfx.VertexLayout[] m_simpleLitVertexBufferDecl = new bgfx.VertexLayout[8];
        public bgfx.VertexLayout[] SimpleLitVertexBufferDecl { get { return m_simpleLitVertexBufferDecl; } }
        private bgfx.TextureHandle m_whiteTexture;
        public bgfx.TextureHandle WhiteTexture { get { return m_whiteTexture; } }
        private bgfx.TextureHandle m_greyTexture;
        public bgfx.TextureHandle GreyTexture { get { return m_greyTexture; } }
        private bgfx.TextureHandle m_blackTexture;
        public bgfx.TextureHandle BlackTexture { get { return m_blackTexture; } }
        private bgfx.TextureHandle m_upTexture;
        public bgfx.TextureHandle UpTexture { get { return m_upTexture; } }
        private bgfx.TextureHandle m_noShadow;
        public bgfx.TextureHandle NoShadowTexture { get { return m_noShadow; } }
        private SimpleShader m_simpleShader;
        public SimpleShader SimpleShader { get { return m_simpleShader; } }
        private LineShader m_lineShader;
        public LineShader LineShader { get { return m_lineShader; } }
        public LitShader m_litShader;
        private ZOnlyShader m_zOnlyShader;
        public ZOnlyShader ZOnlyShader { get { return m_zOnlyShader; } }
        private BlitShader m_blitShader;
        public BlitShader BlitShader { get { return m_blitShader; } }
        private ShadowMapShader m_shadowMapShader;
        public ShadowMapShader ShadowMapShader { get { return m_shadowMapShader; } }

        private SimpleMeshBGFX m_quadMesh;
        public SimpleMeshBGFX QuadMesh { get { return m_quadMesh; } }
        private ExternalBlitES3Shader m_externalBlitES3Shader;
        public ExternalBlitES3Shader ExternalBlitES3Shader { get { return m_externalBlitES3Shader; } }

        private bool m_initialized;
        public bool Initialized { get { return m_initialized; } }
        private bool m_resume = false;
        private int m_fbWidth;
        private int m_fbHeight;

        internal int m_maxPerThreadData;
        internal PerThreadDataBGFX* m_perThreadDataPtr;
        internal NativeArray<PerThreadDataBGFX> m_perThreadData;

        private uint m_persistentFlags;
        public uint PersistentFlags { get { return m_persistentFlags; } set { m_persistentFlags = value; } }
        private uint m_frameFlags;
        public uint FrameFlags { get { return m_frameFlags; } set { m_frameFlags = value; } }
        public bool m_homogeneousDepth;
        public bool m_originBottomLeft;
        private float4 m_outputDebugSelect;
        public float4 OutputDebugSelect { get { return m_outputDebugSelect; } set { m_outputDebugSelect = value; } }

        private bool m_allowSRGBTexture;
        public bool AllowSRGBTextures { get { return m_allowSRGBTexture; } }

        private bool m_blitPrimarySRGB;
        public bool BlitPrimarySRGB { get { return m_blitPrimarySRGB; } }

        private bgfx.PlatformData m_platformData;

        private int m_screenShotWidth;
        public int ScreenShotWidth { get { return m_screenShotWidth; } set { m_screenShotWidth = value; } }
        private int m_screenShotHeight;
        public int ScreenShotHeight { get { return m_screenShotHeight; } set { m_screenShotHeight = value; } }
        private string m_screenShotPath;
        public string ScreenShotPath { get { return m_screenShotPath; } set { m_screenShotPath = value; } }
        private NativeList<byte> m_screenShot;
        public NativeList<byte> ScreenShot { get { return m_screenShot; } }

        void DestroyMesh(ref SimpleMeshBGFX mesh)
        {
            if (!mesh.externalOwner) {
                bgfx.destroy_index_buffer(mesh.indexBufferHandle);
                bgfx.destroy_vertex_buffer(mesh.vertexBufferHandle);
            }
            mesh.indexBufferHandle.idx = 0xffff;
            mesh.vertexBufferHandle.idx = 0xffff;
        }

        // helper: useful for triggering images from disk reload 
        // call DestroyAllTextures() followed by ReloadAllImages() to force reload all textures 
        // or ReloadAllImages() after a deinit and reinit to re-create textures 
        public override void ReloadAllImages()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            // with none Image2DLoadFromFile so we do not touch things where loading is in flight
            Entities.WithNone<Image2DLoadFromFile>().WithAny<Image2DLoadFromFileGuids, Image2DLoadFromFileImageFile, Image2DLoadFromFileMaskFile>().ForEach((Entity e) =>
            {
                if (EntityManager.HasComponent<Image2DLoadFromFileGuids>(e))
                {
                    var guid = EntityManager.GetComponentData<Image2DLoadFromFileGuids>(e);
                    Debug.LogFormatAlways("Trigger reload for image from guid: {0}, {1} at {2}", guid.imageAsset.ToString(), guid.maskAsset.ToString(), e);
                }
                else
                {
                    var fnImage = "";
                    if (EntityManager.HasComponent<Image2DLoadFromFileImageFile>(e))
                        fnImage = EntityManager.GetBufferAsString<Image2DLoadFromFileImageFile>(e);
                    var fnMask = "";
                    if (EntityManager.HasComponent<Image2DLoadFromFileMaskFile>(e))
                        fnMask = EntityManager.GetBufferAsString<Image2DLoadFromFileMaskFile>(e);
                    Debug.LogFormatAlways("Trigger reload for image from file: {0} {1} at {2}", fnImage, fnMask, e);
                }
                ecb.AddComponent<Image2DLoadFromFile>(e);
            });
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        // possible to debug call to force evicting all textures at runtime 
        // will not to trigger a image reload  to re-create them after (via
        internal void DestroyAllTextures()
        {
            if (!m_initialized)
                return;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities.ForEach((Entity e, ref TextureBGFX tex) =>
            {
                if (!tex.externalOwner)
                    bgfx.destroy_texture(tex.handle);
                ecb.RemoveComponent<TextureBGFX>(e);
            });

            // remove material caches 
            Entities.WithAll<LitMaterialBGFX>().ForEach((Entity e) =>
            {
                ecb.RemoveComponent<LitMaterialBGFX>(e);
            });
            Entities.WithAll<SimpleMaterialBGFX>().ForEach((Entity e) =>
            {
                ecb.RemoveComponent<SimpleMaterialBGFX>(e);
            });

            // need to also destroy framebuffers so rtt textures are re-created 
            Entities.ForEach((Entity e, ref FramebufferBGFX fb) =>
            {
                bgfx.destroy_frame_buffer(fb.handle);
                ecb.RemoveComponent<FramebufferBGFX>(e);
            });

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        public override void Shutdown()
        {
            DestroyAllTextures();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            Entities.ForEach((Entity e, ref SimpleMeshBGFX mesh) =>
            {
                DestroyMesh(ref mesh);
                ecb.RemoveComponent<SimpleMeshBGFX>(e);
            });
            Entities.WithAll<LitMaterialBGFX>().ForEach((Entity e) =>
            {
                ecb.RemoveComponent<LitMaterialBGFX>(e);
            });
            Entities.WithAll<SimpleMaterialBGFX>().ForEach((Entity e) =>
            {
                ecb.RemoveComponent<SimpleMaterialBGFX>(e);
            });
            ecb.Playback(EntityManager);
            ecb.Dispose();
            bgfx.destroy_texture(m_whiteTexture);
            bgfx.destroy_texture(m_greyTexture);
            bgfx.destroy_texture(m_blackTexture);
            bgfx.destroy_texture(m_upTexture);
            bgfx.destroy_texture(m_noShadow);
            m_simpleShader.Destroy();
            m_litShader.Destroy();
            m_lineShader.Destroy();
            m_zOnlyShader.Destroy();
            m_blitShader.Destroy();
            m_externalBlitES3Shader.Destroy();
            DestroyMesh(ref m_quadMesh);
            m_perThreadData.Dispose();
            m_perThreadDataPtr = null;
            bgfx.shutdown();
            m_screenShot.Dispose();
            MipMapHelper.Shutdown();
            m_initialized = false;
        }

        protected override void OnCreate()
        {
            InitEntityQueryCache(16);
            base.OnCreate();
        }

        protected override void OnDestroy()
        {
            if (!m_initialized)
                return;
            Shutdown();
            base.OnDestroy();
        }

        public static unsafe bgfx.Memory* CreateMemoryBlock(int size)
        {
            return bgfx.alloc((uint)size);
        }

        public static unsafe bgfx.Memory* CreateMemoryBlock(byte* mem, int size)
        {
            return bgfx.copy(mem, (uint)size);
        }

        public static string GetBackendString()
        {
            var backend = bgfx.get_renderer_type();
            return Marshal.PtrToStringAnsi(bgfx.get_renderer_name(backend));
        }

        public void SetFlagThisFrame(bgfx.DebugFlags flag)
        {
            FrameFlags |= (uint)flag;
            if (Initialized)
                bgfx.set_debug(PersistentFlags | FrameFlags);
        }

        public void SetFlagPersistent(bgfx.DebugFlags flag)
        {
            PersistentFlags |= (uint)flag;
            if (Initialized)
                bgfx.set_debug(PersistentFlags | FrameFlags);
        }

        public void ClearFlagPersistent(bgfx.DebugFlags flag)
        {
            PersistentFlags &= ~(uint)flag;
            if (Initialized)
                bgfx.set_debug(PersistentFlags | FrameFlags);
        }

        public override void Init()
        {
            World.GetExistingSystem<SubmitFrameSystem>().Enabled = false;

            var env = World.TinyEnvironment();
            var di = env.GetConfigData<DisplayInfo>();
            var nwh = World.GetExistingSystem<WindowSystem>().GetPlatformWindowHandle();

            m_screenShot = new NativeList<byte>(Allocator.Persistent);

            unsafe {
                bgfx.Init init = new bgfx.Init();
                init.callback = bgfx.CallbacksInit();
#if DEBUG
                init.debug = 1;
#else
                init.debug = 0;
#endif
                m_platformData = new bgfx.PlatformData { nwh = nwh.ToPointer() };

                // Must be called before bgfx::init
                fixed (bgfx.PlatformData* platformData = &m_platformData) {
                    bgfx.set_platform_data(platformData);
                }
                init.platformData = m_platformData;
                init.resolution.width = (uint)di.framebufferWidth;
                init.resolution.height = (uint)di.framebufferHeight;
                init.resolution.format = bgfx.TextureFormat.RGBA8;
                init.resolution.numBackBuffers = 1;
                init.resolution.reset = GetResetFlags(ref di);

#if UNITY_2019_3_OR_NEWER || UNITY_DOTSPLAYER
                m_maxPerThreadData = JobsUtility.JobWorkerCount; // could be 0 in single threaded mode
#else
                m_maxPerThreadData = JobsUtility.MaxJobThreadCount;
#endif
                if (m_maxPerThreadData == 0) // main thread only mode 
                    m_maxPerThreadData = 1;

                m_perThreadData = new NativeArray<PerThreadDataBGFX>(m_maxPerThreadData, Allocator.Persistent);
                m_perThreadDataPtr = (PerThreadDataBGFX*)m_perThreadData.GetUnsafePtr();
                FlushViewSpaceCache();

                init.limits.maxEncoders = (ushort)(m_maxPerThreadData + 1); // +1 for the default main thread encoder 
                init.limits.transientVbSize = 6 << 20; // BGFX_CONFIG_TRANSIENT_VERTEX_BUFFER_SIZE;
                init.limits.transientIbSize = 1 << 20; // BGFX_CONFIG_TRANSIENT_INDEX_BUFFER_SIZE;

                init.type = bgfx.RendererType.Count;
                //init.type = bgfx.RendererType.OpenGL;

                m_fbHeight = di.framebufferHeight;
                m_fbWidth = di.framebufferWidth;
                if (!bgfx.init(&init))
                    throw new InvalidOperationException("Failed BGFX init.");
            }
            RenderDebug.LogFormatAlways("BGFX init ok, backend is {0}.", GetBackendString());

            var caps = bgfx.get_caps();
            m_homogeneousDepth = caps->homogeneousDepth != 0 ? true : false;
            m_originBottomLeft = caps->originBottomLeft != 0 ? true : false;
            RenderDebug.LogFormatAlways("  Depth: {0} Origin: {1}", m_homogeneousDepth ? "[-1..1]" : "[0..1]", m_originBottomLeft ? "bottom left" : "top left");
            if ((caps->supported & (ulong)bgfx.CapsFlags.TextureCompareLequal) == 0)
                RenderDebug.LogFormatAlways("  No direct shadow map support.");

            m_persistentFlags = (uint)bgfx.DebugFlags.Text;
            bgfx.set_debug(m_persistentFlags);

            var backend = bgfx.get_renderer_type();
            unsafe {
                fixed (bgfx.VertexLayout* declp = m_simpleVertexBufferDecl) {
                    bgfx.vertex_layout_begin(declp, backend);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.Position, 3, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.Color0, 4, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_end(declp);
                    m_simpleVertexBufferDeclHandle = bgfx.create_vertex_layout(declp);
                }

                fixed (bgfx.VertexLayout* declp = m_simpleLitVertexBufferDecl) {
                    bgfx.vertex_layout_begin(declp, backend);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.Position, 3, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.Normal, 3, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.Tangent, 3, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.Bitangent, 3, bgfx.AttribType.Float, false, false);
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.Color0, 4, bgfx.AttribType.Float, false, false); // albedo
                    bgfx.vertex_layout_add(declp, bgfx.Attrib.TexCoord1, 2, bgfx.AttribType.Float, false, false); // metal_smoothness
                    bgfx.vertex_layout_end(declp);
                    m_simpleLitVertexBufferDeclHandle = bgfx.create_vertex_layout(declp);
                }
            }

            // Init from the singleton shader entity that contains shader blob assets
            var singleton = GetSingletonEntity<PrecompiledShaders>();
            Assert.IsTrue(singleton != Entity.Null);
            var shaderData = EntityManager.GetComponentData<PrecompiledShaders>(singleton);
            Assert.IsTrue(shaderData.SimpleShader != Entity.Null);
            Assert.IsTrue(shaderData.LitShader != Entity.Null);
            Assert.IsTrue(shaderData.LineShader != Entity.Null);
            Assert.IsTrue(shaderData.ZOnlyShader != Entity.Null);
            Assert.IsTrue(shaderData.BlitSRGBShader != Entity.Null);
            Assert.IsTrue(shaderData.ShadowMapShader != Entity.Null);

            unsafe
            {
                int fsl, vsl = 0;
                byte* fs_ptr = null;
                byte* vs_ptr = null;
                BGFXShaderHelper.GetPrecompiledShaderData(EntityManager, shaderData.SimpleShader, backend, ref fs_ptr, out fsl, ref vs_ptr, out vsl);
                m_simpleShader.Init(fs_ptr, fsl, vs_ptr, vsl, backend);

                BGFXShaderHelper.GetPrecompiledShaderData(EntityManager, shaderData.LitShader, backend, ref fs_ptr, out fsl, ref vs_ptr, out vsl);
                m_litShader.Init(fs_ptr, fsl, vs_ptr, vsl, backend);

                BGFXShaderHelper.GetPrecompiledShaderData(EntityManager, shaderData.LineShader, backend, ref fs_ptr, out fsl, ref vs_ptr, out vsl);
                m_lineShader.Init(fs_ptr, fsl, vs_ptr, vsl, backend);

                BGFXShaderHelper.GetPrecompiledShaderData(EntityManager, shaderData.ZOnlyShader, backend, ref fs_ptr, out fsl, ref vs_ptr, out vsl);
                m_zOnlyShader.Init(fs_ptr, fsl, vs_ptr, vsl, backend);

                BGFXShaderHelper.GetPrecompiledShaderData(EntityManager, shaderData.BlitSRGBShader, backend, ref fs_ptr, out fsl, ref vs_ptr, out vsl);
                m_blitShader.Init(fs_ptr, fsl, vs_ptr, vsl, backend);

                BGFXShaderHelper.GetPrecompiledShaderData(EntityManager, shaderData.ShadowMapShader, backend, ref fs_ptr, out fsl, ref vs_ptr, out vsl);
                m_shadowMapShader.Init(fs_ptr, fsl, vs_ptr, vsl, backend);
            }

            //if (backend == bgfx.RendererType.OpenGLES)
            //    m_externalBlitES3Shader.Init(backend);

            // default texture
            m_whiteTexture = BGFXShaderHelper.MakeUnitTexture(0xff_ff_ff_ff);
            m_blackTexture = BGFXShaderHelper.MakeUnitTexture(0x00_00_00_00);
            m_greyTexture = BGFXShaderHelper.MakeUnitTexture(0x7f_7f_7f_7f);
            m_upTexture = BGFXShaderHelper.MakeUnitTexture(0xff_ff_7f_7f);
            m_noShadow = BGFXShaderHelper.MakeNoShadowTexture(backend, 0xffff);

            // default mesh
            ushort[] indices = { 0, 1, 2, 2, 3, 0 };
            SimpleVertex[] vertices = { new SimpleVertex { Position = new float3(-1, -1, 0), TexCoord0 = new float2(0, 0), Color = new float4(1) },
                                      new SimpleVertex { Position = new float3( 1, -1, 0), TexCoord0 = new float2(1, 0), Color = new float4(1) },
                                      new SimpleVertex { Position = new float3( 1,  1, 0), TexCoord0 = new float2(1, 1), Color = new float4(1) },
                                      new SimpleVertex { Position = new float3(-1,  1, 0), TexCoord0 = new float2(0, 1), Color = new float4(1) } };
            fixed (ushort* indicesP = indices) fixed (SimpleVertex* verticesP = vertices)
                m_quadMesh = UploadSimpleMesh(indicesP, 6, verticesP, 4, new AABB { Center = new float3(0, 0, 0), Extents = new float3(1, 1, 0) });

            World.GetExistingSystem<SubmitFrameSystem>().Enabled = true;

            if (backend==bgfx.RendererType.OpenGLES)
                m_blitPrimarySRGB = true;

            if (!di.disableSRGB) {
                m_allowSRGBTexture = true;
            } else {
                RenderDebug.LogAlways("SRGB sampling and writing is disabled via DisplayInfo setting.");
                m_allowSRGBTexture = false;
                m_blitPrimarySRGB = false;
            }



            m_initialized = true;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            if (m_initialized)
                return;
            Init();
        }

        internal void FlushViewSpaceCache()
        {
            for (int i = 0; i < m_maxPerThreadData; i++)
                m_perThreadDataPtr[i].viewSpaceLightCache.cacheTag = -1;
        }

        internal float4x4 GetAdjustedProjection(ref RenderPass pass)
        {
            bool rtt = EntityManager.HasComponent<FramebufferBGFX>(pass.inNode);
            bool yflip = !m_originBottomLeft && rtt;
            return AdjustProjection(ref pass.projectionTransform, !m_homogeneousDepth, yflip);
        }

        static internal float4x4 AdjustShadowMapProjection(ref float4x4 m)
        {
            // adjust m so that xyz' = xyz * .5 + .5, to map ndc [-1..1] to [0..1] range 
            return new float4x4(
                (m.c0 + m.c0.wwww) * 0.5f,
                (m.c1 + m.c1.wwww) * 0.5f,
                (m.c2 + m.c2.wwww) * 0.5f,
                (m.c3 + m.c3.wwww) * 0.5f
            );
        }

        static internal float4x4 AdjustProjection(ref float4x4 m, bool zCompress, bool yFlip)
        {
            // adjust m so that z' = z * .5 + .5, to map ndc z from [-1..1] to [0..1] range 
            float4x4 m2 = m;
            if (zCompress) {
                m2.c0.z = (m2.c0.z + m2.c0.w) * 0.5f;
                m2.c1.z = (m2.c1.z + m2.c1.w) * 0.5f;
                m2.c2.z = (m2.c2.z + m2.c2.w) * 0.5f;
                m2.c3.z = (m2.c3.z + m2.c3.w) * 0.5f;
            }
            if (yFlip) {
                m2.c0.y = -m2.c0.y;
                m2.c1.y = -m2.c1.y;
                m2.c2.y = -m2.c2.y;
                m2.c3.y = -m2.c3.y;
            }
            return m2;
        }

        protected static unsafe string StringFromCString(byte *s)
        {
            if (s == null)
                return "";
            string rs = "";
            while ( *s != 0 )
            {
                rs += (char)*s;
                s++;
            }
            return rs;
        }

        protected void HandleCallbacks()
        {
            byte* callbackMem = null;
            bgfx.CallbackEntry* callbackLog = null; 
            int n = bgfx.CallbacksLock(&callbackMem, &callbackLog);
            string s;
            for ( int i=0; i<n; i++ )
            {
                bgfx.CallbackEntry e = callbackLog[i];
                switch ( e.callbacktype )
                {
                    case bgfx.CallbackType.Fatal:
                        s = StringFromCString(callbackMem + e.additionalAllocatedDataStart);
                        RenderDebug.LogAlways(s);
                        bgfx.CallbacksUnlockAndClear();
                        throw new InvalidOperationException("BGFX FATAL");
                    case bgfx.CallbackType.Trace:
                        s = StringFromCString(callbackMem + e.additionalAllocatedDataStart);
                        RenderDebug.LogAlways(s);
                        break;
                    case bgfx.CallbackType.ScreenShotDesc:
                        bgfx.ScreenShotDesc* desc = (bgfx.ScreenShotDesc * )(callbackMem + e.additionalAllocatedDataStart);
                        RenderDebug.LogFormatAlways("Screenshot captured: {0}*{1} {2} pitch={3}", desc->width, desc->height, desc->yflip!=0? "flipped" : "", desc->pitch);
                        Assert.IsTrue(desc->yflip == 0);
                        Assert.IsTrue(desc->width*4 == desc->pitch);
                        Assert.IsTrue(desc->width * desc->height * 4 == desc->size);
                        m_screenShotWidth = desc->width;
                        m_screenShotHeight = desc->height;
                        break;
                    case bgfx.CallbackType.ScreenShotFilename:
                        s = StringFromCString(callbackMem + e.additionalAllocatedDataStart);
                        RenderDebug.LogFormatAlways("  Filename is {0}", s);
                        m_screenShotPath = s;
                        break;
                    case bgfx.CallbackType.ScreenShot:
                        RenderDebug.LogFormatAlways("  Data available {0} bytes.", e.additionalAllocatedDataSize);
                        m_screenShot.ResizeUninitialized(e.additionalAllocatedDataSize);
                        UnsafeUtility.MemCpy(m_screenShot.GetUnsafePtr(), callbackMem + e.additionalAllocatedDataStart, e.additionalAllocatedDataSize);
                        break;
                    default:
                        RenderDebug.Log("Unknown BGFX callback type!");
                        break;
                }
            }
            bgfx.CallbacksUnlockAndClear();
        }

        static private uint GetResetFlags(ref DisplayInfo di)
        {
            uint flags = 0;
            if (!di.disableSRGB)
                flags |= (uint)bgfx.ResetFlags.SrgbBackbuffer;
            if (!di.disableVSync)
                flags |= (uint)bgfx.ResetFlags.Vsync;
            return flags;
        }

        protected override void OnUpdate()
        {
            if (!m_initialized)
            {
                if (m_resume)
                {
                    Init();
                    ReloadAllImages();
                    m_resume = false;
                }
                else
                {
                    return;
                }
            }

            var env = World.TinyEnvironment();
            var di = env.GetConfigData<DisplayInfo>();
            var nwh = World.GetExistingSystem<WindowSystem>().GetPlatformWindowHandle().ToPointer();
            bool needReset = di.width != m_fbWidth || di.height != m_fbHeight;
            if (m_platformData.nwh != nwh) {
                m_platformData.nwh = nwh;
                fixed (bgfx.PlatformData* platformData = &m_platformData) {
                    bgfx.set_platform_data(platformData);
                }
                needReset = true;
                RenderDebug.LogFormatAlways("BGFX native window handler updated");
            }
            if (needReset) {
                bgfx.reset((uint)di.width, (uint)di.height, GetResetFlags(ref di), bgfx.TextureFormat.RGBA8);
                m_fbWidth = di.width;
                m_fbHeight = di.height;
                RenderDebug.LogFormatAlways("Resize BGFX to {0}, {1}", m_fbWidth, m_fbHeight);
            }
            FlushViewSpaceCache();
            UploadTextures();
            UploadMeshes();
            UpdateRTT();
            UpdateExternalTextures();
            HandleCallbacks();
        }

        unsafe SimpleMeshBGFX UploadSimpleMesh(ushort* indices, int nindices, SimpleVertex* vertices, int nvertices, AABB bounds)
        {
            SimpleMeshBGFX outMesh;
            outMesh.indexBufferHandle = bgfx.create_index_buffer(CreateMemoryBlock((byte*)indices, nindices * 2), (ushort)bgfx.BufferFlags.None);
            fixed (bgfx.VertexLayout* declp = m_simpleVertexBufferDecl)
                outMesh.vertexBufferHandle = bgfx.create_vertex_buffer(CreateMemoryBlock((byte*)vertices, nvertices * sizeof(SimpleVertex)), declp, (ushort)bgfx.BufferFlags.None);
            RenderDebug.LogFormat("Uploaded plain BGFX mesh with {0} indices, {1} vertices.", nindices, nvertices);
            outMesh.indexCount = nindices;
            outMesh.vertexCount = nvertices;
            outMesh.vertexFirst = 0;
            outMesh.externalOwner = false;
            outMesh.bounds = bounds;
            outMesh.vertexDeclHandle = m_simpleVertexBufferDeclHandle;
            return outMesh;
        }

        unsafe SimpleMeshBGFX UploadSimpleMeshFromBlobAsset(SimpleMeshRenderData mesh)
        {
            ushort* indices = (ushort*)mesh.Mesh.Value.Indices.GetUnsafePtr();
            SimpleVertex* vertices = (SimpleVertex*)mesh.Mesh.Value.Vertices.GetUnsafePtr();
            int nindices = mesh.Mesh.Value.Indices.Length;
            int nvertices = mesh.Mesh.Value.Vertices.Length;
            return UploadSimpleMesh(indices, nindices, vertices, nvertices, mesh.Mesh.Value.Bounds);
        }

        unsafe SimpleMeshBGFX UploadLitMeshFromBlobAsset(LitMeshRenderData mesh, World w)
        {
            SimpleMeshBGFX outMesh = default;
            ushort* indices = (ushort*)mesh.Mesh.Value.Indices.GetUnsafePtr();
            int nindices = mesh.Mesh.Value.Indices.Length;
            Assert.IsTrue(nindices > 0 && nindices <= ushort.MaxValue);
            LitVertex* vertices = (LitVertex*)mesh.Mesh.Value.Vertices.GetUnsafePtr();
            int nvertices = mesh.Mesh.Value.Vertices.Length;

            outMesh.indexCount = nindices;
            outMesh.vertexCount = nvertices;
            outMesh.vertexFirst = 0;
            outMesh.externalOwner = false;
            outMesh.bounds = mesh.Mesh.Value.Bounds;
            outMesh.indexBufferHandle = bgfx.create_index_buffer(CreateMemoryBlock((byte*)indices, nindices * 2), (ushort)bgfx.BufferFlags.None);
            outMesh.vertexDeclHandle = m_simpleLitVertexBufferDeclHandle;
            fixed (bgfx.VertexLayout* declp = m_simpleLitVertexBufferDecl)
                outMesh.vertexBufferHandle = bgfx.create_vertex_buffer(CreateMemoryBlock((byte*)vertices, nvertices * sizeof(LitVertex)), declp, (ushort)bgfx.BufferFlags.None);
            RenderDebug.LogFormat("Uploaded lit & packed BGFX mesh with {0} indices, {1} vertices.", nindices, nvertices);
            return outMesh;
        }

        static unsafe T* GetBlobPtr<T>(ref BlobArray<T> b, int n) where T : unmanaged
        {
            if (b.Length == 0)
                return null;
            Assert.IsTrue(n == b.Length);
            return (T*)b.GetUnsafePtr();
        }

        private void UploadMeshes()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // simple ones 
            Entities.WithNone<SimpleMeshBGFX>().ForEach((Entity e, ref SimpleMeshRenderData mesh) =>
            {
                ecb.AddComponent(e, UploadSimpleMeshFromBlobAsset(mesh));
                // could remove asset mesh now - but needed for 2d batching, so we will do that only for things without static batching
                //ecb.RemoveComponent<MeshRenderData>(e);
            });

            Entities.WithNone<SimpleMeshBGFX>().ForEach((Entity e, ref LitMeshRenderData mesh) =>
            {
                ecb.AddComponent(e, UploadLitMeshFromBlobAsset(mesh, World));
            });
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private unsafe bgfx.Memory* InitMipMapChain32(int w, int h)
        {
            int countPixels = w * h;
            int wl = w, hl = h;
            for (; ; ) {
                wl = wl == 1 ? 1 : wl >> 1;
                hl = hl == 1 ? 1 : hl >> 1;
                countPixels += wl * hl;
                if (wl == 1 && hl == 1) break;
            }
            return bgfx.alloc((uint)countPixels * 4);
        }

        private unsafe bgfx.Memory* CreateMipMapChain32(int w, int h, uint* src, bool srgb)
        {
            bgfx.Memory* r = InitMipMapChain32(w, h);
            uint* dest = (uint*)r->data;
            UnsafeUtility.MemCpy(dest, src, w * h * 4);
            MipMapHelper.FillMipMapChain32(w, h, dest, srgb);
            return r;
        }

        private void UpdateExternalTextures()
        {
            Entities.ForEach((Entity e, ref TextureBGFX texbgfx, ref TextureBGFXExternal texext) =>
            {
                bgfx.override_internal_texture_ptr(texbgfx.handle, texext.value);
                texbgfx.externalOwner = true;
            });
        }

        private ulong TextureFlagsToBGFXSamplerFlags(Image2D im2d)
        {
            ulong samplerFlags = 0; //Default is repeat and trilinear

            if ((im2d.flags & TextureFlags.UClamp) == TextureFlags.UClamp)
                samplerFlags |= (ulong)bgfx.SamplerFlags.UClamp;
            if ((im2d.flags & TextureFlags.VClamp) == TextureFlags.VClamp)
                samplerFlags |= (ulong)bgfx.SamplerFlags.VClamp;
            if ((im2d.flags & TextureFlags.UMirror) == TextureFlags.UMirror)
                samplerFlags |= (ulong)bgfx.SamplerFlags.UMirror;
            if ((im2d.flags & TextureFlags.VMirror) == TextureFlags.VMirror)
                samplerFlags |= (ulong)bgfx.SamplerFlags.VMirror;
            if ((im2d.flags & TextureFlags.Point) == TextureFlags.Point)
                samplerFlags |= (ulong)bgfx.SamplerFlags.Point;
            if (m_allowSRGBTexture && (im2d.flags & TextureFlags.Srgb) == TextureFlags.Srgb)
                samplerFlags |= (ulong)bgfx.TextureFlags.Srgb;
            if ((im2d.flags & TextureFlags.MimapEnabled) == TextureFlags.MimapEnabled &&
                (im2d.flags & TextureFlags.Linear) == TextureFlags.Linear)
                samplerFlags |= (ulong)bgfx.SamplerFlags.MipPoint;

            return samplerFlags;
        }

        private static bool IsPot(int x)
        {
            if ( x<=0 ) return false;
            return (x & x-1)==0;
        }

        private static void AdjustFlagsForPot(ref Image2D im2d)
        {
            if ( !IsPot(im2d.imagePixelHeight) || !IsPot(im2d.imagePixelWidth) ) {
                if ( (im2d.flags & TextureFlags.UVClamp) != TextureFlags.UVClamp ) {
                    RenderDebug.LogFormat("Texture is not a power of tw  but is not set to UV clamp. Forcing clamp.");
                    im2d.flags &= ~(TextureFlags.UVMirror | TextureFlags.UVRepeat);
                    im2d.flags |= TextureFlags.UVClamp;
                }
                if ( (im2d.flags & TextureFlags.MimapEnabled) == TextureFlags.MimapEnabled ) {
                    RenderDebug.LogFormat("Texture is not a power of two but had mip maps enabled. Turning off mip maps.");
                    im2d.flags &= ~TextureFlags.MimapEnabled;
                }
            }
        }

        private void UploadTextures()
        {
            // upload all texture that need uploading - we do not track changes to images here. need a different mechanic for that. 
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);

            // memory image2d textures
            Entities.WithNone<TextureBGFX>().WithAll<Image2DMemorySource>().ForEach((Entity e, ref Image2D im2d) =>
            {
                if (im2d.status != ImageStatus.Loaded)
                    return;
                AdjustFlagsForPot(ref im2d);
                int w = im2d.imagePixelWidth;
                int h = im2d.imagePixelHeight;
                byte* pixels = (byte * )EntityManager.GetBufferRO<Image2DMemorySource>(e).GetUnsafePtr();
                bool isSRGB = (im2d.flags & TextureFlags.Srgb) == TextureFlags.Srgb;
                bool makeMips = (im2d.flags & TextureFlags.MimapEnabled) == TextureFlags.MimapEnabled;
                ulong flags = TextureFlagsToBGFXSamplerFlags(im2d);
                bgfx.Memory* bgfxblock = makeMips ? CreateMipMapChain32(w, h, (uint*)pixels, isSRGB) : CreateMemoryBlock(pixels, w * h * 4);
                bgfx.TextureHandle  texHandle = bgfx.create_texture_2d((ushort)w, (ushort)h, makeMips, 1, bgfx.TextureFormat.RGBA8, flags, bgfxblock);
                ecb.AddComponent(e, new TextureBGFX
                {
                    handle = texHandle,
                    externalOwner = false
                });
                RenderDebug.LogFormat("Uploaded BGFX texture {0},{1} from memory to bgfx index {2}", w, h, (int)texHandle.idx);
            });

#if !UNITY_WEBGL
            Entities.WithNone<TextureBGFX>().ForEach((Entity e, ref Image2D im2d, ref Image2DSTB imstb) =>
            {
                if (im2d.status != ImageStatus.Loaded)
                    return;
                AdjustFlagsForPot(ref im2d);
                bgfx.TextureHandle texHandle;
                unsafe {
                    int w = 0;
                    int h = 0;
                    byte* pixels = ImageIOSTBNativeCalls.GetImageFromHandle(imstb.imageHandle, ref w, ref h);
                    bool isSRGB = (im2d.flags & TextureFlags.Srgb) == TextureFlags.Srgb;
                    bool makeMips = (im2d.flags & TextureFlags.MimapEnabled) == TextureFlags.MimapEnabled;
                    ulong flags = TextureFlagsToBGFXSamplerFlags(im2d);
                    bgfx.Memory* bgfxblock = makeMips ? CreateMipMapChain32(w, h, (uint*)pixels, isSRGB) : CreateMemoryBlock(pixels, w * h * 4);
                    texHandle = bgfx.create_texture_2d((ushort)w, (ushort)h, makeMips, 1, bgfx.TextureFormat.RGBA8, flags, bgfxblock);
                    RenderDebug.LogFormat("Uploaded BGFX texture {0},{1} from image handle {2} to bgfx index {3}", w, h, imstb.imageHandle, (int)texHandle.idx);
                }
                ImageIOSTBNativeCalls.FreeBackingMemory(imstb.imageHandle);
                ecb.RemoveComponent<Image2DSTB>(e);
                ecb.AddComponent(e, new TextureBGFX {
                    handle = texHandle,
                    externalOwner = false
                });
            });
#else
            Entities.WithNone<TextureBGFX>().ForEach((Entity e, ref Image2D im2d, ref Image2DHTML imhtml) =>
            {
                if (im2d.status != ImageStatus.Loaded)
                    return;
                int w = im2d.imagePixelWidth;
                int h = im2d.imagePixelHeight;
                bgfx.TextureHandle texHandle;
                ulong flags = TextureFlagsToBGFXSamplerFlags(im2d);
                bool makeMips = (im2d.flags & TextureFlags.MimapEnabled) == TextureFlags.MimapEnabled;
                bool isSRGB = (im2d.flags & TextureFlags.Srgb) == TextureFlags.Srgb;
                isSRGB = false;
                unsafe {
                    bgfx.Memory* bgfxblock = makeMips?InitMipMapChain32(w, h):CreateMemoryBlock(w * h * 4);
                    ImageIOHTMLNativeCalls.ImageToMemory(imhtml.imageIndex, w, h, bgfxblock->data);
                    if (makeMips) MipMapHelper.FillMipMapChain32(w, h, (uint*)bgfxblock->data, isSRGB);
                    texHandle = bgfx.create_texture_2d((ushort)w, (ushort)h, makeMips, 1, bgfx.TextureFormat.RGBA8, flags, bgfxblock);
                }
                RenderDebug.LogFormat("Uploaded BGFX texture {0},{1} from image handle {2}", w, h, imhtml.imageIndex);
                ecb.AddComponent(e, new TextureBGFX {
                    handle = texHandle,
                    externalOwner = false
                });
            });
#endif
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void UpdateRTT()
        {
            // create bgfx textures for rtt textures 
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithNone<TextureBGFX>().ForEach((Entity e, ref Image2D im2d, ref Image2DRenderToTexture rtt) =>
            {
                ushort w = (ushort)im2d.imagePixelWidth;
                ushort h = (ushort)im2d.imagePixelHeight;
                bgfx.TextureFormat fmt = bgfx.TextureFormat.Unknown;
                ulong flags = TextureFlagsToBGFXSamplerFlags(im2d) | (ulong)bgfx.TextureFlags.Rt;
                switch (rtt.format) {
                    case RenderToTextureFormat.ShadowMap:
                        fmt = bgfx.TextureFormat.D16;
                        flags |= (ulong)bgfx.SamplerFlags.CompareLess;
                        break;
                    case RenderToTextureFormat.Depth:
                        fmt = bgfx.TextureFormat.D16; // needed for webgl on safari, should be D32 or D24 (see SAFARI_WEBGL_WORKAROUND) 
                        break;
                    case RenderToTextureFormat.DepthStencil:
                        fmt = bgfx.TextureFormat.D24S8;
                        break;
                    case RenderToTextureFormat.RGBA:
                        fmt = bgfx.TextureFormat.RGBA8;
                        break;
                    case RenderToTextureFormat.R:
                        fmt = bgfx.TextureFormat.R8;
                        break;
                    case RenderToTextureFormat.RGBA16f:
                        fmt = bgfx.TextureFormat.RGBA16F;
                        break;
                    case RenderToTextureFormat.R16f:
                        fmt = bgfx.TextureFormat.R16F;
                        break;
                    case RenderToTextureFormat.R32f:
                        fmt = bgfx.TextureFormat.R32F;
                        break;
                }
                var handle = bgfx.create_texture_2d(w, h, false, 1, fmt, flags, null);
                ecb.AddComponent(e, new TextureBGFX {
                    handle = handle,
                    externalOwner = false
                });
                RenderDebug.LogFormat("Created BGFX render target texture {0},{1} to bgfx index {2}", (int)w, (int)h, (int)handle.idx);
            });
            ecb.Playback(EntityManager);
            ecb.Dispose();

            // create bgfx framebuffers for rtt nodes
            ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithNone<FramebufferBGFX>().ForEach((Entity e, ref RenderNodeTexture rtt) =>
            {
                unsafe {
                    byte n = 0;
                    bgfx.Attachment* att = stackalloc bgfx.Attachment[4];
                    if (rtt.colorTexture != Entity.Null) {
                        var texBGFX = EntityManager.GetComponentData<TextureBGFX>(rtt.colorTexture);
                        att[n].access = bgfx.Access.Write; //?
                        att[n].handle = texBGFX.handle;
                        att[n].layer = 0;
                        att[n].mip = 0;
                        att[n].resolve = (byte)bgfx.ResolveFlags.None;
                        n++;
                    }
                    if (rtt.depthTexture != Entity.Null) {
                        var texBGFX = EntityManager.GetComponentData<TextureBGFX>(rtt.depthTexture);
                        att[n].access = bgfx.Access.Write; // ?
                        att[n].handle = texBGFX.handle;
                        att[n].layer = 0;
                        att[n].mip = 0;
                        att[n].resolve = (byte)bgfx.ResolveFlags.None;
                        n++;
                    }
                    ecb.AddComponent(e, new FramebufferBGFX {
                        handle = bgfx.create_frame_buffer_from_attachment(n, att, false)
                    });
                }
            });
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        static private int Clamp(int x, int iMin, int iMax)
        {
            if (x < iMin) return iMin;
            if (x > iMax) return iMax;
            return x;
        }

        static public uint PackColorBGFX(Color c)
        {
            return PackColorBGFX(c.ToLinear());
        }

        static public uint PackColorBGFX(float4 c)
        {
            int ri = Clamp((int)(c.x * 255.0f), 0, 255);
            int gi = Clamp((int)(c.y * 255.0f), 0, 255);
            int bi = Clamp((int)(c.z * 255.0f), 0, 255);
            int ai = Clamp((int)(c.w * 255.0f), 0, 255);
            return ((uint)ai << 0) | ((uint)bi << 8) | ((uint)gi << 16) | ((uint)ri << 24);
        }

        public static ulong MakeBGFXBlend(bgfx.StateFlags srcRGB, bgfx.StateFlags dstRGB, bgfx.StateFlags srcA, bgfx.StateFlags dstA)
        {
            return (((ulong)(srcRGB) | ((ulong)(dstRGB) << 4)))
                 | (((ulong)(srcA) | ((ulong)(dstA) << 4)) << 8);
        }

        public static ulong MakeBGFXBlend(bgfx.StateFlags srcRGB, bgfx.StateFlags dstRGB)
        {
            return (((ulong)(srcRGB) | ((ulong)(dstRGB) << 4)))
                 | (((ulong)(srcRGB) | ((ulong)(dstRGB) << 4)) << 8);
        }

        public bool HasScreenShot()
        {
            return ScreenShot.IsCreated && ScreenShot.Length != 0;
        }

        public void ResetScreenShot()
        {
            ScreenShotWidth = 0;
            ScreenShotHeight = 0;
            ScreenShotPath = null;
            ScreenShot.ResizeUninitialized(0);
        }

        public void RequestScreenShot(string s)
        {
            // invalidate previous
            if (ScreenShot.Length != 0)
                RenderDebug.LogFormat("Warning, previous screen shot {0} still allocated. It will be overwritten.", ScreenShotPath);
            bgfx.request_screen_shot(new bgfx.FrameBufferHandle { idx = 0xffff }, s);
        }

        // TODO: pause state should move to caller, does not belong in here really
        public void Pause(bool paused)
        {
            if (paused)
            {
                Shutdown();
            }
            else
            {
                m_resume = true;
            }
        }
    }

    // system that finalized renders 
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(RendererBGFXSystem))]
    public class SubmitFrameSystem : ComponentSystem
    {
        void CheckState()
        {
#if DEBUG 
            bgfx.dbg_text_clear(0, false);
#endif
            int nprim = GetEntityQuery(ComponentType.ReadOnly<RenderNodePrimarySurface>()).CalculateEntityCount();
            int ncam = GetEntityQuery(ComponentType.ReadOnly<Camera>()).CalculateEntityCount();
            if (nprim == 0 || ncam == 0) {
                uint clearcolor = 0;
#if DEBUG 
                var sys = World.GetExistingSystem<RendererBGFXSystem>();
                sys.SetFlagThisFrame(bgfx.DebugFlags.Text);
                if (nprim == 0) bgfx.dbg_text_printf(0, 0, 0xf, "No primary surface render node.", null);
                if (ncam == 0) bgfx.dbg_text_printf(0, 1, 0xf, "No cameras in scene.", null);
                float t = (float)World.Time.ElapsedTime * .25f;
                float4 warncolor = new float4(math.abs(math.sin(t)), math.abs(math.cos(t * .23f)) * .8f, math.abs(math.sin(t * 7.0f)) * .3f, 1.0f);
                clearcolor = RendererBGFXSystem.PackColorBGFX(warncolor);
#endif
                bgfx.set_view_rect(0, 0, 0, 10000, 10000);
                bgfx.set_view_frame_buffer(0, new bgfx.FrameBufferHandle { idx = 0xffff });
                bgfx.set_view_clear(0, (ushort)bgfx.ClearFlags.Color, clearcolor, 1, 0);
                bgfx.touch(0);
            } 
        }

        protected unsafe override void OnUpdate()
        {
            var sys = World.GetExistingSystem<RendererBGFXSystem>();
            if (!sys.Initialized)
                return;
            for (int i = 0; i < sys.m_maxPerThreadData; i++) {
                if (sys.m_perThreadDataPtr[i].encoder != null) {
                    bgfx.encoder_end(sys.m_perThreadDataPtr[i].encoder);
                    sys.m_perThreadDataPtr[i].encoder = null;
                }
            }

            // flash a calming color when there was nothing to render
            CheckState();

            // go bgfx!
            bgfx.frame(false);

            sys.FrameFlags = 0;
            bgfx.set_debug(sys.PersistentFlags);
        }
    }
}

