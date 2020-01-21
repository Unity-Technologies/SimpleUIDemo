using Bee.Toolchain.Linux;
using DotsBuildTargets;
using Unity.BuildSystem.NativeProgramSupport;

abstract class DotsLinuxTarget : DotsBuildSystemTarget
{
    public override ToolChain ToolChain => new LinuxGccToolchain(LinuxGccSdk.Locatorx64.UserDefaultOrDummy);
}

class DotsLinuxDotNetTarget : DotsLinuxTarget
{
    public override string Identifier => "linux-dotnet";

    public override ScriptingBackend ScriptingBackend => ScriptingBackend.Dotnet;
    public override bool CanUseBurst => false;
}

class DotsLinuxIL2CPPTarget : DotsLinuxTarget
{
    public override string Identifier => "linux-il2cpp";
}
