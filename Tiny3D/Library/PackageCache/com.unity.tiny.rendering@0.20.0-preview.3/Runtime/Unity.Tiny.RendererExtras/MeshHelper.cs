using Unity.Tiny;
using Unity.Tiny.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Unity.Tiny.Rendering
{
    public static class MeshHelper
    {
        public static void SetConstant<T> (ref BlobBuilderArray<T> values, T value) where T : unmanaged
        {
            unsafe {
                int nv = values.Length;
                var colors = (T*)values.GetUnsafePtr();
                for (int i = 0; i < nv; i++)
                    values[i] = value;
            }
        }

        public static void ComputeNormals (ref BlobBuilderArray<LitVertex> vertices, ref BlobBuilderArray<ushort> indices)
        {
            unsafe {
                int nv = vertices.Length;
                var verts = (LitVertex*)vertices.GetUnsafePtr();
                int ni = indices.Length;
                var inds = (ushort*)indices.GetUnsafePtr();
                //var norms = (float3*)normals.GetUnsafePtr();
                //UnsafeUtility.MemClear(norms, nv * sizeof(float3));
                for ( int i=0; i<ni; i+=3 ) {
                    int i0 = inds[i];
                    int i1 = inds[i+1];
                    int i2 = inds[i+2];
                    float3 n = -math.cross(verts[i2].Position - verts[i0].Position, verts[i1].Position - verts[i0].Position); // don't normalize, weights by area 
                    vertices[i0].Normal += n;
                    vertices[i1].Normal += n;
                    vertices[i2].Normal += n;
                }
                for (int i = 0; i < nv; i++) {
                    Assert.IsTrue(math.lengthsq(vertices[i].Normal) > 0.0f);
                    vertices[i].Normal = math.normalize(vertices[i].Normal);
                }
            }
        }

        public static unsafe void ComputeTangentAndBinormal(ref BlobBuilderArray<LitVertex> vertices, ref BlobBuilderArray<ushort> indices)
        {
            int nv = vertices.Length;
            int ni = indices.Length;
            var inds = (ushort*)indices.GetUnsafePtr();

            // assumes normal is valid! 
            for ( int i=0; i<ni; i+=3 ) {
                int i0 = inds[i+2];
                int i1 = inds[i+1];
                int i2 = inds[i];
                float3 edge1 = vertices[i1].Position - vertices[i0].Position;
                Assert.IsTrue(math.lengthsq(edge1) > 0);
                float3 edge2 = vertices[i2].Position - vertices[i0].Position;
                Assert.IsTrue(math.lengthsq(edge2) > 0);
                float2 uv1 = vertices[i1].TexCoord0 - vertices[i0].TexCoord0;
                Assert.IsTrue(math.lengthsq(uv1) > 0);
                float2 uv2 = vertices[i2].TexCoord0 - vertices[i0].TexCoord0;
                Assert.IsTrue(math.lengthsq(uv2) > 0);
                float r = 1.0f / (uv1.x * uv2.y - uv1.y * uv2.x);
                float3 n = math.cross(edge2, edge1);
                float3 tangent = new float3(
                    ((edge1.x * uv2.y) - (edge2.x * uv1.y)) * r,
                    ((edge1.y * uv2.y) - (edge2.y * uv1.y)) * r,
                    ((edge1.z * uv2.y) - (edge2.z * uv1.y)) * r
                );
                float3 bitangent = new float3(
                    ((edge1.x * uv2.x) - (edge2.x * uv1.x)) * r,
                    ((edge1.y * uv2.x) - (edge2.y * uv1.x)) * r,
                    ((edge1.z * uv2.x) - (edge2.z * uv1.x)) * r
                );
                Assert.IsTrue(math.lengthsq(tangent) > 0.0f);
                Assert.IsTrue(math.lengthsq(bitangent) > 0.0f);
                float3 n2 = math.cross(tangent, bitangent);
                if ( math.dot(n2,n) > 0.0f ) {
                    tangent = -tangent;
                    bitangent = -bitangent;
                }
                vertices[i0].Tangent += tangent;
                vertices[i0].BiTangent += bitangent;
                vertices[i1].Tangent += tangent;
                vertices[i1].BiTangent += bitangent;
                vertices[i2].Tangent += tangent;
                vertices[i2].BiTangent += bitangent;
            }

            for (int i = 0; i < nv; i++) {
                Assert.IsTrue(math.lengthsq(vertices[i].Tangent) > 0.0f);
                Assert.IsTrue(math.lengthsq(vertices[i].BiTangent) > 0.0f);
                vertices[i].Tangent = math.normalize(vertices[i].Tangent);
                vertices[i].BiTangent = math.normalize(vertices[i].BiTangent);
            }
        }

        public static void AddBoxFace(ref BlobBuilderArray<LitVertex> vertices, ref BlobBuilderArray<ushort> indices, int side, int sign, float3 size, ref int destI, ref int destV, float uvscale)
        {
            int i0 = destV;
            float3 p = new float3(0);
            p[side] = sign * size[side];
            float3 du = new float3(0);
            int side1 = (side + 1) % 3;
            du[side1] = sign * size[side1];
            float3 dv = new float3(0);
            int side2 = (side + 2) % 3;
            dv[side2] = 1.0f * size[side2];
            float3 p0 = -du - dv + p; float2 uv0 = new float2(0, 0.0f);
            float3 p1 =  du - dv + p; float2 uv1 = new float2(size[side1]*uvscale, 0.0f);
            float3 p2 =  du + dv + p; float2 uv2 = new float2(size[side1]*uvscale,size[side2]*uvscale);
            float3 p3 = -du + dv + p; float2 uv3 = new float2(0,size[side2]*uvscale);
            vertices[destV].Position = p0; vertices[destV].TexCoord0 = uv0; destV++;
            vertices[destV].Position = p1; vertices[destV].TexCoord0 = uv1; destV++;
            vertices[destV].Position = p2; vertices[destV].TexCoord0 = uv2; destV++;
            vertices[destV].Position = p3; vertices[destV].TexCoord0 = uv3; destV++;
            indices[destI+2]   = (ushort)i0;     indices[destI+1] = (ushort)(i0+2); indices[destI] = (ushort)(i0+1);
            indices[destI+5] = (ushort)(i0+2); indices[destI+4] = (ushort)(i0+0); indices[destI+3] = (ushort)(i0+3);
            destI += 6;
        }

        public static void MakeDonut(ref BlobBuilderArray<LitVertex> data, ref BlobBuilderArray<ushort> indices,
                                     float innerR, int innerN, float outerR, int outerN)
        {
            int o = 0;
            float uScale = outerR / innerR;
            for (int aOuter = 0; aOuter < outerN; aOuter++) {
                float fOuter = (float)aOuter / (float)(outerN - 1);
                float u = fOuter * 2.0f * math.PI;
                for (int aInner = 0; aInner < innerN; aInner++) {
                    float fInner = (float)aInner / (float)(innerN - 1);
                    float v = fInner * 2.0f * math.PI;
                    data[o].TexCoord0 = new float2 {
                        x = fOuter * uScale,
                        y = fInner
                    };
                    // from http://mathworld.wolfram.com/Torus.html
                    data[o].Position = new float3 {
                        x = (outerR + innerR * math.cos(v)) * math.cos(u),
                        y = (outerR + innerR * math.cos(v)) * math.sin(u),
                        z = innerR * math.sin(v)
                    };
                    data[o].Normal = math.normalize(new float3 {
                        x = math.cos(v) * math.cos(u),
                        y = math.cos(v) * math.sin(u),
                        z = math.sin(v)
                    });
                    data[o].Albedo_Opacity = new float4(1);
                    data[o].Metal_Smoothness = new float2(1);
                    o++;
                }
            }
            // triangle indices 
            o = 0;
            
            for (int aOuter = 0; aOuter < outerN - 1; aOuter++) {
                for (int aInner = 0; aInner < innerN - 1; aInner++) {
                    int iBase = aInner + aOuter * innerN;
                    indices[o++] = (ushort)(iBase + innerN + 1);
                    indices[o++] = (ushort)(iBase + 1);
                    indices[o++] = (ushort)iBase;
                    indices[o++] = (ushort)(iBase + innerN);
                    indices[o++] = (ushort)(iBase + innerN + 1);
                    indices[o++] = (ushort)iBase;
                }
            }
        }

        static public BlobAssetReference<LitMeshData> CreateDonutMesh(float innerR, int innerN, float outerR, int outerN)
        {
            Assert.IsTrue(innerN * outerN <= ushort.MaxValue);
            Assert.IsTrue(outerR >= innerR * 2.0f);
            using (var builder = new BlobBuilder(Allocator.Temp)) {
                ref var root = ref builder.ConstructRoot<LitMeshData>();
                // in x/y plane 
                var vertices = builder.Allocate(ref root.Vertices, innerN * outerN);
                var indices = builder.Allocate(ref root.Indices, (innerN - 1) * (outerN - 1) * 6);
                MakeDonut(ref vertices, ref indices, innerR, innerN, outerR, outerN);

                ComputeTangentAndBinormal(ref vertices, ref indices);

                // bounds 
                float3 ext = new float3(outerR + innerR, outerR + innerR, innerR * 2);
                root.Bounds.Center = new float3(0.0f);
                root.Bounds.Extents = ext;

                return builder.CreateBlobAssetReference<LitMeshData>(Allocator.Persistent);
            }
        }

        static public BlobAssetReference<LitMeshData> CreateBoxMesh(float3 size)
        {
            using (var builder = new BlobBuilder(Allocator.Temp)) {
                ref var root = ref builder.ConstructRoot<LitMeshData>();
                var vertices = builder.Allocate(ref root.Vertices, 24);

                var indices = builder.Allocate(ref root.Indices, 36);

                int destI = 0, destV = 0;
                AddBoxFace(ref vertices, ref indices, 0, -1, size, ref destI, ref destV, 1.0f);
                AddBoxFace(ref vertices, ref indices, 0,  1, size, ref destI, ref destV, 1.0f);
                AddBoxFace(ref vertices, ref indices, 1, -1, size, ref destI, ref destV, 1.0f);
                AddBoxFace(ref vertices, ref indices, 1,  1, size, ref destI, ref destV, 1.0f);
                AddBoxFace(ref vertices, ref indices, 2, -1, size, ref destI, ref destV, 1.0f);
                AddBoxFace(ref vertices, ref indices, 2,  1, size, ref destI, ref destV, 1.0f);

                ComputeNormals(ref vertices, ref indices);

                ComputeTangentAndBinormal(ref vertices, ref indices);

                for(int i =0; i < 24; i++)
                {
                    vertices[i].Albedo_Opacity = new float4(1);
                    vertices[i].Metal_Smoothness = new float2(1);
                }

                root.Bounds.Center = -new float3(0.0f);
                root.Bounds.Extents = size;

                return builder.CreateBlobAssetReference<LitMeshData>(Allocator.Persistent);
            }
        }

        static public BlobAssetReference<SimpleMeshData> CreatePlane(float3 org, float3 du, float3 dv)
        {
            using (var builder = new BlobBuilder(Allocator.Temp)) {
                ref var root = ref builder.ConstructRoot<SimpleMeshData>();
                var vertices = builder.Allocate(ref root.Vertices, 4);

                var indices = builder.Allocate(ref root.Indices, 6);

                vertices[0] = new SimpleVertex {  Position = org, Color = new float4(1,1,1,1), TexCoord0 = new float2(0,0) };
                vertices[1] = new SimpleVertex {  Position = org + du, Color = new float4(1,1,1,1), TexCoord0 = new float2(1,0) };
                vertices[2] = new SimpleVertex {  Position = org + du + dv, Color = new float4(1,1,1,1), TexCoord0 = new float2(1,1) };
                vertices[3] = new SimpleVertex {  Position = org + dv, Color = new float4(1,1,1,1), TexCoord0 = new float2(0,1) };

                indices[0] = 0; indices[1] = 1; indices[2] = 2;
                indices[3] = 2; indices[4] = 3; indices[5] = 0;

                float3 bbmin = vertices[0].Position;
                float3 bbmax = bbmin;
                for ( int i=1; i<vertices.Length; i++ ) {
                    bbmax = math.max(bbmax, vertices[i].Position);
                    bbmin = math.min(bbmin, vertices[i].Position);
                }
                root.Bounds.Center = (bbmax + bbmin) * .5f;
                root.Bounds.Extents = (bbmax - bbmin) * .5f;
                return builder.CreateBlobAssetReference<SimpleMeshData>(Allocator.Persistent);
            }
        }
    }
}