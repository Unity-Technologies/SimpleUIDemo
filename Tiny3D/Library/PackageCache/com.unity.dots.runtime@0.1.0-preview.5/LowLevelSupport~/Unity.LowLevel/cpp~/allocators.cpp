#include "guard.h"
#include "BumpAllocator.h"

#include <stdlib.h>
#include "string.h"
#if !UNITY_SINGLETHREADED_JOBS
#include <mutex>
#endif
#ifdef GUARD_HEAP
#include <vector>
#endif

#ifdef TRACY_ENABLE
#include "Tracy.hpp"
#endif

#ifdef _WIN32
#define DOEXPORT __declspec(dllexport)
#define CALLEXPORT __stdcall
#else
#define DOEXPORT __attribute__ ((visibility ("default")))
#define CALLEXPORT
#endif


#if INTPTR_MAX == INT64_MAX
static const int ARCH_ALIGNMENT = 16;
#elif INTPTR_MAX == INT32_MAX
static const int ARCH_ALIGNMENT = 8;
#else
#error Unknown pointer size or missing size macros!
#endif

#ifdef GUARD_HEAP
static_assert(sizeof(GuardHeader) % ARCH_ALIGNMENT == 0, "The GuardHeader should be aligned to the architecture.");
#endif

enum class Allocator
{
    // NOTE: The items must be kept in sync with Runtime/Export/Collections/NativeCollectionAllocator.h
    Invalid = 0,
    // NOTE: this is important to let Invalid = 0 so that new NativeArray<xxx>() will lead to an invalid allocation by default.
    None = 1,
    Temp = 2,
    TempJob = 3,
    Persistent = 4
};

static BumpAllocator sBumpAlloc;
static void* lastFreePtr = 0;

#ifdef GUARD_HEAP
static std::vector<void*> sBumpMem;         // Seperately tracks memory in the bump allocator, so it can be checked with Guards
#define SIZEOFGUARD sizeof(GuardHeader)
#else
#define SIZEOFGUARD 0
#endif

#if !UNITY_SINGLETHREADED_JOBS
static std::mutex sBumpAlloc_mutex;
#endif

/* How much memory to ask for, to be sure we have enough to be:
   - aligned
   - write the offset
   - write the guards

   Layout of memory:
                        Size                    Description
                        ----                    -----------
    extendedMem         [varies]                Padding to guarantee alignment.
                        ARCH_ALIGN              integer padded to the architecture alignment; 
                                                number of bytes from extendedMem to mem
    header              sizeof(GuardHeader)     memory guard; only in use of GUARD_HEAP is defined
    mem                 size requested          Memory returned to client. Aligned.
    tail                sizeof(GuardHeader)     memory guard; only in use of GUARD_HEAP is defined
*/
int64_t calcExtendedSize(int64_t size, int alignment)
{
    return alignment + ARCH_ALIGNMENT + SIZEOFGUARD + size + SIZEOFGUARD;
}

uintptr_t offsetFromMalloc(uint8_t* mem, int alignment)
{
    uintptr_t base = reinterpret_cast<uintptr_t>(mem);
    uintptr_t ptr = base + ARCH_ALIGNMENT + SIZEOFGUARD;
    ptr += alignment - 1;
    ptr &= ~(alignment - 1);
    return ptr - base;
}

