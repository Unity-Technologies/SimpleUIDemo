#pragma once

#include "Baselib_ErrorCode.h"
#include "Baselib_SourceLocation.h"
#include <assert.h>

#ifdef __cplusplus
BASELIB_C_INTERFACE
{
#endif

// Native error code type.
typedef enum Baselib_ErrorState_NativeErrorCodeType_t
{
    // Native error code is not present.
    Baselib_ErrorState_NativeErrorCodeType_None = 0,

    // All platform error codes types must be bigger or equal to this value.
    Baselib_ErrorState_NativeErrorCodeType_PlatformDefined,
} Baselib_ErrorCode_NativeErrorCodeType_t;
typedef uint8_t Baselib_ErrorState_NativeErrorCodeType;

// Baselib error information.
//
// All functions that expect a pointer to a error state object will *not* allow to pass a nullptr for it
// If an error state with code other than Success is passed, the function is guaranteed to early out.
// Note that even if an error state is expected, there might be no full argument validation. For details check documentation of individual functions.
typedef struct Baselib_ErrorState
{
    Baselib_ErrorCode                      code;
    Baselib_ErrorState_NativeErrorCodeType nativeErrorCodeType;
    uint64_t                               nativeErrorCode;
    Baselib_SourceLocation                 sourceLocation;
} Baselib_ErrorState;

// Creates a new error state object that is initialized to Baselib_ErrorCode_Success.
static inline Baselib_ErrorState Baselib_ErrorState_Create(void)
{
    Baselib_ErrorState errorState;
    errorState.code = Baselib_ErrorCode_Success;
    return errorState;
}

// Resets an existing error state to success and passes it on. Passes nullptr directly on.
static inline Baselib_ErrorState* Baselib_ErrorState_Reset(Baselib_ErrorState* errorState)
{
    if (errorState)
        errorState->code = Baselib_ErrorCode_Success;
    return errorState;
}

static inline bool Baselib_ErrorState_ErrorRaised(const Baselib_ErrorState* errorState)
{
    BaselibAssert(errorState);
    return errorState->code != Baselib_ErrorCode_Success;
}

static inline void Baselib_ErrorState_RaiseError(
    Baselib_ErrorState*                    errorState,
    Baselib_ErrorCode                      errorCode,
    Baselib_ErrorState_NativeErrorCodeType nativeErrorCodeType,
    uint64_t                               nativeErrorCode,
    Baselib_SourceLocation                 sourceLocation
)
{
    if (!errorState)
        return;
    if (errorState->code != Baselib_ErrorCode_Success)
        return;
    errorState->code                = errorCode;
    errorState->nativeErrorCodeType = nativeErrorCodeType;
    errorState->nativeErrorCode     = nativeErrorCode;
    errorState->sourceLocation      = sourceLocation;
}

// Helper to raise baselib error without native error code
#define Baselib_ErrorState_RaiseErrorBaselib(errorState, errorCode) \
    Baselib_ErrorState_RaiseError(                                  \
        errorState,                                                 \
        errorCode,                                                  \
        Baselib_ErrorState_NativeErrorCodeType_None,                \
        0,                                                          \
        BASELIB_SOURCELOCATION                                      \
    )

// Writes a null terminated string containing native error code value and explanation if possible.
//
// \param errorState  Error state to explain. If null an empty string will be written into buffer.
// \param buffer      Buffer to write explanation into.
//                    If nullptr is passed, nothing will be written but function will still return correct amount of bytes.
// \param bufferLen   Length of buffer in bytes.
//                    If 0 is passed, behaviour is the same as passing nullptr as buffer.
// \param verbose     If false, only writes error code type and value.
//                    If true, adds source location if available and error explanation if available (similar to strerror).
//
// \returns the number of characters that would have been written if buffer had been sufficiently large, including the terminating null character.
BASELIB_API size_t Baselib_ErrorState_Explain(const Baselib_ErrorState* errorState, char* buffer, size_t bufferLen, bool verbose);

#include <C/Baselib_ErrorState.inl.h>

#ifdef __cplusplus
} // BASELIB_C_INTERFACE
#endif
