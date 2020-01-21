using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Tiny.Input;
using UnityInput = UnityEngine.Input;

namespace Unity.Tiny.Hybrid
{
    internal class HybridInputBehaviour : MonoBehaviour
    {
        internal NativeList<HybridInputSystem.KeyEvent> m_KeyEventList;

        public void OnGUI()
        {
            if (!m_KeyEventList.IsCreated)
                return;

            var type = Event.current.type;
            if (type == EventType.KeyDown || type == EventType.KeyUp)
            {
                var keycode = Event.current.keyCode;
                m_KeyEventList.Add(new HybridInputSystem.KeyEvent() {
                    KeyCode = (Unity.Tiny.Input.KeyCode) keycode,
                    Down = type == EventType.KeyDown
                });
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class HybridInputSystem : InputSystem
    {
        private GameObject m_GuiGrabberGO;
        private HybridInputBehaviour m_GuiGrabber;

        internal struct KeyEvent
        {
            public Unity.Tiny.Input.KeyCode KeyCode;
            public bool Down;
        }

        private NativeList<KeyEvent> m_KeyEventList;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_KeyEventList = new NativeList<KeyEvent>(32, Allocator.Persistent);
            m_GuiGrabberGO = new GameObject("GUIEventGrabber");
            m_GuiGrabber = m_GuiGrabberGO.AddComponent<HybridInputBehaviour>();
            m_GuiGrabber.m_KeyEventList = m_KeyEventList;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_GuiGrabber.m_KeyEventList = default;
            UnityEngine.Object.Destroy(m_GuiGrabberGO);
            m_GuiGrabberGO = null;
            m_GuiGrabber = null;
            m_KeyEventList.Dispose();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // keyboard
            for (int i = 0; i < m_KeyEventList.Length; ++i)
            {
                var key = m_KeyEventList[i];
                if (key.Down)
                    m_inputState.KeyDown(key.KeyCode);
                else
                    m_inputState.KeyUp(key.KeyCode);
            }

            m_KeyEventList.Clear();

            // mouse
            m_inputState.hasMouse = UnityInput.mousePresent;
            var mouse = UnityInput.mousePosition;
            m_inputState.mouseX = (int) mouse.x;
            m_inputState.mouseY = (int) mouse.y;
            for (int i = 0; i < 3; ++i)
            {
                if (UnityInput.GetMouseButtonUp(i))
                    m_inputState.MouseUp(i);
                if (UnityInput.GetMouseButtonDown(i))
                    m_inputState.MouseDown(i);
            }
        }
    }
}
