#pragma once

#if defined(__EMSCRIPTEN_PTHREADS__)

#include <pthread.h>
#include <libc/limits.h>

// Emscripten has a bug in its pthread thread-local implementation:
// Newly created values are not guaranteed to be zero.
// We work around this by introducing a generation counter.
// https://github.com/emscripten-core/emscripten/issues/8740

// Note that C++11 thread_local does not work at all.
// https://github.com/emscripten-core/emscripten/issues/3502

#ifdef __cplusplus
BASELIB_C_INTERFACE
{
#endif

BASELIB_API void* Baselib_Memory_Allocate(size_t size);

// Note that we use static data here in the expectation that webgl builds never use dynamic libraries
extern int32_t Baselib_Internal_Emscripten_TLS_SlotGenerations[PTHREAD_KEYS_MAX];

#ifdef __cplusplus
} // BASELIB_C_INTERFACE
#endif

#if __cplusplus
extern "C" {
#endif

typedef struct
{
    uintptr_t data;
    int32_t generation;
} Baselib_Internal_Emscripten_TLS_Slot;

static COMPILER_FORCEINLINE void Baselib_TLS_Set(Baselib_TLS_Handle handle, uintptr_t value)
{
    Baselib_Internal_Emscripten_TLS_Slot* slot = (Baselib_Internal_Emscripten_TLS_Slot*)(pthread_getspecific((pthread_key_t)handle));
    if (!slot)
    {
        slot = (Baselib_Internal_Emscripten_TLS_Slot*)(Baselib_Memory_Allocate(sizeof(Baselib_Internal_Emscripten_TLS_Slot)));
        pthread_setspecific(handle, slot);
    }
    slot->data = value;
    slot->generation = Baselib_Internal_Emscripten_TLS_SlotGenerations[handle];
}

static COMPILER_FORCEINLINE uintptr_t Baselib_TLS_Get(Baselib_TLS_Handle handle)
{
    Baselib_Internal_Emscripten_TLS_Slot* slot = (Baselib_Internal_Emscripten_TLS_Slot*)(pthread_getspecific((pthread_key_t)handle));
    if (!slot)
        return 0;
    if (slot->generation != Baselib_Internal_Emscripten_TLS_SlotGenerations[handle])
        return 0;
    return slot->data;
}

#if __cplusplus
}
#endif

#else

#if __cplusplus
extern "C" {
#endif

static COMPILER_FORCEINLINE void Baselib_TLS_Set(Baselib_TLS_Handle handle, uintptr_t value)
{
    *(uintptr_t*)handle = value;
}

static COMPILER_FORCEINLINE uintptr_t Baselib_TLS_Get(Baselib_TLS_Handle handle)
{
    return *(uintptr_t*)handle;
}

#if __cplusplus
}
#endif

#endif
