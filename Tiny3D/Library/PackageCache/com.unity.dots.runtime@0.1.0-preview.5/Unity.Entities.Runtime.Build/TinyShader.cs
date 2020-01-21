#if EXPORT_TINY_SHADER
using Unity.Entities;
using Unity.Tiny.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System.IO;
using Unity.Build;
using System.Collections.Generic;
using Bgfx;

namespace Unity.Entities.Runtime.Build
{
    public static class TinyShader 
    {
        private static string kBinaryShaderFolderPath = "Packages/com.unity.tiny.rendering/Runtime/Unity.Tiny.Rendering.Native/shaderbin~/";

        private static string GetShaderFileName(string prefix, string type, string backend)
        {
            return prefix + "_" + type + "_" + backend + ".raw";
        }

        private static unsafe BlobAssetReference<PrecompiledShaderData> AddShaderData(EntityManager em, Entity e, ShaderType type, bgfx.RendererType[] types, string prefix)
        {
            using (var allocator = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref allocator.ConstructRoot<PrecompiledShaderData>();
                foreach (bgfx.RendererType sl in types)
                {
                    string path = Path.GetFullPath(kBinaryShaderFolderPath);
                    string fsFileName = GetShaderFileName(prefix, type.ToString(), sl.ToString()).ToLower();
                    using (var data = new NativeArray<byte>(File.ReadAllBytes(Path.Combine(path, fsFileName)), Allocator.Temp))
                    {
                        byte* dest = (byte*)(allocator.Allocate(ref root.DataForBackend(sl), data.Length).GetUnsafePtr());
                        UnsafeUtility.MemCpy(dest, (byte*)data.GetUnsafePtr(), data.Length);
                    }
                }
                return allocator.CreateBlobAssetReference<PrecompiledShaderData>(Allocator.Persistent);
            }
        }

        private static bgfx.RendererType[] GetShaderFormat(string targetName)
        {
            if (targetName == UnityEditor.BuildTarget.StandaloneWindows.ToString() ||
                targetName == UnityEditor.BuildTarget.StandaloneWindows64.ToString())
                return new bgfx.RendererType[] { bgfx.RendererType.OpenGL, bgfx.RendererType.Direct3D9, bgfx.RendererType.Direct3D11, bgfx.RendererType.Vulkan };
            else if (targetName == UnityEditor.BuildTarget.StandaloneLinux64.ToString())
                return new bgfx.RendererType[] { bgfx.RendererType.OpenGL, bgfx.RendererType.Vulkan };
            else if (targetName == UnityEditor.BuildTarget.StandaloneOSX.ToString() ||
                targetName == UnityEditor.BuildTarget.iOS.ToString())
                return new bgfx.RendererType[] { bgfx.RendererType.OpenGL, bgfx.RendererType.Metal, bgfx.RendererType.Vulkan };
            else if (targetName == UnityEditor.BuildTarget.Android.ToString())
                return new bgfx.RendererType[] { bgfx.RendererType.OpenGL, bgfx.RendererType.OpenGLES, bgfx.RendererType.Vulkan };
            else if (targetName == UnityEditor.BuildTarget.WebGL.ToString())
                return new bgfx.RendererType[] { bgfx.RendererType.OpenGLES };
            else
                //TODO: Should we default to a specific shader type?
                Debug.LogError($"Target: {targetName} is not supported. No shaders will be exported");
            return new bgfx.RendererType[] { };
        }

        private static Entity CreateShaderDataEntity(EntityManager em, ShaderType type, bgfx.RendererType[] backends)
        {
            var e = em.CreateEntity();
            var blob = AddShaderData(em, e, type, backends, "vs");
            em.AddComponentData(e, new VertexShaderBinData()
            {
                data = blob
            });

            blob = AddShaderData(em, e, type, backends, "fs");
            em.AddComponentData(e, new FragmentShaderBinData()
            {
                data = blob
            });
            return e;
        }

        public static void Export(EntityManager em, DotsRuntimeBuildProfile profile)
        {
            //Export shaders per build target
            var targetName = profile.Target.UnityPlatformName;
            bgfx.RendererType[] types = GetShaderFormat(targetName);
            PrecompiledShaders data = new PrecompiledShaders();

            data.SimpleShader = CreateShaderDataEntity(em, ShaderType.simple, types);
            data.LitShader = CreateShaderDataEntity(em, ShaderType.simplelit, types);
            data.LineShader = CreateShaderDataEntity(em, ShaderType.line, types);
            data.ZOnlyShader = CreateShaderDataEntity(em, ShaderType.zOnly, types);
            data.BlitSRGBShader = CreateShaderDataEntity(em, ShaderType.blitsrgb, types);
            data.ShadowMapShader = CreateShaderDataEntity(em, ShaderType.shadowmap, types);
            data.SpriteShader = CreateShaderDataEntity(em, ShaderType.sprite, types);

            var singletonEntity = em.CreateEntity();
            em.AddComponentData<PrecompiledShaders>(singletonEntity, data);
        }
    }
}

#endif
