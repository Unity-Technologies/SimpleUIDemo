using System;
using Unity.Entities;
using Unity.Mathematics;
//#if !UNITY_DOTSPLAYER
//using UnityEngine;
//#endif
namespace Tiny3D
{
    [GenerateAuthoringComponent]
    public struct UIPose : IComponentData
    {
        public float2 position;
        public float2 scale;
//#if !UNITY_DOTSPLAYER
//        [HideInInspector]
//#endif
//        public int4 uiclick;
    }
}