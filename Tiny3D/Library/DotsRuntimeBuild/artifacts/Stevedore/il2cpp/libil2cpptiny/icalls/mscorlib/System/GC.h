#pragma once

#include "il2cpp-config.h"

struct Il2CppObject;

namespace tiny
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
    class LIBIL2CPP_CODEGEN_API GC
    {
    public:
        static void InternalCollect(int generation);
    };
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace tiny */
