using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityBenchShared;

namespace Burst.Compiler.IL.Tests
{
    internal class Aliasing
    {
        public unsafe struct NoAliasField
        {
            [NoAlias]
            public int* ptr1;

            [NoAlias]
            public int* ptr2;

            public void Compare(ref NoAliasField other)
            {
#if UNITY_2020_1_OR_NEWER || UNITY_BURST_EXPERIMENTAL_FEATURE_ALIASING
                // Check that we can definitely alias with another struct of the same type as us.
                Unity.Burst.Aliasing.ExpectAlias(ref this, ref other);
#endif
            }

            public void Compare(ref ContainerOfManyNoAliasFields other)
            {
#if UNITY_2020_1_OR_NEWER || UNITY_BURST_EXPERIMENTAL_FEATURE_ALIASING
                // Check that we can definitely alias with another struct which contains the same type as ourself.
                Unity.Burst.Aliasing.ExpectAlias(ref this, ref other);
#endif
            }

            public class Provider : IArgumentProvider
            {
                public object Value => new NoAliasField { ptr1 = null, ptr2 = null };
            }
        }

        public unsafe struct ContainerOfManyNoAliasFields
        {
            public NoAliasField s0;

            public NoAliasField s1;

            [NoAlias]
            public NoAliasField s2;

            [NoAlias]
            public NoAliasField s3;

            public class Provider : IArgumentProvider
            {
                public object Value => new ContainerOfManyNoAliasFields { s0 = new NoAliasField { ptr1 = null, ptr2 = null }, s1 = new NoAliasField { ptr1 = null, ptr2 = null }, s2 = new NoAliasField { ptr1 = null, ptr2 = null }, s3 = new NoAliasField { ptr1 = null, ptr2 = null } };
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Union
        {
            [FieldOffset(0)]
            public ulong a;

            [FieldOffset(1)]
            public int b;

            [FieldOffset(5)]
            public float c;

            public class Provider : IArgumentProvider
            {
                public object Value => new Union { a = 4242424242424242, b = 13131313, c = 42.0f };
            }
        }

        public unsafe struct LinkedList
        {
            public LinkedList* next;

            public class Provider : IArgumentProvider
            {
                public object Value => new LinkedList { next = null };
            }
        }

        [NoAlias]
        public unsafe struct NoAliasWithContentsStruct
        {
            public void* ptr0;
            public void* ptr1;

            public class Provider : IArgumentProvider
            {
                public object Value => new NoAliasWithContentsStruct { ptr0 = null, ptr1 = null };
            }
        }

        [NoAlias]
        public unsafe struct DoesAliasWithSubStructPointersStruct : IDisposable
        {
            public NoAliasWithContentsStruct* s;
            public void* ptr;

            public class Provider : IArgumentProvider
            {
                public object Value
                {
                    get
                    {
                        var noAliasSubStruct = (NoAliasWithContentsStruct*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<NoAliasWithContentsStruct>(), UnsafeUtility.AlignOf<NoAliasWithContentsStruct>(), Allocator.Temp);
                        noAliasSubStruct->ptr0 = null;
                        noAliasSubStruct->ptr1 = null;

                        var s = new DoesAliasWithSubStructPointersStruct { s = noAliasSubStruct, ptr = null };

                        return s;
                    }
                }
            }

            public void Dispose()
            {
                UnsafeUtility.Free(s, Allocator.Temp);
            }
        }

#if UNITY_2020_1_OR_NEWER || UNITY_BURST_EXPERIMENTAL_FEATURE_ALIASING
        [TestCompiler(typeof(NoAliasField.Provider))]
        public unsafe static void CheckNoAliasFieldWithItself(ref NoAliasField s)
        {
            // Check that they correctly alias with themselves.
            Unity.Burst.Aliasing.ExpectAlias(s.ptr1, s.ptr1);
            Unity.Burst.Aliasing.ExpectAlias(s.ptr2, s.ptr2);
        }

        [TestCompiler(typeof(NoAliasField.Provider), ExpectCompilerException = true)]
        public unsafe static void CheckNoAliasFieldWithItselfBadPtr1(ref NoAliasField s)
        {
            Unity.Burst.Aliasing.ExpectNoAlias(s.ptr1, s.ptr1);
        }

        [TestCompiler(typeof(NoAliasField.Provider), ExpectCompilerException = true)]
        public unsafe static void CheckNoAliasFieldWithItselfBadPtr2(ref NoAliasField s)
        {
            Unity.Burst.Aliasing.ExpectNoAlias(s.ptr2, s.ptr2);
        }

        [TestCompiler(typeof(NoAliasField.Provider))]
        public unsafe static void CheckNoAliasFieldWithAnotherPointer(ref NoAliasField s)
        {
            // Check that they do not alias each other because of the [NoAlias] on the ptr1 field above.
            Unity.Burst.Aliasing.ExpectNoAlias(s.ptr1, s.ptr2);
        }

