using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using Unity.Tiny;
using Unity.Tiny.Assertions;

namespace Unity.Tiny.Rendering
{
    public enum ProjectionMode
    {
        Perspective, Orthographic
    }

    public struct RenderOrder : IComponentData
    {
        public short Layer;
        public short Order;
    }

    /// <summary>
    ///  List of options for clearing a camera's viewport before rendering.
    ///  Used by the Camera2D component.
    /// </summary>
    public enum CameraClearFlags {

        /// <summary>
        ///  Do not clear. Use this when the camera renders to the entire screen,
        ///  and in situations where multiple cameras render to the same screen area.
        /// </summary>
        Nothing,

        /// <summary>
        ///  Clears the viewport with a solid background color.
        /// </summary>
        SolidColor
    }

    // LocalToWorld
    public struct Camera : IComponentData
    {
        public Color backgroundColor;
        public CameraClearFlags clearFlags;
        public Rect viewportRect;
        public float clipZNear;
        public float clipZFar;
        public float fov;   // in degrees for perspective, direct scale factor in orthographic 
        public float aspect;
        public ProjectionMode mode;
        public float depth; 
    }

    // tag camera to auto update aspect to primary display 
    public struct CameraAutoAspectFromDisplay : IComponentData
    {
    }

    public struct CameraMatrices : IComponentData
    {
        public float4x4 projection;
        public float4x4 view;
    }

    public struct Frustum : IComponentData
    {
        private float4 p0;
        private float4 p1;
        private float4 p2;
        private float4 p3;
        private float4 p4;
        private float4 p5;
        public int PlanesCount;

        public float4 GetPlane(int idx)
        {
            Assert.IsTrue(idx < PlanesCount && idx >= 0);
            switch (idx)
            {
                case 0:
                    return p0;
                case 1:
                    return p1;
                case 2:
                    return p2;
                case 3:
                    return p3;
                case 4:
                    return p4;
                case 5:
                    return p5;
            }
            return new float4(0);
        }

        public void SetPlane(int idx, float4 p)
        {
            Assert.IsTrue(idx < PlanesCount && idx >= 0);
            switch (idx)
            {
                case 0:
                    p0 = p;
                    break;
                case 1:
                    p1 = p;
                    break;
                case 2:
                    p2 = p;
                    break;
                case 3:
                    p3 = p;
                    break;
                case 4:
                    p4 = p;
                    break;
                case 5:
                    p5 = p;
                    break;
            }
        }
    }

    public static class ProjectionHelper 
    {
        public static float4x4 ProjectionMatrixPerspective(float n, float f, float fovDeg, float aspect)
        {
            var fov = math.radians(fovDeg);
            var t = n * math.tan(fov *.5f);
            var b = -t;
            var l = -t * aspect;
            var r = t * aspect;
            // homogeneous ndc [-1..1] z range, right handed
            return new float4x4(
                (2 * n) / (r - l), 0,                  -(r + l) / (r - l),   0,
                0,                (2 * n) / (t - b),   -(t + b) / (t - b),   0,
                0,                 0,                  (f + n) / (f - n),    (-2 * f * n) / (f - n),
                0,                 0,                  1,                    0
            );
        }

        public static float4x4 ProjectionMatrixOrtho(float n, float f, float size, float aspect)
        {
            var t = size;
            var b = -t;
            var r = t * aspect;
            var l = -r;
            return new float4x4(
                2f / (r - l),   0,            0,            -(r + l) / (r - l),
                0,              2f / (t - b), 0,            -(t + b) / (t - b),
                0,              0,            2f / (f - n), -(f + n) / (f - n),
                0,              0,            0,            1
            );
        }

        public static float4 NormalizePlane(float4 p)
        {
            float l = math.length(p.xyz);
            return p * (1.0f/l); 
        }

        // compute world space frustum
        public static void FrustumFromMatrices(float4x4 projection, float4x4 view, ref Frustum dest)
        {
            // assumes opengl style projection (TODO, check with orthographic!)
            float4x4 vp = math.transpose(math.mul(projection, view));
            dest.PlanesCount = 6;
            dest.SetPlane(0, NormalizePlane(vp.c3 + vp.c0));
            dest.SetPlane(1, NormalizePlane(vp.c3 - vp.c0));
            dest.SetPlane(2, NormalizePlane(vp.c3 + vp.c1));
            dest.SetPlane(3, NormalizePlane(vp.c3 - vp.c1));
            dest.SetPlane(4, NormalizePlane(vp.c3 + vp.c2));
            dest.SetPlane(5, NormalizePlane(vp.c3 - vp.c2));
        }

        public static void FrustumFromCube(float3 pos, float size, ref Frustum dest)
        {
            // TODO
            dest.PlanesCount = 6;
        }
    }

    [UnityEngine.ExecuteAlways]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class UpdateCameraMatricesSystem : ComponentSystem
    {
        public static float4x4 ProjectionMatrixFromCamera(ref Camera camera) 
        {
            if (camera.mode == ProjectionMode.Orthographic)
                return ProjectionHelper.ProjectionMatrixOrtho(camera.clipZNear, camera.clipZFar, camera.fov, camera.aspect);
            else
                return ProjectionHelper.ProjectionMatrixPerspective(camera.clipZNear, camera.clipZFar, camera.fov, camera.aspect);
        }

        static void FrustumFromCamera(ref CameraMatrices cm, ref Frustum dest)
        {
            ProjectionHelper.FrustumFromMatrices(cm.projection, cm.view, ref dest);
        }

        protected override void OnUpdate() 
        {
            Entities.WithAll<CameraAutoAspectFromDisplay>().ForEach((ref Camera c) =>
            {                
                TinyEnvironment env = World.TinyEnvironment();
                DisplayInfo di = env.GetConfigData<DisplayInfo>();
                c.aspect = (float)di.width / (float)di.height;
            });

            // add camera matrices if needed 
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities.WithNone<CameraMatrices>().WithAll<Camera>().ForEach((Entity e) =>
            {
                ecb.AddComponent<CameraMatrices>(e);
            });
            ecb.Playback(EntityManager);
            ecb.Dispose();

            // update 
            Entities.ForEach((ref Camera c, ref LocalToWorld tx, ref CameraMatrices cm, ref Frustum f) =>
            { // with frustum
                cm.projection = ProjectionMatrixFromCamera(ref c);
                cm.view = math.inverse(tx.Value);
                FrustumFromCamera(ref cm, ref f);
            });
            Entities.WithNone<Frustum>().ForEach((ref Camera c, ref LocalToWorld tx, ref CameraMatrices cm) =>
            { // no frustum
                cm.projection = ProjectionMatrixFromCamera(ref c);
                cm.view = math.inverse(tx.Value);
            });
        }
    }
}
