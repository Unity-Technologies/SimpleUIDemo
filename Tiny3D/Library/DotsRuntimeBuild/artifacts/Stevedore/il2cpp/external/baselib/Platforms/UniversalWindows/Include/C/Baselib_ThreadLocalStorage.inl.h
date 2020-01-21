#pragma once

// On UWP, old Windows SDKs (10586 and older) did not have support for Tls* functions
// so instead we used forwarded them to Fls* equivalents. Using Tls* functions directly
// with these old SDKs causes linker errors.

#include <fibersapi.h>
#include <errhandlingapi.h>

#if __cplusplus
extern "C" {
#endif

static COMPILER_FORCEINLINE void Baselib_TLS_Set(Baselib_TLS_Handle handle, uintptr_t value)
{
    BOOL success = FlsSetValue((DWORD)handle, (void*)value);
    BaselibAssert(success != 0);
}

static COMPILER_FORCEINLINE uintptr_t Baselib_TLS_Get(Baselib_TLS_Handle handle)
{
    void* result = FlsGetValue((DWORD)handle);
    // FlsGetValue might report GetLastError() == ERROR_INVALID_PARAMETER in some cases
    // even if result is not NULL, so we skip error checking here
    return (uintptr_t)result;
}

#if __cplusplus
}
#endif
