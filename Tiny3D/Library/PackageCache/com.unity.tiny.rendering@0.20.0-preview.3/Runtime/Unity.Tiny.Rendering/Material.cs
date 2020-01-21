using Unity.Entities;
using Unity.Mathematics;
using System;

namespace Unity.Tiny.Rendering
{
    public struct SimpleMaterial : IComponentData, IEquatable<SimpleMaterial>
    {
        public Entity texAlbedo;
        public Entity texOpacity;

        public float3 constAlbedo;
        public float constOpacity;

        public bool twoSided;
        public BlendOp blend;
        public bool transparent;

        public float2 scale;
        public float2 offset;

        public bool Equals(SimpleMaterial other)
        {
            return texAlbedo.Equals(other.texAlbedo) &&
                   texOpacity.Equals(other.texOpacity) &&
                   constAlbedo.Equals(other.constAlbedo) &&
                   constOpacity.Equals(other.constOpacity) &&
                   (twoSided == other.twoSided) &&
                   (blend == other.blend) &&
                   (transparent == other.transparent) &&
                   scale.Equals(other.scale) &&
                   offset.Equals(other.offset);
        }
    }

    public struct LitMaterial : IComponentData, IEquatable<LitMaterial>
    {
        public Entity texAlbedo;
        public Entity texMetal;
        public Entity texNormal;
        public Entity texOpacity;
        public Entity texEmissive;
        public Entity texSmoothness;

        public float3 constAlbedo;
        public float3 constEmissive;
        public float constOpacity;
        public float constMetal;
        public float constSmoothness;
        public float normalMapZScale;

        public bool twoSided;
        public bool transparent;
        public bool triangleSortTransparent;

        public float2 scale;
        public float2 offset;

        public bool Equals(LitMaterial other)
        {
            return texAlbedo.Equals(other.texAlbedo) &&
                   texMetal.Equals(other.texMetal) &&
                   texNormal.Equals(other.texNormal) &&
                   texEmissive.Equals(other.texEmissive) &&
                   texSmoothness.Equals(other.texSmoothness) &&
                   texOpacity.Equals(other.texOpacity) &&
                   constAlbedo.Equals(other.constAlbedo) &&
                   constEmissive.Equals(other.constEmissive) &&
                   constOpacity.Equals(other.constOpacity) &&
                   constMetal.Equals(other.constMetal) &&
                   constSmoothness.Equals(other.constSmoothness) &&
                   normalMapZScale.Equals(other.normalMapZScale) &&
                   (twoSided == other.twoSided) &&
                   (transparent == other.transparent) &&
                   (triangleSortTransparent == other.triangleSortTransparent) &&
                   scale.Equals(other.scale) &&
                   offset.Equals(other.offset);
        }
    }

    public struct DynamicMaterial : IComponentData{ } // can change material later
}
