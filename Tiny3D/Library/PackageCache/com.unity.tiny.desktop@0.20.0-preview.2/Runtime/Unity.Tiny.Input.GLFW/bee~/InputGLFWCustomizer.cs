using JetBrains.Annotations;

[UsedImplicitly]
class CustomizerForInputGLFW : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Tiny.Input.GLFW";

    public override string[] ImplementationFor => new [] { "Unity.Tiny.Input" };
}
