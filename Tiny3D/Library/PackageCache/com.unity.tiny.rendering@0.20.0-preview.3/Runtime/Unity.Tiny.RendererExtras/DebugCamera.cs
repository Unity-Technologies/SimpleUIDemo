using System;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Tiny;
using Unity.Tiny.Utils;
using Unity.Tiny.Rendering;
using Unity.Tiny.Input;
using Bgfx;
using System.Runtime.InteropServices;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;

namespace Unity.Tiny.Rendering
{
    // attach next to an entity with rotation  
    public struct DemoSpinner : IComponentData
    {
        public quaternion spin;
    };

    public class DemoSpinnerSystem : ComponentSystem
    {
        protected bool m_paused;
        protected override void OnUpdate()
        {
            var input = World.GetExistingSystem<InputSystem>();
            if (input.GetKeyDown(KeyCode.Space))
                m_paused = !m_paused;

            float dt = (float)Time.DeltaTime;
            if (!m_paused)
            {
                // rotate stuff
                Entities.ForEach((ref DemoSpinner s, ref Rotation r) =>
                {
                    quaternion sp = s.spin;
                    sp.value.xyz *= dt;
                    r.Value = math.normalize(math.mul(r.Value, sp));
                });
            }
        }
    }

    // attach next to a Camera 
    public struct CameraKeyControl : IComponentData
    {
        public float movespeed;
        public float mousemovespeed;
        public float mouserotspeed;
        public float fovspeed;

        public void Default()
        {
            movespeed = 10.0f;       // in worldunits/second
            mousemovespeed = 50.0f;  // in worldunits/screen
            mouserotspeed = 120.0f;   // in degrees/screen
            fovspeed = 40.0f;        // in degrees/second
        }
    };

    // attach next to a Light
    public struct LightFromCameraByKey : IComponentData
    {
        public KeyCode key;
    }

    public class KeyControlsSystem : ComponentSystem
    {
        protected float2 m_prevMouse;
        protected int m_nshots;
        public bool m_configAlwaysRun;

#if !UNITY_EDITOR
        protected void ControlCamera(Entity ecam)
        {
            // camera controls
            var input = World.GetExistingSystem<InputSystem>();
            float dt = (float)Time.DeltaTime;
            var env = World.TinyEnvironment();
            var di = env.GetConfigData<DisplayInfo>();
            float2 inpos = input.GetInputPosition();
            float2 deltaMouse = (inpos - m_prevMouse) / new float2(di.width, di.height);
            m_prevMouse = inpos;

            bool mb0 = input.GetMouseButton(0);
            bool mb1 = input.GetMouseButton(1);
            bool mb2 = input.GetMouseButton(2);

            if ( input.IsTouchSupported() )
            {
                if (input.TouchCount() > 0)
                {
                    Touch t0 = input.GetTouch(0);
                    deltaMouse.x = t0.deltaX / (float)di.width;
                    deltaMouse.y = t0.deltaY / (float)di.height;
                }
                if (input.TouchCount() == 1)
                    mb0 = true;
                else if (input.TouchCount() == 2)
                    mb1 = true;
            }

            var tPos = EntityManager.GetComponentData<Translation>(ecam);
            var tRot = EntityManager.GetComponentData<Rotation>(ecam);
            var cam = EntityManager.GetComponentData<Camera>(ecam);
            CameraKeyControl cc = default;
            if (EntityManager.HasComponent<CameraKeyControl>(ecam))
                cc = EntityManager.GetComponentData<CameraKeyControl>(ecam);
            else
                cc.Default();

            float3x3 rMat = new float3x3(tRot.Value);

            if (input.GetKey(KeyCode.UpArrow) || input.GetKey(KeyCode.W))
                tPos.Value += rMat.c2 * cc.movespeed * dt;
            if (input.GetKey(KeyCode.DownArrow) || input.GetKey(KeyCode.S))
                tPos.Value -= rMat.c2 * cc.movespeed * dt;
            if (input.GetKey(KeyCode.LeftArrow) || input.GetKey(KeyCode.A))
                tPos.Value -= rMat.c0 * cc.movespeed * dt;
            if (input.GetKey(KeyCode.RightArrow) || input.GetKey(KeyCode.D))
                tPos.Value += rMat.c0 * cc.movespeed * dt;
            if (input.GetKey(KeyCode.PageUp) || input.GetKey(KeyCode.R))
                cam.fov += cc.fovspeed * dt;
            if (input.GetKey(KeyCode.PageDown) || input.GetKey(KeyCode.F))
                cam.fov -= cc.fovspeed * dt;

            if (input.GetKey(KeyCode.Return))
            {
                tPos.Value = new float3(0, 0, -20.0f);
                tRot.Value = quaternion.identity;
                cam.fov = 60;
            }
            cam.fov = math.clamp(cam.fov, 0.1f, 179.0f);

            if (mb0) {
                var dyAxis = quaternion.EulerXYZ(new float3(0, deltaMouse.x * math.radians(cc.mouserotspeed), 0));
                var dxAxis = quaternion.EulerXYZ(new float3(-deltaMouse.y * math.radians(cc.mouserotspeed), 0, 0));
                tRot.Value = math.mul(tRot.Value, dyAxis);
                tRot.Value = math.mul(tRot.Value, dxAxis);
            }
            if (mb1) {
                tPos.Value += rMat.c0 * -deltaMouse.x * cc.mousemovespeed;
                tPos.Value += rMat.c1 * deltaMouse.y * cc.mousemovespeed;
            }
            if (input.GetMouseButton(2)) {
                tPos.Value += rMat.c2 * deltaMouse.y * cc.mousemovespeed;
            }

            // write back
            EntityManager.SetComponentData<Translation>(ecam, tPos);
            EntityManager.SetComponentData<Rotation>(ecam, tRot);
            EntityManager.SetComponentData<Camera>(ecam, cam);
        }

