#include "Reflection.h"
#include "gc/AppendOnlyGCHashMap.h"
#include "gc/GarbageCollector.h"
#include "os/ReaderWriterLock.h"
#include "utils/HashUtils.h"
#include "codegen/il2cpp-codegen.h"
#include "il2cpp-object-internals.h"

extern TinyType* g_SystemTypeTinyType;

typedef il2cpp::gc::AppendOnlyGCHashMap<const intptr_t, Il2CppReflectionType*, il2cpp::utils::PassThroughHash<intptr_t> > TypeMap;
static TypeMap* s_TypeMap;

namespace tiny
{
namespace vm
{
    static il2cpp::os::ReaderWriterLock s_ReflectionICallsLock;

    Il2CppReflectionType* Reflection::GetTypeObject(intptr_t handle)
    {
        Il2CppReflectionType* object = NULL;

        {
            il2cpp::os::ReaderWriterAutoLock lockShared(&s_ReflectionICallsLock);
            if (s_TypeMap->TryGetValue(handle, &object))
                return object;
        }

        const size_t size = sizeof(Il2CppReflectionType);
        Il2CppReflectionType* typeObject = static_cast<Il2CppReflectionType*>(il2cpp::gc::GarbageCollector::Allocate(size));
        typeObject->typeHandle = reinterpret_cast<TinyType*>(handle);
        typeObject->object.klass = g_SystemTypeTinyType;

        il2cpp::os::ReaderWriterAutoLock lockExclusive(&s_ReflectionICallsLock, true);
        if (s_TypeMap->TryGetValue(handle, &object))
            return object;

        s_TypeMap->Add(handle, typeObject);
        return typeObject;
    }

    void Reflection::Initialize()
    {
        s_TypeMap = new TypeMap();
    }
} /* namespace vm */
} /* namespace tiny */
