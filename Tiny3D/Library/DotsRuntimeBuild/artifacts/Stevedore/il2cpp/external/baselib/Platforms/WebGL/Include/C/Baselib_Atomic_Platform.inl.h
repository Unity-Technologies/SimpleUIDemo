#pragma once

// Mostly using GCC intrinsics (see Baselib_Atomic_GCC), but with a custom compare exchange implementation:

#include <emscripten/threading.h>

#define detail_intrinsic_relaxed __ATOMIC_RELAXED
#define detail_intrinsic_acquire __ATOMIC_ACQUIRE
#define detail_intrinsic_release __ATOMIC_RELEASE
#define detail_intrinsic_acq_rel __ATOMIC_ACQ_REL
#define detail_intrinsic_seq_cst __ATOMIC_SEQ_CST

#define detail_THREAD_FENCE(order, ...)                                                                                     \
static FORCE_INLINE void Baselib_atomic_thread_fence_##order(void)                                                          \
{                                                                                                                           \
    __extension__({__atomic_thread_fence (detail_intrinsic_##order); });                                                    \
}                                                                                                                           \

#define detail_LOAD(op, order, id , bits, int_type, ...)                                                                    \
static FORCE_INLINE void Baselib_atomic_##op##_##id##_##order##_v(const void* obj, void* result)                            \
{                                                                                                                           \
    __extension__({ __atomic_load((int_type*)obj, (int_type*)result, detail_intrinsic_##order); });                         \
}

#define detail_LOAD_NOT_CONST(op, order, id , bits, int_type, ...)                                                          \
static FORCE_INLINE void Baselib_atomic_##op##_##id##_##order##_v(void* obj, void* result)                                  \
{                                                                                                                           \
    __extension__({ __atomic_load((int_type*)obj, (int_type*)result, detail_intrinsic_##order); });                         \
}

#define detail_STORE(op, order, id , bits, int_type, ...)                                                                   \
static FORCE_INLINE void Baselib_atomic_##op##_##id##_##order##_v(void* obj, const void* value)                             \
{                                                                                                                           \
    __extension__({ __atomic_store((int_type*)obj, (int_type*)value, detail_intrinsic_##order); });                         \
}

#define detail_ALU(op, order, id , bits, int_type, ...)                                                                     \
static FORCE_INLINE void Baselib_atomic_##op##_##id##_##order##_v(void* obj, const void* value, void* result)               \
{                                                                                                                           \
    *(int_type*)result = __extension__({ __atomic_##op((int_type*)obj, *(int_type*)value, detail_intrinsic_##order); });    \
}

// When compiling without threads, (compare) exchange atomics lead to compilation failure in some SDK versions.
#ifdef __EMSCRIPTEN_PTHREADS__

// Unity uses use a fairly old emscripten SDK and run into this bug when using intrinsics for compare exchange:
// https://github.com/WebAssembly/binaryen/issues/1718
// GCC intrincs are confirmed working for atomic_exchange with SDKs as early as 1.38.28
//
// But we also can't use emscripten_atomic_cas_u8, due to a different bug as it yields the following error message when linking for wasm:
// "bad processUnshifted "$1"
// See bug ticket with minimal repro case here:
// https://github.com/WebAssembly/binaryen/issues/2170
// Even worse, with the GCC extensions we observe the same compilation failure in 1.38.13 and 1.38.3, but it works in 1.38.28!
// I.e. in old sdks, using this function will always fail compilation
#define detail_XCHG(op, order, id , bits, int_type, ...)                                                                      \
static FORCE_INLINE void Baselib_atomic_##op##_##id##_##order##_v(void* obj, const void* value, void* result)                 \
{                                                                                                                             \
    if (bits > 8)                                                                                                             \
        *(int_type*)result = emscripten_atomic_exchange_u##bits(obj, *(int_type*)value);                                      \
    else                                                                                                                      \
        __extension__({ __atomic_exchange((int_type*)obj, (int_type*)value, (int_type*)result, detail_intrinsic_##order); }); \
}

// Can't use 64 bit compare exchange via GCC intrinsics due to this "compiler"-bug
// https://github.com/emscripten-core/emscripten/issues/8503#issuecomment-488903274
//
// Just like with atomic_exchange, 8 bit atomics don't work at all in older sdks and only with GCC intrinsics in newer ones (see above)
#define detail_CMP_XCHG_STRONG(op, order1, order2, id , bits, int_type, ...)                                                \
static FORCE_INLINE bool Baselib_atomic_##op##_##id##_##order1##_##order2##_v(void* obj, void* expected, const void* value) \
{                                                                                                                           \
    if (bits > 8)                                                                                                           \
    {                                                                                                                       \
        int_type prev = emscripten_atomic_cas_u##bits(obj, *(int_type*)expected, *(int_type*)value);                        \
        if (prev == *(int_type*)expected)                                                                                   \
            return true;                                                                                                    \
        *(int_type*)expected = prev;                                                                                        \
        return false;                                                                                                       \
    }                                                                                                                       \
    return __extension__({ __atomic_compare_exchange(                                                                       \
        (int_type*)obj,                                                                                                     \
        (int_type*)expected,                                                                                                \
        (int_type*)value,                                                                                                   \
        1,                                                                                                                  \
        detail_intrinsic_##order1,                                                                                          \
        detail_intrinsic_##order2);                                                                                         \
    });                                                                                                                     \
}

#else

#define detail_XCHG(op, order, id , bits, int_type, ...)                                                                    \
static FORCE_INLINE void Baselib_atomic_##op##_##id##_##order##_v(void* obj, const void* value, void* result)               \
{                                                                                                                           \
    *(int_type*)result = *(int_type*)obj;                                                                                   \
    *(int_type*)obj = *(int_type*)value;                                                                                    \
}

#define detail_CMP_XCHG_STRONG(op, order1, order2, id , bits, int_type, ...)                                                \
static FORCE_INLINE bool Baselib_atomic_##op##_##id##_##order1##_##order2##_v(void* obj, void* expected, const void* value) \
{                                                                                                                           \
    if (*(int_type*)obj == *(int_type*)expected)                                                                            \
    {                                                                                                                       \
        *(int_type*)obj = *(int_type*)value;                                                                                \
        return true;                                                                                                        \
    }                                                                                                                       \
    else                                                                                                                    \
    {                                                                                                                       \
        *(int_type*)expected = *(int_type*)obj;                                                                             \
        return false;                                                                                                       \
    }                                                                                                                       \
}

#endif

// AsmJs/WASM do not distinguish between strong/weak compare/exchange!
#define detail_CMP_XCHG_WEAK detail_CMP_XCHG_STRONG

#define detail_NOT_SUPPORTED(...)

Baselib_Atomic_FOR_EACH_MEMORY_ORDER(
    detail_THREAD_FENCE
)

Baselib_Atomic_FOR_EACH_ATOMIC_OP_MEMORY_ORDER_AND_TYPE(
    detail_LOAD,            // load
    detail_STORE,           // store
    detail_ALU,             // add
    detail_ALU,             // and
    detail_ALU,             // or
    detail_ALU,             // xor
    detail_XCHG,            // exchange
    detail_CMP_XCHG_WEAK,   // compare_exchange_weak
    detail_CMP_XCHG_STRONG  // compare_exchange_strong
)

Baselib_Atomic_FOR_EACH_ATOMIC_OP_AND_MEMORY_ORDER(
    detail_LOAD_NOT_CONST,      // load
    detail_STORE,               // store
    detail_NOT_SUPPORTED,       // add
    detail_NOT_SUPPORTED,       // and
    detail_NOT_SUPPORTED,       // or
    detail_NOT_SUPPORTED,       // xor
    detail_XCHG,                // exchange
    detail_CMP_XCHG_WEAK,       // compare_exchange_weak
    detail_CMP_XCHG_STRONG,     // compare_exchange_strong
    ptr2x, 64, int64_t          // type information
)

#undef detail_intrinsic_relaxed
#undef detail_intrinsic_acquire
#undef detail_intrinsic_release
#undef detail_intrinsic_acq_rel
#undef detail_intrinsic_seq_cst

#undef detail_THREAD_FENCE
#undef detail_LOAD
#undef detail_STORE
#undef detail_ALU
#undef detail_XCHG
#undef detail_CMP_XCHG_WEAK
#undef detail_CMP_XCHG_STRONG
