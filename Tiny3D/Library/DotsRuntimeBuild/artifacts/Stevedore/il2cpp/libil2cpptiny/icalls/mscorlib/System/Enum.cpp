#include "il2cpp-config.h"

#include "Enum.h"
#include "vm/Runtime.h"

namespace tiny
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
    bool Enum::TinyEnumEquals(Il2CppObject* left, Il2CppObject* right)
    {
        vm::Runtime::FailFast("System.Enum::Equals should never be called. IL2CPP should remap this to a call in the runtime code.");
        return false;
    }
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace tiny */
