using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Tiny.Rendering
{
    /// <summary>
    /// Mesh renderer component containing a reference to a material to render with and submesh information
    /// </summary>
    public struct MeshRenderer : IComponentData
    {
        public Entity material;
        public int startIndex;
        public int indexCount;
    }

    /// <summary>
    /// Component containing a reference to an unlit mesh to add next to a MeshRenderer Component
    /// </summary>
    public struct SimpleMeshReference : IComponentData
    {
        public Entity mesh; // Entity reference to a unlit mesh data
    }

    /// <summary>
    /// Component containing a reference to a lit mesh to add next to a MeshRenderer Component
    /// </summary>
    public struct LitMeshReference : IComponentData
    {
        public Entity mesh; // Entity reference to a lit mesh data
    }

    public struct BlitRenderer : IComponentData
    {
        public Entity texture;
        public float4 color;
        public bool preserveAspect;
        public bool useExternalBlitES3;
        // source/dest rects? 
    }
}
