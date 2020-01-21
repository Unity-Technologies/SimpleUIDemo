using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Tiny.Rendering
{
    public enum ShaderType
    {
        simple,
        simplelit,
        line,
        zOnly,
        blitsrgb,
        etxernalblites3,
        shadowmap,
        sprite
    }

    /// <summary>
    /// Shader data per shader language type
    /// </summary>
    public struct PrecompiledShaderData
    {
        public BlobArray<byte> dx9;
        public BlobArray<byte> dx11;
        public BlobArray<byte> metal;
        public BlobArray<byte> glsles;
        public BlobArray<byte> glsl;
        public BlobArray<byte> spirv;
    }

    /// <summary>
    /// Blob asset reference for each vertex and fragment shaders. To add next to each shader type
    /// </summary>
    public struct VertexShaderBinData : IComponentData
    {
        public BlobAssetReference<PrecompiledShaderData> data;
    }

    public struct FragmentShaderBinData : IComponentData
    {
        public BlobAssetReference<PrecompiledShaderData> data;
    }

    /// <summary>
    /// PrecompiledShaders contains reference to entities for each supported shader type
    /// To add next to a singleton entity
    /// </summary>
    public struct PrecompiledShaders : IComponentData
    {
        public Entity SimpleShader;
        public Entity LitShader;
        public Entity LineShader;
        public Entity ZOnlyShader;
        public Entity BlitSRGBShader;
        public Entity ExternalBlitES3Shader;
        public Entity ShadowMapShader;
        public Entity SpriteShader;
    }
}