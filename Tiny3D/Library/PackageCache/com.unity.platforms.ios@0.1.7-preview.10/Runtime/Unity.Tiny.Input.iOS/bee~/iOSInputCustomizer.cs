using JetBrains.Annotations;

[UsedImplicitly]
class CustomizerForInputiOS : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Tiny.InputiOS";

    public override string[] ImplementationFor => new [] { "Unity.Tiny.Input" };
}
