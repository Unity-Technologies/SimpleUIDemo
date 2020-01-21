using Bee.NativeProgramSupport.Building;
using Bee.Toolchain.Emscripten;
using System;
using System.Collections.Generic;
using DotsBuildTargets;
using Newtonsoft.Json.Linq;
using Unity.BuildSystem.NativeProgramSupport;

abstract class DotsWebTarget : DotsBuildSystemTarget
{
	protected abstract bool UseWasm { get; }

	protected override NativeProgramFormat GetExecutableFormatForConfig(DotsConfiguration config,
		bool enableManagedDebugger)
	{
		var format = new EmscriptenExecutableFormat(ToolChain, "html");

		switch (config)
		{
			case DotsConfiguration.Debug:
				return format.WithLinkerSetting<EmscriptenDynamicLinker>(d =>
					TinyEmscripten.ConfigureEmscriptenLinkerFor(d,
						"debug",
						enableManagedDebugger));

			case DotsConfiguration.Develop:
				return format.WithLinkerSetting<EmscriptenDynamicLinker>(d =>
					TinyEmscripten.ConfigureEmscriptenLinkerFor(d,
						"develop",
						enableManagedDebugger));

			case DotsConfiguration.Release:
				return format.WithLinkerSetting<EmscriptenDynamicLinker>(d =>
					TinyEmscripten.ConfigureEmscriptenLinkerFor(d,
						"release",
						enableManagedDebugger));

			default:
				throw new NotImplementedException("Unknown config: " + config);
		}
	}

	public override NativeProgramFormat CustomizeExecutableForSettings(FriendlyJObject settings)
	{
		return GetExecutableFormatForConfig(DotsConfigs.DotsConfigForSettings(settings, out _),
				settings.GetBool("EnableManagedDebugger"))
			.WithLinkerSetting<EmscriptenDynamicLinker>(e =>
				e.WithEmscriptenSettings(settings.GetDictionary("EmscriptenSettings")));
	}
}

class DotsAsmJSTarget : DotsWebTarget
{
	protected override bool UseWasm => false;

	public override string Identifier => "asmjs";

	public override ToolChain ToolChain => TinyEmscripten.ToolChain_AsmJS;
}

class DotsWasmTarget : DotsWebTarget
{
	protected override bool UseWasm => true;

	public override string Identifier => "wasm";

	public override ToolChain ToolChain => TinyEmscripten.ToolChain_Wasm;
}
