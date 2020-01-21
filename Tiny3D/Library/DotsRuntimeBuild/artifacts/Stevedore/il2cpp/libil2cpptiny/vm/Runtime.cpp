#include "il2cpp-config.h"
#include "Runtime.h"
#include "TypeUniverse.h"
#include "os/CrashHelpers.h"
#include "os/Image.h"
#include "os/Memory.h"
#include "gc/GarbageCollector.h"
#include "utils/Logging.h"
#include "vm/DebugMetadata.h"
#include "vm/StackTrace.h"
#include "vm/Reflection.h"

void Il2CppCallStaticConstructors();
extern const void** GetStaticFieldsStorageArray();

namespace tiny
{
namespace vm
{
    void Runtime::Init()
    {
#if IL2CPP_ENABLE_STACKTRACES
        vm::StackTrace::InitializeStackTracesForCurrentThread();
#endif
        il2cpp::gc::GarbageCollector::Initialize();
        Reflection::Initialize();
        TypeUniverse::Initialize();
        AllocateStaticFieldsStorage();
        Il2CppCallStaticConstructors();
#if IL2CPP_ENABLE_STACKTRACES
        il2cpp::os::Image::Initialize();
        vm::DebugMetadata::InitializeMethodsForStackTraces();
#endif
    }

    void Runtime::Shutdown()
    {
        FreeStaticFieldsStorage();
        il2cpp::gc::GarbageCollector::UninitializeGC();
#if IL2CPP_ENABLE_STACKTRACES
        vm::StackTrace::CleanupStackTracesForCurrentThread();
#endif
    }

    void Runtime::AllocateStaticFieldsStorage()
    {
        const void** StaticFieldsStorage = GetStaticFieldsStorageArray();
        int i = 0;
        while (StaticFieldsStorage[i] != NULL)
        {
            *(void**)StaticFieldsStorage[i] = il2cpp::gc::GarbageCollector::AllocateFixed(*(size_t*)StaticFieldsStorage[i], NULL);
            i++;
        }
    }

    void Runtime::FreeStaticFieldsStorage()
    {
        const void** StaticFieldsStorage = GetStaticFieldsStorageArray();
        int i = 0;
        while (StaticFieldsStorage[i] != NULL)
        {
            il2cpp::gc::GarbageCollector::FreeFixed(*(void**)StaticFieldsStorage[i]);
            i++;
        }
    }

    void Runtime::FailFast(const char* message)
    {
        bool messageWritten = false;
        if (message != NULL)
        {
            if (strlen(message) != 0)
            {
                il2cpp::utils::Logging::Write(message);
                messageWritten = true;
            }
        }

        if (!messageWritten)
            il2cpp::utils::Logging::Write("No error message was provided. Hopefully the stack trace can provide some information.");

        std::string managedStackTrace = vm::StackTrace::GetStackTrace();
        if (!managedStackTrace.empty())
        {
            std::string managedStackTraceMessage = "Managed stack trace:\n" + managedStackTrace;
            il2cpp::utils::Logging::Write(managedStackTraceMessage.c_str());
        }
        else
        {
            il2cpp::utils::Logging::Write("No managed stack trace exists. Make sure this is a development build to enable managed stack traces.");
        }

        il2cpp::os::CrashHelpers::Crash();
    }
}
}
