#if (UNITY_ANDROID)
#include <dlfcn.h>
#include <jni.h>

class AssetLoader
{
    typedef void* (*fp_loadAsset)(const char* path, int *size);
    void *m_libandroid;
    fp_loadAsset m_loadAsset;

public:
    AssetLoader()
    {
        m_libandroid = dlopen("lib_unity_tiny_android.so", RTLD_NOW | RTLD_LOCAL);
        if (m_libandroid != NULL)
        {
            m_loadAsset = reinterpret_cast<fp_loadAsset>(dlsym(m_libandroid, "loadAssetInternal"));
        }
    }
    ~AssetLoader()
    {
        if (m_libandroid != NULL)
        {
            dlclose(m_libandroid);
        }
    }

    void* loadAsset(const char *path, int *size)
    {
        return m_loadAsset(path, size);
    }
};

AssetLoader sAssetLoader;

extern "C"
JNIEXPORT void* loadAsset(const char *path, int *size)
{
    return sAssetLoader.loadAsset(path, size);
}
#endif