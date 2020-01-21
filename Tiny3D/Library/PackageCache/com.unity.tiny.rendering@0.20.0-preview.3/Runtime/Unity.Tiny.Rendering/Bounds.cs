using System;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Tiny;
using Unity.Tiny.Assertions;
using Unity.Tiny.Rendering;
using Unity.Transforms;

namespace Unity.Tiny.Rendering
{
    public struct WorldBounds : IComponentData
    {
        public float3 c000, c001, c011, c010;
        public float3 c100, c101, c111, c110;

        public float3 GetVertex(int idx)
        {
            switch(idx) {
                case 0b_000: return c000;
                case 0b_001: return c001;
                case 0b_011: return c011;
                case 0b_010: return c010;
                case 0b_100: return c100;
                case 0b_101: return c101;
                case 0b_111: return c111;
                case 0b_110: return c110;
            }
            throw new IndexOutOfRangeException();
        }

        public void SetVertex(int idx, float3 value)
        {
            switch(idx) {
                case 0b_000: c000 = value; break;
                case 0b_001: c001 = value; break;
                case 0b_011: c011 = value; break;
                case 0b_010: c010 = value; break;
                case 0b_100: c100 = value; break;
                case 0b_101: c101 = value; break;
                case 0b_111: c111 = value; break;
                case 0b_110: c110 = value; break;
                default:  throw new IndexOutOfRangeException();
            }
            
        }
    }

    public struct ObjectBounds : IComponentData
    {
        public AABB bounds;
    }

    public struct ObjectBoundingSphere : IComponentData
    {
        public float3 position;
        public float radius;
    }

    public struct WorldBoundingSphere : IComponentData
    {
        public float3 position;
        public float radius;
    }

    public struct ChunkWorldBoundingSphere : IComponentData
    {
        public WorldBoundingSphere Value;
    }

    public struct ChunkWorldBounds : IComponentData
    {
        public AABB Value;
    }

    public static class Culling
    {
        static bool IsCulled8(float3 p0, float3 p1, float3 p2, float3 p3, float3 p4, float3 p5, float3 p6, float3 p7, float4 plane)
        {
            float4 acc0; 
            acc0  = plane.xxxx * new float4(p0.x, p1.x, p2.x, p3.x);
            acc0 += plane.yyyy * new float4(p0.y, p1.y, p2.y, p3.y);
            acc0 += plane.zzzz * new float4(p0.z, p1.z, p2.z, p3.z);
            bool4 c0 = acc0 >= -plane.wwww;
            if (math.any(c0))
                return false;
            float4 acc1; 
            acc1  = plane.xxxx * new float4(p4.x, p5.x, p6.x, p7.x);
            acc1 += plane.yyyy * new float4(p4.y, p5.y, p6.y, p7.y);
            acc1 += plane.zzzz * new float4(p4.z, p5.z, p6.z, p7.z);
            bool4 c1 = acc1 >= -plane.wwww;
            if (math.any(c1))
                return false;
            return true; 
        }

        static bool IsCulled(float3 p, float4 plane)
        {
            return math.dot(p, plane.xyz) < -plane.w; // important: (0,0,0,0) plane never culls, can be used to pad Frustum struct
        }

        public enum CullingResult
        {
            Outside = 0,
            Intersects = 1, 
            Inside = 2
        }

        static CullingResult Cull8(float3 p0, float3 p1, float3 p2, float3 p3, float3 p4, float3 p5, float3 p6, float3 p7, float4 plane)
        {
            float4 acc0; 
            acc0  = plane.xxxx * new float4(p0.x, p1.x, p2.x, p3.x);
            acc0 += plane.yyyy * new float4(p0.y, p1.y, p2.y, p3.y);
            acc0 += plane.zzzz * new float4(p0.z, p1.z, p2.z, p3.z);
            bool4 c0 = acc0 >= -plane.wwww; 
            float4 acc1; 
            acc1  = plane.xxxx * new float4(p4.x, p5.x, p6.x, p7.x);
            acc1 += plane.yyyy * new float4(p4.y, p5.y, p6.y, p7.y);
            acc1 += plane.zzzz * new float4(p4.z, p5.z, p6.z, p7.z);
            bool4 c1 = acc1 >= -plane.wwww;
            bool4 cor = c0 | c1;
            if (math.any(cor)) {
                bool4 cand =  c0 & c1;
                if (math.all(cand))
                    return CullingResult.Inside;
                else 
                    return CullingResult.Intersects;
            }
            return CullingResult.Outside;
        }