extern "C"{
DOEXPORT
void* CALLEXPORT unsafeutility_malloc(int64_t size, int alignment, Allocator allocatorType)
{
    // Always allocate memory so we can disambiguate between failed memory allocation (returns null = failure) and
    // requested 0 sized allocation (should not be a failure). This matches Unity Runtime's MemoryManager's implementation,
    // including defining size 1 before any other alignment or padding is calculated.
    if (size == 0) size = 1;
#ifdef GUARD_HEAP
    // Align guards on both front and back of buffer
    size = size + (ARCH_ALIGNMENT - 1) & ~(ARCH_ALIGNMENT - 1);
#endif
    if (alignment < ARCH_ALIGNMENT) alignment = ARCH_ALIGNMENT;
    alignment = alignment + (ARCH_ALIGNMENT - 1) & ~(ARCH_ALIGNMENT - 1);

    int64_t extendedSize = calcExtendedSize(size, alignment);
    uint8_t* extendedMem = 0;
    
    if (allocatorType == Allocator::Temp) {
#if !UNITY_SINGLETHREADED_JOBS
        std::lock_guard<std::mutex> guard(sBumpAlloc_mutex);
#endif
        extendedMem = (uint8_t*) sBumpAlloc.alloc((int)extendedSize, ARCH_ALIGNMENT);        
    }
    else {
        extendedMem = (uint8_t*) malloc(size_t(extendedSize));
#if TRACY_ENABLE
        TracyAlloc(extendedMem, extendedSize);
#endif
    }
    MEM_ASSERT(extendedMem != 0);
    MEM_ASSERT((reinterpret_cast<uintptr_t>(extendedMem) & (ARCH_ALIGNMENT - 1)) == 0);

    uintptr_t offset = offsetFromMalloc(extendedMem, alignment);
    int* offsetPtr = reinterpret_cast<int*>(extendedMem + offset - ARCH_ALIGNMENT - SIZEOFGUARD);
    *offsetPtr = offset;

#ifdef GUARD_HEAP
    setupGuardedMemory(size_t(size), extendedMem + offset);
    if (allocatorType == Allocator::Temp) {
        sBumpMem.push_back(extendedMem + offset);
    }
#endif

    MEM_ASSERT((reinterpret_cast<uintptr_t>(extendedMem + offset) & (alignment - 1)) == 0);
    return extendedMem + offset;
}

DOEXPORT
void CALLEXPORT unsafeutility_assertheap(void* ptr)
{
    MEM_ASSERT(ptr);
#ifdef GUARD_HEAP
    checkGuardedMemory(ptr, false);
#endif
}

DOEXPORT
void CALLEXPORT unsafeutility_free(void* ptr, Allocator allocatorType)
{
    lastFreePtr = ptr;
    if (ptr == nullptr)
        return;

#ifdef GUARD_HEAP
    checkGuardedMemory(ptr, true);
#endif

    if (allocatorType == Allocator::Temp)
        return;

    int* offsetPtr = reinterpret_cast<int*>(static_cast<uint8_t*>(ptr) - ARCH_ALIGNMENT - SIZEOFGUARD);
    int offset = *offsetPtr;
    uint8_t* realPtr = static_cast<uint8_t*>(ptr) - offset;
#if TRACY_ENABLE
    TracyFree(realPtr);
#endif
    free(realPtr);
}

DOEXPORT
void* CALLEXPORT unsafeutility_get_last_free_ptr()
{
    // Useful debugging trick - did the resources we except to be deleted by opaque code get deleted?
    // But only reliable single-threaded.
    return lastFreePtr;
}


DOEXPORT
void CALLEXPORT unsafeutility_memset(void* destination, char value, int64_t size)
{
    memset(destination, value, static_cast<size_t>(size));
}

DOEXPORT
void CALLEXPORT unsafeutility_memclear(void* destination, int64_t size)
{
    memset(destination, 0, static_cast<size_t>(size));
}

DOEXPORT
void CALLEXPORT unsafeutility_freetemp()
{
#if !UNITY_SINGLETHREADED_JOBS
    std::lock_guard<std::mutex> guard(sBumpAlloc_mutex);
#endif

#ifdef GUARD_HEAP
    for(size_t i=0; i<sBumpMem.size(); ++i) {
        checkGuardedMemory(sBumpMem[i], true);
    }
    sBumpMem.clear();
#endif    
    sBumpAlloc.reset();
}

#define UNITY_MEMCPY memcpy
typedef uint8_t UInt8;

DOEXPORT
void CALLEXPORT unsafeutility_memcpy(void* destination, void* source, int64_t count)
{
    UNITY_MEMCPY(destination, source, (size_t)count);
}

DOEXPORT
void CALLEXPORT unsafeutility_memcpystride(void* destination_, int destinationStride, void* source_, int sourceStride, int elementSize, int64_t count)
{   
    UInt8* destination = (UInt8*)destination_;
    UInt8* source = (UInt8*)source_;
    if (elementSize == destinationStride && elementSize == sourceStride)
    {
        UNITY_MEMCPY(destination, source, static_cast<size_t>(count) * static_cast<size_t>(elementSize));
    }
    else
    {
        for (int i = 0; i != count; i++)
        {
            UNITY_MEMCPY(destination, source, elementSize);
            destination += destinationStride;
            source += sourceStride;
        }
    }
}

DOEXPORT
int32_t CALLEXPORT unsafeutility_memcmp(void* ptr1, void* ptr2, uint64_t size)
{
    return memcmp(ptr1, ptr2, (size_t)size);
}

DOEXPORT
void CALLEXPORT unsafeutility_memcpyreplicate(void* dst, void* src, int size, int count)
{
    uint8_t* dstbytes = (uint8_t*)dst;
    // TODO something smarter
    for (int i = 0; i < count; ++i)
    {
        memcpy(dstbytes, src, size);
        dstbytes += size;
    }
}

DOEXPORT
void CALLEXPORT unsafeutility_memmove(void* dst, void* src, uint64_t size)
{
    memmove(dst, src, (size_t)size);
}


typedef void (*Call_p)(void*);
typedef void (*Call_pi)(void*, int);

DOEXPORT
void CALLEXPORT unsafeutility_call_p(void* f, void* data)
{
    MEM_ASSERT(f);
    Call_p func = (Call_p) f;
    func(data);
}

DOEXPORT
void CALLEXPORT unsafeutility_call_pi(void* f, void* data, int i)
{
    MEM_ASSERT(f);
    Call_pi func = (Call_pi)f;
    func(data, i);
}

} // extern "C"
