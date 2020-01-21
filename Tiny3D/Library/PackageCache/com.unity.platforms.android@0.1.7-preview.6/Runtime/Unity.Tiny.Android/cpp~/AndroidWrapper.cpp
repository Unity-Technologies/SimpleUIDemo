#include <Unity/Runtime.h>

#include <dlfcn.h>
#include <jni.h>
#include <android/log.h>
#include <android/asset_manager.h>
#include <android/asset_manager_jni.h>
#include <android/native_window_jni.h>
#include <android/window.h>
#include <unistd.h>
#include <stdlib.h>
#include <stdio.h>
#include <sys/stat.h>
#include <math.h>
#include <time.h>
#include <vector>
#include <string>

static void* m_libmain = NULL;
static bool shouldClose = false;
static int windowW = 0;
static int windowH = 0;
static AAssetManager *nativeAssetManager = NULL;
static ANativeWindow *nativeWindow = NULL;
// input
static std::vector<int> touch_info_stream;
static std::vector<int> key_stream;
// c# delegates
static bool (*raf)() = 0;
static void (*pausef)(int) = 0;
static void (*destroyf)() = 0;

DOTS_EXPORT(bool)
init_android() {
    __android_log_print(ANDROID_LOG_INFO, "AndroidWrapper", "Android C Init\n");
    return true;
}

DOTS_EXPORT(void)
getWindowSize_android(int *width, int *height) {
    *width = windowW;
    *height = windowH;
}

DOTS_EXPORT(void)
getScreenSize_android(int *width, int *height) {
    *width = windowW;
    *height = windowH;
}

DOTS_EXPORT(void)
getFramebufferSize_android(int *width, int *height) {
    *width = windowW;
    *height = windowH;
}

DOTS_EXPORT(void)
getWindowFrameSize(int *left, int *top, int *right, int *bottom) {
    *left = *top = 0;
    *right = windowW;
    *bottom = windowH;
}

DOTS_EXPORT(void)
shutdown_android(int exitCode) {
    // BS call something to kill app
    raf = 0;
}

DOTS_EXPORT(void)
resize_android(int width, int height) {
    //glfwSetWindowSize(mainWindow, width, height);
    windowW = width;
    windowH = height;
}

DOTS_EXPORT(bool)
messagePump_android() {
    /*if (!mainWindow || shouldClose)
        return false;
    glfwMakeContextCurrent(mainWindow);
    glfwPollEvents();*/
    return !shouldClose;
}

DOTS_EXPORT(double)
time_android() {
    static double start_time = -1;
    struct timespec res;
    clock_gettime(CLOCK_REALTIME, &res);
    double t = res.tv_sec + (double) res.tv_nsec / 1e9;
    if (start_time < 0) {
        start_time = t;
    }
    return t - start_time;
}

DOTS_EXPORT(bool)
rafcallbackinit_android(bool (*func)()) {
    if (raf)
        return false;
    raf = func;
    return true;
}

DOTS_EXPORT(bool)
pausecallbacksinit_android(void (*func)(int)) {
    if (pausef)
        return false;
    pausef = func;
    return true;
}

DOTS_EXPORT(bool)
destroycallbacksinit_android(void (*func)()) {
    if (destroyf)
        return false;
    destroyf = func;
    return true;
}

DOTS_EXPORT(const int*)
get_touch_info_stream_android(int *len) {
    if (len == NULL)
        return NULL;
    *len = (int)touch_info_stream.size();
    return touch_info_stream.data();
}

DOTS_EXPORT(const int *)
get_key_stream_android(int *len)
{
    *len = (int)key_stream.size();
    return key_stream.data();
}

DOTS_EXPORT(void)
reset_android_input()
{
    touch_info_stream.clear();
    key_stream.clear();
}

DOTS_EXPORT(int64_t)
get_native_window_android() {
    return (int64_t)nativeWindow ;
}