        static public CullingResult Cull(ref WorldBoundingSphere bounds, float4 plane)
        {
            float dist = math.dot(bounds.position, plane.xyz) + plane.w;
            if (dist <= -bounds.radius) return CullingResult.Outside;
            if (dist >= bounds.radius) return CullingResult.Inside;
            return CullingResult.Intersects;
        }

        static public CullingResult Cull(ref WorldBoundingSphere bounds, ref Frustum f)
        {
            CullingResult rall = CullingResult.Inside;
            for (int i = 0; i < f.PlanesCount; i++) {
                float4 plane = f.GetPlane(i);
                CullingResult r = Cull(ref bounds, plane);
                if (r == CullingResult.Outside)
                    return CullingResult.Outside;
                if (r == CullingResult.Intersects)
                    rall = CullingResult.Intersects;
            }
            return rall; 
        }

        static public CullingResult Cull(ref WorldBounds bounds, ref Frustum f)
        {
            int mall = 0;
            for (int i = 0; i < f.PlanesCount; i++) {
                float4 plane = f.GetPlane(i);
                int m = IsCulled(bounds.c000, plane) ? 1 : 0;
                m |= IsCulled(bounds.c001, plane) ? 2 : 0;
                m |= IsCulled(bounds.c010, plane) ? 4 : 0;
                m |= IsCulled(bounds.c011, plane) ? 8 : 0;
                m |= IsCulled(bounds.c100, plane) ? 16 : 0;
                m |= IsCulled(bounds.c101, plane) ? 32 : 0;
                m |= IsCulled(bounds.c110, plane) ? 64 : 0;
                m |= IsCulled(bounds.c111, plane) ? 128 : 0;
                if (m == 255) return CullingResult.Outside; // all points outside one plane 
                mall |= m; 
            }
            if (mall == 0) return CullingResult.Inside; // all points inside all planes
            return CullingResult.Intersects;
        }

        static public bool IsCulled(ref WorldBounds bounds, ref Frustum f)
        {
            // if all vertices are completely outside of one culling plane, the object is culled 
            for (int i = 0; i < f.PlanesCount; i++) {
                float4 plane = f.GetPlane(i);
                if (IsCulled8(bounds.c000, bounds.c001, bounds.c011, bounds.c010, 
                              bounds.c100, bounds.c101, bounds.c111, bounds.c110, plane))
                    return true;
            }
            return false;
            /* reference
            bool r = false;
            bgfx.dbg_text_clear(0, false);
            for (int i = 0; i < 6; i++) {
                float4 plane = f.GetPlane(i);
                int m = IsCulled(bounds.c000, plane) ? 1 : 0;
                m |= IsCulled(bounds.c001, plane) ? 2 : 0;
                m |= IsCulled(bounds.c010, plane) ? 4 : 0;
                m |= IsCulled(bounds.c011, plane) ? 8 : 0;
                m |= IsCulled(bounds.c100, plane) ? 16 : 0;
                m |= IsCulled(bounds.c101, plane) ? 32 : 0;
                m |= IsCulled(bounds.c110, plane) ? 64 : 0;
                m |= IsCulled(bounds.c111, plane) ? 128 : 0;
                if (m == 255) r = true;

                string s = StringFormatter.Format("{0}: {1} {2}   ", i, m, plane);
                bgfx.dbg_text_printf(0, (ushort)i, 0xf0, s, null);

                //if (IsCulled8(bounds.c000, bounds.c001, bounds.c011, bounds.c010, 
                //              bounds.c100, bounds.c101, bounds.c111, bounds.c110, plane))
                //    return true;
            }
            bgfx.set_debug((uint)bgfx.DebugFlags.Text);
            return r;
            */
        }

