#pragma once

#include "Baselib_ErrorState.h"
#include "Baselib_Memory.h"

#ifdef __cplusplus
BASELIB_C_INTERFACE
{
#endif

/*
Proposed usage:

// Create stuff
auto ctx = Baselib_FileIO_Context_Create(Baselib_FileIO_Context_GetPreconfiguredSchedulingPolicy(), ... );
auto buffer = Baselib_FileIO_Buffer_Register(...);
auto cq = Baselib_FileIO_CompletionQueue_Create(ctx, 100, ...);

// Schedule async operations
Baselib_FileIO_File f = Baselib_FileIO_File_OpenAsync(ctx, "hello.txt", ...);
Baselib_FileIO_File_ReadAsync(ctx, f, ..., cq, ...);
Baselib_FileIO_File_ReadAsync(ctx, f, ..., cq, ...);
Baselib_FileIO_File_ReadAsync(ctx, f, ..., cq, ...);
Baselib_FileIO_File_ReadAsync(ctx, f, ..., cq, ...);
Baselib_FileIO_File_ReadAsync(ctx, f, ..., cq, ...);
Baselib_FileIO_File_ReadAsync(ctx, f, ..., cq, ...);
Baselib_FileIO_File_CloseAsync(ctx, f, ..., cq, ...);

// From I/O thread
Baselib_FileIO_Context_Pump(ctx, 1000);

// Wait for everything to complete
Baselib_FileIO_CompletionQueue_Wait(ctx, cq, true);

// Process results
Baselib_FileIO_CompletionQueue_Result r;
while(Baselib_FileIO_CompletionQueue_Dequeue(ctx, cq, &r, 1))
{
    // Do something
}

// Free stuff
Baselib_FileIO_CompletionQueue_Free(ctx, cq);
Baselib_FileIO_Buffer_Deregister(ctx, buffer);
Baselib_FileIO_Context_Free(ctx, true);

*/

// ------------------------------------------------------------------------------------------------
// Types, structs and enums

// ------------------------------------------------------------------ Handles

typedef struct Baselib_FileIO_Context         {void* ptr;} Baselib_FileIO_Context;
typedef struct Baselib_FileIO_CompletionQueue {void* ptr;} Baselib_FileIO_CompletionQueue;
typedef struct Baselib_FileIO_File            {void* ptr;} Baselib_FileIO_File;
typedef struct Baselib_FileIO_SubmitId        {void* ptr;} Baselib_FileIO_SubmitId;
typedef uintptr_t Baselib_FileIO_Buffer_Id;

static const Baselib_FileIO_Context         Baselib_FileIO_Context_Invalid         = { NULL };
static const Baselib_FileIO_CompletionQueue Baselib_FileIO_CompletionQueue_Invalid = { NULL };
static const Baselib_FileIO_File            Baselib_FileIO_File_Invalid            = { NULL };
static const Baselib_FileIO_SubmitId        Baselib_FileIO_SubmitId_Invalid        = { NULL };
static const Baselib_FileIO_Buffer_Id Baselib_FileIO_Buffer_Id_Invalid = 0;

// ------------------------------------------------------------------ File opening

typedef enum Baselib_FileIO_OpenFlags
{
    Baselib_FileIO_FileFlags_Read                = 0x01,
    Baselib_FileIO_FileFlags_Write               = 0x02,
    Baselib_FileIO_FileFlags_AllowResize         = 0x04, // This will allow writes pass EOF
    Baselib_FileIO_FileFlags_CreateIfDoesntExist = 0x08,
    Baselib_FileIO_FileFlags_CreateAlways        = 0x10
} Baselib_FileIO_OpenFlags;

typedef enum Baselib_FileIO_CreatePermissionsFlags_t
{
    Baselib_FileIO_CreatePermissionsFlags_Default         = 0x00,
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined = 0x10,
} Baselib_FileIO_CreatePermissionsFlags_t;
typedef uint64_t Baselib_FileIO_CreatePermissionsFlags;

typedef enum Baselib_FileIO_OpenShareFlags_t
{
    Baselib_FileIO_OpenShareFlags_Default         = 0x00,
    Baselib_FileIO_OpenShareFlags_PlatformDefined = 0x10,
} Baselib_FileIO_OpenShareFlags_t;
typedef uint64_t Baselib_FileIO_OpenShareFlags;

typedef struct Baselib_FileIO_OpenNativeFlags
{
    Baselib_FileIO_CreatePermissionsFlags createPermissionsFlags;
    Baselib_FileIO_OpenShareFlags         openShareFlags;
} Baselib_FileIO_OpenNativeFlags;

// include platform defined enums, if any
#include <C/Baselib_FileIO.inl.h>

// ------------------------------------------------------------------ Buffer

typedef struct Baselib_FileIO_Buffer
{
    Baselib_FileIO_Buffer_Id      id;
    Baselib_Memory_PageAllocation allocation;
} Baselib_FileIO_Buffer;

typedef struct Baselib_FileIO_BufferSlice
{
    Baselib_FileIO_Buffer_Id id;
    char*                    data; // data of the slice
    uint32_t                 size; // size of the slice
    uint64_t                 offset; // offset in main buffer
} Baselib_FileIO_BufferSlice;

// ------------------------------------------------------------------ Priority

// First we process all requests with high priority, then with normal only then all ones with low priority.
// There's no round-robin, and high priority can starve normal, and normal can starve low.
typedef enum Baselib_FileIO_Priority
{
    Baselib_FileIO_Priority_High   = 2,
    Baselib_FileIO_Priority_Normal = 0,
    Baselib_FileIO_Priority_Low    = 1,

    // Special priority for immediate execution, it will be executed ASAP.
    // If there are two immediate requests scheduled, second will be executed first.
    Baselib_FileIO_Priority_Immediate = 3
} Baselib_FileIO_Priority;

// ------------------------------------------------------------------ File Info

typedef struct Baselib_FileIO_FileInfo
{
    uint64_t size;
    // TODO ctime/mtime
    // TODO anything else?
} Baselib_FileIO_FileInfo;

// ------------------------------------------------------------------ Completion Queue

typedef enum Baselib_FileIO_CompletionQueue_ResultType
{
    Baselib_FileIO_CompletionQueue_ResultType_Invalid       = 0,
    Baselib_FileIO_CompletionQueue_ResultType_OpenFile      = 1,
    Baselib_FileIO_CompletionQueue_ResultType_ReadWriteFile = 2,
    Baselib_FileIO_CompletionQueue_ResultType_FlushFile     = 3,
    Baselib_FileIO_CompletionQueue_ResultType_FileInfo      = 4,
    Baselib_FileIO_CompletionQueue_ResultType_CloseFile     = 5
} Baselib_FileIO_CompletionQueue_ResultType;

typedef struct Baselib_FileIO_CompletionQueue_Result
{
    Baselib_FileIO_SubmitId submitId; // can be Baselib_FileIO_SubmitId_Invalid for CloseFile type.
    uintptr_t               requestUsrptr;
    Baselib_ErrorState      errorState;
    uint8_t                 type;
    union
    {
        struct
        {
            // we get file info during open
            Baselib_FileIO_FileInfo fileInfo;
        } openFile;

        struct
        {
            uint32_t bytesTransfered;
        } readWriteFile;

        struct
        {
            Baselib_FileIO_FileInfo fileInfo;
        } fileInfo;
    };
} Baselib_FileIO_CompletionQueue_Result;

// ------------------------------------------------------------------ Scheduling policy and stats

typedef struct Baselib_FileIO_Context_SchedulingPolicy
{
    // Specifies maximum amount of requests to be in-flight at any given amount of time, must be >0.
    // Testing on modern drives shows that optimal number is with-in [16, 64] range.
    uint32_t queueDepth;

    // If >0, enables batching on supported platforms.
    // On some platforms it have to be enabled to achieve optimum performance.
    uint32_t batching;

    // TODO: do we need this?
    // If true, enables chunking on supported platforms this will split huge requests into smaller ones,
    // or combine smaller requests into bigger ones.
    bool chunking;
    uint32_t chunkingSize;
    uint32_t chunkingWindowSize;
    double chunkingDelay;
} Baselib_FileIO_Context_SchedulingPolicy;

typedef struct Baselib_FileIO_Context_Statistics
{
    // Current queue depth.
    uint32_t queueDepth;

    // Total amount of operations completed and failed, can overflow.
    // This only includes read/writes.
    uint64_t ioOperationsCompleted;
    uint64_t ioOperationsFailed;

    // Total amount of bytes transfered, can overflow.
    uint64_t bytesTransfered;
} Baselib_FileIO_Context_Statistics;

// ------------------------------------------------------------------------------------------------
// Scheduling policy

// Get preconfigured scheduling policy.
// It contains empirically optimal numbers for available hardware at a time of testing.
BASELIB_API Baselib_FileIO_Context_SchedulingPolicy Baselib_FileIO_Context_GetPreconfiguredSchedulingPolicy(void);

// Given scheduling policy, returns optimal amount of IO threads to be used for Pump.
BASELIB_API uint32_t Baselib_FileIO_Context_GetOptimalAmountOfIOThreadsForPolicy(
    Baselib_FileIO_Context_SchedulingPolicy policy
);

// ------------------------------------------------------------------------------------------------
// Context

// Creates context.
BASELIB_API Baselib_FileIO_Context Baselib_FileIO_Context_Create(
    Baselib_FileIO_Context_SchedulingPolicy policy,
    uint32_t                                maxRequests // TODO: do we need this? io_uring wants amount of requests upfront
);

// Updates scheduling policy.
BASELIB_API void Baselib_FileIO_Context_UpdateSchedulingPolicy(
    Baselib_FileIO_Context                  context,
    Baselib_FileIO_Context_SchedulingPolicy policy
);

// Gets realtime statistic of context.
BASELIB_API Baselib_FileIO_Context_Statistics Baselib_FileIO_Context_GetStatistics(
    Baselib_FileIO_Context context
);

// Pumps the context.
// Should be called from multiple threads. Exact amount of threads depend on platform and scheduling config.
// Use Baselib_FileIO_Context_GetOptimalAmountOfIOThreadsForPolicy to get exact amount.
// Please note, on some platforms, this function can block for significant amount of time (>1 second).
BASELIB_API void Baselib_FileIO_Context_Pump(
    Baselib_FileIO_Context context,
    uint32_t               minimumTimeInMilliseconds
);

// Tries to cancel provided submit id.
// Return true if successfully canceled, submit id needs to be released afterwards.
// Returns false if cancelation is failed (request is in progress).
// If false is returned, we're certain that future attemps will also fail,
// because request is no longer in our control at that stage.
// Is no-op for Baselib_FileIO_SubmitId_Invalid, and returns false.
BASELIB_API bool Baselib_FileIO_TryCancel(
    Baselib_FileIO_Context  context,
    Baselib_FileIO_SubmitId id
);

// Tries to change priority of provided submit id.
// Return true if successfull.
// Returns false if failed (request is in progress).
// If false is returned, we're certain that future attemps will also fail,
// because request is no longer in our control at that stage.
// Is no-op for Baselib_FileIO_SubmitId_Invalid, and returns false.
BASELIB_API bool Baselib_FileIO_TryReprioritize(
    Baselib_FileIO_Context  context,
    Baselib_FileIO_SubmitId id,
    Baselib_FileIO_Priority newPriority
);

// Releases submit id.
// Must be called when user code doesn't need submit id anymore.
// Submit id should be treated as invalid after this.
// Is no-op for Baselib_FileIO_SubmitId_Invalid.
BASELIB_API void Baselib_FileIO_Release(
    Baselib_FileIO_Context  context,
    Baselib_FileIO_SubmitId id
);

// Frees the context.
// If waitForCompletion is set to true, will process all pending requests before exiting
BASELIB_API void Baselib_FileIO_Context_Free(
    Baselib_FileIO_Context context,
    bool                   waitForCompletion // TODO: is this needed?
);

// ------------------------------------------------------------------------------------------------
// Completion queue

BASELIB_API Baselib_FileIO_CompletionQueue Baselib_FileIO_CompletionQueue_Create(
    Baselib_FileIO_Context context,
    uint32_t               maxResults,
    Baselib_ErrorState*    errorState
);

BASELIB_API void Baselib_FileIO_CompletionQueue_Free(
    Baselib_FileIO_Context         context,
    Baselib_FileIO_CompletionQueue cq
);

// Similar to poll/epoll, waits for operations to be completed.
// Returns true if successful.
BASELIB_API bool Baselib_FileIO_CompletionQueue_Wait(
    Baselib_FileIO_Context         context,
    Baselib_FileIO_CompletionQueue cq,
    bool                           waitForAll // if false, waits for any
);
BASELIB_API bool Baselib_FileIO_CompletionQueue_TimedWait(
    Baselib_FileIO_Context         context,
    Baselib_FileIO_CompletionQueue cq,
    bool                           waitForAll, // if false, waits for any
    uint32_t                       timeoutInMilliseconds // 0 will exit immediately
);

// Dequeue results from completion queue.
// Return amount of results filled.
BASELIB_API uint32_t Baselib_FileIO_CompletionQueue_Dequeue(
    Baselib_FileIO_Context                 context,
    Baselib_FileIO_CompletionQueue         cq,
    Baselib_FileIO_CompletionQueue_Result* results,
    uint32_t                               count
);

// ------------------------------------------------------------------------------------------------
// Buffers and slices

// Registers file IO buffer
BASELIB_API Baselib_FileIO_Buffer Baselib_FileIO_Buffer_Register(
    Baselib_FileIO_Context        context,
    Baselib_Memory_PageAllocation pageAllocation,
    Baselib_ErrorState*           errorState
);

// Deregisters file IO buffer
BASELIB_API void Baselib_FileIO_Buffer_Deregister(
    Baselib_FileIO_Context context,
    Baselib_FileIO_Buffer  buffer
);

// Creates a slice of provided buffer
BASELIB_API Baselib_FileIO_BufferSlice Baselib_FileIO_BufferSlice_Create(
    Baselib_FileIO_Buffer buffer,
    uint32_t              offset,
    uint32_t              size
);

// Creates empty slice
BASELIB_API Baselib_FileIO_BufferSlice Baselib_FileIO_BufferSlice_Empty(void);

// ------------------------------------------------------------------------------------------------
// Mounts

// TODO do we need async mounts at all?
// It would be nice for stuff like Emscripten IDBFS

// ------------------------------------------------------------------------------------------------
// Files

// Async file open.
// If outSubmitId is provided, it is filled with submit id, and must be freed via Baselib_FileIO_Release.
BASELIB_API Baselib_FileIO_File Baselib_FileIO_File_Open(
    Baselib_FileIO_Context          context,
    const char*                     pathname,
    uint32_t                        openFlags,
    uint64_t                        createFileSize,      // only used if Create flag passed
    Baselib_FileIO_OpenNativeFlags* optionalNativeFlags, // optional native flags, pass nullptr to use default values
    Baselib_FileIO_CompletionQueue  cq,
    uintptr_t                       requestUsrptr,
    Baselib_FileIO_Priority         priority,
    Baselib_FileIO_SubmitId*        outSubmitId, // optional
    Baselib_ErrorState*             errorState
);

// Async file read.
BASELIB_API void Baselib_FileIO_File_Read(
    Baselib_FileIO_Context         context,
    Baselib_FileIO_File            file,
    int64_t                        offset, // negative offsets will be from the end of file
    Baselib_FileIO_BufferSlice     slice,
    Baselib_FileIO_CompletionQueue cq,
    uintptr_t                      requestUsrptr,
    Baselib_FileIO_Priority        priority,
    Baselib_FileIO_SubmitId*       outSubmitId,
    Baselib_ErrorState*            errorState
);

// Async file write.
BASELIB_API void Baselib_FileIO_File_Write(
    Baselib_FileIO_Context         context,
    Baselib_FileIO_File            file,
    int64_t                        offset,
    Baselib_FileIO_BufferSlice     slice,
    Baselib_FileIO_CompletionQueue cq,
    uintptr_t                      requestUsrptr,
    Baselib_FileIO_Priority        priority,
    Baselib_FileIO_SubmitId*       outSubmitId,
    Baselib_ErrorState*            errorState
);

// Async file flush.
// Flushes pending writes, necessary to guarantee that data is visible to other processes.
// To ensure that multiple writes are flushed: schedule writes -> wait for cq -> schedule flush with immediate priority.
BASELIB_API void Baselib_FileIO_File_Flush(
    Baselib_FileIO_Context         context,
    Baselib_FileIO_File            file,
    Baselib_FileIO_CompletionQueue cq,
    uintptr_t                      requestUsrptr,
    Baselib_FileIO_Priority        priority,
    Baselib_FileIO_SubmitId*       outSubmitId,
    Baselib_ErrorState*            errorState
);

// Async get file info.
BASELIB_API void Baselib_FileIO_File_GetFileInfo(
    Baselib_FileIO_Context         context,
    Baselib_FileIO_File            file,
    Baselib_FileIO_CompletionQueue cq,
    uintptr_t                      requestUsrptr,
    Baselib_FileIO_Priority        priority,
    Baselib_FileIO_SubmitId*       outSubmitId,
    Baselib_ErrorState*            errorState
);

// Async file close.
// File is flushed on close.
// This operation cannot be canceled.
BASELIB_API void Baselib_FileIO_File_Close(
    Baselib_FileIO_Context         context,
    Baselib_FileIO_File            file,
    bool                           force, // if true, will not wait for dependent on file operations to complete
    Baselib_FileIO_CompletionQueue cq,
    uintptr_t                      requestUsrptr,
    Baselib_ErrorState*            errorState
);

#ifdef __cplusplus
} // BASELIB_C_INTERFACE
#endif
