using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Tiny;
using Unity.Transforms;
#if !UNITY_WEBGL
using Unity.Tiny.STB;
#else
using Unity.Tiny.Web;
#endif
using Bgfx;
using Unity.Tiny.Assertions;


namespace Unity.Tiny.Rendering
{
    public static class PrecompiledShaderDataExtention
    {
        public static ref BlobArray<byte> DataForBackend(ref this PrecompiledShaderData data, bgfx.RendererType type)
        {
            switch (type)
            {
                case bgfx.RendererType.Direct3D9: return ref data.dx9;
                case bgfx.RendererType.Direct3D11:
                case bgfx.RendererType.Direct3D12:
                    return ref data.dx11;
                case bgfx.RendererType.Metal: return ref data.metal;
                case bgfx.RendererType.OpenGLES: return ref data.glsles;
                case bgfx.RendererType.OpenGL: return ref data.glsl;
                case bgfx.RendererType.Vulkan: return ref data.spirv;
                default:
                    Debug.LogFormatAlways("No shader loaded for render type: {0}.", Marshal.PtrToStringAnsi(bgfx.get_renderer_name(type)));
                    return ref data.dx11;
            }
        }
    }

    public static class BGFXShaderHelper
    {
        public static bgfx.TextureHandle MakeUnitTexture(uint value)
        {
            bgfx.TextureHandle ret;
            unsafe {
                ret = bgfx.create_texture_2d(1, 1, false, 1, bgfx.TextureFormat.RGBA8, (ulong)bgfx.TextureFlags.None, CreateMemoryBlock((byte*)&value, 4));
            }
            return ret;
        }

        public static bgfx.TextureHandle MakeNoShadowTexture(bgfx.RendererType backend, ushort value)
        {
            bgfx.TextureHandle ret;
            unsafe {
                if (backend == bgfx.RendererType.OpenGLES)
                    ret = MakeUnitTexture(0xffffffff);
                else
                    ret = bgfx.create_texture_2d(1, 1, false, 1, bgfx.TextureFormat.D16, (ulong)bgfx.SamplerFlags.UClamp | (ulong)bgfx.SamplerFlags.VClamp | (ulong)bgfx.SamplerFlags.CompareLess, CreateMemoryBlock((byte*)&value, 2));
            }
            return ret;
        }

        public static unsafe bgfx.ProgramHandle MakeProgram(bgfx.RendererType backend, byte* fs, int fsLength, byte* vs, int vsLength, string debugName)
        {
            bgfx.ShaderHandle fshandle, vshandle;

            fshandle = bgfx.create_shader(CreateMemoryBlock(fs, fsLength));
            vshandle = bgfx.create_shader(CreateMemoryBlock(vs, vsLength));

            bgfx.set_shader_name(vshandle, debugName, debugName.Length);
            bgfx.set_shader_name(fshandle, debugName, debugName.Length);
            return bgfx.create_program(vshandle, fshandle, true);
        }

        private static unsafe bgfx.Memory* CreateMemoryBlock(byte* mem, int size)
        {
            return bgfx.copy(mem, (uint)size);
        }

        private static unsafe bgfx.Memory* CreateMemoryBlock(byte[] ar)
        {
            fixed (byte* bar = ar) return bgfx.copy(bar, (uint)ar.Length);
        }

        public static unsafe void GetPrecompiledShaderData(EntityManager em, Entity e, bgfx.RendererType backend, ref byte* fs_ptr, out int fsl, ref byte* vs_ptr, out int vsl)
        {
            var fs_data = em.GetComponentData<FragmentShaderBinData>(e);
            var vs_data = em.GetComponentData<VertexShaderBinData>(e);
            fsl = fs_data.data.Value.DataForBackend(backend).Length;
            vsl = vs_data.data.Value.DataForBackend(backend).Length;
            Assert.IsTrue(fsl > 0 && vsl > 0, "Shader binary for this backend is not present. Try re-converting the scene for the correct target.");
            fs_ptr = (byte*)fs_data.data.Value.DataForBackend(backend).GetUnsafePtr();
            vs_ptr = (byte*)vs_data.data.Value.DataForBackend(backend).GetUnsafePtr();
        }
    };

