using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Tiny.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Light = UnityEngine.Light;

namespace Unity.TinyConversion
{
    [UpdateInGroup(typeof(GameObjectBeforeConversionGroup))]
    public class LightConversion : GameObjectConversionSystem
    {
        private void CheckLightLimitations()
        {
            int numberOfLights = 0;
            int numberOfShadowMappedLights = 0;
            Entities.ForEach((Light uLight) =>
            {
                numberOfLights++;
                if (uLight.shadows != LightShadows.None)
                    numberOfShadowMappedLights++;
            });

            //TODO: Also consider what will be in quality settings when we will support them
            if (numberOfLights > LightingBGFX.maxPointOrDirLights)
                throw new ArgumentException($"Only a maximum of a total of {LightingBGFX.maxPointOrDirLights} directional or point lights is supported. Please reduce the number of directional and/or spot lights in your scene.");
            if (numberOfShadowMappedLights > LightingBGFX.maxMappedLights)
                throw new ArgumentException($"Only a maximum of {LightingBGFX.maxMappedLights} shadow mapped lights is supported. Please reduce the number of shadow mapped lights in your scene.");

        }

        protected override void OnUpdate()
        {
            CheckLightLimitations();

            Entities.ForEach((Light uLight) =>
            {
                Entity eLighting = GetPrimaryEntity(uLight);
                DstEntityManager.AddComponentData(eLighting, new Unity.Tiny.Rendering.Light()
                {
                    intensity = uLight.intensity, //TODO: Need to fix this
                    color = new float3(uLight.color.r, uLight.color.g, uLight.color.b),
                    clipZFar = uLight.range,
                    clipZNear = 0.0f
                });
                if (uLight.type == LightType.Directional)
                {
                    DstEntityManager.AddComponentData(eLighting, new Unity.Tiny.Rendering.DirectionalLight());
                }
                else if(uLight.type == LightType.Spot)
                {
                    if (uLight.shadows == LightShadows.None)
                        Debug.LogWarning("Spot lights with no shadows are not supported. Set a shadow type in light: " + uLight.name);
                    else
                    {
                        DstEntityManager.AddComponentData(eLighting, new Unity.Tiny.Rendering.SpotLight()
                        {
                            fov = uLight.spotAngle,
                            innerRadius = 0.0f,
                            ratio = 1.0f
                        });
                    }
                }
                if(uLight.shadows != LightShadows.None)
                {
                    int shadowMapResolution = 1024;
                    if (uLight.type == LightType.Directional)
                    {
                        shadowMapResolution = 2048;

                        DstEntityManager.AddComponentData(eLighting, new AutoMovingDirectionalLight()
                        {
                            autoBounds = true
                        });
                    }
                    DstEntityManager.AddComponentData(eLighting, new Unity.Tiny.Rendering.ShadowmappedLight
                    {
                        shadowMapResolution = shadowMapResolution, //TODO: Shadow resolutions in Big-U are set in the Quality Settings (or URP settings) globally. (API: Light.LightShadowResolution.Low/Medium/High/VeryHigh)
                        shadowMap = Entity.Null, // auto created
                        shadowMapRenderNode = Entity.Null // auto created
                    });
                    var light = DstEntityManager.GetComponentData<Unity.Tiny.Rendering.Light>(eLighting);
                    light.clipZNear = uLight.shadowNearPlane;
                    DstEntityManager.SetComponentData(eLighting, light);
                }
            });
        }
    }
}
