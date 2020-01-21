using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NiceIO;

static class AsmDefConfigFile
{
    private static JObject Json { get; }
    
    static readonly Dictionary<string,AsmDefDescription> _namesToAsmDefDescription = new Dictionary<string, AsmDefDescription>();
    static readonly Dictionary<NPath,AsmRefDescription> _pathsToAsmRefDescription = new Dictionary<NPath, AsmRefDescription>();
    public static NPath UnityProjectPath { get; }
    public static NPath UnityCompilationPipelineAssemblyPath { get; }
    public static Dictionary<string, string> GuidsToAsmDefNames { get; } = new Dictionary<string, string>();

    static AsmDefConfigFile()
    {
        Json = JObject.Parse(new NPath("asmdefs.json").MakeAbsolute().ReadAllText());
        UnityProjectPath = Json["UnityProjectPath"].Value<string>();
        ProjectName = Json["ProjectName"].Value<string>();
        UnityCompilationPipelineAssemblyPath = Json["CompilationPipelineAssemblyPath"].Value<string>();
        foreach (var asmdef in Json["asmdefs"].Values<JObject>())
        {
            GuidsToAsmDefNames[asmdef["Guid"].Value<string>()] = asmdef["AsmdefName"].Value<string>();
        }
    }

    public static string ProjectName { get; }

    public static void InjectAsmDef(NPath path, string packageSource = "BuiltIn")
    {
        var asmdef = new AsmDefDescription(path, packageSource);
        _namesToAsmDefDescription[asmdef.Name] = asmdef;
    }

    public static AsmDefDescription AsmDefDescriptionFor(string asmdefname)
    {
        if (_namesToAsmDefDescription.TryGetValue(asmdefname, out var result))
            return result;

        var jobject = Json["asmdefs"].Values<JObject>().FirstOrDefault(o => o["AsmdefName"].Value<string>() == asmdefname);
        if (jobject == null)
            return null;
        
        result = new AsmDefDescription(jobject["FullPath"].Value<string>(), jobject["PackageSource"].Value<string>());
        _namesToAsmDefDescription[asmdefname] = result;
        return result;
    }

    public static AsmRefDescription AsmRefDescriptionFor(NPath path)
    {
        if (_pathsToAsmRefDescription.TryGetValue(path, out var result))
            return result;
        var jobject = Json["asmrefs"].Values<JObject>().FirstOrDefault(o => o["FullPath"].Value<string>().ToNPath() == path);
        if (jobject == null)
            return null;

        result = new AsmRefDescription(jobject["FullPath"].Value<string>(), jobject["PackageSource"].Value<string>());
        _pathsToAsmRefDescription[path] = result;
        return result;
    }

    public static DotsRuntimeCSharpProgram CSharpProgramFor(string asmdefname)
    {
        var desc = AsmDefDescriptionFor(asmdefname);
        if (desc == null)
            return null;
        return BuildProgram.GetOrMakeDotsRuntimeCSharpProgramFor(desc);
    }
    
    public static IEnumerable<AsmDefDescription> AssemblyDefinitions
    {
        get
        {
            foreach (var jobject in Json["asmdefs"].Values<JObject>())
                yield return AsmDefDescriptionFor(jobject["AsmdefName"].Value<string>());
        }
    }
    
    public static IEnumerable<AsmRefDescription> AsmRefs
    {
        get
        {
            foreach (var jobject in Json["asmrefs"].Values<JObject>())
                yield return AsmRefDescriptionFor(jobject["FullPath"].Value<string>().ToNPath());
        }
    }

    public static IEnumerable<AsmDefDescription> TestableAssemblyDefinitions
    {
        get
        {
            foreach (var asmdefName in Json["Testables"].Values<string>())
                yield return AsmDefDescriptionFor(asmdefName);
        }
    }
}
