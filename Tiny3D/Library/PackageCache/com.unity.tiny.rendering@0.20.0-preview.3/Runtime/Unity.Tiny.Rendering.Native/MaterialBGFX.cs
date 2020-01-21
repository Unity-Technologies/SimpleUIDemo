using System;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Tiny;
using Unity.Tiny.Rendering;
using System.Collections.Generic;
using Bgfx;
using System.Runtime.InteropServices;
using Unity.Transforms;
 
namespace Unity.Tiny.Rendering
{

    public struct UpdateBGFXMaterialTag : ISystemStateComponentData
    {
    }

    public struct SimpleMaterialBGFX : ISystemStateComponentData
    {
        public bgfx.TextureHandle texAlbedo;
        public bgfx.TextureHandle texOpacity;

        public float4 constAlbedo_Opacity;
        public float4 mainTextureScaleTranslate;//scale: xy translate: zw

        public ulong state; // includes blending and culling!
    }

    public struct LitMaterialBGFX : ISystemStateComponentData
    {
        public bgfx.TextureHandle texAlbedo;
        public bgfx.TextureHandle texMetal;
        public bgfx.TextureHandle texNormal;
        public bgfx.TextureHandle texSmoothness;
        public bgfx.TextureHandle texEmissive;
        public bgfx.TextureHandle texOpacity;

        public float4 constAlbedo_Opacity; 
        public float4 constMetal_Smoothness; // zw unused 
        public float4 constEmissive_normalMapZScale;
        public float4 mainTextureScaleTranslate; //scale: xy translate: zw

