using System;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Tiny
{
    public enum ImageStatus
    {
        Invalid,
        Loaded,
        Loading,
        LoadError
    }

    /// <summary>
    /// Initialize an image from an asset file.
    /// </summary>
    /// <remarks>
    /// Once loading has completed, the asset loading system will remove this component from the image.
    /// Inspect the Image2D component status field for loading results.
    ///
    /// You need to provide an <see cref="Image2DLoadFromFileImageFile"/> and/or <see cref="Image2DLoadFromFileMaskFile"/>
    /// next to this component to specify the file to load.
    /// </remarks>
    public struct Image2DLoadFromFile : IComponentData
    {
    }

    /// <summary>
    /// Memory backed cpu image data. 
    /// This component is incompatible with Image2DLoadFromFile.
    /// It contains raw rgba data that must match the size and flags in the Image2D header.
    /// This component is intended for uploading cpu generated textures.
    /// This memory is NOT released after texture uploads because it can not easily be re-created. 
    /// However, it can be manually removed and rendering will still work until a device reset happens.
    /// </summary>
    public struct Image2DMemorySource : IBufferElementData
    {
        public byte c;
    }

    /// <summary>
    /// The image file/URI to load.
    /// This can be a data URI.
    /// Tag with Image2DLoadFromFile to start a load
    /// Place a Image2DLoadFromFileMaskFile next to it to load alpha from a different file
    /// </summary>
    public struct Image2DLoadFromFileImageFile : IBufferElementData
    {
        public char s;
    }

    /// <summary>
    /// An image to use as the mask. This can be a data URI.
    /// </summary>
    /// <remarks>
    /// Incompatible with Image2DLoadFromFileGuids
    /// The red channel will be used as
    /// the mask (the alpha channel is ignored);
    /// efficient compression can be used (e.g. a single channel PNG or palleted PNG8).
    /// Tag with Image2DLoadFromFile to start a load 
    /// </remarks>
    public struct Image2DLoadFromFileMaskFile : IBufferElementData
    {
        public char s;
    }


    /// <summary>
    /// Asset GUIDs to load the image from
    /// </summary>
    /// Incompatible with Image2DLoadFromFileImageFile and Image2DLoadFromFileMaskFile
    /// Tag with Image2DLoadFromFile to start a load
    public struct Image2DLoadFromFileGuids : IComponentData
    {
        public Guid imageAsset;
        public Guid maskAsset;
    }

    public enum RenderToTextureFormat {
        RGBA,
        Depth,
        DepthStencil,
        ShadowMap,
        RGBA16f,
        R,
        R16f,
        R32f
    }

    /// <summary>
    /// Texture wrapping and filtering mode. Default is repeat and linear
    /// </summary>
    [Flags]
    public enum TextureFlags : uint
    {
        /// <summary>
        /// Repeat
        /// </summary>
        UVRepeat = URepeat | VRepeat,

        /// <summary>
        /// Repeat along texture V-axis
        /// </summary>
        URepeat = 0,

        /// <summary>
        /// Repeat along texture U-axis
        /// </summary>
        VRepeat = 0,

        /// <summary>
        /// Clamp on both axis
        /// </summary>
        UVClamp = UClamp | VClamp,

        /// <summary>
        /// Clamp along texture U-axis
        /// </summary>
        UClamp = 2,

        /// <summary>
        /// Clamp along texture V-axis
        /// </summary>
        VClamp = 8,

        /// <summary>
        /// Mirror on both axis
        /// </summary>
        UVMirror = UMirror | VMirror,

        /// <summary>
        /// Mirror along texture U-axis
        /// </summary>
        UMirror = 1,

        /// <summary>
        /// Mirror along texture V-axis
        /// </summary>
        VMirror = 4,

        /// <summary>
        /// Point filtering
        /// </summary>
        Point = 0x540,
        Nearest = Point,

        /// <summary>
        /// Linear filtering
        /// </summary>
        Linear = 0x400,

        /// <summary>
        /// Trilinear filtering
        /// </summary>
        Trilinear = 0,

        /// <summary>
        /// sRGB Color texture
        /// </summary>
        Srgb = 0x10000,

        /// <summary>
        /// Generate mip map
        /// </summary>
        MimapEnabled = 0x20000,

        /// <summary>
        /// Is texture a normal map
        /// </summary>
        IsNormalMap = 0x40000
    }

    /// <summary>
    /// Tag component that needs to be next to be placed next to an <see cref="Image2D"/> component
    /// if it is intended to be used as a render to texture target.
    /// An image can not be loaded and a texture target at the same time. 
    /// </summary>
    public struct Image2DRenderToTexture : IComponentData
    {
        public RenderToTextureFormat format;
    }

    public struct Image2D : IComponentData
    {
        /// <summary>
        /// Image size in pixels.
        /// Set only after loading (status must be ImageStatus::Loaded).
        /// This is written to by the image loading system and should be treated as read only by user code.
        /// </summary>
        public int imagePixelWidth;

        /// <summary>
        /// Image size in pixels.
        /// Set only after loading (status must be ImageStatus::Loaded).
        /// This is written to by the image loading system and should be treated as read only by user code.
        /// </summary>
        public int imagePixelHeight;

        /// <summary>
        /// Load status of the image.
        /// This is written to by the image loading system and should be treated as read only by user code.
        /// </summary>
        public ImageStatus status;

        /// <summary>
        /// Texture sampling flags
        /// </summary>
        public TextureFlags flags;
    }
}