        static public void WorldBoundsToAxisAligned(ref WorldBounds wBounds, out AABB aab)
        {
            float3 bbMin = wBounds.c000;
            float3 bbMax = wBounds.c000;
            GrowBounds(ref bbMin, ref bbMax, in wBounds);
            aab.Center = (bbMax + bbMin) * .5f;
            aab.Extents = (bbMax - bbMin) * .5f;
        }

        static public void AxisAlignedToWorldBounds(ref float4x4 tx, ref AABB aaBounds, out WorldBounds wBounds)
        {
            float3 o = math.mul(tx, new float4(aaBounds.Min, 1)).xyz;
            float3 dx = math.mul(tx, new float4(aaBounds.Size.x, 0, 0, 0)).xyz;
            float3 dy = math.mul(tx, new float4(0, aaBounds.Size.y, 0, 0)).xyz;
            float3 dz = math.mul(tx, new float4(0, 0, aaBounds.Size.z, 0)).xyz;
            wBounds.c000 = o;
            wBounds.c001 = o + dx;
            wBounds.c011 = o + dx + dy;
            wBounds.c010 = o + dy;
            wBounds.c100 = o + dz;
            wBounds.c101 = o + dz + dx;
            wBounds.c111 = o + dz + dx + dy;
            wBounds.c110 = o + dz + dy;
        }

        static public int SphereInSphere(float4 sphere1, float4 sphere2) 
        {
            float d = math.length(sphere1.xyz - sphere2.xyz);
            if (d + sphere2.w <= sphere1.w) return 1; // 2 inside 1
            if (d + sphere1.w <= sphere2.w) return 2; // 1 inside 2
            return 0; // intersecting
        }

        // modifies sphere1 with result 
        static public void MergeSpheres ( ref float4 sphere1, float4 sphere2 )
        {
            int check = SphereInSphere(sphere1, sphere2);
            if (check == 0)
            {
                float3 resultPos = (sphere1.xyz + sphere2.xyz) * .5f;
                float rMaxTo1 = math.length(resultPos - sphere1.xyz) + sphere1.w;
                float rMaxTo2 = math.length(resultPos - sphere2.xyz) + sphere2.w;
                sphere1 = new float4(resultPos, math.max(rMaxTo1, rMaxTo2));
                return;
            }

            if (check == 2)
            {
                sphere1 = sphere2;
                return;
            }

            Assert.IsTrue(check == 1);
            // sphere1 unchanged
        }

        static public float4 PlaneFromTri(float3 p0, float3 p1, float3 p2)
        {
            float3 n = math.normalize(math.cross(p1 - p0, p2 - p0));
            return new float4(n, math.dot(n, p0));
        }

        static public bool PointInBounds ( ref WorldBounds bounds, float3 p )
        {
            float4 pfront = PlaneFromTri(bounds.c000, bounds.c001, bounds.c011);
            if (IsCulled(p, pfront))
                return false;
            float4 pback = PlaneFromTri(bounds.c100, bounds.c111, bounds.c101);
            if (IsCulled(p, pback))
                return false;
            // etc TODO
            Assert.IsTrue(false);

            return true;
        }

        // modifies bounds1 with results
        static public void MergeBounds ( ref WorldBounds bounds1, ref WorldBounds bounds2 )
        {
            // TODO
            Assert.IsTrue(false);
        }

