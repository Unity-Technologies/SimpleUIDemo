using System;

namespace Unity.Burst
{
    /// <summary>
    /// Can be used to specify that a parameter to a function, a field of a struct, or a struct will not alias. (Advanced - see User Manual for a description of Aliasing).
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Struct)]
    public class NoAliasAttribute : Attribute
    {
    }

#if UNITY_2020_1_OR_NEWER || UNITY_BURST_EXPERIMENTAL_FEATURE_ALIASING
    public static class Aliasing
    {
        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b do not alias.
        /// </summary>
        public unsafe static void ExpectAlias(void* a, void* b) { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b do not alias.
        /// </summary>
        public static void ExpectAlias<T, U>(ref T a, ref U b) where T : struct where U : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b do not alias.
        /// </summary>
        public unsafe static void ExpectAlias<T>(void* a, ref T b) where T : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b do not alias.
        /// </summary>
        public unsafe static void ExpectAlias<T>(ref T a, void* b) where T : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b can alias.
        /// </summary>
        public unsafe static void ExpectNoAlias(void* a, void* b) { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b can alias.
        /// </summary>
        public static void ExpectNoAlias<T, U>(ref T a, ref U b) where T : struct where U : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b can alias.
        /// </summary>
        public unsafe static void ExpectNoAlias<T>(void* a, ref T b) where T : struct { }

        /// <summary>
        /// Will cause a compiler error in Burst-compiled code if a and b can alias.
        /// </summary>
        public unsafe static void ExpectNoAlias<T>(ref T a, void* b) where T : struct { }
    }
#endif
}
