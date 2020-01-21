#include "il2cpp-config.h"

#include "il2cpp-class-internals.h"
#include "il2cpp-object-internals.h"
#include "DebugMetadata.h"
#include "vm/StackTrace.h"
#include "vm-utils/NativeSymbol.h"

#include <vector>

#if IL2CPP_TINY_DEBUG_METADATA
extern int g_NumberOfIl2CppTinyMethods;
extern Il2CppMethodPointer g_Il2CppTinyMethodPointers[];
extern TinyMethod* g_Il2CppTinyMethods[];
#endif

namespace tiny
{
namespace vm
{
    void DebugMetadata::InitializeMethodsForStackTraces()
    {
#if IL2CPP_ENABLE_NATIVE_STACKTRACES && IL2CPP_TINY_DEBUG_METADATA
        std::vector<MethodDefinitionKey> managedMethods;
        for (int i = 0; i < g_NumberOfIl2CppTinyMethods; ++i)
        {
            MethodDefinitionKey methodKey;
            methodKey.methodIndex = i;
            methodKey.method = (Il2CppMethodPointer)g_Il2CppTinyMethodPointers[i];
            managedMethods.push_back(methodKey);
        }

        il2cpp::utils::NativeSymbol::RegisterMethods(managedMethods);
#endif
    }

    const TinyMethod* DebugMetadata::GetMethodNameFromMethodDefinitionIndex(MethodIndex methodDefinitionIndex)
    {
#if IL2CPP_TINY_DEBUG_METADATA
        IL2CPP_ASSERT(methodDefinitionIndex >= 0 && methodDefinitionIndex < g_NumberOfIl2CppTinyMethods);
        return g_Il2CppTinyMethods[methodDefinitionIndex];
#else
        return NULL;
#endif
    }
}
}
