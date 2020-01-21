#pragma once

// Baselib_Monitor

// In concurrent programming, a monitor is a synchronization construct that allows threads to have both mutual exclusion and the ability to wait (block) for a
// certain condition to become true. Monitors also have a mechanism for signaling other threads that their condition has been met. A monitor consists of a
// mutex (lock) object and condition variables. A condition variable is basically a container of threads that are waiting for a certain condition.
// Monitors provide a mechanism for threads to temporarily give up exclusive access in order to wait for some condition to be met, before regaining exclusive
// access and resuming their task.
//
// "Monitor (synchronization)", Wikipedia: The Free Encyclopedia
// https://en.wikipedia.org/w/index.php?title=Monitor_(synchronization)&oldid=866340268

#include "Baselib_ErrorState.h"
#ifdef __cplusplus
BASELIB_C_INTERFACE
{
#endif

typedef struct Baselib_Monitor Baselib_Monitor;

typedef bool(*Baselib_Monitor_Condition)(void* data);

// Creates a synchronization primitive that can be used to obtain a yieldable mutually exclusive lock.
// If there are not enough system resources to create a monitor, process abort is triggered.
//
// \returns Pointer to a monitor instance. Use Baselib_Monitor_Free to free the monitor.
BASELIB_API Baselib_Monitor* Baselib_Monitor_Create(void);

// Obtain a mutually exclusive lock. The lock is not recursive i.e. can not be obtained again by the same thread.
// Some platforms will perform deadlock detection. The ones that do trigger process abort.
//
// \param monitor       A pointer to a monitor object.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument:     is null
// - Baselib_ErrorCode_InvalidState:        monitor lock is already held by this thread
BASELIB_API void Baselib_Monitor_Lock(Baselib_Monitor* monitor, Baselib_ErrorState* errorState);

// Tries to obtain a mutually exclusive lock. The lock is not recursive i.e. can not be obtained again by the same thread.
// If lock is held by another thread this function return false.
// Some platforms will perform deadlock detection. The ones that do trigger process abort.
//
// \param monitor       A pointer to a monitor object.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument:     is null
// - Baselib_ErrorCode_InvalidState:        monitor lock is already held by this thread
//
// \returns true if lock was successfully obtained, false otherwise.
BASELIB_API bool Baselib_Monitor_TryLock(Baselib_Monitor* monitor, Baselib_ErrorState* errorState);

// Unlock a mutually exclusive lock previously obtained by this thread.
//
// \param monitor       A pointer to a monitor object.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument:     monitor is null
// - Baselib_ErrorCode_InvalidState:        thread doesn't hold a lock on this monitor
BASELIB_API void Baselib_Monitor_Unlock(Baselib_Monitor* monitor, Baselib_ErrorState* errorState);

// Wait for condition to become true.
//
// To be able to call this function the calling thread must first obtain a mutually exclusive lock through Baselib_Monitor_Lock.
// While waiting for condition to become true this function will unlock the mutually exclusive lock.
// The lock will unconditionally be reaquired before this function return.
// A thread can trigger a condition check by calling Baselib_Monitor_Notify or Baselib_Monitor_NotifyAll.
// The system may decide to do condition checks even if no code calls have been made to Baselib_Monitor_Notify or Baselib_Monitor_NotifyAll.
//
// Timeout passed to this function may be subject to system clock resolution.
// If the system clock has a resolution of e.g. 16ms that means this function may exit with a timeout error 16ms earlier than originally scheduled.
//
// \param monitor        A pointer to a monitor object.
// \param timeout        Time to wait for a notification.
// \param condition      Condition that will be checked to avoid spurious wakeup
// \param conditionData  Data passed to condition function
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument:     monitor or condition is null
// - Baselib_ErrorCode_InvalidState:        current thread doesn't hold a lock on this monitor
// - Baselib_ErrorCode_Timeout:             timeout is reached
BASELIB_API void Baselib_Monitor_Wait(Baselib_Monitor* monitor, uint32_t timeoutInMilliseconds, Baselib_Monitor_Condition condition, void* conditionData, Baselib_ErrorState* errorState);

// Notify monitor that we want one waiting thread to wake up and do a condition check. See Baselib_Monitor_Wait.
// This function can be called regardless if the calling thread has previously obtained a mutually exclusive lock or not.
//
// \param monitor       A pointer to a monitor object.
// \param errorState    Possible error codes:
//   Baselib_ErrorCode_InvalidArgument - if monitor is null
BASELIB_API void Baselib_Monitor_Notify(Baselib_Monitor* monitor, Baselib_ErrorState* errorState);

// Notify monitor that we want all waiting threads to wake up and do a condition check. See Baselib_Monitor_Wait.
// This function can be called regardless if the calling thread has previously obtained a mutually exclusive lock or not.
//
// \param monitor       A pointer to a monitor object.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument:     monitor is null
BASELIB_API void Baselib_Monitor_NotifyAll(Baselib_Monitor* monitor, Baselib_ErrorState* errorState);

// Reclaim resources and memory held by the monitor.
// If a lock is held on the monitor, calling free may cause process abort.
//
// \param monitor:       A pointer to a monitor object.
BASELIB_API void Baselib_Monitor_Free(Baselib_Monitor* monitor);

#ifdef __cplusplus
} // BASELIB_C_INTERFACE
#endif
