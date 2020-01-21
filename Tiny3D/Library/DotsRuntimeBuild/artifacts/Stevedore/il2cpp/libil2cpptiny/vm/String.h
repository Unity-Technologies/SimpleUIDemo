#pragma once

struct Il2CppString;

namespace tiny
{
namespace vm
{
    class String
    {
    public:
        static Il2CppString* NewLen(uint32_t length);
        static Il2CppString* NewLen(const Il2CppChar* characters, uint32_t length);
    };
}
}
