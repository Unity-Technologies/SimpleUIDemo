using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Tiny;
using Unity.Tiny.Rendering;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.Assertions;

namespace Unity.TinyConversion
{
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    class MeshRendererDeclareAssets : GameObjectConversionSystem
    {
        protected override void OnUpdate() =>
            Entities.ForEach((UnityEngine.MeshRenderer uMeshRenderer) =>
            {
                foreach (Material mat in uMeshRenderer.sharedMaterials)
                    DeclareReferencedAsset(mat);

                MeshFilter uMeshFilter = uMeshRenderer.gameObject.GetComponent<MeshFilter>();
                if (uMeshFilter == null)
                    UnityEngine.Debug.LogWarning("Missing MeshFilter component on gameobject " + uMeshRenderer.gameObject);

                if (uMeshFilter.sharedMesh == null)
                    UnityEngine.Debug.LogWarning("Missing mesh in MeshFilter on gameobject: " + uMeshRenderer.gameObject.name);

                DeclareReferencedAsset(uMeshFilter.sharedMesh);
            });
    }
    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [UpdateBefore(typeof(MeshConversion))]
    [UpdateAfter(typeof(MeshRendererConversion))]
    public class AddMeshRenderDataSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.MeshRenderer uMeshRenderer) =>
            {
                MeshFilter uMeshfilter = uMeshRenderer.gameObject.GetComponent<MeshFilter>();
                UnityEngine.Mesh uMesh = uMeshfilter.sharedMesh;
                var meshEntity = GetPrimaryEntity(uMesh);

                var mrEntity = GetPrimaryEntity(uMeshRenderer);
                var matEntity = DstEntityManager.GetComponentData<Unity.Tiny.Rendering.MeshRenderer>(mrEntity).material;

                //Add empty MeshRenderData component that will be filled by the mesh conversion.
                if(DstEntityManager.HasComponent<SimpleMaterial>(matEntity))
                    DstEntityManager.AddComponent<SimpleMeshRenderData>(meshEntity); 
                else if(DstEntityManager.HasComponent<LitMaterial>(matEntity))
                    DstEntityManager.AddComponent<LitMeshRenderData>(meshEntity);
                
            });
        }
    }

    [UpdateInGroup(typeof(GameObjectConversionGroup))]
    [UpdateAfter(typeof(MaterialConversion))]
    public class MeshRendererConversion : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.MeshRenderer uMeshRenderer) =>
            {
                var primaryEntity = GetPrimaryEntity(uMeshRenderer);
                MeshFilter uMeshfilter = uMeshRenderer.gameObject.GetComponent<MeshFilter>();
                UnityEngine.Mesh mesh = uMeshfilter.sharedMesh;

                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    //Use the primary entity on the first submesh
                    if (i == 0)
                    {
                        ConvertSubmesh(uMeshRenderer, uMeshfilter, primaryEntity, 0);
                    }
                    else
                    {
                        Entity newSubMeshEntity = CreateAdditionalEntity(uMeshRenderer);
                        ConvertSubmesh(uMeshRenderer, uMeshfilter, newSubMeshEntity, i);
                        AddTransformComponent(primaryEntity, newSubMeshEntity);
                    }  
                }
            });
        }

        private void ConvertSubmesh(UnityEngine.MeshRenderer uMeshRenderer, MeshFilter uMeshFilter, Entity entity, int subMeshIndex)
        {
            UnityEngine.Mesh mesh = uMeshFilter.sharedMesh;

            //Get all entities on the shared mesh and just use the one with same type as the material
            var meshEntity = GetPrimaryEntity(mesh);

            //In Big-U, If there are more materials than sub-meshes, the last submesh will be rendered with each of the remaining materials.
            List<Material> materials = new List<Material>();
            uMeshRenderer.GetSharedMaterials(materials);

            //If there are less materials than submeshes, just use the last material on the remaining meshrenderers
            var entityMaterial = GetPrimaryEntity(materials[materials.Count - 1]);
            if (subMeshIndex < materials.Count)
                entityMaterial = GetPrimaryEntity(materials[subMeshIndex]);

            DstEntityManager.AddComponentData(entity, new Unity.Tiny.Rendering.MeshRenderer()
            {
                material = entityMaterial,
                startIndex = Convert.ToUInt16(mesh.GetIndexStart(subMeshIndex)),
                indexCount = Convert.ToUInt16(mesh.GetIndexCount(subMeshIndex))
            });
            DstEntityManager.AddComponentData(entity, new WorldBounds());
            if (DstEntityManager.HasComponent<LitMaterial>(entityMaterial))
            {
                DstEntityManager.AddComponentData(entity, new LitMeshReference() { mesh = meshEntity });
            }
            else if (DstEntityManager.HasComponent<SimpleMaterial>(entityMaterial))
            {
                DstEntityManager.AddComponentData(entity, new SimpleMeshReference() { mesh = meshEntity });
            }
        }

        private void AddTransformComponent(Entity uMeshRenderer, Entity subMeshRendererEntity)
        {
            DstEntityManager.AddComponentData<Parent>(subMeshRendererEntity, new Parent()
            {
                Value = uMeshRenderer
            });

            DstEntityManager.AddComponentData<LocalToWorld>(subMeshRendererEntity, new LocalToWorld());
            DstEntityManager.AddComponentData<LocalToParent>(subMeshRendererEntity, new LocalToParent() {
                Value = float4x4.identity
            });
        }
    }
}
