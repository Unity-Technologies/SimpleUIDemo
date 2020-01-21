using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Tiny;
using Unity.Tiny.Rendering;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.TinyConversion
{
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [UpdateAfter(typeof(AddMeshRenderDataSystem))]
    public class MeshConversion : GameObjectConversionSystem
    {
        struct UMeshData
        {
            public Vector3[] positions;
            public Vector2[] uvs;
            public Vector3[] normals; //normals are assigned to vertices, not triangles.
            public Vector3[] tangents;
            public Vector3[] biTangents;
            public UnityEngine.Color[] colors;
            public int subMeshCount;
        }

        private UMeshData GetMeshData(Mesh uMesh)
        {
            Vector4[] tang4 = uMesh.tangents;//uMesh.tangents is vector4 with x,y,z components, and w used to flip the binormal.
            Vector3[] tang3 = new Vector3[tang4.Length];
            Vector3[] nor = uMesh.normals;
            Vector3[] biTang = new Vector3[tang4.Length];

            if (tang3.Length != nor.Length)
                UnityEngine.Debug.LogWarning($"The mesh {uMesh.name} should have the same number of normals {nor.Length} and tangents {tang3.Length}");

            for (int i = 0; i < Math.Min(tang3.Length, nor.Length); i++)
            {
                tang3[i] = tang4[i];
                nor[i].Normalize();
                tang3[i].Normalize();

                // Orthogonalize
                tang3[i] = tang3[i] - nor[i] * Vector3.Dot(nor[i], tang3[i]);
                tang3[i].Normalize();

                // Fix T orientation
                if (Vector3.Dot(Vector3.Cross(nor[i], tang3[i]), biTang[i]) < 0.0f)
                {
                    tang3[i] = tang3[i] * -1.0f;
                }

                biTang[i] = Vector3.Cross(nor[i], tang3[i]) * tang4[i].w; // tang.w should be 1 or -1
            }

            //Invert uvs
            Vector2[] uvs = uMesh.uv;
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i].y = 1 - uvs[i].y;
            }
            return new UMeshData() { positions = uMesh.vertices, uvs = uvs, normals = nor, tangents = tang3, biTangents = biTang, colors = uMesh.colors, subMeshCount = uMesh.subMeshCount };
        }

        private unsafe void CreateLitMeshData(Entity entity, Mesh uMesh)
        {
            using (var allocator = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref allocator.ConstructRoot<LitMeshData>();
                root.Bounds.Center = uMesh.bounds.center;
                root.Bounds.Extents = uMesh.bounds.extents;

                UMeshData data = GetMeshData(uMesh);

                int nVertices = data.positions.Length;
                    
                var vertices = allocator.Allocate(ref root.Vertices, nVertices);

                int offset = 0;
                byte* dest = (byte*)vertices.GetUnsafePtr();

                if (data.positions.Length != 0)
                {
                    fixed (void* uPositions = data.positions)
                    {
                        UnsafeUtility.MemCpyStride(dest + offset, sizeof(LitVertex), uPositions, sizeof(float3), sizeof(float3), nVertices);
                    }
                }
                offset += sizeof(float3);
                if (data.uvs.Length != 0)
                {
                    fixed (void* uUvs = data.uvs)
                    {
                        UnsafeUtility.MemCpyStride(dest + offset, sizeof(LitVertex), uUvs, sizeof(float2), sizeof(float2), nVertices);
                    }
                }
                offset += sizeof(float2);
                if (data.normals.Length != 0)
                {
                    fixed (void* uNormals = data.normals)
                    {
                        UnsafeUtility.MemCpyStride(dest + offset, sizeof(LitVertex), uNormals, sizeof(float3), sizeof(float3), nVertices);
                    }
                }
                offset += sizeof(float3);
                if (data.tangents.Length != 0)
                {
                    fixed (void* uTangents = data.tangents)
                    {
                        UnsafeUtility.MemCpyStride(dest + offset, sizeof(LitVertex), uTangents, sizeof(float3), sizeof(float3), nVertices);
                    }
                }
                offset += sizeof(float3);
                if (data.biTangents.Length != 0)
                {
                    fixed (void* uBitangents = data.biTangents)
                    {
                        UnsafeUtility.MemCpyStride(dest + offset, sizeof(LitVertex), uBitangents, sizeof(float3), sizeof(float3), nVertices);
                    }
                }
                offset += sizeof(float3);

                //Vertex color is not supported in URP lit shader, override to white for now
                float4 albedo = new float4(1);
                UnsafeUtility.MemCpyStride(dest + offset, sizeof(LitVertex), &albedo, 0, sizeof(float4), nVertices);
                offset += sizeof(float4);

                //Vertex metal smoothness are not present in UnityEngine.Mesh
                float2 metal = new float2(1);
                UnsafeUtility.MemCpyStride(dest + offset, sizeof(LitVertex), &metal, 0, sizeof(float2), nVertices);

                int indexCount = 0;
                for (int i = 0; i < data.subMeshCount; i++)
                {
                    indexCount += (int)uMesh.GetIndexCount(i);
                }

                var indices = allocator.Allocate(ref root.Indices, indexCount);
                offset = 0;
                for (int i = 0; i < data.subMeshCount; i++)
                {
                    int[] uIndices = uMesh.GetIndices(i);
                    for (int j = 0; j < uIndices.Length; j++)
                    {
                        indices[offset + j] = Convert.ToUInt16(uIndices[j]);
                    }
                    offset += uIndices.Length;
                }

                var mesh = allocator.CreateBlobAssetReference<LitMeshData>(Allocator.Persistent);
                DstEntityManager.AddComponentData(entity, new LitMeshRenderData()
                {
                    Mesh = mesh
                });
            }
        }

        private unsafe void CreateSimpleMeshData(Entity entity, Mesh uMesh)
        {
            using (var allocator = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref allocator.ConstructRoot<SimpleMeshData>();
                root.Bounds.Center = uMesh.bounds.center;
                root.Bounds.Extents = uMesh.bounds.extents;

                UMeshData data = GetMeshData(uMesh);

                int nVertices = data.positions.Length;

                var vertices = allocator.Allocate(ref root.Vertices, nVertices);

                int offset = 0;
                byte* dest = (byte*)vertices.GetUnsafePtr();

                if (data.positions.Length != 0)
                {
                    fixed (void* uPositions = data.positions)
                    {
                        UnsafeUtility.MemCpyStride(dest + offset, sizeof(SimpleVertex), uPositions, sizeof(float3), sizeof(float3), nVertices);
                    }
                }
                offset += sizeof(float3);
                if (data.uvs.Length != 0)
                {
                    fixed (void* uUvs = data.uvs)
                    {
                        UnsafeUtility.MemCpyStride(dest + offset, sizeof(SimpleVertex), uUvs, sizeof(float2), sizeof(float2), nVertices);
                    }
                }
                offset += sizeof(float2);


                //Vertex color is not supported in URP lit shader, override to white for now
                float4 albedo = new float4(1);
                UnsafeUtility.MemCpyStride(dest + offset, sizeof(SimpleVertex), &albedo, 0, sizeof(float4), nVertices);

                int indexCount = 0;
                for (int i = 0; i < data.subMeshCount; i++)
                {
                    indexCount += (int)uMesh.GetIndexCount(i);
                }

                var indices = allocator.Allocate(ref root.Indices, indexCount);
                offset = 0;
                for (int i = 0; i < data.subMeshCount; i++)
                {
                    int[] uIndices = uMesh.GetIndices(i);
                    for (int j = 0; j < uIndices.Length; j++)
                    {
                        indices[offset + j] = Convert.ToUInt16(uIndices[j]);
                    }
                    offset += uIndices.Length;
                }

                var mesh = allocator.CreateBlobAssetReference<SimpleMeshData>(Allocator.Persistent);
                DstEntityManager.AddComponentData(entity, new SimpleMeshRenderData()
                {
                    Mesh = mesh
                });
            }
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.Mesh uMesh) =>
            {
                var entity = GetPrimaryEntity(uMesh);
                if(DstEntityManager.HasComponent<LitMeshRenderData>(entity))
                    CreateLitMeshData(entity, uMesh);
                if (DstEntityManager.HasComponent<SimpleMeshRenderData>(entity))
                    CreateSimpleMeshData(entity, uMesh);
            });
        }
    }
}
