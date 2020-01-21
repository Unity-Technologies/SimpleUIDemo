#include "Type.h"
#include "vm/Type.h"

namespace tiny
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
    Il2CppReflectionType* Type::internal_from_handle(intptr_t ptr)
    {
        return vm::Type::GetTypeFromHandle(ptr);
    }
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace tiny */
