using JetBrains.Annotations;
using Unity.BuildSystem.NativeProgramSupport;

[UsedImplicitly]
class CustomizerForTinyGLFW : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Tiny.GLFW";

    // not exactly right, but good enough for now
    public override string[] ImplementationFor => new[] {"Unity.Tiny.Rendering"};

    public override void CustomizeSelf(AsmDefCSharpProgram program)
    {
        if (program.MainSourcePath.FileName == "Unity.Tiny.GLFW")
        {
            External.GLFWStaticLibrary = External.SetupGLFW();
            program.NativeProgram.Libraries.Add(new NativeProgramAsLibrary(External.GLFWStaticLibrary){BuildMode = NativeProgramLibraryBuildMode.BagOfObjects});
        }
    }
}
