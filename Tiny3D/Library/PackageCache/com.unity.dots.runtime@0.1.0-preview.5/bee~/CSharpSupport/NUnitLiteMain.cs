using NUnitLite;
using Unity.Jobs.LowLevel.Unsafe;

public static class Program {
    public static int Main(string[] args)
    {
        var result = new AutoRun().Execute(args);
#if !UNITY_SINGLETHREADED_JOBS
        // Currently, Windows (.NET) will exit without requiring other threads to complete
        // OSX (Mono), on the other hand, requires all other threads to complete
        JobsUtility.Shutdown();
#endif        
        return result;
    }
}
