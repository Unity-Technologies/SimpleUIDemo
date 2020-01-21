#include "il2cpp-config.h"
#include "il2cpp-object-internals.h"
#include "TypeUniverse.h"

namespace tiny
{
namespace vm
{
    void TypeUniverse::Initialize()
    {
        uint8_t* typeUniverse = Il2CppGetTinyTypeUniverse();
        uint32_t typeCount = Il2CppGetTinyTypeUniverseTypeCount();
        const Il2CppMethodPointer* virtualMethods = Il2CppGetTinyVirtualMethodUniverse();

        uint8_t* typeUniverseCursor = typeUniverse;

#ifdef IL2CPP_TINY_DEBUG
        {
            int id = reinterpret_cast<TinyType*>(typeUniverseCursor)->GetId(); //Call GetId() once so it doesn't get removed by the compiler and can be called in the watch window of the debugger
        }
#endif
        for (uint32_t i = 0; i < typeCount; i++)
        {
            TinyType* typeInfo = reinterpret_cast<TinyType*>(typeUniverseCursor);
            typeInfo->klass = typeInfo;

            uint32_t vtableCount = typeInfo->vtableSize;
            uint32_t typeHierarchyAndInterfaceCount = typeInfo->typeHierarchySize + typeInfo->interfacesSize;

            for (uint32_t j = 0; j < vtableCount; j++)
            {
                Il2CppMethodPointer* methodPointer = &reinterpret_cast<Il2CppMethodPointer*>(typeInfo + 1)[j];
                *methodPointer = virtualMethods[*reinterpret_cast<uintptr_t*>(methodPointer)];
            }

            TinyType** typeHierarchyStart = reinterpret_cast<TinyType**>(reinterpret_cast<Il2CppMethodPointer*>(typeInfo + 1) + vtableCount);
            for (uint32_t j = 0; j < typeHierarchyAndInterfaceCount; j++)
                typeHierarchyStart[j] = reinterpret_cast<TinyType*>(typeUniverse + reinterpret_cast<uintptr_t>(typeHierarchyStart[j]));

            typeUniverseCursor += sizeof(TinyType) + sizeof(Il2CppMethodPointer) * vtableCount + sizeof(TinyType*) * typeHierarchyAndInterfaceCount + sizeof(uintptr_t) * typeInfo->NumberOfPackedInterfaceOffsetElements();
#ifdef IL2CPP_TINY_DEBUG
            typeUniverseCursor += sizeof(uintptr_t);
#endif
        }

        InitializeStringLiterals();
        InitializeSystemTypeInstance();
    }
}
}
