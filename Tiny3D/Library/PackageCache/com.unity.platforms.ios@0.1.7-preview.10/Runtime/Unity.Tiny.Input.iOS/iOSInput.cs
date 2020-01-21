using Unity.Tiny.Input;

namespace Unity.Tiny.iOS
{
    public class iOSInputSystem : InputSystem
    {
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate(); // advances input state one frame
            unsafe
            {
                // touch
                int touchInfoStreamLen = 0;
                int* touchInfoStream = iOSNativeCalls.getTouchInfoStream(ref touchInfoStreamLen);
                for (int i = 0; i < touchInfoStreamLen; i += 4)
                {
                    if (touchInfoStream[i + 1] == 0) //ACTION_DOWN
                        m_inputState.TouchEvent(touchInfoStream[i], TouchState.Began, touchInfoStream[i + 2], touchInfoStream[i + 3]);
                    else if (touchInfoStream[i + 1] == 1) //ACTION_UP
                        m_inputState.TouchEvent(touchInfoStream[i], TouchState.Ended, touchInfoStream[i + 2], touchInfoStream[i + 3]);
                    else if (touchInfoStream[i + 1] == 2) //ACTION_MOVE
                        m_inputState.TouchEvent(touchInfoStream[i], TouchState.Moved, touchInfoStream[i + 2], touchInfoStream[i + 3]);
                    else if (touchInfoStream[i + 1] == 3) //ACTION_CANCEL
                        m_inputState.TouchEvent(touchInfoStream[i], TouchState.Canceled, touchInfoStream[i + 2], touchInfoStream[i + 3]);
                }

                if (touchInfoStreamLen != 0)
                    m_inputState.hasTouch = true;
            }

            iOSNativeCalls.resetStreams();
        }

    }
}