        static public void GrowBounds(ref float3 bbMin, ref float3 bbMax, in WorldBounds wb)
        {
            bbMin = math.min(wb.c000, bbMin);
            bbMin = math.min(wb.c001, bbMin);
            bbMin = math.min(wb.c010, bbMin);
            bbMin = math.min(wb.c011, bbMin);
            bbMin = math.min(wb.c100, bbMin);
            bbMin = math.min(wb.c101, bbMin);
            bbMin = math.min(wb.c110, bbMin);
            bbMin = math.min(wb.c111, bbMin);

            bbMax = math.max(wb.c000, bbMax);
            bbMax = math.max(wb.c001, bbMax);
            bbMax = math.max(wb.c010, bbMax);
            bbMax = math.max(wb.c011, bbMax);
            bbMax = math.max(wb.c100, bbMax);
            bbMax = math.max(wb.c101, bbMax);
            bbMax = math.max(wb.c110, bbMax);
            bbMax = math.max(wb.c111, bbMax);
        }
    }

    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UpdateCameraMatricesSystem))]
    public class UpdateWorldBoundsSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            // add WorldBounds to ObjectBounds if not present
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithNone<WorldBounds>().WithAll<ObjectBounds, LocalToWorld>().ForEach((Entity e) =>
            {
                ecb.AddComponent<WorldBounds>(e); // zero bounds 
            });

            Entities.WithNone<ObjectBounds>().ForEach((Entity e, ref MeshRenderer mr, ref SimpleMeshReference m) =>
            {
                Assert.IsTrue(EntityManager.HasComponent<SimpleMeshRenderData>(m.mesh) == true);

                ObjectBounds ob = default;
                var mrd = EntityManager.GetComponentData<SimpleMeshRenderData>(m.mesh);
                ob.bounds = mrd.Mesh.Value.Bounds;
                ecb.AddComponent(e, ob);
            });

            Entities.WithNone<ObjectBounds>().ForEach((Entity e, ref MeshRenderer mr, ref LitMeshReference m) =>
            {
                Assert.IsTrue(EntityManager.HasComponent<LitMeshRenderData>(m.mesh) == true);

                ObjectBounds ob = default;
                var mrd = EntityManager.GetComponentData<LitMeshRenderData>(m.mesh);
                ob.bounds = mrd.Mesh.Value.Bounds;
                ecb.AddComponent(e, ob);
            });

            ecb.Playback(EntityManager);
            ecb.Dispose();

            // add and init sphere bounds from object bounds
            ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithNone<ObjectBoundingSphere>().ForEach((Entity e, ref ObjectBounds ob) =>
            {
                float3 halfext = ob.bounds.Extents;
                ecb.AddComponent(e,new ObjectBoundingSphere {
                    position = ob.bounds.Min + halfext,
                    radius = math.length(halfext)
                });
            });
            ecb.Playback(EntityManager);
            ecb.Dispose();

            // add world sphere bounds from object bounds
            ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithNone<WorldBoundingSphere>().ForEach((Entity e, ref ObjectBounds ob) =>
            {
                ecb.AddComponent(e,typeof(WorldBoundingSphere));
            });
            ecb.Playback(EntityManager);
            ecb.Dispose();

            // update bounds -- aa box
            Entities.ForEach((Entity e, ref ObjectBounds ob, ref LocalToWorld tx, ref WorldBounds b) =>
            {
                Culling.AxisAlignedToWorldBounds(ref tx.Value, ref ob.bounds, out b);
            });

            // update bounds -- spheres
            Entities.WithAny<Scale,NonUniformScale>().ForEach((Entity e, ref ObjectBoundingSphere ob, ref LocalToWorld tx, ref WorldBoundingSphere b) =>
            {
                b.position = math.transform(tx.Value, ob.position);
                // only if there is scale 
                float3 scale = new float3(math.lengthsq(tx.Value.c0), math.lengthsq(tx.Value.c1), math.lengthsq(tx.Value.c2));
                float s = math.sqrt(math.cmax(scale));
                b.radius = s * ob.radius;
            });
            Entities.WithNone<Scale,NonUniformScale>().ForEach((Entity e, ref ObjectBoundingSphere ob, ref LocalToWorld tx, ref WorldBoundingSphere b) =>
            {
                b.position = math.transform(tx.Value, ob.position);
                b.radius = ob.radius;
            });

            // experimental: chunk bounds, if we keep this it should be done in one loop, updating bounds and chunk bounds at the same time

            // add chunk bounds
            var q = EntityManager.CreateEntityQuery(new EntityQueryDesc {
                All = new ComponentType[] {ComponentType.ReadOnly<WorldBoundingSphere>()},
                None = new ComponentType[] {ComponentType.ChunkComponent<ChunkWorldBoundingSphere>() }
            });
            EntityManager.AddChunkComponentData(q, new ChunkWorldBoundingSphere());
            q.Dispose();

            q = EntityManager.CreateEntityQuery(new EntityQueryDesc {
                All = new ComponentType[] {ComponentType.ReadOnly<WorldBounds>()},
                None = new ComponentType[] {ComponentType.ChunkComponent<ChunkWorldBounds>() }
            });
            EntityManager.AddChunkComponentData(q, new ChunkWorldBounds());
            q.Dispose();

            // update all chunk bounds
            q = EntityManager.CreateEntityQuery(new EntityQueryDesc {
                All = new ComponentType[] {ComponentType.ReadOnly<WorldBounds>(), ComponentType.ReadOnly<WorldBoundingSphere>(), 
                                           ComponentType.ChunkComponent<ChunkWorldBoundingSphere>(), ComponentType.ChunkComponent<ChunkWorldBounds>()},
            });
            var chunks = q.CreateArchetypeChunkArray(Allocator.TempJob);
            var chunkBoundsType = EntityManager.GetArchetypeChunkComponentType<ChunkWorldBounds>(false);
            var worldBoundsType = EntityManager.GetArchetypeChunkComponentType<WorldBounds>(true);
            var chunkBoundingSphereType = EntityManager.GetArchetypeChunkComponentType<ChunkWorldBoundingSphere>(false);
            var worldBoundingSphereType = EntityManager.GetArchetypeChunkComponentType<WorldBoundingSphere>(true);
            for ( int i=0; i<chunks.Length; i++ ) {
                var chunk = chunks[i];
                var worldBounds = chunk.GetNativeArray<WorldBounds>(worldBoundsType);
                var worldBoundingSpheres = chunk.GetNativeArray<WorldBoundingSphere>(worldBoundingSphereType);
                float3 bbMin, bbMax;
                float4 sphere; 
                unsafe {
                    WorldBounds *wbPtr = (WorldBounds*)worldBounds.GetUnsafeReadOnlyPtr();
                    WorldBoundingSphere *wbsPtr = (WorldBoundingSphere*)worldBoundingSpheres.GetUnsafeReadOnlyPtr();
                    int k = worldBounds.Length;
                    Assert.IsTrue(k > 0);
                    Assert.IsTrue(k == worldBoundingSpheres.Length);
                    bbMin = wbPtr[0].c000;
                    bbMax = bbMin;
                    sphere = new float4(wbsPtr[0].position, wbsPtr[0].radius);
                    for (int j = 0; j < k; j++) {
                        Culling.GrowBounds(ref bbMin, ref bbMax, in wbPtr[j]);
                        Culling.MergeSpheres(ref sphere, new float4(wbsPtr[j].position, wbsPtr[j].radius));
                    }
                }
                chunks[i].SetChunkComponentData<ChunkWorldBounds>(chunkBoundsType, new ChunkWorldBounds {
                    Value = new AABB { 
                        Center = bbMin + (bbMax - bbMin) * 0.5f,
                        Extents = (bbMax - bbMin) * 0.5f
                    }});
                chunks[i].SetChunkComponentData<ChunkWorldBoundingSphere>(chunkBoundingSphereType, new ChunkWorldBoundingSphere {
                    Value = new WorldBoundingSphere {
                        position = sphere.xyz,
                        radius = sphere.w
                    }});
            }
            chunks.Dispose();
            q.Dispose();
        }
    }
}
