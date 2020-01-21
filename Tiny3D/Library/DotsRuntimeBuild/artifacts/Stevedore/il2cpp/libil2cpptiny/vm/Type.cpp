#include "Reflection.h"
#include "Type.h"
#include "il2cpp-object-internals.h"

namespace tiny
{
namespace vm
{
    Il2CppReflectionType* Type::GetTypeFromHandle(intptr_t handle)
    {
        return vm::Reflection::GetTypeObject(handle);
    }
} /* namespace vm */
} /* namespace il2cpp */
