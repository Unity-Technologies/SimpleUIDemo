#include <windows.h>

typedef BOOL (WINAPI* DllMainFunc)(HINSTANCE hinstDLL, DWORD reason, LPVOID lpvReserved);

static DllMainFunc s_Libil2cppDllMain;

__declspec(dllexport) void Libil2cppLackeySetDllMain(DllMainFunc dllMain)
{
    s_Libil2cppDllMain = dllMain;
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD reason, LPVOID lpvReserved)
{
    DllMainFunc libil2cppDllMain = s_Libil2cppDllMain;
    if (libil2cppDllMain != NULL)
        return libil2cppDllMain(hinstDLL, reason, lpvReserved);

    return TRUE;
}
