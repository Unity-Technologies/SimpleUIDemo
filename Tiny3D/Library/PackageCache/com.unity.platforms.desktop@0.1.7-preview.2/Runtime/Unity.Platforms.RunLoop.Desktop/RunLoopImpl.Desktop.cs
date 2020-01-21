#if ((UNITY_WINDOWS || UNITY_MACOSX || UNITY_LINUX || UNITY_EDITOR))
namespace Unity.Platforms
{
    public class RunLoopImpl
    {
        public static void EnterMainLoop(RunLoop.RunLoopDelegate runLoopDelegate)
        {
            while (true)
            {
                if (runLoopDelegate() == false)
                    break;
            }
        }
    }
}
#endif