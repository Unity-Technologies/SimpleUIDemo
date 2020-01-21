using Bee.Toolchain.Xcode;
using JetBrains.Annotations;
using Unity.BuildSystem.NativeProgramSupport;

[UsedImplicitly]
class CustomizerForTinyiOS : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Tiny.iOS";

    // not exactly right, but good enough for now
    public override string[] ImplementationFor => new[] {"Unity.Tiny.Core"};
}
