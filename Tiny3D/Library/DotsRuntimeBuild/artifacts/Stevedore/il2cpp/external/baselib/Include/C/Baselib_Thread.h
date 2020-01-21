#pragma once

#include "Baselib_Timer.h"
#include "Baselib_ErrorState.h"

#ifdef __cplusplus
BASELIB_C_INTERFACE
{
#endif

// The minimum guaranteed number of max concurrent threads that works on all platforms.
//
// This only applies if all the threads are created with Baselib.
// In practice, it might not be possible to create this many threads either. If memory is
// exhausted, by for example creating threads with very large stacks, that might translate to
// a lower limit in practice.
// Note that on many platforms the actual limit is way higher.
static const int Baselib_Thread_MinGuaranteedMaxConcurrentThreads = 64;

typedef struct Baselib_Thread Baselib_Thread;

typedef void (*Baselib_Thread_EntryPointFunction)(void* arg);

typedef struct
{
    uint32_t uninitializedDetectionMagic;      // Don't set this, it is set by Baselib_Thread_ConfigCreate
    // Name of the created thread (optional)
    // Does not need to contain null terminator
    const char* name;
    // Length of the name (optional)
    size_t nameLen;
    // The minimum size in bytes to allocate for the thread stack. (optional)
    // If not set, a platform/system specific default stack size will be used.
    // If the value set does not conform to platform specific minimum values or alignment requirements,
    // the actual stack size used will be bigger than what was requested.
    size_t stackSize;
    // Required, this is set by calling Baselib_Thread_ConfigCreate with a valid entry point function.
    Baselib_Thread_EntryPointFunction entryPoint;
    // Argument to the entry point function, does only need to be set if entryPoint takes an argument.
    void* entryPointArgument;
} Baselib_Thread_Config;

// Unique thread id that can be used to compare different threads or stored for bookkeeping etc..
typedef intptr_t Baselib_Thread_Id;

// Baselib_Thread_Id that is guaranteed not to represent a thread
static const Baselib_Thread_Id Baselib_Thread_InvalidId = 0;

// Creates a thread configuration (defined above), which is an argument to Thread_Create further down.
//
// Always use this function to create a new configuration to ensure that it is properly initialized.
//
// \param entryPoint  The function that will be executed by the thread
BASELIB_API Baselib_Thread_Config Baselib_Thread_ConfigCreate(Baselib_Thread_EntryPointFunction entryPoint);


// Creates and starts a new thread.
//
// On some platforms the thread name is not set until the thread has begun executing, which is not guaranteed
// to have happened when the creation function returns. On some platforms there is a limit on the length of
// the thread name. If config->name is longer than that (platform dependent) limit, the name will be truncated.
//
// \param config        A pointer to a config object. This object should be constructed with Baselib_Thread_ConfigCreate
//
// Possible error codes:
// - Baselib_ErrorCode_UninitializedThreadConfig:        config is null or uninitialized
// - Baselib_ErrorCode_ThreadEntryPointFunctionNotSet:   config->entryPoint is null
// - Baselib_ErrorCode_OutOfSystemResources:             there is not enough memory to create a thread with that stack size or the system limit of number of concurrent threads has been reached
BASELIB_API Baselib_Thread* Baselib_Thread_Create(const Baselib_Thread_Config* config, Baselib_ErrorState* errorState);


// Waits until a thread has finished its execution.
//
// Also frees its resources.
// If called and completed successfully, no Baselib_Thread function can be called again on the same Baselib_Thread.
//
// \param thread                 A pointer to a thread object.
// \param timeoutInMilliseconds  Time to wait for the thread to finish
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument:       thread is null
// - Baselib_ErrorCode_ThreadCannotJoinSelf:  the thread parameter points to the current thread, i.e. the thread that is calling this function
// - Baselib_ErrorCode_Timeout:               timeout is reached before the thread has finished
BASELIB_API void Baselib_Thread_Join(Baselib_Thread* thread, uint32_t timeoutInMilliseconds, Baselib_ErrorState* errorState);


// Yields the execution context of the current thread to other threads, potentially causing a context switch.
//
// The operating system may decide to not switch to any other thread.
BASELIB_API void Baselib_Thread_YieldExecution(void);

// Return the thread id of the current thread, i.e. the thread that is calling this function
BASELIB_API Baselib_Thread_Id Baselib_Thread_GetCurrentThreadId(void);

// Return the thread id of the thread given as argument
//
// \param thread        A pointer to a thread object.
BASELIB_API Baselib_Thread_Id Baselib_Thread_GetId(Baselib_Thread* thread);


// Returns true if there is support in baselib for threads on this platform, otherwise false.
BASELIB_API bool Baselib_Thread_SupportsThreads(void);


#ifdef __cplusplus
} // BASELIB_C_INTERFACE
#endif
