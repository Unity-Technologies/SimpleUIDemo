#pragma once

#include "Internal/PlatformDetection.h"
#include "Internal/ArchitectureDetection.h"
#include "Internal/PlatformEnvironment.h"


#ifdef BASELIB_INLINE_NAMESPACE
    #ifndef __cplusplus
        #error "BASELIB_INLINE_NAMESPACE is not available when compiling C code"
    #endif

    #define BASELIB_CPP_INTERFACE     inline namespace BASELIB_INLINE_NAMESPACE
    #define BASELIB_C_INTERFACE       BASELIB_CPP_INTERFACE
#else
    #define BASELIB_CPP_INTERFACE     extern "C++"
    #define BASELIB_C_INTERFACE       extern "C"
#endif

#if defined(BASELIB_USE_DYNAMICLIBRARY)
    #define BASELIB_API     IMPORTED_SYMBOL
#elif defined(BASELIB_DYNAMICLIBRARY)
    #define BASELIB_API     EXPORTED_SYMBOL
#else
    #define BASELIB_API
#endif


#include "Internal/BasicTypes.h"
#include "Internal/CoreMacros.h"
#include "Internal/Assert.h"
