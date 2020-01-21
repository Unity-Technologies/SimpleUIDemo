using System;
using System.Linq;
using Bee.DotNet;
using NiceIO;
using Unity.BuildSystem.CSharpSupport;

public class AsmDefCSharpProgram : DotsRuntimeCSharpProgram
{
    public DotsRuntimeCSharpProgram[] ReferencedPrograms { get; }
    public AsmDefDescription AsmDefDescription { get; }

    // We don't have the ability to have asmdef references which are required by Hybrid but are incompatible 
    // with DOTS Runtime. So we manually remove them here for now :(
    string[] IncompatibleDotRuntimeAsmDefs =
    {
        "Unity.Properties",
        "Unity.Properties.Reflection"
    };

    public AsmDefCSharpProgram(AsmDefDescription asmDefDescription)
        : base(asmDefDescription.Directory,
            asmDefDescription.IncludedAsmRefs.Select(asmref => asmref.Path.Parent),
            deferConstruction: true)
    {
        AsmDefDescription = asmDefDescription;

        var asmDefReferences = AsmDefDescription.References.Select(BuildProgram.GetOrMakeDotsRuntimeCSharpProgramFor).ToList();
        ReferencedPrograms = asmDefReferences.Where(r => !IncompatibleDotRuntimeAsmDefs.Contains(r.AsmDefDescription.Name)).ToArray();

        var isTinyRoot = AsmDefDescription.NamedReferences.Contains("Unity.Tiny.Main")
                         || asmDefDescription.Path.Parent.Files("*.project").Any();

        var isExe = asmDefDescription.DefineConstraints.Contains("UNITY_DOTS_ENTRYPOINT") || asmDefDescription.Name.EndsWith(".Tests");
        
        Construct(asmDefDescription.Name, isExe);

        ProjectFile.AdditionalFiles.Add(asmDefDescription.Path);

        IncludePlatforms = AsmDefDescription.IncludePlatforms;
        ExcludePlatforms = AsmDefDescription.ExcludePlatforms;
        Unsafe = AsmDefDescription.Unsafe;
        References.Add(config =>
        {
            if (config is DotsRuntimeCSharpProgramConfiguration dotsConfig)
                return ReferencedPrograms.Where(rp => rp.IsSupportedFor(dotsConfig));

            //this codepath will be hit for the bindgem invocation
            return ReferencedPrograms;
        });

        if (isTinyRoot || isExe)
        {
            AsmDefCSharpProgramCustomizer.RunAllAddPlatformImplementationReferences(this);
        }

        if (BuildProgram.ZeroJobs != null)
            References.Add(BuildProgram.ZeroJobs);
        if (BuildProgram.UnityLowLevel != null)
            References.Add(BuildProgram.UnityLowLevel);

        if (IsTestAssembly)
        {
            References.Add(BuildProgram.NUnitFramework);
            var nunitLiteMain = BuildProgram.BeeRoot.Combine("CSharpSupport/NUnitLiteMain.cs");
            Sources.Add(nunitLiteMain);
            ProjectFile.AddCustomLinkRoot(nunitLiteMain.Parent, "TestRunner");
            References.Add(BuildProgram.NUnitLite);
            References.Add(BuildProgram.GetOrMakeDotsRuntimeCSharpProgramFor(AsmDefConfigFile.AsmDefDescriptionFor("Unity.Entities")));
        }
        else if(IsILPostProcessorAssembly)
        {
            References.Add(BuildProgram.UnityCompilationPipeline);
            References.Add(StevedoreUnityCecil.Paths);
        }
    }

    public override bool IsSupportedFor(CSharpProgramConfiguration config)
    {
        //UNITY_DOTS_ENTRYPOINT is actually a fake define constraint we use to signal the buildsystem, 
        //so don't impose it as a constraint
        return base.IsSupportedFor(config) &&
               AsmDefDescription.DefineConstraints.All(dc =>
                   dc == "UNITY_DOTS_ENTRYPOINT" || Defines.For(config).Contains(dc));
    }

    protected override bool ShouldTargetTinyCorlib(CSharpProgramConfiguration config, DotsRuntimeCSharpProgram program)
    {
        if (DoesTargetFullDotNet || IsTestAssembly || IsILPostProcessorAssembly)
            return false;

        return base.ShouldTargetTinyCorlib(config, program);
    }

    public void AddPlatformImplementationFor(string baseFeatureAsmDefName, string platformImplAsmDefName)
    {
        if (AsmDefDescription.Name == platformImplAsmDefName)
            return;

        if (AsmDefDescription.References.Any(r => r.Name == baseFeatureAsmDefName))
        {
            var impl = AsmDefConfigFile.CSharpProgramFor(platformImplAsmDefName);
            if (impl == null)
            {
                Console.WriteLine($"Missing assembly for {platformImplAsmDefName}, named in a customizer for {baseFeatureAsmDefName}.  Are you missing a package, or is the customizer in the wrong place?");
                return;
            }
            References.Add(c => impl.IsSupportedFor(c), impl);
        }
    }

    protected override NPath DeterminePathForProjectFile() =>
        DoesPackageSourceIndicateUserHasControlOverSource(AsmDefDescription.PackageSource) 
            ? AsmDefDescription.Path.Parent.Combine(AsmDefDescription.Name + ".gen.csproj") 
            : base.DeterminePathForProjectFile();

    public bool IsTestAssembly => AsmDefDescription.OptionalUnityReferences.Contains("TestAssemblies");
    public bool IsILPostProcessorAssembly => AsmDefDescription.Name.EndsWith(".CodeGen");
    public bool DoesTargetFullDotNet => AsmDefDescription.NamedReferences.Contains("Unity.FullDotNet");
}
