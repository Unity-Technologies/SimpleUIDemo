#include "il2cpp-config.h"
#include "il2cpp-api.h"
#include "vm/Runtime.h"
#include "gc/GarbageCollector.h"

void il2cpp_init()
{
    tiny::vm::Runtime::Init();
}

void il2cpp_shutdown()
{
    tiny::vm::Runtime::Shutdown();
}

void il2cpp_gc_disable()
{
    il2cpp::gc::GarbageCollector::Disable();
}

const Il2CppType* il2cpp_class_get_type(Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

Il2CppClass* il2cpp_object_get_class(Il2CppObject* obj)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

void il2cpp_gc_wbarrier_set_field(Il2CppObject *obj, void **targetAddress, void *object)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
}

const char* il2cpp_method_get_name(const MethodInfo *method)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

const Il2CppType* il2cpp_field_get_type(FieldInfo *field)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

bool il2cpp_type_is_byref(const Il2CppType *type)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return false;
}

int il2cpp_type_get_type(const Il2CppType *type)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return 0;
}

bool il2cpp_class_is_valuetype(const Il2CppClass* klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return false;
}

int il2cpp_class_get_rank(const Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return 0;
}

bool il2cpp_class_is_enum(const Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return false;
}

uint32_t il2cpp_type_get_attrs(const Il2CppType *type)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return 0;
}

const char* il2cpp_class_get_namespace(Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

bool il2cpp_class_is_interface(const Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return false;
}

const MethodInfo* il2cpp_object_get_virtual_method(Il2CppObject *obj, const MethodInfo *method)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

bool il2cpp_class_is_abstract(const Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return false;
}

Il2CppClass* il2cpp_class_from_name(const Il2CppImage* image, const char* namespaze, const char *name)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

const MethodInfo* il2cpp_class_get_method_from_name(Il2CppClass *klass, const char* name, int argsCount)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

const char* il2cpp_image_get_name(const Il2CppImage *image)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

const Il2CppAssembly* il2cpp_image_get_assembly(const Il2CppImage *image)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

Il2CppClass* il2cpp_field_get_parent(FieldInfo *field)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

Il2CppClass* il2cpp_class_get_interfaces(Il2CppClass *klass, void* *iter)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

const Il2CppImage* il2cpp_class_get_image(Il2CppClass* klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

Il2CppClass* il2cpp_class_get_parent(Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

Il2CppClass* il2cpp_class_get_element_class(Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

uint32_t il2cpp_class_get_type_token(Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return 0;
}

int il2cpp_class_get_flags(const Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return 0;
}

bool il2cpp_class_is_generic(const Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return false;
}

bool il2cpp_class_is_inflated(const Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return false;
}

Il2CppClass* il2cpp_method_get_class(const MethodInfo *method)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

uint32_t il2cpp_method_get_flags(const MethodInfo *method, uint32_t *iflags)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return 0;
}

uint32_t il2cpp_method_get_token(const MethodInfo *method)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return 0;
}

const Il2CppType* il2cpp_method_get_param(const MethodInfo *method, uint32_t index)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

const char* il2cpp_class_get_name(Il2CppClass *klass)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

size_t il2cpp_field_get_offset(FieldInfo *field)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return 0;
}

const char* il2cpp_property_get_name(PropertyInfo *prop)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

const MethodInfo* il2cpp_property_get_get_method(PropertyInfo *prop)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

const MethodInfo* il2cpp_property_get_set_method(PropertyInfo *prop)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}

Il2CppClass* il2cpp_property_get_parent(PropertyInfo *prop)
{
    IL2CPP_ASSERT(0 && "Not implemented on Tiny");
    return NULL;
}