    public struct LitShader
    {
        public struct MappedLight
        {
            public bgfx.UniformHandle m_samplerShadow;
            public bgfx.UniformHandle m_uniformColorIVR;
            public bgfx.UniformHandle m_uniformViewPosOrDir;
            public bgfx.UniformHandle m_uniformLightMask;
            public bgfx.UniformHandle m_uniformMatrix;
        }

        // fragment
        public bgfx.UniformHandle m_numLights;

        public bgfx.UniformHandle m_simplelightPosOrDir;
        public bgfx.UniformHandle m_simplelightColorIVR;
        
        public MappedLight m_mappedLight0;
        public MappedLight m_mappedLight1;
        public bgfx.UniformHandle m_texShadow01sis;

        public bgfx.UniformHandle m_samplerAlbedo;
        public bgfx.UniformHandle m_samplerEmissive;
        public bgfx.UniformHandle m_samplerOpacity;
        public bgfx.UniformHandle m_samplerSmoothness;
        public bgfx.UniformHandle m_samplerMetal;
        public bgfx.UniformHandle m_samplerNormal;

        public bgfx.UniformHandle m_uniformAmbient;
        public bgfx.UniformHandle m_uniformEmissiveNormalZScale;
        public bgfx.UniformHandle m_uniformOutputDebugSelect;

        // vertex 
        public bgfx.UniformHandle m_uniformAlbedoOpacity;
        public bgfx.UniformHandle m_uniformMetalSmoothness;
        public bgfx.UniformHandle m_uniformTexMad;
        public bgfx.UniformHandle m_uniformModelInverseTranspose;

        // program
        public bgfx.ProgramHandle m_prog;

        private void InitMappedLight(ref MappedLight dest, string namePostFix)
        {
            // samplers
            dest.m_samplerShadow = bgfx.create_uniform("s_texShadow"+namePostFix, bgfx.UniformType.Sampler, 1);
            // fs
            dest.m_uniformColorIVR = bgfx.create_uniform("u_light_color_ivr"+namePostFix, bgfx.UniformType.Vec4, 1);
            dest.m_uniformViewPosOrDir = bgfx.create_uniform("u_light_pos"+namePostFix, bgfx.UniformType.Vec4, 1);
            dest.m_uniformLightMask = bgfx.create_uniform("u_light_mask"+namePostFix, bgfx.UniformType.Vec4, 1);
            // vs
            dest.m_uniformMatrix = bgfx.create_uniform("u_wl_light"+namePostFix, bgfx.UniformType.Mat4, 1);
        }

        private void DestroyMappedLight(ref MappedLight dest)
        {
            bgfx.destroy_uniform(dest.m_samplerShadow);
            bgfx.destroy_uniform(dest.m_uniformLightMask);
            bgfx.destroy_uniform(dest.m_uniformColorIVR);
            bgfx.destroy_uniform(dest.m_uniformViewPosOrDir);
            bgfx.destroy_uniform(dest.m_uniformMatrix);
        }
     
        public unsafe void Init(byte* fs_ptr, int fsl, byte* vs_ptr, int vsl, bgfx.RendererType backend)
        {
            m_prog = BGFXShaderHelper.MakeProgram(backend, fs_ptr, fsl, vs_ptr, vsl, "lit");

            m_samplerAlbedo = bgfx.create_uniform("s_texAlbedo", bgfx.UniformType.Sampler, 1);
            m_samplerMetal = bgfx.create_uniform("s_texMetal", bgfx.UniformType.Sampler, 1);
            m_samplerNormal = bgfx.create_uniform("s_texNormal", bgfx.UniformType.Sampler, 1);
            m_samplerSmoothness = bgfx.create_uniform("s_texSmoothness", bgfx.UniformType.Sampler, 1);
            m_samplerEmissive = bgfx.create_uniform("s_texEmissive", bgfx.UniformType.Sampler, 1);
            m_samplerOpacity = bgfx.create_uniform("s_texOpacity", bgfx.UniformType.Sampler, 1);

            m_uniformAlbedoOpacity = bgfx.create_uniform("u_albedo_opacity", bgfx.UniformType.Vec4, 1);
            m_uniformMetalSmoothness = bgfx.create_uniform("u_metal_smoothness", bgfx.UniformType.Vec4, 1);

            m_uniformAmbient = bgfx.create_uniform("u_ambient", bgfx.UniformType.Vec4, 1);
            m_uniformTexMad = bgfx.create_uniform("u_texmad", bgfx.UniformType.Vec4, 1);
            m_uniformModelInverseTranspose = bgfx.create_uniform("u_modelInverseTranspose", bgfx.UniformType.Mat3, 1);
            m_uniformEmissiveNormalZScale = bgfx.create_uniform("u_emissive_normalz", bgfx.UniformType.Vec4, 1);

            InitMappedLight(ref m_mappedLight0, "0");
            InitMappedLight(ref m_mappedLight1, "1");
            m_texShadow01sis = bgfx.create_uniform("u_texShadow01sis", bgfx.UniformType.Vec4, 1);

            Assert.IsTrue(LightingBGFX.maxPointOrDirLights == 8); // must match array size in shader
            m_simplelightPosOrDir = bgfx.create_uniform("u_simplelight_posordir", bgfx.UniformType.Vec4, 8);
            m_simplelightColorIVR = bgfx.create_uniform("u_simplelight_color_ivr", bgfx.UniformType.Vec4, 8);

            m_numLights = bgfx.create_uniform("u_numlights", bgfx.UniformType.Vec4, 1);

            m_uniformOutputDebugSelect = bgfx.create_uniform("u_outputdebugselect", bgfx.UniformType.Vec4, 1);
        }

