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
    // helper gizmos for debugging and visualization 
    // those are not optimized and can be quite slow 

    public struct GizmoNormalsAndTangents : IComponentData // next to a mesh render
    {
        public float length; // in object space
        public float width; // line width, in pixels 
    }

    public struct GizmoObjectBoundingBox : IComponentData // next to something with object bounds
    {
        public float width; // line width, in pixels 
        public float4 color;
    }

    public struct GizmoWorldBoundingBox : IComponentData // next to something with world bounds
    {
        public float width; // line width, in pixels 
        public float4 color;
    }

    public struct GizmoBoundingSphere : IComponentData // next to something with object bounding sphere
    {
        public float width; // line width, in pixels
        public int subdiv; // number of circle subdivisions, must be > 4
    }

    public struct GizmoTransform : IComponentData // next to something with localToWorld transform
    {
        public float length; // in object space
        public float width; // line width, in pixels 
    }

    public struct GizmoLight : IComponentData // next to a light (can detect spot or directional) 
    {
        public float width; // line width, in pixels 
        public bool overrideColor; // if true use color specified here, otherwise use color from light
        public float4 color;
    }

    public struct GizmoWireframe : IComponentData // next to a mesh render
    {
        // TODO 
        public float width; // line width, in pixels 
        public float4 color;
    }

    [UpdateInGroup(typeof(SubmitSystemGroup))]
    public class SubmitGizmos : ComponentSystem
    {
        public readonly int[] EdgeTable = { 0b_000_001, 0b_000_100, 0b001_101, 0b101_100, // top
                                            0b_010_011, 0b_010_110, 0b011_111, 0b111_110, // bottom
                                            0b_000_010, 0b_001_011, 0b_101_111, 0b_100_110 // sides 
                                          };

        static private float3 SelectCoords(float3 c, int mask)
        {
            return SelectCoordsMinMax(new float3(0), c, mask);
        }

        static private float3 SelectCoordsMinMax(float3 cMin, float3 cMax, int mask)
        {
            return new float3((mask & 1) == 1 ? cMax.x : cMin.x, (mask & 2) == 2 ? cMax.y : cMin.y, (mask & 4) == 4 ? cMax.z : cMin.z);
        }

        static unsafe private void Circle(RendererBGFXSystem sys, bgfx.Encoder* encoder, ushort viewId, float3 org, float3 du, float3 dv, float r, int n, float4 color, float2 normWidth,
                                          ref float4x4 tx, ref float4x4 txView, ref float4x4 txProj)
        {
            float3 pprev = org + dv * r;
            for (int i = 1; i <= n; i++) {
                float a = ((float)i / (float)n) * math.PI * 2.0f;
                float u = math.sin(a) * r;
                float v = math.cos(a) * r;
                float3 p = org + du * u + dv * v;
                SubmitHelper.EncodeLine(sys, encoder, viewId, pprev, p, color, normWidth, ref tx, ref txView, ref txProj);
                pprev = p;
            }
        }

        static float3 MakeFrustumVertex(int idx, float w, float h, float near, float far)
        {
            float3 r = new float3(w, h, 1.0f);
            if (idx < 4) { r *= near; } else { r *= far; }
            switch (idx & 3) {
                case 0: break;
                case 1: r.x = -r.x; break;
                case 3: r.x = -r.x; r.y = -r.y; break;
                case 2: r.y = -r.y; break;
            }
            return r;
        }

        // gizmos for debuging 
        protected unsafe override void OnUpdate()
        {
            var sys = World.GetExistingSystem<RendererBGFXSystem>();
            if (!sys.Initialized)
                return;

            bgfx.Encoder* encoder = bgfx.encoder_begin(false);

            // tangents & normals
            Entities.ForEach((Entity e, ref MeshRenderer mr, ref LitMeshReference meshRef, ref LocalToWorld tx, ref GizmoNormalsAndTangents giz) =>
            {
                if (!EntityManager.HasComponent<RenderToPasses>(e))
                    return;
                RenderToPasses toPassesRef = EntityManager.GetSharedComponentData<RenderToPasses>(e);
                DynamicBuffer<RenderToPassesEntry> toPasses = EntityManager.GetBufferRO<RenderToPassesEntry>(toPassesRef.e);
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    float4x4 adjustedProjection = sys.GetAdjustedProjection(ref pass);
                    var meshBase = EntityManager.GetComponentData<LitMeshRenderData>(meshRef.mesh);
                    Assert.IsTrue(giz.length > 0);
                    float2 normWidth = new float2(giz.width / pass.viewport.w, giz.width / pass.viewport.h);
                    SubmitHelper.EncodeDebugTangents(sys, encoder, pass.viewId, normWidth, giz.length, ref meshBase, ref tx.Value, ref pass.viewTransform, ref adjustedProjection);
                }
            });

            // object bounding box
            Entities.ForEach((Entity e, ref ObjectBounds b, ref LocalToWorld tx, ref GizmoObjectBoundingBox giz) =>
            {
                if (!EntityManager.HasComponent<RenderToPasses>(e))
                    return;
                RenderToPasses toPassesRef = EntityManager.GetSharedComponentData<RenderToPasses>(e);
                DynamicBuffer<RenderToPassesEntry> toPasses = EntityManager.GetBufferRO<RenderToPassesEntry>(toPassesRef.e);
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    float4x4 adjustedProjection = sys.GetAdjustedProjection(ref pass);
                    float2 normWidth = new float2(giz.width / pass.viewport.w, giz.width / pass.viewport.h);
                    for (int j = 0; j < EdgeTable.Length; j++) {
                        float3 p0 = b.bounds.Min + SelectCoords(b.bounds.Size, EdgeTable[j]&7);
                        float3 p1 = b.bounds.Min + SelectCoords(b.bounds.Size, EdgeTable[j]>>3);
                        SubmitHelper.EncodeLine(sys, encoder, pass.viewId, p0, p1, giz.color, normWidth,
                            ref tx.Value, ref pass.viewTransform, ref adjustedProjection);
                    }
                }
            });

            // world bounds 
            Entities.ForEach((Entity e, ref WorldBounds b, ref LocalToWorld tx, ref GizmoWorldBoundingBox giz) =>
            {
                if (!EntityManager.HasComponent<RenderToPasses>(e))
                    return;
                RenderToPasses toPassesRef = EntityManager.GetSharedComponentData<RenderToPasses>(e);
                DynamicBuffer<RenderToPassesEntry> toPasses = EntityManager.GetBufferRO<RenderToPassesEntry>(toPassesRef.e);
                float4x4 idm = float4x4.identity;
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    float4x4 adjustedProjection = sys.GetAdjustedProjection(ref pass);
                    float2 normWidth = new float2(giz.width / pass.viewport.w, giz.width / pass.viewport.h);
                    for (int j = 0; j < EdgeTable.Length; j++) {
                        float3 p0 = b.GetVertex(EdgeTable[j] & 7);
                        float3 p1 = b.GetVertex(EdgeTable[j] >> 3);
                        SubmitHelper.EncodeLine(sys, encoder, pass.viewId, p0, p1, giz.color, normWidth,
                            ref idm, ref pass.viewTransform, ref adjustedProjection);
                    }
                }
            });

            // transform
            Entities.ForEach((Entity e, ref LocalToWorld tx, ref GizmoTransform giz) =>
            {
                if (!EntityManager.HasComponent<RenderToPasses>(e))
                    return;
                RenderToPasses toPassesRef = EntityManager.GetSharedComponentData<RenderToPasses>(e);
                DynamicBuffer<RenderToPassesEntry> toPasses = EntityManager.GetBufferRO<RenderToPassesEntry>(toPassesRef.e);
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    float4x4 adjustedProjection = sys.GetAdjustedProjection(ref pass);
                    float2 normWidth = new float2(giz.width / pass.viewport.w, giz.width / pass.viewport.h);
                    Assert.IsTrue(giz.length > 0);
                    SubmitHelper.EncodeLine(sys, encoder, pass.viewId, new float3(0), new float3(giz.length, 0, 0), new float4(1, 0, 0, 1), normWidth, ref tx.Value, ref pass.viewTransform, ref adjustedProjection);
                    SubmitHelper.EncodeLine(sys, encoder, pass.viewId, new float3(0), new float3(0, giz.length, 0), new float4(0, 1, 0, 1), normWidth, ref tx.Value, ref pass.viewTransform, ref adjustedProjection);
                    SubmitHelper.EncodeLine(sys, encoder, pass.viewId, new float3(0), new float3(0, 0, giz.length), new float4(0, 0, 1, 1), normWidth, ref tx.Value, ref pass.viewTransform, ref adjustedProjection);
                }
            });

            // sphere 
            Entities.ForEach((Entity e, ref LocalToWorld tx, ref ObjectBoundingSphere bs, ref GizmoBoundingSphere giz) =>
            {
                if (!EntityManager.HasComponent<RenderToPasses>(e))
                    return;
                RenderToPasses toPassesRef = EntityManager.GetSharedComponentData<RenderToPasses>(e);
                DynamicBuffer<RenderToPassesEntry> toPasses = EntityManager.GetBufferRO<RenderToPassesEntry>(toPassesRef.e);
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    float4x4 adjustedProjection = sys.GetAdjustedProjection(ref pass);
                    float2 normWidth = new float2(giz.width / pass.viewport.w, giz.width / pass.viewport.h);
                    Assert.IsTrue(giz.subdiv >= 4);
                    Circle(sys, encoder, pass.viewId, bs.position, new float3(0, 0, 1), new float3(0, 1, 0), bs.radius, giz.subdiv, new float4(1, 0, 0, 1), normWidth, ref tx.Value, ref pass.viewTransform, ref adjustedProjection); // z/y plane 
                    Circle(sys, encoder, pass.viewId, bs.position, new float3(1, 0, 0), new float3(0, 0, 1), bs.radius, giz.subdiv, new float4(0, 1, 0, 1), normWidth, ref tx.Value, ref pass.viewTransform, ref adjustedProjection); // x/z plane
                    Circle(sys, encoder, pass.viewId, bs.position, new float3(0, 1, 0), new float3(1, 0, 0), bs.radius, giz.subdiv, new float4(0, 0, 1, 1), normWidth, ref tx.Value, ref pass.viewTransform, ref adjustedProjection); // y/x plane
                }
            });

            // spot lights 
            Entities.ForEach((Entity e, ref LocalToWorld tx, ref Light l, ref SpotLight sl, ref GizmoLight giz) =>
            {
                if (!EntityManager.HasComponent<RenderToPasses>(e))
                    return;
                RenderToPasses toPassesRef = EntityManager.GetSharedComponentData<RenderToPasses>(e);
                DynamicBuffer<RenderToPassesEntry> toPasses = EntityManager.GetBufferRO<RenderToPassesEntry>(toPassesRef.e);
                float4 color = giz.overrideColor ? giz.color : new float4(l.color, 1.0f);
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    float2 normWidth = new float2(giz.width / pass.viewport.w, giz.width / pass.viewport.h);
                    float4x4 adjustedProjection = sys.GetAdjustedProjection(ref pass);
                    // render frustum
                    float t = math.tan(math.radians(sl.fov) * .5f);
                    for (int j = 0; j < EdgeTable.Length; j++) {
                        float3 pp0 = MakeFrustumVertex(EdgeTable[j] & 7, t, t, l.clipZNear, l.clipZFar);
                        float3 pp1 = MakeFrustumVertex(EdgeTable[j] >> 3, t, t, l.clipZNear, l.clipZFar);
                        SubmitHelper.EncodeLine(sys, encoder, pass.viewId, pp0, pp1, color, normWidth,
                            ref tx.Value, ref pass.viewTransform, ref adjustedProjection);
                    }
                }
            });

            // directional lights
            Entities.ForEach((Entity e, ref LocalToWorld tx, ref Light l, ref DirectionalLight dl, ref GizmoLight giz) =>
            {
                if (!EntityManager.HasComponent<RenderToPasses>(e))
                    return;
                RenderToPasses toPassesRef = EntityManager.GetSharedComponentData<RenderToPasses>(e);
                DynamicBuffer<RenderToPassesEntry> toPasses = EntityManager.GetBufferRO<RenderToPassesEntry>(toPassesRef.e);
                float4 color = giz.overrideColor ? giz.color : new float4(l.color, 1.0f);
                float3 cMin = new float3(-dl.size, -dl.size, l.clipZNear);
                float3 cMax = new float3( dl.size,  dl.size, l.clipZFar);
                for (int i = 0; i < toPasses.Length; i++) {
                    Entity ePass = toPasses[i].e;
                    var pass = EntityManager.GetComponentData<RenderPass>(ePass);
                    float2 normWidth = new float2(giz.width / pass.viewport.w, giz.width / pass.viewport.h);
                    float4x4 adjustedProjection = sys.GetAdjustedProjection(ref pass);
                    // render box
                    for (int j = 0; j < EdgeTable.Length; j++) {
                        float3 p0 = SelectCoordsMinMax(cMin, cMax, EdgeTable[j]&7);
                        float3 p1 = SelectCoordsMinMax(cMin, cMax, EdgeTable[j]>>3);
                        SubmitHelper.EncodeLine(sys, encoder, pass.viewId, p0, p1, color, normWidth,
                            ref tx.Value, ref pass.viewTransform, ref adjustedProjection);
                    }
                }
            });

            // point lights 



            bgfx.encoder_end(encoder);
        }
    }


}
