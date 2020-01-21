#pragma once

extern uint8_t* Il2CppGetTinyTypeUniverse();
extern uint32_t Il2CppGetTinyTypeUniverseTypeCount();
extern uint8_t* Il2CppGetStringLiterals();
extern uint32_t Il2CppGetStringLiteralCount();
extern const Il2CppMethodPointer* Il2CppGetTinyVirtualMethodUniverse();
extern void InitializeStringLiterals();
extern void InitializeSystemTypeInstance();

namespace tiny
{
namespace vm
{
    class TypeUniverse
    {
    public:
        static void Initialize();
    };
}
}
