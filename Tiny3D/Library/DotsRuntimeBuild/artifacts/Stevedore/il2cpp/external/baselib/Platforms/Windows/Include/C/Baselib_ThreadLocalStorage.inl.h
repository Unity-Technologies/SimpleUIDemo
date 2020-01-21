#pragma once

#include <processthreadsapi.h>
#include <errhandlingapi.h>

#if __cplusplus
extern "C" {
#endif

static COMPILER_FORCEINLINE void Baselib_TLS_Set(Baselib_TLS_Handle handle, uintptr_t value)
{
    BOOL success = TlsSetValue((DWORD)handle, (void*)value);
    BaselibAssert(success != 0);
}

static COMPILER_FORCEINLINE uintptr_t Baselib_TLS_Get(Baselib_TLS_Handle handle)
{
    void* result = TlsGetValue((DWORD)handle);
    BaselibAssert((result != NULL) || (GetLastError() == 0L));
    return (uintptr_t)result;
}

#if __cplusplus
}
#endif
