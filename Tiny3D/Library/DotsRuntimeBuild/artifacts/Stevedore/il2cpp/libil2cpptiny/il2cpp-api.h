#pragma once

#include <stdint.h>

#if defined(__cplusplus)
extern "C"
{
#endif

void il2cpp_init();
void il2cpp_shutdown();
void il2cpp_gc_disable();

typedef struct Il2CppObject Il2CppObject;
typedef struct Il2CppType Il2CppType;
typedef struct Il2CppClass Il2CppClass;
typedef struct MethodInfo MethodInfo;
typedef struct FieldInfo FieldInfo;
typedef struct Il2CppImage Il2CppImage;
typedef struct Il2CppAssembly Il2CppAssembly;
typedef struct PropertyInfo PropertyInfo;

const Il2CppType* il2cpp_class_get_type(Il2CppClass *klass);
Il2CppClass* il2cpp_object_get_class(Il2CppObject* obj);
void il2cpp_gc_wbarrier_set_field(Il2CppObject *obj, void **targetAddress, void *object);
const char* il2cpp_method_get_name(const MethodInfo *method);
const Il2CppType* il2cpp_field_get_type(FieldInfo *field);
bool il2cpp_type_is_byref(const Il2CppType *type);
int il2cpp_type_get_type(const Il2CppType *type);
bool il2cpp_class_is_valuetype(const Il2CppClass* klass);
int il2cpp_class_get_rank(const Il2CppClass *klass);
bool il2cpp_class_is_enum(const Il2CppClass *klass);
uint32_t il2cpp_type_get_attrs(const Il2CppType *type);
const char* il2cpp_class_get_namespace(Il2CppClass *klass);
bool il2cpp_class_is_interface(const Il2CppClass *klass);
const MethodInfo* il2cpp_object_get_virtual_method(Il2CppObject *obj, const MethodInfo *method);
bool il2cpp_class_is_abstract(const Il2CppClass *klass);
Il2CppClass* il2cpp_class_from_name(const Il2CppImage* image, const char* namespaze, const char *name);
const MethodInfo* il2cpp_class_get_method_from_name(Il2CppClass *klass, const char* name, int argsCount);
const char* il2cpp_image_get_name(const Il2CppImage *image);
const Il2CppAssembly* il2cpp_image_get_assembly(const Il2CppImage *image);
Il2CppClass* il2cpp_field_get_parent(FieldInfo *field);
Il2CppClass* il2cpp_class_get_interfaces(Il2CppClass *klass, void* *iter);
const Il2CppImage* il2cpp_class_get_image(Il2CppClass* klass);
Il2CppClass* il2cpp_class_get_parent(Il2CppClass *klass);
Il2CppClass* il2cpp_class_get_element_class(Il2CppClass *klass);
uint32_t il2cpp_class_get_type_token(Il2CppClass *klass);
int il2cpp_class_get_flags(const Il2CppClass *klass);
bool il2cpp_class_is_inflated(const Il2CppClass *klass);
Il2CppClass* il2cpp_method_get_class(const MethodInfo *method);
uint32_t il2cpp_method_get_flags(const MethodInfo *method, uint32_t *iflags);
uint32_t il2cpp_method_get_token(const MethodInfo *method);
const Il2CppType* il2cpp_method_get_param(const MethodInfo *method, uint32_t index);
const char* il2cpp_class_get_name(Il2CppClass *klass);
bool il2cpp_class_is_generic(const Il2CppClass *klass);
size_t il2cpp_field_get_offset(FieldInfo *field);
const char* il2cpp_property_get_name(PropertyInfo *prop);
const MethodInfo* il2cpp_property_get_get_method(PropertyInfo *prop);
const MethodInfo* il2cpp_property_get_set_method(PropertyInfo *prop);
Il2CppClass* il2cpp_property_get_parent(PropertyInfo *prop);

#if defined(__cplusplus)
}
#endif
