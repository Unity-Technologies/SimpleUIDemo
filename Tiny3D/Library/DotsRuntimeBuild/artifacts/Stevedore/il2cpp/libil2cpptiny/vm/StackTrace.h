#pragma once

#include <stdint.h>
#include <vector>
#include <string>
#include "il2cpp-config.h"
#include "il2cpp-metadata.h"
#include "il2cpp-debug-metadata.h"

namespace tiny
{
namespace vm
{
    typedef std::vector<TinyStackFrameInfo> StackFrames;

    class LIBIL2CPP_CODEGEN_API StackTrace
    {
    public:
        static void InitializeStackTracesForCurrentThread();
        static void CleanupStackTracesForCurrentThread();

        static std::string GetStackTrace();

        static void PushFrame(TinyStackFrameInfo& frame);
        static void PopFrame();
    };
}
}
