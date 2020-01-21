#include "il2cpp-config.h"

#include "Debugger.h"
#include "vm-utils/Debugger.h"

namespace tiny
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
namespace Diagnostics
{
// Until we have il2cpp debugger, return whether a native debugger is attached
    bool Debugger::IsAttached_internal()
    {
        return il2cpp::utils::Debugger::GetIsDebuggerAttached();
    }

    bool Debugger::IsLogging()
    {
        return false;
    }

    void Debugger::Log(int32_t level, Il2CppString* category, Il2CppString* message)
    {
    }
} /* namespace Diagnostics */
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace tiny */
