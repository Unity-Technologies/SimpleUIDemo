#pragma once

#ifndef EXPORTED_SYMBOL
    #define EXPORTED_SYMBOL __attribute__((visibility("default")))
#endif
#ifndef IMPORTED_SYMBOL
    #define IMPORTED_SYMBOL
#endif

#ifndef PLATFORM_FUTEX_NATIVE_SUPPORT
    #ifdef __EMSCRIPTEN_PTHREADS__
        #define PLATFORM_FUTEX_NATIVE_SUPPORT 1
    #else
        #define PLATFORM_FUTEX_NATIVE_SUPPORT 0
    #endif
#endif

// Has custom atomics instead of compiler intrinsics.
#define PLATFORM_CUSTOM_ATOMICS 1
