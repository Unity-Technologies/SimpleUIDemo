#include "il2cpp-config.h"
#include "il2cpp-object-internals.h"
#include "String.h"
#include "utils/MemoryUtils.h"
#include "gc/gc_wrapper.h"
#include "gc/GarbageCollector.h"

extern TinyType* g_StringTinyType;

namespace tiny
{
namespace vm
{
    Il2CppString* String::NewLen(uint32_t length)
    {
        size_t lengthNeeded = sizeof(Il2CppObject) + sizeof(size_t) + (length + 1) * sizeof(Il2CppChar);
        Il2CppString* str = static_cast<Il2CppString*>(il2cpp::gc::GarbageCollector::Allocate(lengthNeeded));

        str->klass = g_StringTinyType;
        str->length = length;
        return str;
    }

    Il2CppString* String::NewLen(const Il2CppChar* characters, uint32_t length)
    {
        Il2CppString* str = NewLen(length);
        il2cpp::utils::MemoryUtils::MemoryCopy(str->chars, characters, length * sizeof(Il2CppChar));
        return str;
    }
}
}
