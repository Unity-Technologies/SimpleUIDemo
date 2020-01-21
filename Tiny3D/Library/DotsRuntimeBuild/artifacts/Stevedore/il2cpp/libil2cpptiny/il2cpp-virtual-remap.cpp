#include "il2cpp-config.h"
#include "il2cpp-object-internals.h"
#include "vm/Object.h"

#include <cstring>

template<int size>
bool il2cpp_virtual_remap_enum_equals(Il2CppObject* left, Il2CppObject* right)
{
    if (left->klass != right->klass)
        return false;

    void* leftEnumField = tiny::vm::Object::Unbox(left);
    void* rightEnumField = tiny::vm::Object::Unbox(right);

    return std::memcmp(leftEnumField, rightEnumField, size) == 0;
}

extern "C"
{
    bool il2cpp_virtual_remap_enum1_equals(Il2CppObject* left, Il2CppObject* right)
    {
        return il2cpp_virtual_remap_enum_equals<1>(left, right);
    }

    bool il2cpp_virtual_remap_enum2_equals(Il2CppObject* left, Il2CppObject* right)
    {
        return il2cpp_virtual_remap_enum_equals<2>(left, right);
    }

    bool il2cpp_virtual_remap_enum4_equals(Il2CppObject* left, Il2CppObject* right)
    {
        return il2cpp_virtual_remap_enum_equals<4>(left, right);
    }

    bool il2cpp_virtual_remap_enum8_equals(Il2CppObject* left, Il2CppObject* right)
    {
        return il2cpp_virtual_remap_enum_equals<8>(left, right);
    }
}
