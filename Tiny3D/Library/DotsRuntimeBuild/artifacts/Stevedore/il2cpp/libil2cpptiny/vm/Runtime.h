#pragma once

namespace tiny
{
namespace vm
{
    class Runtime
    {
    public:
        static void Init();
        static void Shutdown();
        static void AllocateStaticFieldsStorage();
        static void FreeStaticFieldsStorage();
        static void FailFast(const char* message);
    };
}
}