        public void Destroy()
        {
            bgfx.destroy_program(m_prog);
            bgfx.destroy_uniform(m_samplerAlbedo);
            bgfx.destroy_uniform(m_samplerMetal);
            bgfx.destroy_uniform(m_samplerSmoothness);
            bgfx.destroy_uniform(m_samplerEmissive);
            bgfx.destroy_uniform(m_samplerNormal);
            bgfx.destroy_uniform(m_samplerOpacity);

            bgfx.destroy_uniform(m_uniformAlbedoOpacity);
            bgfx.destroy_uniform(m_uniformMetalSmoothness);

            bgfx.destroy_uniform(m_uniformAmbient);
            bgfx.destroy_uniform(m_uniformEmissiveNormalZScale);
            bgfx.destroy_uniform(m_uniformOutputDebugSelect);

            bgfx.destroy_uniform(m_uniformTexMad);
            bgfx.destroy_uniform(m_uniformModelInverseTranspose);

            bgfx.destroy_uniform(m_numLights);

            DestroyMappedLight(ref m_mappedLight0);
            DestroyMappedLight(ref m_mappedLight1);
            bgfx.destroy_uniform(m_texShadow01sis);

            bgfx.destroy_uniform(m_simplelightPosOrDir);
            bgfx.destroy_uniform(m_simplelightColorIVR);
        }
    }

    public struct SimpleShader
    {
        public bgfx.ProgramHandle m_prog;
        public bgfx.UniformHandle m_samplerTexColor0;
        public bgfx.UniformHandle m_uniformColor0;
        public bgfx.UniformHandle m_uniformTexMad;

        public unsafe void Init(byte* fs_ptr, int fsl, byte* vs_ptr, int vsl, bgfx.RendererType backend)
        {
            m_prog = BGFXShaderHelper.MakeProgram(backend, fs_ptr, fsl, vs_ptr, vsl, "simple");

            m_samplerTexColor0 = bgfx.create_uniform("s_texColor", bgfx.UniformType.Sampler, 1);
            m_uniformColor0 = bgfx.create_uniform("u_color0", bgfx.UniformType.Vec4, 1);
            m_uniformTexMad = bgfx.create_uniform("u_texmad", bgfx.UniformType.Vec4, 1);
        }

        public void Destroy()
        {
            bgfx.destroy_program(m_prog);
            bgfx.destroy_uniform(m_samplerTexColor0);
            bgfx.destroy_uniform(m_uniformColor0);
            bgfx.destroy_uniform(m_uniformTexMad);
        }
    }

    public struct BlitShader
    {
        public bgfx.ProgramHandle m_prog;
        public bgfx.UniformHandle m_uniformTexMad;
        public bgfx.UniformHandle m_samplerTexColor0;
        public bgfx.UniformHandle m_colormul;
        public bgfx.UniformHandle m_coloradd;
        public bgfx.UniformHandle m_decodeSRGB_encodeSRGB_reinhard_premultiply;

