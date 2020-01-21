#include "guard.h"

#include <stdlib.h>
#include "string.h"
#include <stdint.h>

#ifdef DEBUG
void memfail() {
#ifdef _WIN32
    __debugbreak();
#else
    abort();
#endif
}
#endif

#ifdef GUARD_HEAP

#define GUARD_HEAP_POISON 0xec  // the underlying allocators should poison on free; but be very sure.

static void guardcheck(unsigned char *p, unsigned char x, size_t s) {
    for ( size_t i=0; i<s; i++ ) {
        if (p[i]!=x) {
            memfail();
            return;
        }
    }
}

void setupGuardedMemory(size_t size, void* mem)
{
    memset(mem, 0xbc, size);

    uint8_t* r = (uint8_t*)mem;
    GuardHeader *hstart = (GuardHeader*)(r - sizeof(GuardHeader));
    GuardHeader *hend = (GuardHeader*)(r + size);

    hstart->size = size;
    memset(hstart->front, 0xf1, sizeof(hstart->front));
    memset(hstart->back, 0xf2, sizeof(hstart->back));

    memset(hend->front, 0xa1, sizeof(hend->front));
    memset(hend->back, 0xa2, sizeof(hend->back));
    hend->size = size;
}

void checkGuardedMemory(void* mem, bool poison)
{
    uint8_t* r = (uint8_t*)mem;

    GuardHeader *hstart = (GuardHeader*)(r - sizeof(GuardHeader));
    GuardHeader *hend = (GuardHeader*)(r + hstart->size);

    if ( hstart->size != hend->size) {
        memfail();
    }

    guardcheck(hstart->front, 0xf1, sizeof(hstart->front));
    guardcheck(hstart->back, 0xf2, sizeof(hstart->back));
    guardcheck (hend->front, 0xa1, sizeof(hend->front));
    guardcheck (hend->back, 0xa2, sizeof(hend->back));

    if (poison)
        memset(mem, GUARD_HEAP_POISON, size_t(hstart->size));
}

#endif
