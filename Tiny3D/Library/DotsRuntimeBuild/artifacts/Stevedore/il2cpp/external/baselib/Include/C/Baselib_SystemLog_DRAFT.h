#pragma once

#include "Baselib_ErrorState.h"
#include "Internal/Baselib_EnumSizeCheck.h"
#include <stdarg.h>

#ifdef __cplusplus
BASELIB_C_INTERFACE
{
#endif

typedef struct Baselib_SystemLog Baselib_SystemLog;

typedef enum
{
    Assert,
    Message,
    Data,
} Baselib_SystemLog_Type;
BASELIB_ENUM_ENSURE_ABI_COMPATIBILITY(Baselib_SystemLog_Type);

typedef enum
{
    Trace,
    Debug,
    Info,
    Warning,
    Error,
    Fatal,
} Baselib_SystemLog_Level;
BASELIB_ENUM_ENSURE_ABI_COMPATIBILITY(Baselib_SystemLog_Level);

extern Baselib_SystemLog* Baselib_SystemLog_Default;

BASELIB_API Baselib_SystemLog* Baselib_SystemLog_Open(const char* applicationIdentifier, size_t applicationIdentifierLen, const char* logIdentifier, size_t logIdentifierLen, Baselib_ErrorState* errorState);
BASELIB_API void Baselib_SystemLog_WriteMessage(Baselib_SystemLog* handle, Baselib_SystemLog_Type type, Baselib_SystemLog_Level level, const char* message, size_t messageLen, Baselib_ErrorState* errorState);
BASELIB_API void Baselib_SystemLog_WriteMessageV(Baselib_SystemLog* handle, Baselib_SystemLog_Type type, Baselib_SystemLog_Level level, const char* format, va_list arg, Baselib_ErrorState* errorState);
BASELIB_API void Baselib_SystemLog_Close(Baselib_SystemLog* handle);

#ifdef __cplusplus
} // BASELIB_C_INTERFACE
#endif
