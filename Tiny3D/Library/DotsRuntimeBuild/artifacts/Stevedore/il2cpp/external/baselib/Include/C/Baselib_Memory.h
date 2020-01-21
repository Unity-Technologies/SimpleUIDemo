#pragma once

#include "Baselib_ErrorState.h"
#include "Internal/Baselib_EnumSizeCheck.h"

#ifdef __cplusplus
BASELIB_C_INTERFACE
{
#endif

typedef enum
{
    // Max alignment that can be passed to Baselib_AlignedAlloc and Baselib_AlignedRealloc functions
    Baselib_Memory_MaxAlignment = 64 * 1024,
} Baselib_Memory_Constants;

// Information about available pages sizes.
typedef struct
{
    // Commonly used page size on this platform.
    size_t defaultPageSize;

    // pageSizesLen valid page sizes, ordered from small to large.
    size_t pageSizes[6];
    size_t pageSizesLen;
} Baselib_Memory_PageSizeInfo;

typedef struct
{
    void*   ptr;
    size_t  pageSize;
    size_t  pageCount;
} Baselib_Memory_PageAllocation;

static const Baselib_Memory_PageAllocation Baselib_Memory_PageAllocation_Invalid = {0, 0, 0};

// Fills out a Baselib_Memory_PageSizeInfo struct.
//
// \param outPagesSizeInfo:      Pointer to page size info struct. Passing 'nullptr' will return immediately.
BASELIB_API void Baselib_Memory_GetPageSizeInfo(Baselib_Memory_PageSizeInfo* outPagesSizeInfo);


// Allocates memory using a system allocator like malloc.
//
// Return value is guaranteed to be a unique pointer. This is true for zero sized allocations as well.
// Allocation failures trigger process abort.
BASELIB_API void* Baselib_Memory_Allocate(size_t size);

// Reallocates memory previously allocated by Baselib_Memory_Allocate or Baselib_Memory_Reallocate.
//
// Reallocating an already freed pointer or a pointer that was not previously allocated by Baselib_Memory_Allocate or Baselib_Memory_Reallocate leads to undefined behavior.
//
// Passing `nullptr` yield the same result as calling Baselib_Memory_Allocate.
//
// Return value is guaranteed to be a unique pointer. This is true for zero sized allocations as well.
// Allocation failures trigger process abort.
BASELIB_API void* Baselib_Memory_Reallocate(void* ptr, size_t newSize);

// Frees memory allocated by Baselib_Memory_Allocate Baselib_Memory_Reallocate.
//
// Passing `nullptr` result in a no-op.
// Freeing an already freed pointer or a pointer that was not previously allocated by Baselib_Memory_Allocate or Baselib_Memory_Reallocate leads to undefined behavior.
BASELIB_API void Baselib_Memory_Free(void* ptr);

// Allocates memory using a system allocator like malloc and guarantees that the returned pointer is aligned to the specified alignment.
//
// Alignment needs to be a power of two which is also a multiple of sizeof(void *) but less or equal to Baselib_Memory_MaxAlignment.
//
// Return value is guaranteed to be a unique pointer. This is true for zero sized allocations as well.
// Allocation failures or invalid alignments will trigger process abort.
BASELIB_API void* Baselib_Memory_AlignedAllocate(size_t size, size_t alignment);

// Reallocates memory previously allocated by Baselib_Memory_AlignedAllocate or Baselib_Memory_AlignedReallocate.
//
// Alignment needs to be a power of two which is also a multiple of sizeof(void *) but less or equal to Baselib_Memory_MaxAlignment.
// Reallocating an already freed pointer or a pointer that was not previously allocated by Baselib_Memory_AlignedAllocate or Baselib_Memory_AlignedReallocate leads to undefined behavior.
//
// Passing `nullptr` yield the same result as calling Baselib_Memory_AlignedAllocate.
//
// Return value is guaranteed to be a unique pointer. This is true for zero sized allocations as well.
// Allocation failures or invalid alignments will trigger process abort.
BASELIB_API void* Baselib_Memory_AlignedReallocate(void* ptr, size_t newSize, size_t alignment);

// Frees memory allocated by Baselib_Memory_AlignedAllocate or Baselib_Memory_AlignedReallocate.
//
// Freeing an already freed pointer or a pointer that was not previously allocated by Baselib_Memory_AlignedAllocate or Baselib_Memory_AlignedReallocate leads to undefined behavior.
//
// Passing `nullptr` result in a no-op.
BASELIB_API void Baselib_Memory_AlignedFree(void* ptr);


// Page state options
typedef enum
{
    // The page are in a reserved state and any access will cause a seg-fault/access violation.
    // On some platforms that support this state this may be just a hint to the OS and there is no guarantee pages in this state behave differently from Baselib_Memory_PageState_NoAccess.
    // The Baselib implementation does a best effort and tries to ensure as best as possible that pages in this state are not commited.
    Baselib_Memory_PageState_Reserved             = 0x00,

    // This is a no access page and will cause a seg-fault/access violation when accessed.
    Baselib_Memory_PageState_NoAccess             = 0x01,
    // The memory can only be read.
    Baselib_Memory_PageState_ReadOnly             = 0x02,
    // The memory can be read and written.
    Baselib_Memory_PageState_ReadWrite            = 0x04,

    // The memory can be used to execute code and can be read.
    Baselib_Memory_PageState_ReadOnly_Executable  = 0x10 | Baselib_Memory_PageState_ReadOnly,
    // The memory can be used to execute code and can be both read and written.
    Baselib_Memory_PageState_ReadWrite_Executable = 0x10 | Baselib_Memory_PageState_ReadWrite,
} Baselib_Memory_PageState;
BASELIB_ENUM_ENSURE_ABI_COMPATIBILITY(Baselib_Memory_PageState);

// Allocates a given number of memory pages and guarantees that the returned pointer is aligned to specified multiple of the page size.
//
// Large alignments may lead to a significantly higher use of virtual address space than the amount of memory requested.
// This may result in an aligned page allocation to fail where a less/non-aligned allocation would succeed.
// Note that this is especially common in 32bit applications but a platform may impose additional restrictions on the size of its virtual address space.
// Whether a page allocation is pure virtual address space or already commited memory depends on the platform and passed page state flag.
//
// \param pageCount                      Number of pages requested (each will have pageSize size)
// \param alignmentInMultipleOfPageSize  Specified alignment in multiple of page sizes (a value of 1 implies alignment to page size).
//                                       Value needs to be larger than zero and a power of two, otherwise UnsupportedAlignment will be raised.
// \param pageState:                     In which state the pages should be. Certain values may raise UnsupportedPageState on certain platforms.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidPageSize:         Page size doesn't match any of the available page sizes (see Baselib_Memory_GetPageSizeInfo).
// - Baselib_ErrorCode_InvalidPageCount:        Requested number of pages is zero.
// - Baselib_ErrorCode_UnsupportedAlignment:    Requested alignment is invalid.
// - Baselib_ErrorCode_UnsupportedPageState:    The underlying system doesn't support the requested page state (see Baselib_Memory_PageState).
// - Baselib_ErrorCode_OutOfMemory:             If there is not enough continuous address space available, or physical memory space when acquiring committed memory.
//
// \returns Page allocation info or Baselib_Memory_PageAllocation_Invalid in case of an error.
BASELIB_API Baselib_Memory_PageAllocation Baselib_Memory_AllocatePages(size_t pageSize, size_t pageCount, size_t alignmentInMultipleOfPageSize, Baselib_Memory_PageState pageState, Baselib_ErrorState* errorState);

// Releases the previously allocated pages (using either Baselib_Memory_AllocatePages)
//
// A single call of ReleasePages must encompass all pages that were originally allocated with a single call of AllocatePages.
// Passing Baselib_Memory_PageAllocation with a nullptr or a zero page count result in a no-op.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidAddressRange:     Address of the first page or number of pages doesn't match a previous allocation. This may also trigger undefined behavior.
// - Baselib_ErrorCode_InvalidPageSize:         If page size doesn't match a previous allocation at `pageAllocation.ptr`.
//
// Implementation note:
// We could be able to allow granular ReleasePages call, but even then only in the _allocation granularity_ which might be different from the page size.
// (e.g. windows page size 4k allocation granularity 64k)
BASELIB_API void Baselib_Memory_ReleasePages(Baselib_Memory_PageAllocation pageAllocation, Baselib_ErrorState* errorState);

// Modifies the page state property of an already allocated virtual address range.
//
// It is possible to modify only some of the pages allocated by Baselib_Memory_AllocatePages.
// Passing `nullptr` or a zero page count result in a no-op.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidAddressRange:     Address of the first page or number of pages doesn't match a previous allocation. This may also trigger undefined behavior.
// - Baselib_ErrorCode_InvalidPageSize:         If page size doesn't match the previous allocation at `addressOfFirstPage`.
// - Baselib_ErrorCode_UnsupportedPageState:    The underlying system doesn't support the requested page state (see Baselib_Memory_PageState).
BASELIB_API void Baselib_Memory_SetPageState(void* addressOfFirstPage, size_t pageSize, size_t pageCount, Baselib_Memory_PageState pageState, Baselib_ErrorState* errorState);

#ifdef __cplusplus
} // BASELIB_C_INTERFACE
#endif
