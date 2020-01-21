using System;
using Bee.Core;
using Bee.Stevedore;
using NiceIO;

static class StevedoreUnityCecil
{
    public static NPath[] Paths => _paths.Value;
    
    static readonly Lazy<NPath[]> _paths = new Lazy<NPath[]>(() =>
    {
        var il2cppArtifact = new StevedoreArtifact("il2cpp");
        Backend.Current.Register(il2cppArtifact);

        return new[]
        {
            il2cppArtifact.Path.Combine("build/deploy/net471/Mono.Cecil.dll"),
            il2cppArtifact.Path.Combine("build/deploy/net471/Mono.Cecil.Mdb.dll"),
            il2cppArtifact.Path.Combine("build/deploy/net471/Mono.Cecil.Pdb.dll"),
            il2cppArtifact.Path.Combine("build/deploy/net471/Mono.Cecil.Rocks.dll"),
        };
    });
}
