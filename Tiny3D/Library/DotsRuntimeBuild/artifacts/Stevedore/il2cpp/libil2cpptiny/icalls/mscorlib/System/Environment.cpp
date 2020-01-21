#include "il2cpp-config.h"

#include "il2cpp-object-internals.h"
#include "il2cpp-class-internals.h"
#include "il2cpp-string-types.h"
#include "Environment.h"
#include "os/StackTrace.h"
#include "utils/StringUtils.h"
#include "vm/Runtime.h"
#include "vm/String.h"
#include "vm/StackTrace.h"

#include <string>

namespace tiny
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
    Il2CppString* Environment::GetStackTrace_internal()
    {
        std::string stackTrace = tiny::vm::StackTrace::GetStackTrace();
#if IL2CPP_TINY_WITHOUT_DEBUGGER
        UTF16String utf16Chars = il2cpp::utils::StringUtils::Utf8ToUtf16(stackTrace.c_str(), stackTrace.length());
        return vm::String::NewLen(utf16Chars.c_str(), (uint32_t)stackTrace.length());
#else
        return vm::String::NewLen(stackTrace.c_str(), (uint32_t)stackTrace.length());
#endif
    }

    void Environment::FailFast_internal(Il2CppString* message)
    {
        std::string messageUtf8;
        if (message != NULL)
            messageUtf8 = il2cpp::utils::StringUtils::Utf16ToUtf8(message->chars, message->length);

        vm::Runtime::FailFast(messageUtf8.c_str());
    }
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace tiny */
