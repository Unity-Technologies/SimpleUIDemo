using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NiceIO;
using Unity.BuildSystem.NativeProgramSupport;

public class AsmRefDescription
{
    public NPath Path { get; }
    public string PackageSource { get; }
    private JObject Json;
    
    public AsmRefDescription(NPath path, string packageSource)
    {
        Path = path;
        PackageSource = packageSource;
        Json = JObject.Parse(path.ReadAllText());
    }

    public string Reference
    {
        get
        {
            var target = Json["reference"].Value<string>();
            if (target.StartsWith("GUID:"))
            {
                target = AsmDefConfigFile.GuidsToAsmDefNames[target.Substring(5)];
            }
            return target;
        }
    }
}


public class AsmDefDescription
{
    public NPath Path { get; }
    public string PackageSource { get; }
    private JObject Json;
    
    public AsmDefDescription(NPath path, string packageSource)
    {
        Path = path;
        PackageSource = packageSource;
        Json = JObject.Parse(path.ReadAllText());
        IncludedAsmRefs = AsmDefConfigFile.AsmRefs.Where(desc => desc.Reference == Name).ToList();
    }

    public string Name => Json["name"].Value<string>();
    public List<AsmRefDescription> IncludedAsmRefs { get; }

    public string[] NamedReferences => Json["references"]?.Values<string>().ToArray() ?? Array.Empty<string>();
    public AsmDefDescription[] References => NamedReferences.Select(AsmDefConfigFile.AsmDefDescriptionFor).Where(d => d != null&& IsSupported(d.Name)).ToArray();

    public Platform[] IncludePlatforms => ReadPlatformList(Json["includePlatforms"]);
    public Platform[] ExcludePlatforms => ReadPlatformList(Json["excludePlatforms"]);
    public bool Unsafe => Json["allowUnsafeCode"]?.Value<bool>() == true;
    public NPath Directory => Path.Parent;

    public string[] DefineConstraints => Json["defineConstraints"]?.Values<string>().ToArray() ?? Array.Empty<string>();

    public string[] OptionalUnityReferences => Json["optionalUnityReferences"]?.Values<string>()?.ToArray() ?? Array.Empty<string>();
    

    private static Platform[] ReadPlatformList(JToken platformList)
    {
        if (platformList == null)
            return Array.Empty<Platform>();

        return platformList.Select(token => PlatformFromAsmDefPlatformName(token.ToString())).Where(p => p != null).ToArray();
    }

    private static Platform PlatformFromAsmDefPlatformName(string name)
    {
        switch(name)
        {
            case "macOSStandalone":
                return new MacOSXPlatform();
            case "WindowsStandalone32":
            case "WindowsStandalone64":
                return new WindowsPlatform();
            case "Editor":
                return null;
            default:
            {
                var typeName = $"{name}Platform";
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
                if (type == null)
                {
                    Console.WriteLine($"Couldn't find Platform for {name} (tried {name}Platform), ignoring it.");
                    return null;
                }
                return (Platform)Activator.CreateInstance(type);
            }
        }
    }
    private bool IsSupported(string referenaceName)
    {
        if (referenaceName.Contains("Unity.Collections.Tests"))
            return false;

        return true;
    }
}
