#pragma once

#include <pthread.h>

#if __cplusplus
extern "C" {
#endif

static COMPILER_FORCEINLINE void Baselib_TLS_Set(Baselib_TLS_Handle handle, uintptr_t value)
{
    int rc = pthread_setspecific((pthread_key_t)handle, (void*)value);
    BaselibAssert(rc == 0);
}

static COMPILER_FORCEINLINE uintptr_t Baselib_TLS_Get(Baselib_TLS_Handle handle)
{
    return (uintptr_t)pthread_getspecific((pthread_key_t)handle);
}

#if __cplusplus
}
#endif
