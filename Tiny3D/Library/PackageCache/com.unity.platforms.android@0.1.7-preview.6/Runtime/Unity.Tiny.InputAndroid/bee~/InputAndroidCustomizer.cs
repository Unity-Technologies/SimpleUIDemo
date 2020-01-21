using JetBrains.Annotations;

[UsedImplicitly]
class CustomizerForInputAndroid : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Tiny.InputAndroid";

    public override string[] ImplementationFor => new [] { "Unity.Tiny.Input" };
}
