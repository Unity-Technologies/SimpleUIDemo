using System;
using Unity.Platforms;

namespace Unity.Tiny.EntryPoint
{
    public static class Program
    {
        private static void Main()
        {
            var unity = UnityInstance.Initialize();

            unity.OnTick = () =>
            {
                var shouldContinue = unity.Update();
                if (shouldContinue == false)
                {
                    unity.Deinitialize();
                }
                return shouldContinue;
            };

            RunLoop.EnterMainLoop(unity.OnTick);
        }
    }
}
 