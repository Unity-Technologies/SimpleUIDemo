using JetBrains.Annotations;
using Unity.BuildSystem.NativeProgramSupport;

[UsedImplicitly]
class CustomizerForRunLoopHTML : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Platforms.RunLoop";

    public override void CustomizeSelf(AsmDefCSharpProgram program)
    {
        program.NativeProgram.Libraries.Add(c => c.Platform is WebGLPlatform && !Il2Cpp.ManagedDebuggingIsEnabled(c), Il2Cpp.LibIL2Cpp);
        program.NativeProgram.Libraries.Add(c => c.Platform is WebGLPlatform && Il2Cpp.ManagedDebuggingIsEnabled(c), Il2Cpp.BigLibIL2Cpp);
    }
}
