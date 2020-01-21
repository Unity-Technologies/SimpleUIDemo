#pragma once

#include <stdint.h>
#include "il2cpp-config.h"

struct Il2CppString;
struct Il2CppArray;

namespace tiny
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
    class LIBIL2CPP_CODEGEN_API Environment
    {
    public:
        static Il2CppString* GetStackTrace_internal();
        static void FailFast_internal(Il2CppString* message);
    };
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace il2cpp */
