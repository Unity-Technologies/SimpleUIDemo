using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Tiny;

namespace Unity.Tiny.Rendering
{
    public struct LightMatrices : IComponentData
    {
        public float4x4 projection;
        public float4x4 view;
        public float4x4 mvp;
    }

    public struct Light : IComponentData
    {
        // always points in z direction 
        public float clipZNear; // near clip, applies only for mapped lights 
        public float clipZFar;
        
        public float intensity;
        public float3 color;
        // if no other components are not to a light, it's a simple non-shadowed omni light
    }

    public struct ShadowmappedLight : IComponentData // next to light
    {
        public int shadowMapResolution;     // for auto creation 
        public Entity shadowMap;            // the shadow map texture
        public Entity shadowMapRenderNode;  // node used for shadow map creation 
    }

    public struct SpotLight : IComponentData // next to light
    {
        // always points in z direction
        public float fov; // in degrees 
        public float innerRadius; // 0..1, start of circle falloff 1=sharp circle, 0=smooth
        public float ratio; // ]0..1] 1=circle, 0=line
    }

    public struct DirectionalLight : IComponentData // next to light
    {
        // always points in z direction
        public float size; // in world units, for mapped light 
    }

    // this component automatically updates a directional lights position & size 
    // so the shadow map covers the intersection of the bounds of interest and the cameras frustum
    // because it changes the size and position of the directional light it is not suitable for projection textures in the light
    public struct AutoMovingDirectionalLight : IComponentData // next to mapped directional light 
    {
        public AABB boundsCasters;      // caster bounds of the world to track
        public AABB boundsReceivers;    // receiver bounds of the world to track
        public bool autoBounds;         // automatically get bounds from world bounds of renderers 
    }
    
    public struct LightToBGFXLightingSetup : IBufferElementData
    {
        public Entity e; // the light should add itself to this lighting setup
    }

    /// <summary>
    /// Ambient light. To add next to entity with a Light component on it.
    /// The ambient light color and intensity must be set in the Light Component.
    /// </summary>
    public struct AmbientLight : IComponentData { }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(UpdateLightMatricesSystem))]
    public class UpdateAutoMovingLightSystem : ComponentSystem
    {
        private AABB RotateBounds (ref float4x4 tx, ref AABB b)
        {
            WorldBounds wBounds;
            Culling.AxisAlignedToWorldBounds(ref tx, ref b, out wBounds);
            // now turn those bounds back to axis aligned.. 
            AABB aab;
            Culling.WorldBoundsToAxisAligned(ref wBounds, out aab);
            return aab;
        }

        protected override void OnUpdate() 
        {
            // check if we need bounds 
            bool needBounds = false;
            Entities.ForEach((Entity e, ref AutoMovingDirectionalLight amdl) =>
            {
                if (amdl.autoBounds)
                    needBounds = true;
            });

            // compute bounds if needed
            // TODO: separate bounds for shadow casters and receivers?
            AABB autoBounds = default;
            if ( needBounds ) {
                // TODO: use chunk bounds instead!
                float3 bbMin = new float3(float.MaxValue); 
                float3 bbMax = new float3(-float.MaxValue);
                Entities.ForEach((Entity e, ref WorldBounds wb) =>
                {
                    Culling.GrowBounds(ref bbMin, ref bbMax, wb);
                });
                autoBounds = new AABB {
                    Center = (bbMin + bbMax) * .5f, 
                    Extents = (bbMax - bbMin) * .5f
                };
                if ( math.any(autoBounds.Extents < 0.0f) ) {
                    autoBounds.Center = new float3(0);
                    autoBounds.Extents = new float3(0);
                }
            }

            // do assignment
            Entities.ForEach((Entity e, ref AutoMovingDirectionalLight amdl, ref DirectionalLight dl, ref Light l, ref LocalToWorld tx) => 
            {
                if (amdl.autoBounds)
                {
                    amdl.boundsCasters = autoBounds;
                    amdl.boundsReceivers = autoBounds;
                }

                // transform bounds into light space rotation
                float4x4 rotOnlyTx = tx.Value;
                rotOnlyTx.c3 = new float4(0,0,0,1);
                float4x4 rotOnlyTxInv = math.inverse(rotOnlyTx);

                AABB aabCasters = RotateBounds(ref rotOnlyTx, ref amdl.boundsCasters);
                AABB aabReceivers = RotateBounds(ref rotOnlyTx, ref amdl.boundsReceivers);

                l.clipZFar = l.clipZNear + aabReceivers.Extents.z + aabCasters.Extents.z; // do not change near clip 
                float3 posls;
                posls.x = aabCasters.Center.x;
                posls.y = aabCasters.Center.y;
                posls.z = aabCasters.Center.z - aabCasters.Extents.z - l.clipZNear;
                tx.Value.c3.xyz = math.transform(rotOnlyTx, posls); // back to world space 
                dl.size = math.max(aabCasters.Extents.x, aabCasters.Extents.y);
            });
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class UpdateLightMatricesSystem : ComponentSystem
    {
        protected override void OnUpdate() 
        {
            // add matrices component if needed 
            Entities.WithNone<LightMatrices>().WithAll<Light>().ForEach((Entity e) =>
            {
                EntityManager.AddComponent<LightMatrices>(e);
            });

            // add frustum component if needed 
            Entities.WithNone<Frustum>().WithAll<Light>().ForEach((Entity e) =>
            {
                EntityManager.AddComponent<Frustum>(e);
            }); 
            
            // update 
            Entities.ForEach((ref Light c, ref LocalToWorld tx, ref LightMatrices m, ref SpotLight sl, ref Frustum f) =>
            { // spot light
                m.projection = ProjectionHelper.ProjectionMatrixPerspective(c.clipZNear, c.clipZFar, sl.fov, 1.0f);
                m.view = math.inverse(tx.Value);
                m.mvp = math.mul(m.projection, m.view);
                ProjectionHelper.FrustumFromMatrices(m.projection, m.view, ref f);
            });
            Entities.ForEach((ref Light c, ref LocalToWorld tx, ref LightMatrices m, ref DirectionalLight dr, ref Frustum f) =>
            { // directional
                m.projection = ProjectionHelper.ProjectionMatrixOrtho(c.clipZNear, c.clipZFar, dr.size, 1.0f);
                m.view = math.inverse(tx.Value);
                m.mvp = math.mul(m.projection, m.view);
                ProjectionHelper.FrustumFromMatrices(m.projection, m.view, ref f);
            });
            Entities.WithNone<DirectionalLight, SpotLight>().ForEach((ref Light c, ref LocalToWorld tx, ref LightMatrices m, ref Frustum f) =>
            { // point
                m.projection = float4x4.identity;
                m.view = math.inverse(tx.Value);
                m.mvp = math.mul(m.projection, m.view);
                // build furstum from bounds 
                ProjectionHelper.FrustumFromCube(tx.Value.c3.xyz, c.clipZFar, ref f);
            });
        }
    }
}
