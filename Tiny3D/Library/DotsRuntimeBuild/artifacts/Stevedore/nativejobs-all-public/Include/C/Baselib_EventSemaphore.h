#pragma once

// Baselib_EventSemaphore

// In computer science, an event (also called event semaphore) is a type of synchronization mechanism that is used to indicate to waiting processes when a
// particular condition has become true.
// An event is an abstract data type with a boolean state and the following operations:
// * wait - when executed, causes the suspension of the executing process until the state of the event is set to true. If the state is already set to true has no effect.
// * set - sets the event's state to true, release all waiting processes.
// * clear - sets the event's state to false.
//
// "Event (synchronization primitive)", Wikipedia: The Free Encyclopedia
// https://en.wikipedia.org/w/index.php?title=Event_(synchronization_primitive)&oldid=781517732

#include "Baselib_Atomic.h"
#include "Baselib_CappedSemaphore.h"

enum Detail_Baselib_EventSemaphore_State
{
    Detail_Baselib_EventSemaphore_SET,
    Detail_Baselib_EventSemaphore_UNSET
};
typedef struct Baselib_EventSemaphore
{
    Baselib_CappedSemaphore semaphore;
    int8_t                  state;
} Baselib_EventSemaphore;

// Creates an event semaphore synchronization primitive. Initial state of event is unset.
//
// If there are not enough system resources to create a semaphore, process abort is triggered.
//
// \returns     A struct representing a semaphore instance. Use Baselib_EventSemaphore_Free to free the semaphore.
static inline Baselib_EventSemaphore Baselib_EventSemaphore_Create(void)
{
    Baselib_EventSemaphore semaphore = {Baselib_CappedSemaphore_Create(0), Detail_Baselib_EventSemaphore_UNSET};
    return semaphore;
}

// Try to acquire semaphore.
//
// When semaphore is acquired this function is guaranteed to emit an acquire barrier.
//
// \returns true if event is set, false other wise.
COMPILER_WARN_UNUSED_RESULT
static inline bool Baselib_EventSemaphore_TryAcquire(Baselib_EventSemaphore* semaphore)
{
    return Baselib_atomic_load_8_acquire(&semaphore->state) == Detail_Baselib_EventSemaphore_SET;
}

// Acquire semaphore.
//
// This function is guaranteed to emit an acquire barrier.
static inline void Baselib_EventSemaphore_Acquire(Baselib_EventSemaphore* semaphore)
{
    if (!Baselib_EventSemaphore_TryAcquire(semaphore))
        Baselib_CappedSemaphore_Acquire(&semaphore->semaphore);
}

// Try to acquire semaphore.
//
// If event is set this function return true, otherwise the thread will wait for event to be set or for release to be called.
//
// When semaphore is acquired this function is guaranteed to emit an acquire barrier.
//
// Acquire with a zero timeout differs from TryAcquire in that TryAcquire is guaranteed to be a user space operation
// while Acquire may enter the kernel and cause a context switch.
//
// Timeout passed to this function may be subject to system clock resolution.
// If the system clock has a resolution of e.g. 16ms that means this function may exit with a timeout error 16ms earlier than originally scheduled.
//
// \returns     true if semaphore was acquired.
COMPILER_WARN_UNUSED_RESULT
static inline bool Baselib_EventSemaphore_TryTimedAcquire(Baselib_EventSemaphore* semaphore, const uint32_t timeoutInMilliseconds)
{
    return Baselib_EventSemaphore_TryAcquire(semaphore) || Baselib_CappedSemaphore_TryTimedAcquire(&semaphore->semaphore, timeoutInMilliseconds);
}

// Release up to `count` number of threads.
//
// If there are threads waiting, then up to `count` number of threads are released without changing the state of the event.
// I.e if the event was unset it will remain unset.
//
// When threads are released this function is guaranteed to emit a release barrier.
//
// \returns     number of woken threads.
static inline uint16_t Baselib_EventSemaphore_Release(Baselib_EventSemaphore* semaphore, const uint16_t count)
{
    return Baselib_CappedSemaphore_Release(&semaphore->semaphore, count);
}

// Set event
//
// Setting the event will cause all waiting threads to wakeup. And will let all future acquiring threads through until Baselib_EventSemaphore_Reset is called.
//
// Guaranteed to emit a release barrier.
//
// \returns     number of woken threads.
static inline uint16_t Baselib_EventSemaphore_Set(Baselib_EventSemaphore* semaphore)
{
    Baselib_atomic_store_8_release(&semaphore->state, Detail_Baselib_EventSemaphore_SET);
    return Baselib_CappedSemaphore_Release(&semaphore->semaphore, UINT16_MAX);
}

// Reset event
//
// Resetting the event will cause all future acquiring threads to enter a wait state.
//
// Guaranteed to emit a release barrier.
static inline void Baselib_EventSemaphore_Reset(Baselib_EventSemaphore* semaphore)
{
    Baselib_atomic_store_8_release(&semaphore->state, Detail_Baselib_EventSemaphore_UNSET);
}

// Reclaim resources and memory held by the semaphore.
//
// If threads are waiting on the semaphore, calling free may trigger an assert and may cause process abort.
// Calling this function with a nullptr result in a no-op
static inline void Baselib_EventSemaphore_Free(Baselib_EventSemaphore* semaphore)
{
    if (!semaphore)
        return;
    Baselib_CappedSemaphore_Free(&semaphore->semaphore);
}