        public unsafe void Init(byte* fs_ptr, int fsl, byte* vs_ptr, int vsl, bgfx.RendererType backend)
        {
            m_prog = BGFXShaderHelper.MakeProgram(backend, fs_ptr, fsl, vs_ptr, vsl, "blitsrgb");

            m_uniformTexMad = bgfx.create_uniform("u_texmad", bgfx.UniformType.Vec4, 1);
            m_samplerTexColor0 = bgfx.create_uniform("s_texColor", bgfx.UniformType.Sampler, 1);
            m_colormul = bgfx.create_uniform("u_colormul", bgfx.UniformType.Vec4, 1);
            m_coloradd = bgfx.create_uniform("u_coloradd", bgfx.UniformType.Vec4, 1);
            m_decodeSRGB_encodeSRGB_reinhard_premultiply = bgfx.create_uniform("u_decodeSRGB_encodeSRGB_reinhard_premultiply", bgfx.UniformType.Vec4, 1);
        }

        public void Destroy()
        {
            bgfx.destroy_program(m_prog);
            bgfx.destroy_uniform(m_uniformTexMad);
            bgfx.destroy_uniform(m_samplerTexColor0);
            bgfx.destroy_uniform(m_colormul);
            bgfx.destroy_uniform(m_coloradd);
            bgfx.destroy_uniform(m_decodeSRGB_encodeSRGB_reinhard_premultiply);
        }
    }

    public struct ExternalBlitES3Shader
    {
        public bgfx.ProgramHandle m_prog;
        public bgfx.UniformHandle m_samplerTexColor0;
        public bgfx.UniformHandle m_uniformColor0;

        public unsafe void Init(byte* fs_ptr, int fsl, byte* vs_ptr, int vsl, bgfx.RendererType backend)
        {
            m_prog = BGFXShaderHelper.MakeProgram(backend, fs_ptr, fsl, vs_ptr, vsl, "etxernalblites3");
            m_samplerTexColor0 = bgfx.create_uniform("s_texColor", bgfx.UniformType.Sampler, 1);
            m_uniformColor0 = bgfx.create_uniform("u_color0", bgfx.UniformType.Vec4, 1);
        }

        public void Destroy()
        {
            if (m_prog.idx == 0)
                return; 
            bgfx.destroy_program(m_prog);
            bgfx.destroy_uniform(m_samplerTexColor0);
            bgfx.destroy_uniform(m_uniformColor0);
        }
    }

    public struct LineShader
    {
        public bgfx.ProgramHandle m_prog;

        public unsafe void Init(byte* fs_ptr, int fsl, byte* vs_ptr, int vsl, bgfx.RendererType backend)
        {
           m_prog = BGFXShaderHelper.MakeProgram(backend, fs_ptr, fsl, vs_ptr, vsl, "line");
        }

        public void Destroy()
        {
            bgfx.destroy_program(m_prog);
        }
    }

    public struct ZOnlyShader
    {
        public bgfx.ProgramHandle m_prog;

        public bgfx.UniformHandle m_uniformDebugColor;

        public unsafe void Init(byte* fs_ptr, int fsl, byte* vs_ptr, int vsl, bgfx.RendererType backend)
        {
            m_prog = BGFXShaderHelper.MakeProgram(backend, fs_ptr, fsl, vs_ptr, vsl, "zOnly");
            m_uniformDebugColor = bgfx.create_uniform("u_colorDebug", bgfx.UniformType.Vec4, 1);
        }

        public void Destroy()
        {
            bgfx.destroy_program(m_prog);
            bgfx.destroy_uniform(m_uniformDebugColor);
        }
    }

    public struct ShadowMapShader
    {
        public bgfx.ProgramHandle m_prog;

        public bgfx.UniformHandle m_uniformBias;

        public unsafe void Init(byte* fs_ptr, int fsl, byte* vs_ptr, int vsl, bgfx.RendererType backend)
        {
            m_prog = BGFXShaderHelper.MakeProgram(backend, fs_ptr, fsl, vs_ptr, vsl, "shadowmap");
            m_uniformBias = bgfx.create_uniform("u_bias", bgfx.UniformType.Vec4, 1);
        }

        public void Destroy()
        {
            bgfx.destroy_program(m_prog);
            bgfx.destroy_uniform(m_uniformBias);
        }
    }
}

