using System;
using System.Linq;
using Bee.Core;
using Bee.DotNet;
using NiceIO;
using Unity.BuildSystem.CSharpSupport;

class UnsafeUtility
{
    private static readonly Lazy<DotNetAssembly> _unsafeUtility = new Lazy<DotNetAssembly>(() =>
    {
        var builtPatcher = new CSharpProgram() {
            Path = "artifacts/UnsafeUtilityPatcher/UnsafeUtilityPatcher.exe",
            Sources = {$"{BuildProgram.LowLevelRoot}/UnsafeUtilityPatcher"},
            Defines = {"NDESK_OPTIONS"},
            References =
            {
                ReferenceAssemblies471.Paths,
                StevedoreUnityCecil.Paths,
            },
            LanguageVersion = "7.3"
        }.SetupDefault();

        var nonPatchedUnsafeUtility = new CSharpProgram() {
            Path = "artifacts/UnsafeUtilityUnpatched/UnsafeUtility.dll",
            Sources = {$"{BuildProgram.LowLevelRoot}/UnsafeUtility"},
            LanguageVersion = "7.3",
            Framework = {Framework.FrameworkNone},
            Unsafe = true,
            ProjectFilePath = $"UnsafeUtility.csproj",
            References = {Il2Cpp.TinyCorlib},
            CopyReferencesNextToTarget = false
        }.SetupDefault();

        var builtPatcherProgram = new DotNetRunnableProgram(builtPatcher);
        NPath nPath = "artifacts/UnsafeUtility/UnsafeUtility.dll";
        var args = new[] {
            $"--output={nPath}",
            $"--assembly={nonPatchedUnsafeUtility.Path}",
        };

        var result = new DotNetAssembly(nPath, nonPatchedUnsafeUtility.Framework,
            nonPatchedUnsafeUtility.DebugFormat,
            nPath.ChangeExtension("pdb"), nonPatchedUnsafeUtility.RuntimeDependencies,
            nonPatchedUnsafeUtility.ReferenceAssemblyPath);

        Backend.Current.AddAction("Patch", result.Paths,
            nonPatchedUnsafeUtility.Paths.Concat(builtPatcher.Paths).ToArray(), builtPatcherProgram.InvocationString,
            args);
        return result;
    });

    public static DotNetAssembly DotNetAssembly => _unsafeUtility.Value;
}