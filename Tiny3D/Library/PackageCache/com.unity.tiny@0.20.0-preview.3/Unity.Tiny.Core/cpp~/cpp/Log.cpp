#include <cstdlib>
#include <set>
#include <string>
#include <vector>
#include <stdarg.h>
#include <cstdio>
#include <functional>

#include "../include/Log.h"
#include "../include/GeminiAssert.h"
using namespace ut;

#if !defined(__EMSCRIPTEN__)
thread_local bool tHandlingAssert;
DOTS_EXPORT(void) InitAssertHandling() { }
DOTS_EXPORT(bool) GetHandlingAssert() { return tHandlingAssert; }
DOTS_EXPORT(void) SetHandlingAssert(bool v) { tHandlingAssert = v; }
#else
#include <pthread.h>

pthread_key_t tHandlingAssert;
pthread_once_t tAssertKeyOnce;
void
CreateTLSKey()
{
    pthread_key_create(&tHandlingAssert, NULL);
}

#endif

#if defined(DEBUG)
static std::vector<AssertFailureFn> sAssertFailFunctions;

void
PushAssertionFailureHandler(AssertFailureFn fn)
{
    sAssertFailFunctions.push_back(fn);
}

void
PopAssertionFailureHandler()
{
    if (sAssertFailFunctions.empty()) {
        std::abort();
    }
    sAssertFailFunctions.pop_back();
}
#endif

#if !defined(__EMSCRIPTEN__)

DOTS_CPP_EXPORT void
ut::assertFailed(const char* const exprstring, const char* const file, uint32_t line, const char* format, ...)
{
    va_list args;
    va_start(args, format);

    const size_t kMsgLen = 1024;
    char msg[kMsgLen];
    vsnprintf(msg, kMsgLen, format, args);
    msg[kMsgLen - 1] = '\0';
    va_end(args);

    // We aren't technically done handling the assert yet but we flag it as such since user callbacks could assert and
    // we may actually want those to fire, additionally we don't know if user callbacks are trapping signals or changing
    // control flow (such as in tests).
    SetHandlingAssert(false);

#if defined(DEBUG)
    for (size_t i = sAssertFailFunctions.size(); i > 0; --i) {
        auto keepGoing = sAssertFailFunctions[i - 1](msg, exprstring, file, line);
        if (!keepGoing)
            return;
    }
#endif
    printf("Assertion failed!\n");
    if (msg && msg[0]) {
        printf("    %s\n", msg);
    }
    printf("    %s  at %s:%d\n", exprstring, file, line);
    printf("\n");

    std::abort();
}

#else

#include <assert.h>

extern "C" void js_print(const char* message);

void
ut::assertFailed(const char* const exprstring, const char* const file, uint32_t line, const char* format, ...)
{
    va_list args;
    va_start(args, format);

    const size_t kMsgLen = 1024;
    char msg[kMsgLen];
    vsnprintf(msg, kMsgLen, format, args);
    msg[kMsgLen - 1] = '\0';
    va_end(args);

    SetHandlingAssert(false);

    if (msg[0]) {
        EM_ASM({ console.error(UTF8ToString($0)); }, msg);
    }
    __assert_fail(exprstring, file, line, "");
}

#endif

void
ut::logWarningString(const char* str)
{
#ifdef DEBUG
    static std::set<std::string> warnedAlready;
    std::string s2 = str;
    if (warnedAlready.find(s2) != warnedAlready.end())
        return;
    warnedAlready.insert(str);
    log(str);
#endif
}
