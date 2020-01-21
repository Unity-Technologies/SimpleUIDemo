using System;
using System.Runtime.InteropServices;
#if !NET_DOTS
using System.Text.RegularExpressions;
#endif
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

//unity.properties has an unused "using UnityEngine.Bindings".
namespace UnityEngine.Bindings
{
    public class Dummy
    {
    }
}

namespace UnityEngine.Internal
{
    public class ExcludeFromDocsAttribute : Attribute {}
}

namespace Unity.Burst
{
    //why is this not in the burst package!?
    public class BurstDiscardAttribute : Attribute{}

    // Static init to support burst. Still needs called if burst not used (i.e. some tests)
    //
    // It is not needed outside of DOTS RT because the static init happening
    // is actually impl. in C++ code in Big DOTS, whereas here we init C#
    // statics that will potentially be burst compiled.
    public class DotsRuntimeInitStatics
    {
        internal static bool needInitStatics = true;

        public static void Init()
        {
            if (needInitStatics)
            {
                needInitStatics = false;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // This is a good space to do static initialization or other Burst-unfriendly things
                // if it doesn't need reflection (i.e. happens with CodeGen) to keep code burst compilable.
                Unity.Collections.LowLevel.Unsafe.AtomicSafetyHandle.StaticInit();
#endif
            }
        }

    }
}

namespace System
{
    public class CodegenShouldReplaceException : NotImplementedException
    {
        public CodegenShouldReplaceException() : base("This function should have been replaced by codegen")
        {
        }

        public CodegenShouldReplaceException(string msg) : base(msg)
        {
        }
    }
}