        public ulong state; // includes blending and culling!
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(SubmitSystemGroup))]
    [UpdateAfter(typeof(RendererBGFXSystem))]
    internal class UpdateMaterialsSystem : ComponentSystem
    {
        public void UpdateLitMaterialBGFX(RendererBGFXSystem sys, Entity e) {
            var mat = EntityManager.GetComponentData<LitMaterial>(e);
            var matBGFX = EntityManager.GetComponentData<LitMaterialBGFX>(e);
            UpdateLitMaterialBGFX(sys, ref mat, ref matBGFX);
            EntityManager.SetComponentData(e, matBGFX);
        }

        // true if still loading
        private bool InitTexture(ref bgfx.TextureHandle dest, Entity src, bgfx.TextureHandle defValue ) {
            dest = defValue;
            if (src == Entity.Null)
                return false;
            Image2D im = EntityManager.GetComponentData<Image2D>(src); // must have that one, no check?
            if ( im.status == ImageStatus.Loaded && EntityManager.HasComponent<TextureBGFX>(src) ) {
                TextureBGFX tex = EntityManager.GetComponentData<TextureBGFX>(src);
                dest = tex.handle;
                return false;
            }
            return true;
        }

        public bool UpdateLitMaterialBGFX(RendererBGFXSystem sys, ref LitMaterial mat, ref LitMaterialBGFX matBGFX) {
            bool stillLoading = false;
            if (InitTexture(ref matBGFX.texAlbedo, mat.texAlbedo, sys.WhiteTexture)) stillLoading = true;
            if (InitTexture(ref matBGFX.texOpacity, mat.texOpacity, sys.WhiteTexture)) stillLoading = true;
            if (InitTexture(ref matBGFX.texNormal, mat.texNormal, sys.UpTexture)) stillLoading = true;
            if (InitTexture(ref matBGFX.texMetal, mat.texMetal, sys.BlackTexture)) stillLoading = true;
            if (InitTexture(ref matBGFX.texEmissive, mat.texEmissive, sys.BlackTexture)) stillLoading = true;
            if (InitTexture(ref matBGFX.texSmoothness, mat.texSmoothness, sys.GreyTexture)) stillLoading = true;

            matBGFX.constAlbedo_Opacity = new float4(mat.constAlbedo, mat.constOpacity);
            matBGFX.constMetal_Smoothness = new float4(mat.constMetal, mat.constSmoothness, 0, 0);
            matBGFX.constEmissive_normalMapZScale = new float4(mat.constEmissive, mat.normalMapZScale);
            matBGFX.mainTextureScaleTranslate = new float4(mat.scale, mat.offset);

            // if twoSided, need to update state
            matBGFX.state = (ulong)(bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA | bgfx.StateFlags.DepthTestLess);
            if (!mat.twoSided)
                matBGFX.state |= (ulong)bgfx.StateFlags.CullCcw;
            if (mat.transparent)
                matBGFX.state |= RendererBGFXSystem.MakeBGFXBlend(bgfx.StateFlags.BlendOne, bgfx.StateFlags.BlendInvSrcAlpha);
            else
                matBGFX.state |= (ulong)bgfx.StateFlags.WriteZ;
            return !stillLoading;
        }

        public void UpdateSimpleMaterialBGFX(RendererBGFXSystem sys, Entity e) {
            var mat = EntityManager.GetComponentData<SimpleMaterial>(e);
            var matBGFX = EntityManager.GetComponentData<SimpleMaterialBGFX>(e);
            UpdateSimpleMaterialBGFX(sys, ref mat, ref matBGFX);
            EntityManager.SetComponentData(e, matBGFX);
        }

        public bool UpdateSimpleMaterialBGFX(RendererBGFXSystem sys, ref SimpleMaterial mat, ref SimpleMaterialBGFX matBGFX) {
            // if constants changed, need to update packed value
            matBGFX.constAlbedo_Opacity = new float4(mat.constAlbedo, mat.constOpacity);
            // if texture entity OR load state changed need to update texture handles 
            // content of texture change should transparently update texture referenced by handle
            bool stillLoading = false;
            if (InitTexture(ref matBGFX.texAlbedo, mat.texAlbedo, sys.WhiteTexture)) stillLoading = true;
            if (InitTexture(ref matBGFX.texOpacity, mat.texOpacity, sys.WhiteTexture)) stillLoading = true;

            // if twoSided or hasalpha changed, need to update state
            matBGFX.state = (ulong)(bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA | bgfx.StateFlags.DepthTestLess);
            if (!mat.twoSided)
                matBGFX.state |= (ulong)bgfx.StateFlags.CullCw;
            if (mat.transparent)
                matBGFX.state |= RendererBGFXSystem.MakeBGFXBlend(bgfx.StateFlags.BlendOne, bgfx.StateFlags.BlendInvSrcAlpha);
            else
                matBGFX.state |= (ulong)bgfx.StateFlags.WriteZ;
            matBGFX.mainTextureScaleTranslate = new float4(mat.scale, mat.offset);
            return !stillLoading;
        }

        protected override void OnUpdate()
        {
            var sys = World.GetExistingSystem<RendererBGFXSystem>();

            // add bgfx version of materials, system states 
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            Entities.WithNone<LitMaterialBGFX>().WithAll<LitMaterial>().ForEach((Entity e) =>
            {
                ecb.AddComponent<LitMaterialBGFX>(e);
                if (!EntityManager.HasComponent<UpdateBGFXMaterialTag>(e))
                    ecb.AddComponent<UpdateBGFXMaterialTag>(e);
            });
            Entities.WithNone<SimpleMaterialBGFX>().WithAll<SimpleMaterial>().ForEach((Entity e) =>
            {
                ecb.AddComponent<SimpleMaterialBGFX>(e);
                if (!EntityManager.HasComponent<UpdateBGFXMaterialTag>(e))
                    ecb.AddComponent<UpdateBGFXMaterialTag>(e);
            });
            ecb.Playback(EntityManager); // playback once here, so we do not have a one frame delay
            ecb.Dispose();

            // upload materials 
            ecb = new EntityCommandBuffer(Allocator.TempJob);
            Entities.WithAll<UpdateBGFXMaterialTag>().ForEach((Entity e, ref LitMaterial mat, ref LitMaterialBGFX matBGFX) =>
            {
                if ( UpdateLitMaterialBGFX(sys, ref mat, ref matBGFX) ) {
                    if (!EntityManager.HasComponent<DynamicMaterial>(e))
                        ecb.RemoveComponent<UpdateBGFXMaterialTag>(e);
                }
            });

            Entities.WithAll<UpdateBGFXMaterialTag>().ForEach((Entity e, ref SimpleMaterial mat, ref SimpleMaterialBGFX matBGFX) =>
            {
                if ( UpdateSimpleMaterialBGFX(sys, ref mat, ref matBGFX) ) {
                    if (!EntityManager.HasComponent<DynamicMaterial>(e))
                        ecb.RemoveComponent<UpdateBGFXMaterialTag>(e);
                }
            });

            // system state cleanup - can reuse the same ecb, it does not matter if there is a bit of delay
            Entities.WithAll<SimpleMaterialBGFX>().WithNone<SimpleMaterial>().ForEach((Entity e) =>
            {
                ecb.RemoveComponent<SimpleMaterialBGFX>(e);
            });

            Entities.WithAll<LitMaterialBGFX>().WithNone<LitMaterial>().ForEach((Entity e) =>
            {
                ecb.RemoveComponent<LitMaterialBGFX>(e);
            });

            Entities.WithAll<UpdateBGFXMaterialTag>().WithNone<LitMaterialBGFX, SimpleMaterialBGFX>().ForEach((Entity e) =>
            {
                ecb.RemoveComponent<UpdateBGFXMaterialTag>(e);
            });

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
