using DotsBuildTargets;
using Unity.BuildSystem.MacSDKSupport;
using Unity.BuildSystem.NativeProgramSupport;

abstract class DotsMacOSTarget : DotsBuildSystemTarget
{
    public override ToolChain ToolChain => new MacToolchain(MacSdk.Locatorx64.UserDefaultOrDummy);
}

class DotsMacOSDotNetTarget : DotsMacOSTarget
{
    public override string Identifier => "macos-dotnet";

    public override ScriptingBackend ScriptingBackend => ScriptingBackend.Dotnet;
    public override bool CanUseBurst => true;
}

class DotsMacOSIL2CPPTarget : DotsMacOSTarget
{
    public override string Identifier => "macos-il2cpp";
}
