using System;
using System.Runtime.InteropServices;
#if !NET_DOTS
using System.Text.RegularExpressions;
#endif
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.PlayerLoop
{
    public struct Initialization {}

    public struct Update
    {
        public struct ScriptRunBehaviourUpdate
        {
        }

        public struct ScriptRunDelayedDynamicFrameRate
        {
        }
    }
}

namespace UnityEngine.Experimental.LowLevel
{
    public struct PlayerLoopSystem
    {
        public Type type;
        public PlayerLoopSystem[] subSystemList;
        public UpdateFunction updateDelegate;

        /*
        public IntPtr updateFunction;
        public IntPtr loopConditionFunction;*/
        public delegate void UpdateFunction();
    }

    public static class PlayerLoop
    {
        private static readonly PlayerLoopSystem _default = new PlayerLoopSystem()
        {
            type = typeof(int), subSystemList = new PlayerLoopSystem[1]
            {
                new PlayerLoopSystem()
                {
                    subSystemList = Array.Empty<PlayerLoopSystem>(),
                    type = null,
                    updateDelegate = Nothing
                }
            }, updateDelegate = Tick
        };

        private static void Nothing()
        {
        }

        private static PlayerLoopSystem _current;

        public static PlayerLoopSystem GetDefaultPlayerLoop() => _default;

        public static void Tick()
        {
            ProcessSystem(_current);
        }

        private static void ProcessSystem(PlayerLoopSystem playerLoopSystem)
        {
            playerLoopSystem.updateDelegate?.Invoke();

            foreach (var subSystem in playerLoopSystem.subSystemList ?? Array.Empty<PlayerLoopSystem>())
                ProcessSystem(subSystem);
        }

        public static void SetPlayerLoop(PlayerLoopSystem loop) => _current = loop;
    }
}
