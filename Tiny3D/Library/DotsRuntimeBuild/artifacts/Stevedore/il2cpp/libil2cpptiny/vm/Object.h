#pragma once

#include "il2cpp-config.h"
#include "il2cpp-object-internals.h"

namespace tiny
{
namespace vm
{
    class LIBIL2CPP_CODEGEN_API Object
    {
    public:
        static void* Unbox(Il2CppObject* obj)
        {
            return reinterpret_cast<uint8_t*>(obj) + sizeof(Il2CppObject);
        }
    };
} /* namespace vm */
} /* namespace tiny */
