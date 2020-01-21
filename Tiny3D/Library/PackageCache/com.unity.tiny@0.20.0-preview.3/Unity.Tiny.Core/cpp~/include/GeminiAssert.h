#pragma once

// Assert macros

#include "Log.h"

typedef int (*AssertFailureFn)(const char* msg, const char* expr, const char* file, unsigned int line);

#if !defined(__EMSCRIPTEN__)
    extern thread_local bool tHandlingAssert;
    DOTS_EXPORT(void) InitAssertHandling();
    DOTS_EXPORT(bool) GetHandlingAssert();
    DOTS_EXPORT(void) SetHandlingAssert(bool v);
#else
    #include <pthread.h>

    extern pthread_key_t tHandlingAssert;
    extern pthread_once_t tAssertKeyOnce;
    void CreateTLSKey();

    #define InitAssertHandling() pthread_once(&tAssertKeyOnce, CreateTLSKey)
    #define GetHandlingAssert() pthread_getspecific(tHandlingAssert)
    #define SetHandlingAssert(v) pthread_setspecific(tHandlingAssert, reinterpret_cast<const void*>(v))
#endif

#if defined(DEBUG)
void PushAssertionFailureHandler(AssertFailureFn fn);
void PopAssertionFailureHandler();

// Debug or development get full stacks and files
#define AlwaysAssert(expr, ...)                                                                                        \
    do {                                                                                                               \
        InitAssertHandling();                                                                                          \
        if (!static_cast<bool>(expr) && !GetHandlingAssert()) {                                                        \
            SetHandlingAssert(true);                                                                                   \
            ::ut::assertFailed(#expr, __FILE__, __LINE__, __VA_ARGS__);                                                \
            SetHandlingAssert(false);                                                                                  \
        }                                                                                                              \
    } while (0)
#else
inline void
PushAssertionFailureHandler(AssertFailureFn fn)
{
}
inline void
PopAssertionFailureHandler()
{
}

// Other builds (full release) just gets a line number
#define AlwaysAssert(expr, ...)                                                                                        \
    do {                                                                                                               \
        if (!static_cast<bool>(expr)) {                                                                                \
            ::ut::assertFailed(#expr, 0, __LINE__, __VA_ARGS__);                                                       \
        }                                                                                                              \
    } while (0)
#endif

// not reached asserts are always compiled in
#define AssertNotReached(...) AlwaysAssert(false, "" __VA_ARGS__)
#define ReleaseAssert(expr, ...) AlwaysAssert(expr, "" __VA_ARGS__)

#if defined(DEBUG)
#define Assert(expr, ...) AlwaysAssert(expr, "" __VA_ARGS__)
#else
#define Assert(...)                                                                                                    \
    do {                                                                                                               \
    } while (0)
#endif

#define CompileTimeAssert(expression, message) static_assert(expression, message)
#define CompileTimeAssertArraySize(array, size)                                                                        \
    CompileTimeAssert(ARRAY_SIZE(array) == (size), "Wrong number of elements in array " #array)
