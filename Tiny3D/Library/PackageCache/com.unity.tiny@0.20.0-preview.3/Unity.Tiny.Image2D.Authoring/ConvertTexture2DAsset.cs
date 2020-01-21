using System;
using System.IO;
using Unity.Entities;
using UnityEditor;
using Unity.Tiny;

namespace Unity.TinyConversion
{
    internal class ConvertTexture2DAsset : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.Texture2D texture) =>
            {
                var entity = GetPrimaryEntity(texture);
                string textPath = AssetDatabase.GetAssetPath(texture);
                TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(textPath);
                TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureImporterSettings);
                DstEntityManager.AddComponentData(entity, new Image2D()
                {
                    imagePixelWidth = texture.width,
                    imagePixelHeight = texture.height,
                    status = ImageStatus.Invalid,
                    flags = Texture2DExportUtils.GetTextureFlags(textureImporterSettings, texture)
                });

                DstEntityManager.AddComponent<Image2DLoadFromFile>(entity);

                var exportGuid = GetGuidForAssetExport(texture);

                DstEntityManager.AddComponentData(entity, new Image2DLoadFromFileGuids()
                {
                    imageAsset = exportGuid,
                    maskAsset = Guid.Empty
                });
            });
        }
    }

    [UpdateInGroup(typeof(GameObjectExportGroup))]
    internal class ExportTexture2DAsset : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.Texture2D texture) =>
            {
                using (var writer = TryCreateAssetExportWriter(texture))
                {
                    if (writer != null)
                        Texture2DExportUtils.ExportPng(writer, texture);
                }
            });
        }
    }

    internal static class Texture2DExportUtils
    {
        internal static bool IsPowerOfTwo(UnityEngine.Texture2D texture)
        {
            return texture.width > 0 && texture.height > 0 && ((texture.width & (texture.width - 1)) == 0) && ((texture.height & (texture.height - 1)) == 0);
        }
        
        internal static TextureFlags GetTextureFlags(TextureImporterSettings textImporter, UnityEngine.Texture2D texture)
        {
            TextureFlags flags = 0;

            // Handle Atlas. It does not have an Importer for now.
            if (textImporter == null)
            {
                flags |= TextureFlags.UClamp;
                flags |= TextureFlags.VClamp;
                flags |= TextureFlags.Point;
                return flags;
            }
            
            switch(textImporter.wrapModeU)
            {
                case UnityEngine.TextureWrapMode.Clamp:
                    flags |= TextureFlags.UClamp;
                    break;
                case UnityEngine.TextureWrapMode.Mirror:
                    flags |= TextureFlags.UMirror;
                    break;
                case UnityEngine.TextureWrapMode.Repeat:
                    flags |= TextureFlags.URepeat;
                    break;
            }

            switch (textImporter.wrapModeV)
            {
                case UnityEngine.TextureWrapMode.Clamp:
                    flags |= TextureFlags.VClamp;
                    break;
                case UnityEngine.TextureWrapMode.Mirror:
                    flags |= TextureFlags.VMirror;
                    break;
                case UnityEngine.TextureWrapMode.Repeat:
                    flags |= TextureFlags.URepeat;
                    break;
            }
          
            if (textImporter.filterMode == UnityEngine.FilterMode.Point)
                flags |= TextureFlags.Point;

            if (textImporter.filterMode == UnityEngine.FilterMode.Trilinear)
                flags |= TextureFlags.Trilinear;

            if (textImporter.mipmapEnabled)
                flags |= TextureFlags.MimapEnabled;

            if (textImporter.sRGBTexture)
                flags |= TextureFlags.Srgb;

            if (textImporter.textureType == TextureImporterType.NormalMap)
                flags |= TextureFlags.IsNormalMap;

            if(!IsPowerOfTwo(texture))
            {
                if ((flags & TextureFlags.MimapEnabled) == TextureFlags.MimapEnabled)
                    throw new ArgumentException($"Mipmapping is incompatible with NPOT textures. Update texture: {texture.name} to be power of two or disable mipmaps on it.");
                else if((flags & TextureFlags.UClamp) != TextureFlags.UClamp ||
                    (flags & TextureFlags.VClamp) != TextureFlags.VClamp)
                    throw new ArgumentException($"NPOT textures must use clamp wrap mode. Update texture: {texture.name} to be power of two or use clamp mode on it.");
            }

            return flags;
        }

        internal static bool HasColor(UnityEngine.Texture2D texture)
        {
            return texture.format != UnityEngine.TextureFormat.Alpha8;
        }

        internal static bool HasAlpha(UnityEngine.Texture2D texture)
        {
            if (!HasAlpha(texture.format))
            {
                return false;
            }

            if (texture.format == UnityEngine.TextureFormat.ARGB4444 ||
                texture.format == UnityEngine.TextureFormat.ARGB32 ||
                texture.format == UnityEngine.TextureFormat.RGBA32)
            {
                var tmp = BlitTexture(texture, UnityEngine.TextureFormat.ARGB32);
                UnityEngine.Color32[] pix = tmp.GetPixels32();
                for (int i = 0; i < pix.Length; ++i)
                {
                    if (pix[i].a != byte.MaxValue)
                    {
                        return true;
                    }
                }

                // image has alpha channel, but every alpha value is opaque
                return false;
            }

            return true;
        }

        internal static bool HasAlpha(UnityEngine.TextureFormat format)
        {
            return format == UnityEngine.TextureFormat.Alpha8 ||
                   format == UnityEngine.TextureFormat.ARGB4444 ||
                   format == UnityEngine.TextureFormat.ARGB32 ||
                   format == UnityEngine.TextureFormat.RGBA32 ||
                   format == UnityEngine.TextureFormat.DXT5 ||
                   format == UnityEngine.TextureFormat.PVRTC_RGBA2 ||
                   format == UnityEngine.TextureFormat.PVRTC_RGBA4 ||
                   format == UnityEngine.TextureFormat.ETC2_RGBA8;
        }

        internal static TextureFormatType RealFormatType(UnityEngine.Texture2D texture, TextureSettings settings)
        {
            var format = settings.FormatType;
            if (format != TextureFormatType.Source)
            {
                return format;
            }

            // If the texture doesn't exist in asset database, we can't use "Source" format type, default to PNG.
            var assetPath = UnityEditor.AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(assetPath))
            {
                return TextureFormatType.PNG;
            }

            // If the main asset loaded from the texture asset path is not the texture, default to PNG.
            var mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (mainAsset != texture)
            {
                return TextureFormatType.PNG;
            }

            return format;
        }

        internal static UnityEngine.Texture2D BlitTexture(UnityEngine.Texture2D texture, UnityEngine.TextureFormat format, bool alphaOnly = false)
        {
            // Create a temporary RenderTexture of the same size as the texture
            var tmp = UnityEngine.RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                UnityEngine.RenderTextureFormat.Default,
                UnityEngine.RenderTextureReadWrite.sRGB);

            // Blit the pixels on texture to the RenderTexture
            UnityEngine.Graphics.Blit(texture, tmp);

            // Backup the currently set RenderTexture
            var previous = UnityEngine.RenderTexture.active;

            // Set the current RenderTexture to the temporary one we created
            UnityEngine.RenderTexture.active = tmp;

            // Create a new readable Texture2D to copy the pixels to it
            var result = new UnityEngine.Texture2D(texture.width, texture.height, format, false);

            // Copy the pixels from the RenderTexture to the new Texture
            result.ReadPixels(new UnityEngine.Rect(0, 0, tmp.width, tmp.height), 0, 0);
            result.Apply();

            // Broadcast alpha to color
            if (alphaOnly || !HasColor(texture))
            {
                var pixels = result.GetPixels();
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i].r = pixels[i].a;
                    pixels[i].g = pixels[i].a;
                    pixels[i].b = pixels[i].a;
                }
                result.SetPixels(pixels);
                result.Apply();
            }

            // Reset the active RenderTexture
            UnityEngine.RenderTexture.active = previous;

            // Release the temporary RenderTexture
            UnityEngine.RenderTexture.ReleaseTemporary(tmp);
            return result;
        }

        public static void ExportPng(Stream writer, UnityEngine.Texture2D texture)
        {
            var format = HasAlpha(texture) ? UnityEngine.TextureFormat.RGBA32 : UnityEngine.TextureFormat.RGB24;
            var outputTexture = BlitTexture(texture, format);
            WritePng(writer, outputTexture);
        }

        internal static void WritePng(Stream writer, UnityEngine.Texture2D texture)
        {
            var bytes = UnityEngine.ImageConversion.EncodeToPNG(texture);
            writer.Write(bytes, 0, bytes.Length);
        }
    }

    internal enum TextureFormatType
    {
        Source,
        PNG,
        JPG,
        WebP
    }

    internal class TextureSettings
    {
        public static TextureSettings Default => new TextureSettings
        {
            FormatType = TextureFormatType.PNG,
            JpgCompressionQuality = 100,
            WebPCompressionQuality = 100
        };

        public TextureFormatType FormatType;
        public uint JpgCompressionQuality;
        public uint WebPCompressionQuality;
    }
}

