#pragma once

#include "il2cpp-object-internals.h"
#include "il2cpp-pinvoke-support.h"
#include "utils/StringView.h"

namespace tiny
{
namespace vm
{
    class LIBIL2CPP_CODEGEN_API PlatformInvoke
    {
    public:
        static Il2CppMethodPointer Resolve(const PInvokeArguments& pinvokeArgs);

        static char* MarshalCSharpStringToCppString(const Il2CppChar* str, uint32_t length);
        static void MarshalCSharpStringToFixedCppStringBuffer(const Il2CppChar* str, uint32_t length, char* buffer, uint32_t numberOfCharacters);
        static Il2CppChar* MarshalCSharpStringToCppWString(const Il2CppChar* str, uint32_t length);
        static void MarshalCSharpStringToFixedCppWStringBuffer(const Il2CppChar* str, uint32_t length, Il2CppChar* buffer, uint32_t numberOfCharacters);
        static Il2CppString* MarshalCppStringToCSharpStringResult(const char* value);
        static Il2CppString* MarshalCppWStringToCSharpStringResult(const Il2CppChar* value);

        static void* MarshalAllocate(size_t size);
        static void MarshalFree(void* ptr);
    };
} /* namespace vm */
} /* namespace tiny */