        protected Entity FindCamera() {
            var ecam = Entity.Null;
            float bestdepth = 0;
            Entities.WithAll<Translation, Rotation, CameraKeyControl>().ForEach((Entity e, ref Camera cam) =>
            {
                if (ecam == Entity.Null || cam.depth > bestdepth) {
                    bestdepth = cam.depth;
                    ecam = e;
                }
            });
            if ( ecam==Entity.Null ) {
                Entities.WithAll<Translation, Rotation, Camera>().ForEach((Entity e, ref Camera cam) =>
                {
                    if (ecam == Entity.Null || cam.depth > bestdepth) {
                        bestdepth = cam.depth;
                        ecam = e;
                    }
                });
            }
            return ecam;
        }
#endif

        protected override void OnUpdate()
        {
#if !UNITY_EDITOR
            var env = World.TinyEnvironment();
            var input = World.GetExistingSystem<InputSystem>();
            var renderer = World.GetExistingSystem<RendererBGFXSystem>();

            if (!m_configAlwaysRun && !input.GetKey(KeyCode.LeftShift))
                return;

            // debug bgfx stuff
            if (input.GetKey(KeyCode.F2))
                renderer.SetFlagThisFrame(bgfx.DebugFlags.Stats);

            renderer.OutputDebugSelect = new float4(0, 0, 0, 0);
            if (input.GetKey(KeyCode.Alpha1))
                renderer.OutputDebugSelect = new float4(1, 0, 0, 0);
            if (input.GetKey(KeyCode.Alpha2))
                renderer.OutputDebugSelect = new float4(0, 1, 0, 0);
            if (input.GetKey(KeyCode.Alpha3))
                renderer.OutputDebugSelect = new float4(0, 0, 1, 0);
            if (input.GetKey(KeyCode.Alpha4))
                renderer.OutputDebugSelect = new float4(0, 0, 0, 1);
            if (input.GetKeyDown(KeyCode.Z))
            {
                string fn = StringFormatter.Format("screenshot{0}.tga", m_nshots++);
                renderer.RequestScreenShot(fn);
            }
            if (input.GetKeyDown(KeyCode.Escape))
            {
                Debug.LogFormatAlways("Reloading all textures.");
                // free all textures - this releases the bgfx textures and invalidates all cached bgfx state that might contain texture handles 
                // note that this does not free system textures like single pixel white, default spotlight etc. 
                renderer.DestroyAllTextures();
                // now force a reload on all image2d's from files
                renderer.ReloadAllImages();
                // once images are loaded, but don't have a texture, the texture will be uploaded and the cpu memory freed
            }
            if (renderer.HasScreenShot())
            {
                // TODO: save out 32bpp pixel data:
                Debug.LogFormat("Write screen shot to disk: {0}, {1}*{2}",
                    renderer.ScreenShotPath, renderer.ScreenShotWidth, renderer.ScreenShotHeight);
                renderer.ResetScreenShot();
            }

            // camera related stuff
            var ecam = FindCamera();
            if (ecam == Entity.Null)
                return;

            ControlCamera(ecam);

            Entities.WithAll<Light>().ForEach((Entity eLight, ref LightFromCameraByKey lk, ref Translation tPos, ref Rotation tRot) =>
            {
                if (input.GetKeyDown(lk.key))
                {
                    tPos = EntityManager.GetComponentData<Translation>(ecam);
                    tRot = EntityManager.GetComponentData<Rotation>(ecam);
                    Debug.LogFormat("Set light {0} to {1} {2}", eLight, tPos.Value, tRot.Value.value);
                }
            });
#endif
        }
    }
}
