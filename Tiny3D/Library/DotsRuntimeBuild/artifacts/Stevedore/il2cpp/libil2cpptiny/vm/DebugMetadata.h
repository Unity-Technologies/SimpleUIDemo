#pragma once

#include "il2cpp-config.h"
#include "il2cpp-class-internals.h"

struct TinyMethod;

namespace tiny
{
namespace vm
{
    class DebugMetadata
    {
    public:
        static void InitializeMethodsForStackTraces();
        static const TinyMethod* GetMethodNameFromMethodDefinitionIndex(MethodIndex methodDefinitionIndex);
    };
}
}
