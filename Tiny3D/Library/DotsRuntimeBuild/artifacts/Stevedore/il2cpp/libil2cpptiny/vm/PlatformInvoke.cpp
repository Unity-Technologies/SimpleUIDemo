#include "il2cpp-config.h"

#include "Exception.h"
#include "os/LibraryLoader.h"
#include "os/MarshalAlloc.h"
#include "PlatformInvoke.h"
#include "String.h"
#include "utils/StringUtils.h"
#include "utils/MemoryUtils.h"

#include <algorithm>

namespace tiny
{
namespace vm
{
    Il2CppMethodPointer PlatformInvoke::Resolve(const PInvokeArguments& pinvokeArgs)
    {
        void* dynamicLibrary = il2cpp::os::LibraryLoader::LoadDynamicLibrary(pinvokeArgs.moduleName);
        IL2CPP_ASSERT(dynamicLibrary != NULL);

        Il2CppMethodPointer function = il2cpp::os::LibraryLoader::GetFunctionPointer(dynamicLibrary, pinvokeArgs);
        IL2CPP_ASSERT(function != NULL);

        return function;
    }

    template<typename T>
    static T minimum(T x, T y)
    {
        if (x < y)
            return x;
        return y;
    }

    char* PlatformInvoke::MarshalCSharpStringToCppString(const Il2CppChar* str, uint32_t length)
    {
        if (str == NULL)
            return NULL;

        std::string utf8String = il2cpp::utils::StringUtils::Utf16ToUtf8(str);

        char* nativeString = static_cast<char*>(il2cpp::os::MarshalAlloc::Allocate(utf8String.size() + 1));
        strcpy(nativeString, utf8String.c_str());

        return nativeString;
    }

    void PlatformInvoke::MarshalCSharpStringToFixedCppStringBuffer(const Il2CppChar* str, uint32_t length, char* buffer, uint32_t numberOfCharacters)
    {
        if (str == NULL)
        {
            *buffer = '\0';
        }
        else
        {
            std::string utf8String = il2cpp::utils::StringUtils::Utf16ToUtf8(str, numberOfCharacters - 1);
            strcpy(buffer, utf8String.c_str());
        }
    }

    Il2CppChar* PlatformInvoke::MarshalCSharpStringToCppWString(const Il2CppChar* str, uint32_t length)
    {
        uint32_t bytesToCopy = (length + 1) * sizeof(Il2CppChar);
        Il2CppChar* result = static_cast<Il2CppChar*>(il2cpp::os::MarshalAlloc::Allocate(bytesToCopy));
        il2cpp::utils::MemoryUtils::MemoryCopy(result, str, bytesToCopy);
        return result;
    }

    void PlatformInvoke::MarshalCSharpStringToFixedCppWStringBuffer(const Il2CppChar* str, uint32_t length, Il2CppChar* buffer, uint32_t numberOfCharacters)
    {
        uint32_t charactersToCopy = minimum(length, numberOfCharacters - 1);
        il2cpp::utils::MemoryUtils::MemoryCopy(buffer, str, charactersToCopy * sizeof(Il2CppChar));
        buffer[charactersToCopy] = '\0';
    }

    Il2CppString* PlatformInvoke::MarshalCppStringToCSharpStringResult(const char* value)
    {
        size_t length = il2cpp::utils::StringUtils::StrLen(value);
        Il2CppString* result = String::NewLen(static_cast<uint32_t>(length));

        for (Il2CppChar* it = result->chars, *end = result->chars + length; it != end; it++, value++)
            *it = *value;

        return result;
    }

    Il2CppString* PlatformInvoke::MarshalCppWStringToCSharpStringResult(const Il2CppChar* value)
    {
        size_t length = il2cpp::utils::StringUtils::StrLen(value);
        return String::NewLen(value, static_cast<uint32_t>(length));
    }

    void* PlatformInvoke::MarshalAllocate(size_t size)
    {
        return il2cpp::os::MarshalAlloc::Allocate(size);
    }

    void PlatformInvoke::MarshalFree(void* ptr)
    {
        il2cpp::os::MarshalAlloc::Free(ptr);
    }
} /* namespace vm */
} /* namespace tiny */
