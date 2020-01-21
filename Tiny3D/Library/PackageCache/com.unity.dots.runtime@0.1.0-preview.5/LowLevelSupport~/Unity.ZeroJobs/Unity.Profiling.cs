#if !UNITY_PROFILING_API_EXTERNAL
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

namespace Unity.Profiling
{
    public static class Profiler
    {
        public static void FrameBegin()
        {
        }

        public static void FrameEnd()
        {
        }
    }

    public class ProfilerMarker
    {
        public ProfilerMarker(string s)
        {
        }

        public void Begin()
        {
        }

        public void End()
        {
        }

        class DummyDisposable : IDisposable
        {
            public void Dispose()
            {
            }

        }

        public IDisposable Auto()
        {
            return new DummyDisposable();
        }
    }
}
#endif
