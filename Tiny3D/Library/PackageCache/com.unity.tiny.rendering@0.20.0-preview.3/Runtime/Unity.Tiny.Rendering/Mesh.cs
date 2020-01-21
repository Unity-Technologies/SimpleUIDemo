using Unity.Tiny;
using Unity.Tiny.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Tiny.Rendering.Native")]
namespace Unity.Tiny.Rendering
{
    /// <summary>
    /// Simple Vertex data used with a Simple Shader
    /// </summary>
    public struct SimpleVertex
    {
        public float3 Position;
        public float2 TexCoord0;
        public float4 Color;
    }

    /// <summary>
    /// Single vertex data used with a Lit Shader
    /// </summary>
    public struct LitVertex
    {
        public float3 Position;
        public float2 TexCoord0;
        public float3 Normal;
        public float3 Tangent;
        public float3 BiTangent;
        public float4 Albedo_Opacity;
        public float2 Metal_Smoothness;
    }

    /// <summary>
    /// Mesh structure (used for 3D cases)
    /// </summary>
    public struct LitMeshData
    {
        public AABB Bounds;
        public BlobArray<ushort> Indices;
        public BlobArray<LitVertex> Vertices;
    }

    /// <summary>
    /// Simple mesh data. (Use with Simple shader and 2D cases)
    /// </summary>
    public struct SimpleMeshData
    {
        public AABB Bounds;
        public BlobArray<ushort> Indices;
        public BlobArray<SimpleVertex> Vertices;
    }

    /// <summary>
    /// Blob asset component to add next to a mesh entity containg all mesh data to work with a lit shader. 
    /// </summary>
    public struct LitMeshRenderData : IComponentData
    {
        public BlobAssetReference<LitMeshData> Mesh;
    }

    /// <summary>
    /// Blob asset component to add next to a mesh entity containing only vertex positions, colors and texture coordinates to work with a simple shader.
    /// </summary>
    public struct SimpleMeshRenderData : IComponentData
    {
        public BlobAssetReference<SimpleMeshData> Mesh;
    }

}
