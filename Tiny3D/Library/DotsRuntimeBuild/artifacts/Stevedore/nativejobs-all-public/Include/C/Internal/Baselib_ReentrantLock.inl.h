#pragma once

#include "../Baselib_Lock.h"
#include "../Baselib_StaticAssert.h"
#include "../Baselib_Alignment.h"
#include "../Baselib_Thread.h"

typedef struct Baselib_ReentrantLock
{
    Baselib_Lock       lock;
    Baselib_Thread_Id  owner;
    int32_t            count;
} Baselib_ReentrantLock;

BASELIB_STATIC_ASSERT((ALIGN_OF(Baselib_ReentrantLock) + offsetof(Baselib_ReentrantLock, owner)) % sizeof(Baselib_Thread_Id) == 0, "Baselib_ReentrantLock::owner is not aligned for atomic use");
BASELIB_STATIC_ASSERT((ALIGN_OF(Baselib_ReentrantLock) + offsetof(Baselib_ReentrantLock, count)) % sizeof(int32_t) == 0, "Baselib_ReentrantLock::count is not aligned for atomic use");

static inline Baselib_ReentrantLock Baselib_ReentrantLock_Create(void)
{
    Baselib_ReentrantLock lock = {Baselib_Lock_Create(), Baselib_Thread_InvalidId, 0};
    return lock;
}

COMPILER_WARN_UNUSED_RESULT
static inline bool Baselib_ReentrantLock_TryAcquire(Baselib_ReentrantLock* lock)
{
    const Baselib_Thread_Id currentThreadId = Baselib_Thread_GetCurrentThreadId();
    const Baselib_Thread_Id lockOwner       = Baselib_atomic_load_ptr_relaxed(&lock->owner);
    if (OPTIMIZER_LIKELY(currentThreadId != lockOwner))
    {
        if (!Baselib_Lock_TryAcquire(&lock->lock))
            return false;
        lock->owner = currentThreadId;
        lock->count = 1;
        return true;
    }
    lock->count++;
    return true;
}

static inline void Baselib_ReentrantLock_Acquire(Baselib_ReentrantLock* lock)
{
    const Baselib_Thread_Id currentThreadId = Baselib_Thread_GetCurrentThreadId();
    const Baselib_Thread_Id lockOwner       = Baselib_atomic_load_ptr_relaxed(&lock->owner);
    if (OPTIMIZER_LIKELY(currentThreadId != lockOwner))
    {
        Baselib_Lock_Acquire(&lock->lock);
        lock->owner = currentThreadId;
        lock->count = 1;
        return;
    }
    lock->count++;
}

COMPILER_WARN_UNUSED_RESULT
static inline bool Baselib_ReentrantLock_TryTimedAcquire(Baselib_ReentrantLock* lock, const uint32_t timeoutInMilliseconds)
{
    const Baselib_Thread_Id currentThreadId = Baselib_Thread_GetCurrentThreadId();
    const Baselib_Thread_Id lockOwner       = Baselib_atomic_load_ptr_relaxed(&lock->owner);
    if (OPTIMIZER_LIKELY(currentThreadId != lockOwner))
    {
        if (!Baselib_Lock_TryTimedAcquire(&lock->lock, timeoutInMilliseconds))
            return false;
        lock->owner = currentThreadId;
        lock->count = 1;
        return true;
    }
    lock->count++;
    return true;
}

static inline void Baselib_ReentrantLock_Release(Baselib_ReentrantLock* lock)
{
    if (lock->count > 0)
    {
        BaselibAssert(Baselib_atomic_load_ptr_relaxed(&lock->owner) == Baselib_Thread_GetCurrentThreadId(), "A recursive lock can only be unlocked by the locking thread");
        if (OPTIMIZER_LIKELY(lock->count == 1))
        {
            lock->owner = Baselib_Thread_InvalidId;
            lock->count = 0;
            Baselib_Lock_Release(&lock->lock);
            return;
        }
        lock->count--;
    }
}

static inline void Baselib_ReentrantLock_Free(Baselib_ReentrantLock* lock)
{
    if (!lock)
        return;
    Baselib_Lock_Free(&lock->lock);
}