extern "C"
JNIEXPORT void JNICALL Java_com_unity3d_tinyplayer_UnityTinyAndroidJNILib_start(JNIEnv * env, jobject obj, jstring name)
{
    typedef void(*fp_main)();
    fp_main mainfunc;
    const char* mainlib = env->GetStringUTFChars(name, NULL);
    __android_log_print(ANDROID_LOG_INFO, "AndroidWrapper", "mainlib name: %s", mainlib);
    m_libmain = dlopen(mainlib, RTLD_NOW | RTLD_LOCAL);
    env->ReleaseStringUTFChars(name, mainlib);
    if (m_libmain)
    {
        mainfunc = reinterpret_cast<fp_main>(dlsym(m_libmain, "start"));
        if (mainfunc)
        {
            mainfunc();
            return;
        }
    }
    __android_log_print(ANDROID_LOG_INFO, "AndroidWrapper", "%s", dlerror());
}

extern "C"
JNIEXPORT void* loadAssetInternal(const char *path, int *size)
{
    AAsset* asset = AAssetManager_open(nativeAssetManager, path, AASSET_MODE_STREAMING);
    if (asset == NULL)
    {
        __android_log_print(ANDROID_LOG_INFO, "AndroidWrapper", "can't read asset %s", path);
        return NULL;
    }
    else
    {
        *size = (int)AAsset_getLength(asset);
        void* data = malloc(*size);
        unsigned char *ptr = (unsigned char*)data;
        int remaining = (int)AAsset_getRemainingLength(asset);
        int nb_read = 0;
        while (remaining > 0)
        {
            nb_read = AAsset_read(asset, ptr, 1000 * 1024); // 1Mb is maximum chunk size for compressed assets
            if (nb_read > 0) ptr += nb_read;
            remaining = AAsset_getRemainingLength64(asset);
        }
        AAsset_close(asset);
        return data;
    }
}

extern "C"
JNIEXPORT void JNICALL Java_com_unity3d_tinyplayer_UnityTinyAndroidJNILib_setAssetManager(JNIEnv* env, jobject obj, jobject assetManager) 
{
    __android_log_print(ANDROID_LOG_INFO, "AndroidWrapper", "UnityTinyAndroidJNILib_setAssetManager\n");
    nativeAssetManager = AAssetManager_fromJava(env, assetManager);
}

extern "C"
JNIEXPORT void JNICALL Java_com_unity3d_tinyplayer_UnityTinyAndroidJNILib_init(JNIEnv* env, jobject obj, jobject surface, jint width, jint height)
{
    __android_log_print(ANDROID_LOG_INFO, "AndroidWrapper", "UnityTinyAndroidJNILib_init\n");
    if (nativeWindow != NULL)
        ANativeWindow_release(nativeWindow);
    nativeWindow = surface != NULL ? ANativeWindow_fromSurface(env, surface) : NULL;
    windowW = width;
    windowH = height;
}

extern "C"
JNIEXPORT void JNICALL Java_com_unity3d_tinyplayer_UnityTinyAndroidJNILib_step(JNIEnv* env, jobject obj)
{
    if (raf && !raf())
        shutdown_android(2);
}

extern "C"
JNIEXPORT void JNICALL Java_com_unity3d_tinyplayer_UnityTinyAndroidJNILib_pause(JNIEnv* env, jobject obj, jint paused)
{
    if (pausef)
        pausef(paused);
}

extern "C"
JNIEXPORT void JNICALL Java_com_unity3d_tinyplayer_UnityTinyAndroidJNILib_destroy(JNIEnv* env, jobject obj)
{
    if (destroyf)
        destroyf();
    if (m_libmain)
        dlclose(m_libmain);
}

extern "C"
JNIEXPORT void JNICALL Java_com_unity3d_tinyplayer_UnityTinyAndroidJNILib_touchevent(JNIEnv* env, jobject obj, jint id, jint action, jint xpos, jint ypos)
{
    touch_info_stream.push_back((int)id);
    touch_info_stream.push_back((int)action);
    touch_info_stream.push_back((int)xpos);
    touch_info_stream.push_back(windowH - 1 - (int)ypos);
}

extern "C"
JNIEXPORT void JNICALL Java_com_unity3d_tinyplayer_UnityTinyAndroidJNILib_keyevent(JNIEnv* env, jobject obj, jint key, jint scancode, jint action, jint mods)
{
    __android_log_print(ANDROID_LOG_INFO, "AndroidWrapper", "Key %d scancode %d action %d mods %d\n", key, scancode, action, mods);
    key_stream.push_back(key);
    key_stream.push_back(scancode);
    key_stream.push_back(action);
    key_stream.push_back(mods);
}
