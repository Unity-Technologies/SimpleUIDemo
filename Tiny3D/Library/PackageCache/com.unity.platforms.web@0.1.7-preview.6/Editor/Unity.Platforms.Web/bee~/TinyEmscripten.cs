using System;
using System.Collections.Generic;
using Bee.NativeProgramSupport.Building;
using Bee.Stevedore;
using Bee.Toolchain.Emscripten;
using Bee.Tools;
using NiceIO;
using Unity.BuildSystem.NativeProgramSupport;
using Unity.BuildTools;

internal static class TinyEmscripten
{
    public static ToolChain ToolChain_AsmJS { get; } = MakeEmscripten(new AsmJsArchitecture());

    public static ToolChain ToolChain_Wasm { get; } = MakeEmscripten(new WasmArchitecture());

    public static NPath NodeExe;

    public static EmscriptenToolchain MakeEmscripten(EmscriptenArchitecture arch)
    {
        var emscripten = new StevedoreArtifact("emscripten");
        var emscriptenVersion = new Version(1, 38, 28);
        var emscriptenRoot = emscripten.Path.Combine("emscripten-nightly-1.38.28-2019_04_05_07_52");

        EmscriptenSdk sdk = null;

        if (Environment.GetEnvironmentVariable("EMSDK") != null)
        {
            Console.WriteLine("Using pre-set environment EMSDK=" + Environment.GetEnvironmentVariable("EMSDK") +
                              ". This should only be used for local development. Unset EMSDK env. variable to use tagged Emscripten version from Stevedore.");
            NodeExe = Environment.GetEnvironmentVariable("EMSDK_NODE");
            return new EmscriptenToolchain(new EmscriptenSdk(
                Environment.GetEnvironmentVariable("EMSCRIPTEN"),
                llvmRoot: Environment.GetEnvironmentVariable("LLVM_ROOT"),
                pythonExe: Environment.GetEnvironmentVariable("EMSDK_PYTHON"),
                nodeExe: Environment.GetEnvironmentVariable("EMSDK_NODE"),
                architecture: arch,
                // Use a dummy/hardcoded version string to represent Emscripten "incoming" branch (it should be always considered
                // a "dirty" branch that does not correspond to any tagged release)
                version: new Version(9, 9, 9),
                isDownloadable: false
            ));
        }

        if (HostPlatform.IsWindows)
        {
            var llvm = new StevedoreArtifact("emscripten-llvm-win-x64");

            var python = new StevedoreArtifact("winpython2-x64");
            var node = new StevedoreArtifact("node-win-x64");
            NodeExe = node.Path.Combine("node.exe");

            sdk = new EmscriptenSdk(
                emscriptenRoot,
                llvmRoot: llvm.Path.Combine("emscripten-llvm-e1.38.28-2019_04_05_07_52"),
                pythonExe: python.Path.Combine("WinPython-64bit-2.7.13.1Zero/python-2.7.13.amd64/python.exe"),
                nodeExe: NodeExe,
                architecture: arch,
                version: emscriptenVersion,
                isDownloadable: true,
                backendRegistrables: new[] {emscripten, llvm, python, node});
        }

        if (HostPlatform.IsLinux)
        {
            var llvm = new StevedoreArtifact("emscripten-llvm-linux-x64");
            var node = new StevedoreArtifact("node-linux-x64");
            NodeExe = node.Path.Combine("bin/node");

            sdk = new EmscriptenSdk(
                emscriptenRoot,
                llvmRoot: llvm.Path.Combine("emscripten-llvm-e1.38.28-2019_03_07_23_26"),
                pythonExe: "/usr/bin/python2",
                nodeExe: NodeExe,
                architecture: arch,
                version: emscriptenVersion,
                isDownloadable: true,
                backendRegistrables: new[] {emscripten, llvm, node});
        }

        if (HostPlatform.IsOSX)
        {
            var llvm = new StevedoreArtifact("emscripten-llvm-mac-x64");
            var node = new StevedoreArtifact("node-mac-x64");
            NodeExe = node.Path.Combine("bin/node");

            sdk = new EmscriptenSdk(
                emscriptenRoot: emscriptenRoot,
                llvmRoot: llvm.Path.Combine("emscripten-llvm-e1.38.28-2019_04_05_07_52"),
                pythonExe: "/usr/bin/python",
                nodeExe: NodeExe,
                architecture: arch,
                version: emscriptenVersion,
                isDownloadable: true,
                backendRegistrables: new[] {emscripten, llvm, node});
        }

        if (sdk == null)
            return null;

        return new EmscriptenToolchain(sdk);
    }
    
    // Development time configuration: Set to true to generate a HTML5 build that runs in a Web Worker instead of the (default) main browser thread.
    public static bool RunInBackgroundWorker { get; } = false;

