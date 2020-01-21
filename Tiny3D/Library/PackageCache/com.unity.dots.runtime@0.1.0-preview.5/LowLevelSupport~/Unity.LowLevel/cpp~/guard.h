#pragma once
#include <stdint.h>
#include <stdlib.h>

#ifdef DEBUG
#define GUARD_HEAP
#endif

#ifdef DEBUG
void memfail();
#define MEM_FAIL()    { memfail(); }
#define MEM_ASSERT(x) { if (!(x)) { memfail(); }}
#else
#define MEM_FAIL() {}
#define MEM_ASSERT(x) {}
#endif

#ifdef GUARD_HEAP

/* Aligned to a 16 byte size, so it doesn't impact alignment computation. */
struct GuardHeader {
    static const int PAD = 16;
    int64_t size;
    int64_t _pad;
    uint8_t front[PAD];
    uint8_t back[PAD];
};

// Pointer to the memory that will be returned; setting up the padding
// is done before this call.
void setupGuardedMemory(size_t requestedSize, void* mem);
void checkGuardedMemory(void* mem, bool poison);
#endif

