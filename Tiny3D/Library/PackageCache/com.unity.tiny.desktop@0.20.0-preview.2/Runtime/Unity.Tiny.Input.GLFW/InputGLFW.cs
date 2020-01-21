using Unity.Tiny;
using Unity.Tiny.Input;
using Unity.Entities;

namespace Unity.Tiny.GLFW
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(GLFWWindowSystem))]
    public class GLFWInputSystem : InputSystem
    {
        private bool initialized = false;

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            if (initialized)
                return;

            // must init after window
            initialized = GLFWNativeCalls.init_input();
            GLFWNativeCalls.resetStreams();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (!initialized)
                return;

            base.OnUpdate(); // advances input state one frame
            unsafe
            {
                // key, scancode, action, mods
                int keyStreamLen = 0;
                int* keyStream = GLFWNativeCalls.getKeyStream(ref keyStreamLen);
                for (int i = 0; i < keyStreamLen; i += 4)
                {
                    int key = keyStream[i];
                    int scancode = keyStream[i + 1];
                    int action = keyStream[i + 2];
                    int mods = keyStream[i + 3];
                    KeyCode translatedKey = TranslateKey(key, scancode, mods);
                    if (translatedKey == KeyCode.None)
                        continue;
                    if (action == GLFW_RELEASE)
                        m_inputState.KeyUp(translatedKey);
                    else if (action == GLFW_PRESS)
                        m_inputState.KeyDown(translatedKey);
                }

                // button, action, mods
                int mouseButtonStreamLen = 0;
                int* mouseButtonStream = GLFWNativeCalls.getMouseButtonStream(ref mouseButtonStreamLen);
                for (int i = 0; i < mouseButtonStreamLen; i += 3)
                {
                    int button = mouseButtonStream[i];
                    int action = mouseButtonStream[i + 1];
                    int mods = mouseButtonStream[i + 2];
                    if (action == GLFW_RELEASE)
                        m_inputState.MouseUp(button);
                    else if (action == GLFW_PRESS)
                        m_inputState.MouseDown(button);
                }

                // position
                int mousePosStreamLen = 0;
                int* mousePosStream = GLFWNativeCalls.getMousePosStream(ref mousePosStreamLen);
                for (int i = 0; i < mousePosStreamLen; i += 2)
                {
                    m_inputState.mouseX = mousePosStream[i];
                    m_inputState.mouseY = mousePosStream[i + 1];
                }

                if (mouseButtonStreamLen != 0 || mousePosStreamLen != 0)
                    m_inputState.hasMouse = true;
            }

            GLFWNativeCalls.resetStreams();
        }

        // GLFW key map
        private const int GLFW_KEY_UNKNOWN = -1;
        private const int GLFW_KEY_SPACE = 32;
        private const int GLFW_KEY_APOSTROPHE = 39; /* ' */
        private const int GLFW_KEY_COMMA = 44; /* , */
        private const int GLFW_KEY_MINUS = 45; /* - */
        private const int GLFW_KEY_PERIOD = 46; /* . */
        private const int GLFW_KEY_SLASH = 47; /* / */
        private const int GLFW_KEY_0 = 48;
        private const int GLFW_KEY_1 = 49;
        private const int GLFW_KEY_2 = 50;
        private const int GLFW_KEY_3 = 51;
        private const int GLFW_KEY_4 = 52;
        private const int GLFW_KEY_5 = 53;
        private const int GLFW_KEY_6 = 54;
        private const int GLFW_KEY_7 = 55;
        private const int GLFW_KEY_8 = 56;
        private const int GLFW_KEY_9 = 57;
        private const int GLFW_KEY_SEMICOLON = 59; /* ; */
        private const int GLFW_KEY_EQUAL = 61; /* = */
        private const int GLFW_KEY_A = 65;
        private const int GLFW_KEY_B = 66;
        private const int GLFW_KEY_C = 67;
        private const int GLFW_KEY_D = 68;
        private const int GLFW_KEY_E = 69;
        private const int GLFW_KEY_F = 70;
        private const int GLFW_KEY_G = 71;
        private const int GLFW_KEY_H = 72;
        private const int GLFW_KEY_I = 73;
        private const int GLFW_KEY_J = 74;
        private const int GLFW_KEY_K = 75;
        private const int GLFW_KEY_L = 76;
        private const int GLFW_KEY_M = 77;
        private const int GLFW_KEY_N = 78;
        private const int GLFW_KEY_O = 79;
        private const int GLFW_KEY_P = 80;
        private const int GLFW_KEY_Q = 81;
        private const int GLFW_KEY_R = 82;
        private const int GLFW_KEY_S = 83;
        private const int GLFW_KEY_T = 84;
        private const int GLFW_KEY_U = 85;
        private const int GLFW_KEY_V = 86;
        private const int GLFW_KEY_W = 87;
        private const int GLFW_KEY_X = 88;
        private const int GLFW_KEY_Y = 89;
        private const int GLFW_KEY_Z = 90;
        private const int GLFW_KEY_LEFT_BRACKET = 91; /* [ */
        private const int GLFW_KEY_BACKSLASH = 92; /* \ */
        private const int GLFW_KEY_RIGHT_BRACKET = 93; /* ] */
        private const int GLFW_KEY_GRAVE_ACCENT = 96; /* ` */
        private const int GLFW_KEY_WORLD_1 = 161; /* non-US #1 */
        private const int GLFW_KEY_WORLD_2 = 162; /* non-US #2 */
        private const int GLFW_KEY_ESCAPE = 256;
        private const int GLFW_KEY_ENTER = 257;
        private const int GLFW_KEY_TAB = 258;
        private const int GLFW_KEY_BACKSPACE = 259;
        private const int GLFW_KEY_INSERT = 260;
        private const int GLFW_KEY_DELETE = 261;
        private const int GLFW_KEY_RIGHT = 262;
        private const int GLFW_KEY_LEFT = 263;
        private const int GLFW_KEY_DOWN = 264;
        private const int GLFW_KEY_UP = 265;
        private const int GLFW_KEY_PAGE_UP = 266;
        private const int GLFW_KEY_PAGE_DOWN = 267;
        private const int GLFW_KEY_HOME = 268;
        private const int GLFW_KEY_END = 269;
        private const int GLFW_KEY_CAPS_LOCK = 280;
        private const int GLFW_KEY_SCROLL_LOCK = 281;
        private const int GLFW_KEY_NUM_LOCK = 282;
        private const int GLFW_KEY_PRINT_SCREEN = 283;
        private const int GLFW_KEY_PAUSE = 284;
        private const int GLFW_KEY_F1 = 290;
        private const int GLFW_KEY_F2 = 291;
        private const int GLFW_KEY_F3 = 292;
        private const int GLFW_KEY_F4 = 293;
        private const int GLFW_KEY_F5 = 294;
        private const int GLFW_KEY_F6 = 295;
        private const int GLFW_KEY_F7 = 296;
        private const int GLFW_KEY_F8 = 297;
        private const int GLFW_KEY_F9 = 298;
        private const int GLFW_KEY_F10 = 299;
        private const int GLFW_KEY_F11 = 300;
        private const int GLFW_KEY_F12 = 301;
        private const int GLFW_KEY_F13 = 302;
        private const int GLFW_KEY_F14 = 303;
        private const int GLFW_KEY_F15 = 304;
        private const int GLFW_KEY_F16 = 305;
        private const int GLFW_KEY_F17 = 306;
        private const int GLFW_KEY_F18 = 307;
        private const int GLFW_KEY_F19 = 308;
        private const int GLFW_KEY_F20 = 309;
        private const int GLFW_KEY_F21 = 310;
        private const int GLFW_KEY_F22 = 311;
        private const int GLFW_KEY_F23 = 312;
        private const int GLFW_KEY_F24 = 313;
        private const int GLFW_KEY_F25 = 314;
        private const int GLFW_KEY_KP_0 = 320;
        private const int GLFW_KEY_KP_1 = 321;
        private const int GLFW_KEY_KP_2 = 322;
        private const int GLFW_KEY_KP_3 = 323;
        private const int GLFW_KEY_KP_4 = 324;
        private const int GLFW_KEY_KP_5 = 325;
        private const int GLFW_KEY_KP_6 = 326;
        private const int GLFW_KEY_KP_7 = 327;
        private const int GLFW_KEY_KP_8 = 328;
        private const int GLFW_KEY_KP_9 = 329;
        private const int GLFW_KEY_KP_DECIMAL = 330;
        private const int GLFW_KEY_KP_DIVIDE = 331;
        private const int GLFW_KEY_KP_MULTIPLY = 332;
        private const int GLFW_KEY_KP_SUBTRACT = 333;
        private const int GLFW_KEY_KP_ADD = 334;
        private const int GLFW_KEY_KP_ENTER = 335;
        private const int GLFW_KEY_KP_EQUAL = 336;
        private const int GLFW_KEY_LEFT_SHIFT = 340;
        private const int GLFW_KEY_LEFT_CONTROL = 341;
        private const int GLFW_KEY_LEFT_ALT = 342;
        private const int GLFW_KEY_LEFT_SUPER = 343;
        private const int GLFW_KEY_RIGHT_SHIFT = 344;
        private const int GLFW_KEY_RIGHT_CONTROL = 345;
        private const int GLFW_KEY_RIGHT_ALT = 346;
        private const int GLFW_KEY_RIGHT_SUPER = 347;
        private const int GLFW_KEY_MENU = 348;
        private const int GLFW_KEY_LAST = GLFW_KEY_MENU;

        // mods
        private const int GLFW_MOD_SHIFT = 0x0001;
        private const int GLFW_MOD_CONTROL = 0x0002;
        private const int GLFW_MOD_ALT = 0x0004;
        private const int GLFW_MOD_SUPER = 0x0008;
        private const int GLFW_MOD_CAPS_LOCK = 0x0010;
        private const int GLFW_MOD_NUM_LOCK = 0x0020;

        // actions
        private const int GLFW_RELEASE = 0;
        private const int GLFW_PRESS = 1;

        private KeyCode TranslateKey(int key, int scancode, int mods)
        {
            switch (key)
            {
                // TODO, add more, this is just the ones we already had in tiny
                case GLFW_KEY_A: return KeyCode.A;
                case GLFW_KEY_B: return KeyCode.B;
                case GLFW_KEY_C: return KeyCode.C;
                case GLFW_KEY_D: return KeyCode.D;
                case GLFW_KEY_E: return KeyCode.E;
                case GLFW_KEY_F: return KeyCode.F;
                case GLFW_KEY_G: return KeyCode.G;
                case GLFW_KEY_H: return KeyCode.H;
                case GLFW_KEY_I: return KeyCode.I;
                case GLFW_KEY_J: return KeyCode.J;
                case GLFW_KEY_K: return KeyCode.K;
                case GLFW_KEY_L: return KeyCode.L;
                case GLFW_KEY_M: return KeyCode.M;
                case GLFW_KEY_N: return KeyCode.N;
                case GLFW_KEY_O: return KeyCode.O;
                case GLFW_KEY_P: return KeyCode.P;
                case GLFW_KEY_Q: return KeyCode.Q;
                case GLFW_KEY_R: return KeyCode.R;
                case GLFW_KEY_S: return KeyCode.S;
                case GLFW_KEY_T: return KeyCode.T;
                case GLFW_KEY_U: return KeyCode.U;
                case GLFW_KEY_V: return KeyCode.V;
                case GLFW_KEY_W: return KeyCode.W;
                case GLFW_KEY_X: return KeyCode.X;
                case GLFW_KEY_Y: return KeyCode.Y;
                case GLFW_KEY_Z: return KeyCode.Z;

                case GLFW_KEY_F1: return KeyCode.F1;
                case GLFW_KEY_F2: return KeyCode.F2;
                case GLFW_KEY_F3: return KeyCode.F3;
                case GLFW_KEY_F4: return KeyCode.F4;
                case GLFW_KEY_F5: return KeyCode.F5;
                case GLFW_KEY_F6: return KeyCode.F6;
                case GLFW_KEY_F7: return KeyCode.F7;
                case GLFW_KEY_F8: return KeyCode.F8;
                case GLFW_KEY_F9: return KeyCode.F9;
                case GLFW_KEY_F10: return KeyCode.F10;
                case GLFW_KEY_F11: return KeyCode.F11;
                case GLFW_KEY_F12: return KeyCode.F12;

                case GLFW_KEY_0: return KeyCode.Alpha0;
                case GLFW_KEY_1: return KeyCode.Alpha1;
                case GLFW_KEY_2: return KeyCode.Alpha2;
                case GLFW_KEY_3: return KeyCode.Alpha3;
                case GLFW_KEY_4: return KeyCode.Alpha4;
                case GLFW_KEY_5: return KeyCode.Alpha5;
                case GLFW_KEY_6: return KeyCode.Alpha6;
                case GLFW_KEY_7: return KeyCode.Alpha7;
                case GLFW_KEY_8: return KeyCode.Alpha8;
                case GLFW_KEY_9: return KeyCode.Alpha9;

                case GLFW_KEY_UP: return KeyCode.UpArrow;
                case GLFW_KEY_DOWN: return KeyCode.DownArrow;
                case GLFW_KEY_LEFT: return KeyCode.LeftArrow;
                case GLFW_KEY_RIGHT: return KeyCode.RightArrow;

                case GLFW_KEY_SPACE: return KeyCode.Space;
                case GLFW_KEY_BACKSPACE: return KeyCode.Backspace;
                case GLFW_KEY_ENTER: return KeyCode.Return;
                case GLFW_KEY_ESCAPE: return KeyCode.Escape;
                case GLFW_KEY_TAB: return KeyCode.Tab;

                case GLFW_KEY_LEFT_SHIFT: return KeyCode.LeftShift;
                case GLFW_KEY_RIGHT_SHIFT: return KeyCode.RightShift;
                case GLFW_KEY_LEFT_ALT: return KeyCode.LeftAlt;
                case GLFW_KEY_RIGHT_ALT: return KeyCode.RightAlt;
                case GLFW_KEY_LEFT_CONTROL: return KeyCode.LeftControl;
                case GLFW_KEY_RIGHT_CONTROL: return KeyCode.RightControl;
            }

            return KeyCode.None; // unknown key
        }
    }
}
