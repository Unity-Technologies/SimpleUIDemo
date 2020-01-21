#pragma once

#if 0
    #if UNITY_ANDROID
        #include <android/log.h>
        #define LOGE(fmt, ...) __android_log_print(ANDROID_LOG_VERBOSE, "Unity.Tiny.Audio.Native", "[audio] " #fmt "\n", ##__VA_ARGS__)
        #define ASSERT((x)) do { if (!(x)) __android_log_print(ANDROID_LOG_VERBOSE, "Unity.Tiny.Audio.Native", "[audio] ASSERT: " #x); } while(false)
    #else
        #define LOGE(fmt, ...) printf("[audio] " #fmt "\n", ##__VA_ARGS__)
        #define ASSERT((x)) do { if (!(x)) printf("[audio] ASSERT: " #x); } while(false)
    #endif
#else
    #define LOGE(fmt, ...);
    #define ASSERT(x)
#endif

