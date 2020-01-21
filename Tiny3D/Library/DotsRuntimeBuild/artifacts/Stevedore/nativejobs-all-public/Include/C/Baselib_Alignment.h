#pragma once

// C99 versions of align_of, align_as
#ifndef ALIGN_OF
    #define ALIGN_OF(TYPE_) COMPILER_ALIGN_OF(TYPE_)
#endif

#ifndef ALIGN_AS
    #define ALIGN_AS(ALIGNMENT_) COMPILER_ALIGN_AS(ALIGNMENT_)
#endif

// Convenience macros to declare aligned types
#define ALIGNED(SIZE_, TYPE_) ALIGN_AS(SIZE_) TYPE_

#if PLATFORM_ARCH_64
    #define ALIGNED_AS_POINTER(TYPE_) ALIGN_AS(8) TYPE_
#else
    #define ALIGNED_AS_POINTER(TYPE_) ALIGN_AS(4) TYPE_
#endif
