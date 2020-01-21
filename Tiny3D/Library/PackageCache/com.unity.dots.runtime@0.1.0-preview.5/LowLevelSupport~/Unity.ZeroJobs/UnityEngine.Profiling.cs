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

namespace UnityEngine.Profiling
{
    public class CustomSampler
    {
        public static CustomSampler Create(string s) => throw new NotImplementedException();
        public void Begin() => throw new NotImplementedException();
        public void End() => throw new NotImplementedException();
    }

    public static class Profiler
    {
        public static void BeginSample(string s)
        {
        }

        public static void EndSample(){}
    }
}
