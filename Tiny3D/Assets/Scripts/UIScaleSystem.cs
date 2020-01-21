using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
//using UnityEngine;
using Unity.Jobs;
#if UNITY_DOTSPLAYER
using Unity.Tiny.Rendering;
using Unity.Tiny.Input;
#endif
namespace Tiny3D
{
    /// <summary>
    ///     scale ui with screen resolution
    /// </summary>
    //public class mult
    //{
    //    public static float4 mulmatwithvec(float4x4 mat,float4 vec)
    //    {
    //        float4 c1, c2, c3, c4;
    //        c1 = mat.c0 * vec.x;
    //        c2 = mat.c1 * vec.y;
    //        c3 = mat.c2 * vec.z;
    //        c4 = mat.c3 * vec.w;
    //        return c1 + c2 + c3 + c4;
    //    }
    //}
    public class UIScaleSystem : JobComponentSystem
    {
        //public float4x4 view;
        //public float4x4 projection;
        //public float4x4 VP;

        //        protected override void OnStartRunning()
        //        {
        //#if !UNITY_DOTSPLAYER
        //            view = UnityEngine.Camera.main.worldToCameraMatrix;
        //            projection = UnityEngine.Camera.main.projectionMatrix;
        //            VP = math.mul(projection, view);
        //#endif

        //        }
        int count = 0;
        EntityQuery m_Query;
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            count++;
            
            float4x4 projection = 0;
            float4x4 view = 0;
#if UNITY_DOTSPLAYER

            var cme=GetSingletonEntity<Camera>();
            
            if(count>=5)
            {
                projection=EntityManager.GetComponentData<CameraMatrices>(cme).projection;
                view =EntityManager.GetComponentData<CameraMatrices>(cme).view;
            }

#else
            if (count >= 5)
            {
                view = UnityEngine.Camera.main.worldToCameraMatrix;
                projection = UnityEngine.Camera.main.projectionMatrix;
                //UnityEngine.Debug.Log(count);
            }

#endif
            float4x4 VP = math.mul(projection, view);
            var deltaTime = Time.DeltaTime;
            float4x4 intermat = VP;
            int count2 = count;
            return Entities.ForEach((ref Translation translation,ref NonUniformScale nonuniscale, in UIPose uipose) =>
            {
                if (count2 >= 5) {

                    var ratio = new float2(uipose.position.x, uipose.position.y);
                    float4 vv = new float4(translation.Value.x, translation.Value.y, translation.Value.z, 1);
                    float4 pointInNdc = math.mul(intermat, vv);
                    pointInNdc.x = (ratio.x * 2 - 1f) * pointInNdc.w;
                    pointInNdc.y = (ratio.y * 2 - 1f) * pointInNdc.w;
                    float4 nw = math.mul(math.inverse(intermat), pointInNdc);
                    translation.Value = nw.xyz / nw.w;
                }

                nonuniscale.Value.x = uipose.scale.x;
                nonuniscale.Value.y = uipose.scale.y;

            }).Schedule(inputDeps);



        }
    }
}