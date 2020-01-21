using Bee.Toolchain.Xcode;
using JetBrains.Annotations;
using Unity.BuildSystem.NativeProgramSupport;
using static Unity.BuildSystem.NativeProgramSupport.NativeProgramConfiguration;


[UsedImplicitly]
class CustomizerForTinyImage2DNative : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Tiny.Image2D.Native";

    public override string[] ImplementationFor => new [] { "Unity.Tiny.Image2D" };

    public override void CustomizeSelf(AsmDefCSharpProgram program)
    {
        program.NativeProgram.Libraries.Add(IsWindows, new SystemLibrary("opengl32.lib"));
        program.NativeProgram.Libraries.Add(c => c.Platform is MacOSXPlatform, new SystemFramework("OpenGL"));
        program.NativeProgram.Libraries.Add(IsLinux, new SystemLibrary("GL"));
    }
}