    public static EmscriptenDynamicLinker ConfigureEmscriptenLinkerFor(EmscriptenDynamicLinker e,
        string variation, bool enableManagedDebugger)
    {
        var linkflags = new Dictionary<string, string>
        {
            // Bee defaults to PRECISE_F32=2, which is not an interesting feature for Dots. In Dots asm.js builds, we don't
            // care about single-precision floats, but care more about code size.
            {"PRECISE_F32", "0"},
            // No exceptions machinery needed, saves code size
            {"DISABLE_EXCEPTION_CATCHING", "1"},
            //// No virtual filesystem needed, saves code size
            {"NO_FILESYSTEM", "1"},
            // Make generated builds only ever executable from web, saves code size.
            // TODO: if/when we are generating a build for node.js test harness purposes, remove this line.
            {"ENVIRONMENT", "web"},
            // In -Oz builds, Emscripten does compile time global initializer evaluation in hope that it can
            // optimize away some ctors that can be compile time executed. This does not really happen often,
            // and with MINIMAL_RUNTIME we have a better "super-constructor" approach that groups all ctors
            // together into one, and that saves more code size. Unfortunately grouping constructors is
            // not possible if EVAL_CTORS is used, so disable EVAL_CTORS to enable grouping.
            {"EVAL_CTORS", "0"},
            // We don't want malloc() failures to trigger program exit and abort handling, but instead behave
            // like C runtimes do, and make malloc() return null. This saves code size and lets our code
            // handle oom failures.
            {"ABORTING_MALLOC", "0"},
            // By default the musl C runtime used by Emscripten is POSIX errno aware. We do not care about
            // errno, so opt out from errno management to save a tiny bit of performance and code size.
            {"SUPPORT_ERRNO", "0"},
            // Safari does not support WebAssembly.instantiateStreaming(), so revert to the older
            // WebAssembly.instantiate() API. This has the drawback that WebAssembly compilation will not
            // occur while downloading the .wasm file, but enables Safari compatibility.
            {"STREAMING_WASM_COMPILATION", "0"}
        };

        if (enableManagedDebugger)
            linkflags["PROXY_POSIX_SOCKETS"] = "1";

        if (e.Toolchain.Architecture is AsmJsArchitecture)
        {
            linkflags["LEGACY_VM_SUPPORT"] = "1";
            e = e.WithSeparateAsm(true);
        }

        if (variation == "debug" || variation == "develop")
        {
            linkflags["ASSERTIONS"] = "2";
            linkflags["DEMANGLE_SUPPORT"] = "1";
        }
        else
        {
            linkflags["ASSERTIONS"] = "0";
            linkflags["AGGRESSIVE_VARIABLE_ELIMINATION"] = "1";
            linkflags["ELIMINATE_DUPLICATE_FUNCTIONS"] = "1";
        }

        e = e.WithEmscriptenSettings(linkflags);
        e = e.WithNoExitRuntime(true);

        switch (variation)
        {
            case "debug":
                e = e.WithDebugLevel("3");
                e = e.WithOptLevel("0");
                e = e.WithLinkTimeOptLevel(0);
                e = e.WithEmitSymbolMap(true);
                break;
            case "develop":
                e = e.WithDebugLevel("2");
                e = e.WithOptLevel("1");
                e = e.WithLinkTimeOptLevel(0);
                e = e.WithEmitSymbolMap(false); 
                break;
            case "release":
                e = e.WithDebugLevel("0");
                e = e.WithOptLevel("z");
                e = e.WithLinkTimeOptLevel(3);
                e = e.WithEmitSymbolMap(true);
                break;
            default:
                throw new ArgumentException();
        }

        e = e.WithMinimalRuntime(EmscriptenMinimalRuntimeMode.EnableDangerouslyAggressive);

        // Bee is not yet aware of the new --closure-externs (and --closure-annotations) linker flags, so add them using the generic
        // escape hatch hook.
        e = e.WithCustomFlags_workaround(new[]
        {
            "--closure-externs", BuildProgram.BeeRoot.Combine("closure_externs.js").ToString().QuoteForProcessStart()
        });

        // TODO: Remove this line once Bee fix is in to support SystemLibrary() objects on web builds. Then restore
        // the line Libraries.Add(c => c.ToolChain.Platform is WebGLPlatform, new SystemLibrary("GL")); at the top of this file
        e = e.WithCustomFlags_workaround(new[] {"-lGL"});

        e=e.WithMemoryInitFile(e.Toolchain.Architecture is AsmJsArchitecture || RunInBackgroundWorker);
        
        if (RunInBackgroundWorker)
        {
            // Specify Emscripten -s USE_PTHREADS=1 at compile time, so that C++ code that is compiled will have
            // the __EMSCRIPTEN_PTHREADS__ preprocessor #define available to it to detect if code will be compiled
            // single- or multithreaded.
            e=e.WithCustomFlags_workaround(new[] { "-s USE_PTHREADS=1 " });
        }

        // Enables async requests for web IO and Disabling IndexDB support as this is not fully implemented yet in emscripten
        // Using custom flags as there appears to be no standard way to set the option in bee and passing the flags to the linker settings
        // normally will cause bee to error
        e = e.WithCustomFlags_workaround(new[] { "-s FETCH=1 -s FETCH_SUPPORT_INDEXEDDB=0" });

        return e;
    }
}
