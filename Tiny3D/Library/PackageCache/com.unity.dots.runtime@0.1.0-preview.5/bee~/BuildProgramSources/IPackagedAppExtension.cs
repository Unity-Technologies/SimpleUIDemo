using System;
using System.Collections.Generic;
using Bee.Core;
using NiceIO;
using Unity.BuildSystem.NativeProgramSupport;

namespace Bee.Toolchain.Extension
{
    public interface IPackagedAppExtension
    {
        void SetAppPackagingParameters(String gameName, CodeGen codegen, IEnumerable<IDeployable> supportFiles);
    }
}
