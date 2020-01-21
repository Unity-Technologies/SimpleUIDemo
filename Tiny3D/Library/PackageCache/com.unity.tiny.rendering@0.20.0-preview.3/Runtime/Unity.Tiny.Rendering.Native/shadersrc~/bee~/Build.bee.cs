using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bee.Core;
using NiceIO;
using Unity.BuildTools;

class Build
{
    static void Main()
    {
        // add new shaders here 
        var allShaders = new[] { "line", "simple", "zonly", "simplelit", "blitsrgb", "sprite", "shadowmap" };

        var allShaderOutputs = new List<NPath>();
        var shaderOutputdir = $"../shaderbin~";
        foreach (var shader in allShaders.Select(s => new NPath(Directory.GetCurrentDirectory()).Combine(s)))
        {
            allShaderOutputs = allShaderOutputs.Concat(SetupShaderCompilation(shader, shaderOutputdir)).ToList();
        }
        allShaderOutputs = allShaderOutputs.Concat(SetupShaderCompilation("externalblites3", shaderOutputdir, true)).ToList();
    }

    private static NPath DoShaderC(NPath inputDir, NPath outputDir, string shaderName, string fsORvs, string backend, string extraArgs)
    {
        var thisOutput = outputDir.Combine($"{fsORvs}_{shaderName}_{backend}.raw");
        Backend.Current.AddAction("Shaderc",
            new[] { thisOutput },
            new[] { inputDir.Combine($"{fsORvs}_{shaderName}.sc"), inputDir.Combine("varying.def.sc") },
            "shaderc.exe",
            $"-f {shaderName}/{fsORvs}_{shaderName}.sc -o {outputDir.Combine($"{fsORvs}_{shaderName}_{backend}.raw")} {extraArgs} --varyingdef {shaderName}/varying.def.sc"
                .Split(' ').ToArray());
        return thisOutput;
    }

    private static List<NPath> SetupShaderCompilation(NPath inputDir, NPath outputDir, bool raw = false)
    {
        var shaderName = inputDir.FileName;
        var allOutputs = new List<NPath>();

        if (!raw)
        {
            // dx9
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "fs", "direct3d9", "--type fragment --profile ps_3_0 --platform windows"));
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "vs", "direct3d9", "--type vertex --profile vs_3_0 --platform windows"));
            // dx11
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "fs", "direct3d11", "--type fragment --profile ps_4_0 --platform windows"));
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "vs", "direct3d11", "--type vertex --profile vs_4_0 --platform windows"));
            // metal
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "fs", "metal", "--type fragment --profile metal --platform ios"));
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "vs", "metal", "--type vertex --profile metal --platform ios"));
            // glsl
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "fs", "opengl", "--type fragment --platform linux -p 120"));
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "vs", "opengl", "--type vertex --platform linux -p 120"));
            // glsl es
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "fs", "opengles", "--type fragment --platform android"));
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "vs", "opengles", "--type vertex --platform android"));
            // spirv
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "fs", "vulkan", "--type fragment --profile spirv --platform linux"));
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "vs", "vulkan", "--type vertex --profile spirv --platform linux"));
        }
        else
        {
            // glsl es - raw 
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "fs", "opengl", "--type fragment --platform linux --raw --profile 3"));
            allOutputs.Add(DoShaderC(inputDir, outputDir, shaderName, "vs", "opengl", "--type vertex --platform linux --raw --profile 3"));
        }
        return allOutputs;
    }
}
