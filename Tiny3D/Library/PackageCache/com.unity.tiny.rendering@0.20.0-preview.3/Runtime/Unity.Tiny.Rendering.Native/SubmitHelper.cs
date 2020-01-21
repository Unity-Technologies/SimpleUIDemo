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
    // submit helpers for bgfx
    internal static class SubmitHelper
    {
        // ---------------- z only, with mesh ----------------------------------------------------------------------------------------------------------------------
        public static unsafe void SubmitZOnlyDirect(RendererBGFXSystem sys, ushort viewId, ref SimpleMeshBGFX mesh, ref float4x4 tx, int startIndex, int indexCount, byte flipCulling)
        {
            bgfx.Encoder* encoder = bgfx.encoder_begin(false);
            EncodeZOnly(sys, encoder, viewId, ref mesh, ref tx, startIndex, indexCount, flipCulling);
            bgfx.encoder_end(encoder);
        }

        public unsafe static void EncodeZOnly(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, ref SimpleMeshBGFX mesh, ref float4x4 tx, int startIndex, int indexCount, byte flipCulling)
        {
            ulong state = (ulong)(bgfx.StateFlags.WriteZ | bgfx.StateFlags.DepthTestLess | bgfx.StateFlags.CullCcw);
            if (flipCulling != 0) state = FlipCulling(state);
            bgfx.encoder_set_state(encoder, state, 0);
            unsafe { fixed (float4x4* p = &tx) bgfx.encoder_set_transform(encoder, p, 1); }
            bgfx.encoder_set_index_buffer(encoder, mesh.indexBufferHandle, (uint)startIndex, (uint)indexCount);
            bgfx.encoder_set_vertex_buffer(encoder, 0, mesh.vertexBufferHandle, (uint)mesh.vertexFirst, (uint)mesh.vertexCount, mesh.vertexDeclHandle);
            float4 color = new float4(1, 0, 0, 1);
            bgfx.encoder_set_uniform(encoder, sys.ZOnlyShader.m_uniformDebugColor, &color, 1);
            bgfx.encoder_submit(encoder, viewId, sys.ZOnlyShader.m_prog, 0, false);
        }

        // ---------------- shadow map ----------------------------------------------------------------------------------------------------------------------
        public unsafe static void EncodeShadowMap(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, ref SimpleMeshBGFX mesh, ref float4x4 tx, int startIndex, int indexCount, byte flipCulling, float4 bias)
        {
            ulong state = (ulong)(bgfx.StateFlags.WriteZ | bgfx.StateFlags.DepthTestLess | bgfx.StateFlags.CullCcw);
            if (flipCulling != 0) state = FlipCulling(state);
            bgfx.encoder_set_state(encoder, state, 0);
            unsafe { fixed (float4x4* p = &tx) bgfx.encoder_set_transform(encoder, p, 1); }
            bgfx.encoder_set_index_buffer(encoder, mesh.indexBufferHandle, (uint)startIndex, (uint)indexCount);
            bgfx.encoder_set_vertex_buffer(encoder, 0, mesh.vertexBufferHandle, (uint)mesh.vertexFirst, (uint)mesh.vertexCount, mesh.vertexDeclHandle);
            bgfx.encoder_set_uniform(encoder, sys.ShadowMapShader.m_uniformBias, &bias, 1);
            bgfx.encoder_submit(encoder, viewId, sys.ShadowMapShader.m_prog, 0, false);
        }

        // ---------------- debug line rendering helper ----------------------------------------------------------------------------------------------------------------------

        public static unsafe void SubmitLineDirect(RendererBGFXSystem sys, ushort viewId, float3 p0, float3 p1, float4 color, float2 width, ref float4x4 objTx, ref float4x4 viewTx, ref float4x4 projTx)
        {
            bgfx.Encoder* encoder = bgfx.encoder_begin(false);
            EncodeLine(sys, encoder, viewId, p0, p1, color, width, ref objTx, ref viewTx, ref projTx);
            bgfx.encoder_end(encoder);
        }

        private static bool ClipLinePositive(ref float4 p0, ref float4 p1, int coord)
        {
            bool isinside0 = p0[coord] <= p0.w;
            bool isinside1 = p1[coord] <= p1.w;
            if (isinside0 && isinside1) // no clipping
                return true;
            if (!isinside0 && !isinside1) // all out 
                return false;
            float4 d = p1 - p0;
            float t = (p0[coord] - p0.w) / (d.w - d[coord]); // p = p0 + d * t && p.z = p.w
            Assert.IsTrue(t >= 0.0f && t <= 1.0f);
            float4 p = p0 + d * t;
            if (!isinside0) p0 = p;
            else p1 = p;
            return true;
        }

        private static bool ClipLineNegative(ref float4 p0, ref float4 p1, int coord)
        {
            bool isinside0 = p0[coord] >= -p0.w;
            bool isinside1 = p1[coord] >= -p1.w;
            if (isinside0 && isinside1) // no clipping
                return true;
            if (!isinside0 && !isinside1) // all out 
                return false;
            float4 d = p1 - p0;
            float t = (p0[coord] + p0.w) / (-d.w - d[coord]); // p = p0 + d * t && p[coord] = -p.w
            Assert.IsTrue(t >= 0.0f && t <= 1.0f);
            float4 p = p0 + d * t;
            if (!isinside0) p0 = p;
            else p1 = p;
            return true;
        }

        public static unsafe void EncodeLine(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, float3 p0, float3 p1, float4 color, float2 width, ref float4x4 objTx, ref float4x4 viewTx, ref float4x4 projTx)
        {
            float4 p0t = math.mul(projTx,
                         math.mul(viewTx,
                         math.mul(objTx, new float4(p0, 1))));
            float4 p1t = math.mul(projTx,
                         math.mul(viewTx,
                         math.mul(objTx, new float4(p1, 1))));
            for (int i = 0; i < 3; i++) { // really only need to clip z near, but clip all to make sure clipping works
                if (!ClipLinePositive(ref p0t, ref p1t, i))
                    return;
                if (!ClipLineNegative(ref p0t, ref p1t, i))
                    return;
            }
            SimpleVertex* buf = stackalloc SimpleVertex[4];
            p0t.xyz *= 1.0f / p0t.w;
            p1t.xyz *= 1.0f / p1t.w;
            float2 dp = math.normalizesafe(p1t.xy - p0t.xy);
            float2 dprefl = new float2(-dp.y, dp.x);
            float3 dv = new float3(dprefl * width, 0);
            float3 du = new float3(dp * width * .5f, 0);
            buf[0].Position = p0t.xyz + dv - du; buf[0].Color = color; buf[0].TexCoord0 = new float2(0, 1);
            buf[1].Position = p0t.xyz - dv - du; buf[1].Color = color; buf[1].TexCoord0 = new float2(0, -1);
            buf[2].Position = p1t.xyz - dv + du; buf[2].Color = color; buf[2].TexCoord0 = new float2(1, -1);
            buf[3].Position = p1t.xyz + dv + du; buf[3].Color = color; buf[3].TexCoord0 = new float2(1, 1);
            EncodeLinePreTransformed(sys, encoder, viewId, buf, 4);
        }

        public static unsafe void EncodeDebugTangents(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, float2 width, float length, ref LitMeshRenderData mesh, ref float4x4 objTx, ref float4x4 viewTx, ref float4x4 projTx)
        {
            int nv = (int)mesh.Mesh.Value.Vertices.Length;
            LitVertex* vertices = (LitVertex*)mesh.Mesh.Value.Vertices.GetUnsafePtr();
            for (int i = 0; i < nv; i++) {
                EncodeLine(sys, encoder, viewId, vertices[i].Position, vertices[i].Position + vertices[i].Normal * length, new float4(0, 0, 1, 1), width, ref objTx, ref viewTx, ref projTx);
                EncodeLine(sys, encoder, viewId, vertices[i].Position, vertices[i].Position + vertices[i].Tangent * length, new float4(1, 0, 0, 1), width, ref objTx, ref viewTx, ref projTx);
                EncodeLine(sys, encoder, viewId, vertices[i].Position, vertices[i].Position + vertices[i].BiTangent * length, new float4(0, 1, 0, 1), width, ref objTx, ref viewTx, ref projTx);
            }
        }

        public static unsafe void EncodeLinePreTransformed(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, SimpleVertex* vertices, int n)
        {
            bgfx.TransientIndexBuffer tib;
            bgfx.TransientVertexBuffer tvb;
            int ni = (n / 4) * 6;
            fixed (bgfx.VertexLayout* declp = sys.SimpleVertexBufferDecl) {
                if (!bgfx.alloc_transient_buffers(&tvb, declp, (uint)n, &tib, (uint)ni))
                    throw new InvalidOperationException("Out of transient bgfx memory!");
            }
            UnsafeUtility.MemCpy((SimpleVertex*)tvb.data, vertices, sizeof(SimpleVertex) * n);
            ushort* indices = (ushort*)tib.data;
            for (int i = 0; i < n; i += 4) {
                indices[0] = (ushort)i; indices[1] = (ushort)(i + 1); indices[2] = (ushort)(i + 2);
                indices[3] = (ushort)(i + 2); indices[4] = (ushort)(i + 3); indices[5] = (ushort)i;
                indices += 6;
            }
            bgfx.encoder_set_transient_index_buffer(encoder, &tib, 0, (uint)ni);
            bgfx.encoder_set_transient_vertex_buffer(encoder, 0, &tvb, 0, (uint)n, sys.SimpleVertexBufferDeclHandle);

            // material uniforms setup
            ulong state = (ulong)(bgfx.StateFlags.DepthTestLess | bgfx.StateFlags.WriteRgb) | RendererBGFXSystem.MakeBGFXBlend(bgfx.StateFlags.BlendOne, bgfx.StateFlags.BlendInvSrcAlpha);
            bgfx.encoder_set_state(encoder, state, 0);
            bgfx.encoder_submit(encoder, viewId, sys.LineShader.m_prog, 0, false);
        }

        // ---------------- simple, lit, with mesh ----------------------------------------------------------------------------------------------------------------------
        public unsafe static void SubmitLitDirect(RendererBGFXSystem sys, ushort viewId, ref SimpleMeshBGFX mesh, ref float4x4 tx,
                                                 ref LitMaterialBGFX mat, ref LightingBGFX lighting,
                                                 ref float4x4 viewTx, int startIndex, int indexCount, byte flipCulling)
        {
            bgfx.Encoder* encoder = bgfx.encoder_begin(false);
            LightingViewSpaceBGFX vsLight = default;
            vsLight.cacheTag = -1;
            EncodeLit(sys, encoder, viewId, ref mesh, ref tx, ref mat, ref lighting, ref viewTx, startIndex, indexCount, flipCulling, ref vsLight);
            bgfx.encoder_end(encoder);
        }

        public static ulong FlipCulling(ulong state)
        {
            ulong cull = state & (ulong)bgfx.StateFlags.CullMask;
            ulong docull = cull >> (int)bgfx.StateFlags.CullShift;
            docull = ((docull >> 1) ^ docull) & 1;
            docull = docull | (docull << 1);
            ulong r = state ^ (docull << (int)bgfx.StateFlags.CullShift);
            return r;
        }

        private unsafe static void EncodeMappedLight(bgfx.Encoder* encoder, ref MappedLightBGFX light, ref LitShader.MappedLight shader, byte samplerOffset, float4 viewPosOrDir)
        {
            fixed (float4x4* p = &light.projection)
                bgfx.encoder_set_uniform(encoder, shader.m_uniformMatrix, p, 1);
            fixed (float4* p = &light.color_invrangesqr)
                bgfx.encoder_set_uniform(encoder, shader.m_uniformColorIVR, p, 1);
            fixed (float4* p = &light.mask)
                bgfx.encoder_set_uniform(encoder, shader.m_uniformLightMask, p, 1);
            bgfx.encoder_set_uniform(encoder, shader.m_uniformViewPosOrDir, &viewPosOrDir, 1);
            bgfx.encoder_set_texture(encoder, (byte)(6 + samplerOffset), shader.m_samplerShadow, light.shadowMap, UInt32.MaxValue);
            
        }

        public unsafe static void EncodeLit(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, ref SimpleMeshBGFX mesh, ref float4x4 tx,
                                                 ref LitMaterialBGFX mat, ref LightingBGFX lighting,
                                                 ref float4x4 viewTx, int startIndex, int indexCount, byte flipCulling, ref LightingViewSpaceBGFX viewSpaceLightCache)
        {
            ulong state = mat.state;
            if (flipCulling != 0)
                state = FlipCulling(state);
            bgfx.encoder_set_state(encoder, state, 0);
            fixed (float4x4* p = &tx)
                bgfx.encoder_set_transform(encoder, p, 1);
            float3x3 minvt = math.transpose(math.inverse(new float3x3(tx.c0.xyz, tx.c1.xyz, tx.c2.xyz))); 
            //float3x3 minvt = new float3x3(tx.c0.xyz, tx.c1.xyz, tx.c2.xyz);
            bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_uniformModelInverseTranspose, &minvt, 1);

            bgfx.encoder_set_index_buffer(encoder, mesh.indexBufferHandle, (uint)startIndex, (uint)indexCount);
            bgfx.encoder_set_vertex_buffer(encoder, 0, mesh.vertexBufferHandle, (uint)mesh.vertexFirst, (uint)mesh.vertexCount, mesh.vertexDeclHandle);
            // material uniforms setup
            fixed (float4* p = &mat.constAlbedo_Opacity)
                bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_uniformAlbedoOpacity, p, 1);
            fixed (float4* p = &mat.constMetal_Smoothness)
                bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_uniformMetalSmoothness, p, 1);
            fixed (float4* p = &mat.constEmissive_normalMapZScale)
                bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_uniformEmissiveNormalZScale, p, 1);
            float4 debugVect = sys.OutputDebugSelect;
            bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_uniformOutputDebugSelect, &debugVect, 1);
            // textures 
            bgfx.encoder_set_texture(encoder, 0, sys.m_litShader.m_samplerAlbedo, mat.texAlbedo, UInt32.MaxValue);
            bgfx.encoder_set_texture(encoder, 1, sys.m_litShader.m_samplerMetal, mat.texMetal, UInt32.MaxValue);
            bgfx.encoder_set_texture(encoder, 2, sys.m_litShader.m_samplerNormal, mat.texNormal, UInt32.MaxValue);
            bgfx.encoder_set_texture(encoder, 3, sys.m_litShader.m_samplerSmoothness, mat.texSmoothness, UInt32.MaxValue);
            bgfx.encoder_set_texture(encoder, 4, sys.m_litShader.m_samplerEmissive, mat.texEmissive, UInt32.MaxValue);
            bgfx.encoder_set_texture(encoder, 5, sys.m_litShader.m_samplerOpacity, mat.texOpacity, UInt32.MaxValue);

            fixed (float4* p = &mat.mainTextureScaleTranslate)
                bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_uniformTexMad, p, 1);

            // ambient
            fixed (float4* p = &lighting.ambient)
                bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_uniformAmbient, p, 1);

            // transform lighting to view space, if needed: this only needs to re-compute if the viewId changed 
            // also the lighting view space is per-thread, hence it is passed in
            lighting.TransformToViewSpace(ref viewTx, ref viewSpaceLightCache, viewId);

            // dir or point lights
            fixed (float* p = viewSpaceLightCache.podl_positionOrDirViewSpace)
                bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_simplelightPosOrDir, p, (ushort)lighting.numPointOrDirLights);
            fixed (float* p = lighting.podl_colorIVR)
                bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_simplelightColorIVR, p, (ushort)lighting.numPointOrDirLights);

            // mapped lights (always have to set those or there are undefined samplers) 
            EncodeMappedLight(encoder, ref lighting.mappedLight0, ref sys.m_litShader.m_mappedLight0, 0, viewSpaceLightCache.mappedLight0_viewPosOrDir);
            EncodeMappedLight(encoder, ref lighting.mappedLight1, ref sys.m_litShader.m_mappedLight1, 1, viewSpaceLightCache.mappedLight1_viewPosOrDir);
            fixed (float4* p = &lighting.mappedLight01sis)
                bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_texShadow01sis, p, 1);

            float4 numlights = new float4(lighting.numPointOrDirLights, lighting.numMappedLights, 0.0f, 0.0f);
            bgfx.encoder_set_uniform(encoder, sys.m_litShader.m_numLights, &numlights, 1);

            // submit
            bgfx.encoder_submit(encoder, viewId, sys.m_litShader.m_prog, 0, false);
        }

        // ---------------- simple, unlit, with mesh ----------------------------------------------------------------------------------------------------------------------
        public static unsafe void SubmitSimpleDirect(RendererBGFXSystem sys, ushort viewId, ref SimpleMeshBGFX mesh, ref float4x4 tx, ref SimpleMaterialBGFX mat, int startIndex, int indexCount, byte flipCulling)
        {
            bgfx.Encoder* encoder = bgfx.encoder_begin(false);
            EncodeSimple(sys, encoder, viewId, ref mesh, ref tx, ref mat, startIndex, indexCount, flipCulling);
            bgfx.encoder_end(encoder);
        }

        public static unsafe void EncodeSimple(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, ref SimpleMeshBGFX mesh, ref float4x4 tx, ref SimpleMaterialBGFX mat, int startIndex, int indexCount, byte flipCulling)
        {
            bgfx.set_state(mat.state, 0);
            fixed (float4x4* p = &tx)
                bgfx.encoder_set_transform(encoder, p, 1);
            bgfx.encoder_set_index_buffer(encoder, mesh.indexBufferHandle, (uint)startIndex, (uint)indexCount);
            bgfx.encoder_set_vertex_buffer(encoder, 0, mesh.vertexBufferHandle, (uint)mesh.vertexFirst, (uint)mesh.vertexCount, mesh.vertexDeclHandle);
            // material uniforms setup
            fixed (float4* p = &mat.constAlbedo_Opacity)
                bgfx.encoder_set_uniform(encoder, sys.SimpleShader.m_uniformColor0, p, 1);
            fixed (float4* p = &mat.mainTextureScaleTranslate)
                bgfx.encoder_set_uniform(encoder, sys.SimpleShader.m_uniformTexMad, p, 1);
            bgfx.encoder_set_texture(encoder, 0, sys.SimpleShader.m_samplerTexColor0, mat.texAlbedo, UInt32.MaxValue);
            bgfx.encoder_submit(encoder, viewId, sys.SimpleShader.m_prog, 0, false);
        }

        // ---------------- blit ----------------------------------------------------------------------------------------------------------------------
        public static void SubmitBlitDirectFast(RendererBGFXSystem sys, ushort viewId, ref float4x4 tx, float4 color, bgfx.TextureHandle tetxure)
        {
            unsafe {
                bgfx.set_state((uint)(bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA), 0);
                fixed (float4x4* p = &tx)
                    bgfx.set_transform(p, 1);
                bgfx.set_index_buffer(sys.QuadMesh.indexBufferHandle, 0, 6);
                bgfx.set_vertex_buffer(0, sys.QuadMesh.vertexBufferHandle, 0, 4);
                // material uniforms setup
                bgfx.set_uniform(sys.SimpleShader.m_uniformColor0, &color, 1);
                float4 noTexMad = new float4(1, 1, 0, 0);
                bgfx.set_uniform(sys.SimpleShader.m_uniformTexMad, &noTexMad, 1);
                bgfx.set_texture(0, sys.SimpleShader.m_samplerTexColor0, tetxure, UInt32.MaxValue);
            }
            // submit
            bgfx.submit(viewId, sys.SimpleShader.m_prog, 0, false);
        }

        public static void SubmitBlitDirectExtended(RendererBGFXSystem sys, ushort viewId, ref float4x4 tx, bgfx.TextureHandle tetxure,
            bool fromSRGB, bool toSRGB, float reinhard, float4 mulColor, float4 addColor, bool premultiply)
        {
            unsafe {
                bgfx.set_state((uint)(bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA), 0);
                fixed (float4x4* p = &tx)
                    bgfx.set_transform(p, 1);
                bgfx.set_index_buffer(sys.QuadMesh.indexBufferHandle, 0, 6);
                bgfx.set_vertex_buffer(0, sys.QuadMesh.vertexBufferHandle, 0, 4);
                // material uniforms setup
                bgfx.set_uniform(sys.BlitShader.m_colormul, &mulColor, 1);
                bgfx.set_uniform(sys.BlitShader.m_coloradd, &addColor, 1);
                float4 noTexMad = new float4(1, 1, 0, 0);
                bgfx.set_uniform(sys.BlitShader.m_uniformTexMad, &noTexMad, 1);
                bgfx.set_texture(0, sys.BlitShader.m_samplerTexColor0, tetxure, UInt32.MaxValue);
                float4 s = new float4(fromSRGB ? 1.0f : 0.0f, toSRGB ? 1.0f : 0.0f, reinhard, premultiply ? 1.0f : 0.0f);
                bgfx.set_uniform(sys.BlitShader.m_decodeSRGB_encodeSRGB_reinhard_premultiply, &s, 1);
            }
            // submit
            bgfx.submit(viewId, sys.BlitShader.m_prog, 0, false);
        }

        // ---------------- simple, transient, for ui/text ----------------------------------------------------------------------------------------------------------------------
        public static unsafe void SubmitSimpleTransientDirect(RendererBGFXSystem sys, ushort viewId, SimpleVertex* vertices, int nvertices, ushort* indices, int nindices, ref float4x4 tx, ref SimpleMaterialBGFX mat)
        {
            bgfx.Encoder* encoder = bgfx.encoder_begin(false);
            EncodeSimpleTransient(sys, encoder, viewId, vertices, nvertices, indices, nindices, ref tx, ref mat);
            bgfx.encoder_end(encoder);
        }

        public static unsafe void SubmitSimpleTransientDirect(RendererBGFXSystem sys, ushort viewId, SimpleVertex* vertices, int nvertices, ushort* indices, int nindices, ref float4x4 tx, float4 color, bgfx.TextureHandle texture, ulong state)
        {
            bgfx.Encoder* encoder = bgfx.encoder_begin(false);
            EncodeSimpleTransient(sys, encoder, viewId, vertices, nvertices, indices, nindices, ref tx, color, texture, state);
            bgfx.encoder_end(encoder);
        }

        public static unsafe void EncodeSimpleTransient(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, SimpleVertex* vertices, int nvertices, ushort* indices, int nindices, ref float4x4 tx, ref SimpleMaterialBGFX mat)
        {
            EncodeSimpleTransient(sys, encoder, viewId, vertices, nvertices, indices, nindices, ref tx, mat.constAlbedo_Opacity, mat.texAlbedo, mat.state);
        }

        public static unsafe bool EncodeSimpleTransientAlloc(RendererBGFXSystem sys, bgfx.Encoder* encoder, int nindices, int nvertices, SimpleVertex** vertices, ushort** indices)
        {
            bgfx.TransientIndexBuffer tib;
            bgfx.TransientVertexBuffer tvb;
            fixed (bgfx.VertexLayout* declp = sys.SimpleVertexBufferDecl) {
                if (!bgfx.alloc_transient_buffers(&tvb, declp, (uint)nvertices, &tib, (uint)nindices)) {
#if DEBUG
                    // TODO: throw or ignore draw? 
                    throw new InvalidOperationException("Out of transient bgfx memory!");
#else
                    return false; 
#endif
                }
            }
            bgfx.encoder_set_transient_index_buffer(encoder, &tib, 0, (uint)nindices);
            bgfx.encoder_set_transient_vertex_buffer(encoder, 0, &tvb, 0, (uint)nvertices, sys.SimpleVertexBufferDeclHandle);
            *vertices = (SimpleVertex*)tvb.data;
            *indices = (ushort*)tib.data;
            return true;
        }

        public static unsafe void EncodeSimpleTransient(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, ref float4x4 tx, float4 color, bgfx.TextureHandle texture, ulong state)
        {
            // must call EncodeSimpleTransientAlloc before
            // material uniforms setup
            bgfx.encoder_set_state(encoder, state, 0);
            fixed (float4x4* p = &tx)
                bgfx.encoder_set_transform(encoder, p, 1);
            bgfx.encoder_set_uniform(encoder, sys.SimpleShader.m_uniformColor0, &color, 1);
            float4 noTexMad = new float4(1, 1, 0, 0);
            bgfx.encoder_set_uniform(encoder, sys.SimpleShader.m_uniformTexMad, &noTexMad, 1);
            bgfx.encoder_set_texture(encoder, 0, sys.SimpleShader.m_samplerTexColor0, texture, UInt32.MaxValue);
            bgfx.encoder_submit(encoder, viewId, sys.SimpleShader.m_prog, 0, false);
        }

        public static unsafe void EncodeSimpleTransient(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, SimpleVertex* vertices, int nvertices, ushort* indices, int nindices, ref float4x4 tx, float4 color, bgfx.TextureHandle texture, ulong state)
        {
            // upload 
            SimpleVertex* destVertices = null;
            ushort* destIndices = null;
            if (!EncodeSimpleTransientAlloc(sys, encoder, nindices, nvertices, &destVertices, &destIndices))
                return;
            UnsafeUtility.MemCpy(destIndices, indices, nindices * 2);
            UnsafeUtility.MemCpy(destVertices, vertices, nvertices * sizeof(SimpleVertex));
            // material uniforms setup
            EncodeSimpleTransient(sys, encoder, viewId, ref tx, color, texture, state);
        }
    }
}
