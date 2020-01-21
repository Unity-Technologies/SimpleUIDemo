#if EXPORT_TINY_SHADER
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Entities.Runtime.Build
{
    static class RenderSettingsConversion
    {
        public static void ConvertRenderSettings(EntityManager em)
        {
            //Get the ambient light color from the current active scene
            Entity e = em.CreateEntity();
            em.AddComponentData<Unity.Tiny.Rendering.Light>(e, new Unity.Tiny.Rendering.Light()
            {
                color = new float3(RenderSettings.ambientLight.r, RenderSettings.ambientLight.g, RenderSettings.ambientLight.b),
                intensity = 1.0f
            });
            em.AddComponent<Unity.Tiny.Rendering.AmbientLight>(e);
        }
    }
}
#endif