        [TestCompiler(typeof(NoAliasField.Provider))]
        public unsafe static void CheckNoAliasFieldWithNull(ref NoAliasField s)
        {
            // Check that comparing a pointer with null is no alias.
            Unity.Burst.Aliasing.ExpectNoAlias(s.ptr1, null);
        }

        [TestCompiler(typeof(NoAliasField.Provider))]
        public unsafe static void CheckAliasFieldWithNull(ref NoAliasField s)
        {
            // Check that comparing a pointer with null is no alias.
            Unity.Burst.Aliasing.ExpectNoAlias(s.ptr2, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe static void NoAliasInfoSubFunctionAlias(int* a, int* b)
        {
            Unity.Burst.Aliasing.ExpectAlias(a, b);
        }

        [TestCompiler(typeof(NoAliasField.Provider))]
        public unsafe static void CheckNoAliasFieldSubFunctionAlias(ref NoAliasField s)
        {
            NoAliasInfoSubFunctionAlias(s.ptr1, s.ptr1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe static void NoAliasInfoSubFunctionNoAlias(int* a, int* b)
        {
            Unity.Burst.Aliasing.ExpectNoAlias(a, b);
        }

        [TestCompiler(typeof(NoAliasField.Provider), ExpectCompilerException = true)]
        public unsafe static void CheckNoAliasFieldSubFunctionNoAlias(ref NoAliasField s)
        {
            NoAliasInfoSubFunctionNoAlias(s.ptr1, s.ptr1);
        }

        [TestCompiler(typeof(NoAliasField.Provider))]
        public unsafe static void CheckCompareWithItself(ref NoAliasField s)
        {
            s.Compare(ref s);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe static void AliasInfoSubFunctionNoAlias([NoAlias] int* a, int* b)
        {
            Unity.Burst.Aliasing.ExpectNoAlias(a, b);
        }

        [TestCompiler(typeof(NoAliasField.Provider))]
        public unsafe static void CheckNoAliasFieldSubFunctionWithNoAliasParameter(ref NoAliasField s)
        {
            AliasInfoSubFunctionNoAlias(s.ptr1, s.ptr1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe static void AliasInfoSubFunctionTwoSameTypedStructs(ref NoAliasField s0, ref NoAliasField s1)
        {
            // Check that they do not alias within their own structs.
            Unity.Burst.Aliasing.ExpectNoAlias(s0.ptr1, s0.ptr2);
            Unity.Burst.Aliasing.ExpectNoAlias(s1.ptr1, s1.ptr2);

            // But that they do alias across structs.
            Unity.Burst.Aliasing.ExpectAlias(s0.ptr1, s1.ptr1);
            Unity.Burst.Aliasing.ExpectAlias(s0.ptr1, s1.ptr2);
            Unity.Burst.Aliasing.ExpectAlias(s0.ptr2, s1.ptr1);
            Unity.Burst.Aliasing.ExpectAlias(s0.ptr2, s1.ptr2);

        }

        [TestCompiler(typeof(NoAliasField.Provider), typeof(NoAliasField.Provider))]
        public unsafe static void CheckNoAliasFieldAcrossTwoSameTypedStructs(ref NoAliasField s0, ref NoAliasField s1)
        {
            AliasInfoSubFunctionTwoSameTypedStructs(ref s0, ref s1);
        }

        [TestCompiler(4, 13)]
        public static void CheckNoAliasRefs([NoAlias] ref int a, ref int b)
        {
            Unity.Burst.Aliasing.ExpectAlias(ref a, ref a);
            Unity.Burst.Aliasing.ExpectAlias(ref b, ref b);
            Unity.Burst.Aliasing.ExpectNoAlias(ref a, ref b);
        }

        [TestCompiler(4, 13.53f)]
        public static void CheckNoAliasRefsAcrossTypes([NoAlias] ref int a, ref float b)
        {
            Unity.Burst.Aliasing.ExpectNoAlias(ref a, ref b);
        }

        [TestCompiler(typeof(Union.Provider))]
        public static void CheckNoAliasRefsInUnion(ref Union u)
        {
            Unity.Burst.Aliasing.ExpectAlias(ref u.a, ref u.b);
            Unity.Burst.Aliasing.ExpectAlias(ref u.a, ref u.c);
            Unity.Burst.Aliasing.ExpectNoAlias(ref u.b, ref u.c);
        }

        [TestCompiler(typeof(ContainerOfManyNoAliasFields.Provider))]
        public unsafe static void CheckNoAliasOfSubStructs(ref ContainerOfManyNoAliasFields s)
        {
            // Since ptr1 and ptr2 have [NoAlias], they do not alias within the same struct instance.
            Unity.Burst.Aliasing.ExpectNoAlias(s.s0.ptr1, s.s0.ptr2);
            Unity.Burst.Aliasing.ExpectNoAlias(s.s1.ptr1, s.s1.ptr2);
            Unity.Burst.Aliasing.ExpectNoAlias(s.s2.ptr1, s.s2.ptr2);
            Unity.Burst.Aliasing.ExpectNoAlias(s.s3.ptr1, s.s3.ptr2);

            // Across s0 and s1 their pointers can alias each other though.
            Unity.Burst.Aliasing.ExpectAlias(s.s0.ptr1, s.s1.ptr1);
            Unity.Burst.Aliasing.ExpectAlias(s.s0.ptr1, s.s1.ptr2);
            Unity.Burst.Aliasing.ExpectAlias(s.s0.ptr2, s.s1.ptr1);
            Unity.Burst.Aliasing.ExpectAlias(s.s0.ptr2, s.s1.ptr2);

            // Also s2 can alias with s0 and s1 (because they do not have [NoAlias]).
            Unity.Burst.Aliasing.ExpectAlias(s.s2.ptr1, s.s0.ptr1);
            Unity.Burst.Aliasing.ExpectAlias(s.s2.ptr1, s.s0.ptr2);
            Unity.Burst.Aliasing.ExpectAlias(s.s2.ptr2, s.s1.ptr1);
            Unity.Burst.Aliasing.ExpectAlias(s.s2.ptr2, s.s1.ptr2);

            // Also s3 can alias with s0 and s1 (because they do not have [NoAlias]).
            Unity.Burst.Aliasing.ExpectAlias(s.s3.ptr1, s.s0.ptr1);
            Unity.Burst.Aliasing.ExpectAlias(s.s3.ptr1, s.s0.ptr2);
            Unity.Burst.Aliasing.ExpectAlias(s.s3.ptr2, s.s1.ptr1);
            Unity.Burst.Aliasing.ExpectAlias(s.s3.ptr2, s.s1.ptr2);

            // But s2 and s3 cannot alias each other (they both have [NoAlias]).
            Unity.Burst.Aliasing.ExpectNoAlias(s.s2.ptr1, s.s3.ptr1);
            Unity.Burst.Aliasing.ExpectNoAlias(s.s2.ptr1, s.s3.ptr2);
            Unity.Burst.Aliasing.ExpectNoAlias(s.s2.ptr2, s.s3.ptr1);
            Unity.Burst.Aliasing.ExpectNoAlias(s.s2.ptr2, s.s3.ptr2);
        }

        [TestCompiler(typeof(ContainerOfManyNoAliasFields.Provider))]
        public unsafe static void CheckNoAliasFieldCompareWithParentStruct(ref ContainerOfManyNoAliasFields s)
        {
            s.s0.Compare(ref s);
            s.s1.Compare(ref s);
            s.s2.Compare(ref s);
            s.s3.Compare(ref s);
        }

        [TestCompiler(typeof(LinkedList.Provider))]
        public unsafe static void CheckStructPointerOfSameTypeInStruct(ref LinkedList l)
        {
            Unity.Burst.Aliasing.ExpectAlias(ref l, l.next);
        }

        [TestCompiler(typeof(NoAliasWithContentsStruct.Provider))]
        public unsafe static void CheckStructWithNoAlias(ref NoAliasWithContentsStruct s)
        {
            // Since NoAliasWithContentsStruct has [NoAlias] on the struct definition, it cannot alias with any pointers within the struct.
            Unity.Burst.Aliasing.ExpectNoAlias(ref s, s.ptr0);
            Unity.Burst.Aliasing.ExpectNoAlias(ref s, s.ptr1);
        }

        [TestCompiler(typeof(DoesAliasWithSubStructPointersStruct.Provider))]
        public unsafe static void CheckStructWithNoAliasAndSubStructs(ref DoesAliasWithSubStructPointersStruct s)
        {
            // Since DoesAliasWithSubStructPointersStruct has [NoAlias] on the struct definition, it cannot alias with any pointers within the struct.
            Unity.Burst.Aliasing.ExpectNoAlias(ref s, s.s);
            Unity.Burst.Aliasing.ExpectNoAlias(ref s, s.ptr);

            // s.s is a [NoAlias] struct, so it shouldn't alias with pointers within it.
            Unity.Burst.Aliasing.ExpectNoAlias(s.s, s.s->ptr0);
            Unity.Burst.Aliasing.ExpectNoAlias(s.s, s.s->ptr1);

            // But we don't know whether s.s and s.ptr alias.
            Unity.Burst.Aliasing.ExpectAlias(s.s, s.ptr);

            // And we cannot assume that s does not alias with the sub-pointers of s.s.
            Unity.Burst.Aliasing.ExpectAlias(ref s, s.s->ptr0);
            Unity.Burst.Aliasing.ExpectAlias(ref s, s.s->ptr1);
        }
#endif
    }
}
