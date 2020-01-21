using Unity.Entities;
using Unity.Tiny;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Tiny.Authoring
{
    public class TinyDisplayInfo : MonoBehaviour, IConvertGameObjectToEntity
    {
        public Vector2Int Resolution;
        public bool AutoSizeToFrame = true;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var di = default(DisplayInfo);
            di.width = Resolution.x;
            di.height = Resolution.y;
            di.autoSizeToFrame = AutoSizeToFrame;
            di.visible = true;
            di.disableSRGB = PlayerSettings.colorSpace == ColorSpace.Gamma;
            dstManager.AddComponentData(entity, di);
        }
    }
}
