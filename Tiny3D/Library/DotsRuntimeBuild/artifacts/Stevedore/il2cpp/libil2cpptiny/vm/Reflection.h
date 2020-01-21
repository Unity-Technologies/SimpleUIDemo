#pragma once

#include "il2cpp-config.h"

namespace tiny
{
namespace vm
{
    class LIBIL2CPP_CODEGEN_API Reflection
    {
// exported
    public:
        static Il2CppReflectionType* GetTypeObject(intptr_t handle);

// internal
    public:
        static void Initialize();
    };
} /* namespace vm */
} /* namespace il2cpp */
