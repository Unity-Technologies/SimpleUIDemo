using System.Collections.Generic;
using Bee.Toolchain.Xcode;
using JetBrains.Annotations;
using Unity.BuildSystem.NativeProgramSupport;

[UsedImplicitly]
class CustomizerForTinyAudioNative : AsmDefCSharpProgramCustomizer
{
    public override string CustomizerFor => "Unity.Tiny.Audio.Native";

    public override string[] ImplementationFor => new[] {"Unity.Tiny.Audio"};
    
    public override void CustomizeSelf(AsmDefCSharpProgram program)
    {           
        program.NativeProgram.Libraries.Add(c => c.Platform is AndroidPlatform, new List<string>
        {
            "android", "log", "OpenSLES"
        }.ConvertAll(s => new SystemLibrary(s)));
        
        program.NativeProgram.Libraries.Add(c => c.Platform is LinuxPlatform, new List<string>
        {
            "dl", "rt"
        }.ConvertAll(s => new SystemLibrary(s)));

        program.NativeProgram.Libraries.Add(c => c.Platform is IosPlatform, new List<string>
        {
            "AudioToolbox", "AVFoundation"
        }.ConvertAll(s => new SystemFramework(s)));
        
        program.NativeProgram.CompilerSettingsForIos().Add(c => c.WithLanguage(Language.ObjectiveCpp));
    }
}
