#pragma once

#include "il2cpp-config.h"

#if IL2CPP_TINY

#include "il2cpp-blob.h"
#include "il2cpp-runtime-metadata.h"

typedef int32_t TypeIndex;
typedef int32_t TypeDefinitionIndex;
typedef int32_t MethodIndex;
typedef int32_t GenericParameterIndex;

typedef struct TinyType TinyType;

typedef struct Il2CppType Il2CppType;
typedef struct Il2CppProfiler Il2CppProfiler;
typedef struct Il2CppDomain Il2CppDomain;
typedef struct Il2CppAppDomain Il2CppAppDomain;
typedef struct Il2CppImage Il2CppImage;
typedef struct Il2CppClass Il2CppClass;
typedef struct Il2CppMethodHeaderInfo Il2CppMethodHeaderInfo;
typedef struct Il2CppVTable Il2CppVTable;
typedef struct FieldInfo FieldInfo;
typedef struct PropertyInfo PropertyInfo;
typedef struct Il2CppArraySize Il2CppArraySize;
typedef struct Il2CppGenericInst Il2CppGenericInst;
typedef struct Il2CppGenericContext Il2CppGenericContext;
typedef struct Il2CppGenericContainer Il2CppGenericContainer;
typedef struct Il2CppGenericParameter Il2CppGenericParameter;
typedef struct Il2CppGenericClass Il2CppGenericClass;
typedef struct Il2CppInternalThread Il2CppInternalThread;
typedef struct Il2CppReflectionType Il2CppReflectionType;
typedef struct Il2CppException Il2CppException;
typedef struct Il2CppString Il2CppString;
typedef struct Il2CppAppDomainSetup Il2CppAppDomainSetup;
typedef struct Il2CppAppContext Il2CppAppContext;
typedef struct Il2CppArray Il2CppArray;
typedef struct ParameterInfo ParameterInfo;
typedef struct Il2CppRGCTXData Il2CppRGCTXData;
typedef struct Il2CppMethodDefinition Il2CppMethodDefinition;
typedef struct Il2CppGenericMethod Il2CppGenericMethod;
typedef struct Il2CppGenericContainer Il2CppGenericContainer;
typedef struct Il2CppDelegate Il2CppDelegate;
typedef struct Il2CppArrayBounds Il2CppArrayBounds;

typedef struct MethodInfo MethodInfo;
typedef void* (*InvokerMethod)(Il2CppMethodPointer, const MethodInfo*, void*, void**);

typedef struct MethodInfo
{
    Il2CppMethodPointer methodPointer;
    InvokerMethod invoker_method;
    const char* name;
    Il2CppClass *klass;
    const Il2CppType *return_type;
    const ParameterInfo* parameters;

    union
    {
        const Il2CppRGCTXData* rgctx_data; /* is_inflated is true and is_generic is false, i.e. a generic instance method */
        const Il2CppMethodDefinition* methodDefinition;
    };

    /* note, when is_generic == true and is_inflated == true the method represents an uninflated generic method on an inflated type. */
    union
    {
        const Il2CppGenericMethod* genericMethod; /* is_inflated is true */
        const Il2CppGenericContainer* genericContainer; /* is_inflated is false and is_generic is true */
    };

    uint32_t token;
    uint16_t flags;
    uint16_t iflags;
    uint16_t slot;
    uint8_t parameters_count;
    uint8_t is_generic : 1; /* true if method is a generic method definition */
    uint8_t is_inflated : 1; /* true if declaring_type is a generic instance or if method is a generic instance*/
    uint8_t wrapper_type : 1; /* always zero (MONO_WRAPPER_NONE) needed for the debugger */
    uint8_t is_marshaled_from_native : 1; /* a fake MethodInfo wrapping a native function pointer */
} MethodInfo;

typedef enum Il2CppTypeNameFormat
{
    IL2CPP_TYPE_NAME_FORMAT_IL,
    IL2CPP_TYPE_NAME_FORMAT_REFLECTION,
    IL2CPP_TYPE_NAME_FORMAT_FULL_NAME,
    IL2CPP_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED
} Il2CppTypeNameFormat;

typedef struct Il2CppDefaults
{
    Il2CppImage *corlib;
    Il2CppClass *object_class;
    Il2CppClass *byte_class;
    Il2CppClass *void_class;
    Il2CppClass *boolean_class;
    Il2CppClass *sbyte_class;
    Il2CppClass *int16_class;
    Il2CppClass *uint16_class;
    Il2CppClass *int32_class;
    Il2CppClass *uint32_class;
    Il2CppClass *int_class;
    Il2CppClass *uint_class;
    Il2CppClass *int64_class;
    Il2CppClass *uint64_class;
    Il2CppClass *single_class;
    Il2CppClass *double_class;
    Il2CppClass *char_class;
    Il2CppClass *string_class;
    Il2CppClass *enum_class;
    Il2CppClass *array_class;
    Il2CppClass *delegate_class;
    Il2CppClass *multicastdelegate_class;
    Il2CppClass *asyncresult_class;
    Il2CppClass *manualresetevent_class;
    Il2CppClass *typehandle_class;
    Il2CppClass *fieldhandle_class;
    Il2CppClass *methodhandle_class;
    Il2CppClass *systemtype_class;
    Il2CppClass *monotype_class;
    Il2CppClass *exception_class;
    Il2CppClass *threadabortexception_class;
    Il2CppClass *thread_class;
    Il2CppClass *internal_thread_class;
    /*Il2CppClass *transparent_proxy_class;
    Il2CppClass *real_proxy_class;
    Il2CppClass *mono_method_message_class;*/
    Il2CppClass *appdomain_class;
    Il2CppClass *appdomain_setup_class;
    Il2CppClass *field_info_class;
    Il2CppClass *method_info_class;
    Il2CppClass *property_info_class;
    Il2CppClass *event_info_class;
    Il2CppClass *mono_event_info_class;
    Il2CppClass *stringbuilder_class;
    /*Il2CppClass *math_class;*/
    Il2CppClass *stack_frame_class;
    Il2CppClass *stack_trace_class;
    Il2CppClass *marshal_class;
    /*Il2CppClass *iserializeable_class;
    Il2CppClass *serializationinfo_class;
    Il2CppClass *streamingcontext_class;*/
    Il2CppClass *typed_reference_class;
    /*Il2CppClass *argumenthandle_class;*/
    Il2CppClass *marshalbyrefobject_class;
    /*Il2CppClass *monitor_class;
    Il2CppClass *iremotingtypeinfo_class;
    Il2CppClass *runtimesecurityframe_class;
    Il2CppClass *executioncontext_class;
    Il2CppClass *internals_visible_class;*/
    Il2CppClass *generic_ilist_class;
    Il2CppClass *generic_icollection_class;
    Il2CppClass *generic_ienumerable_class;
    Il2CppClass *generic_ireadonlylist_class;
    Il2CppClass *generic_ireadonlycollection_class;
    Il2CppClass *runtimetype_class;
    Il2CppClass *generic_nullable_class;
    /*Il2CppClass *variant_class;
    Il2CppClass *com_object_class;*/
    Il2CppClass *il2cpp_com_object_class;
    /*Il2CppClass *com_interop_proxy_class;
    Il2CppClass *iunknown_class;
    Il2CppClass *idispatch_class;
    Il2CppClass *safehandle_class;
    Il2CppClass *handleref_class;*/
    Il2CppClass *attribute_class;
    Il2CppClass *customattribute_data_class;
    //Il2CppClass *critical_finalizer_object;
    Il2CppClass *version;
    Il2CppClass *culture_info;
    Il2CppClass *async_call_class;
    Il2CppClass *assembly_class;
    Il2CppClass *mono_assembly_class;
    Il2CppClass *assembly_name_class;
    Il2CppClass *mono_field_class;
    Il2CppClass *mono_method_class;
    Il2CppClass *mono_method_info_class;
    Il2CppClass *mono_property_info_class;
    Il2CppClass *parameter_info_class;
    Il2CppClass *mono_parameter_info_class;
    Il2CppClass *module_class;
    Il2CppClass *pointer_class;
    Il2CppClass *system_exception_class;
    Il2CppClass *argument_exception_class;
    Il2CppClass *wait_handle_class;
    Il2CppClass *safe_handle_class;
    Il2CppClass *sort_key_class;
    Il2CppClass *dbnull_class;
    Il2CppClass *error_wrapper_class;
    Il2CppClass *missing_class;
    Il2CppClass *value_type_class;

    // Stuff used by the mono code
    Il2CppClass *threadpool_wait_callback_class;
    MethodInfo *threadpool_perform_wait_callback_method;
    Il2CppClass *mono_method_message_class;

    // Windows.Foundation.IReference`1<T>
    Il2CppClass* ireference_class;
    // Windows.Foundation.Collections.IKeyValuePair`2<K, V>
    Il2CppClass* ikey_value_pair_class;
    // System.Collections.Generic.KeyValuePair`2<K, V>
    Il2CppClass* key_value_pair_class;
    // Windows.Foundation.Uri
    Il2CppClass* windows_foundation_uri_class;
    // Windows.Foundation.IUriRuntimeClass
    Il2CppClass* windows_foundation_iuri_runtime_class_class;
    // System.Uri
    Il2CppClass* system_uri_class;
    // System.Guid
    Il2CppClass* system_guid_class;

    Il2CppClass* sbyte_shared_enum;
    Il2CppClass* int16_shared_enum;
    Il2CppClass* int32_shared_enum;
    Il2CppClass* int64_shared_enum;

    Il2CppClass* byte_shared_enum;
    Il2CppClass* uint16_shared_enum;
    Il2CppClass* uint32_shared_enum;
    Il2CppClass* uint64_shared_enum;
} Il2CppDefaults;

typedef struct Il2CppDomain
{
    Il2CppAppDomain* domain;
    Il2CppAppDomainSetup* setup;
    Il2CppAppContext* default_context;
    const char* friendly_name;
    uint32_t domain_id;

    volatile int threadpool_jobs;
    void* agent_info;
} Il2CppDomain;

#define PUBLIC_KEY_BYTE_LENGTH 8

typedef struct Il2CppAssemblyName
{
    const char* name;
    const char* culture;
    const char* hash_value;
    const char* public_key;
    uint32_t hash_alg;
    int32_t hash_len;
    uint32_t flags;
    int32_t major;
    int32_t minor;
    int32_t build;
    int32_t revision;
    uint8_t public_key_token[PUBLIC_KEY_BYTE_LENGTH];
} Il2CppAssemblyName;

typedef struct Il2CppAssembly
{
    Il2CppImage* image;
    uint32_t token;
    int32_t referencedAssemblyStart;
    int32_t referencedAssemblyCount;
    Il2CppAssemblyName aname;
} Il2CppAssembly;

typedef enum MethodVariableKind
{
    kMethodVariableKind_This,
    kMethodVariableKind_Parameter,
    kMethodVariableKind_LocalVariable
} MethodVariableKind;

typedef enum SequencePointKind
{
    kSequencePointKind_Normal,
    kSequencePointKind_StepOut
} SequencePointKind;

typedef struct Il2CppMethodExecutionContextInfo
{
    TypeIndex typeIndex;
    int32_t nameIndex;
    int32_t scopeIndex;
} Il2CppMethodExecutionContextInfo;

typedef struct Il2CppMethodExecutionContextInfoIndex
{
    int8_t tableIndex;
    int32_t startIndex;
    int32_t count;
} Il2CppMethodExecutionContextInfoIndex;

typedef struct Il2CppMethodScope
{
    int32_t startOffset;
    int32_t endOffset;
} Il2CppMethodScope;

typedef struct Il2CppMethodHeaderInfo
{
    int32_t codeSize;
    int32_t startScope;
    int32_t numScopes;
} Il2CppMethodHeaderInfo;

typedef struct Il2CppSequencePointIndex
{
    uint8_t tableIndex;
    int32_t index;
} Il2CppSequencePointIndex;

typedef struct Il2CppSequencePointSourceFile
{
    const char *file;
    uint8_t hash[16];
} Il2CppSequencePointSourceFile;

typedef struct Il2CppTypeSourceFilePair
{
    TypeDefinitionIndex klassIndex;
    int32_t sourceFileIndex;
} Il2CppTypeSourceFilePair;

typedef struct Il2CppSequencePoint
{
    MethodIndex methodDefinitionIndex;
    TypeIndex catchTypeIndex;
    int32_t sourceFileIndex;
    int32_t lineStart, lineEnd;
    int32_t columnStart, columnEnd;
    int32_t ilOffset;
    SequencePointKind kind;
    int32_t isActive;
    int32_t id;
    uint8_t tryDepth;
} Il2CppSequencePoint;

typedef struct Il2CppDebuggerMetadataRegistration
{
    Il2CppMethodExecutionContextInfo** methodExecutionContextInfos;
    Il2CppMethodExecutionContextInfoIndex* methodExecutionContextInfoIndexes;
    Il2CppMethodScope* methodScopes;
    Il2CppMethodHeaderInfo* methodHeaderInfos;
    Il2CppSequencePointSourceFile* sequencePointSourceFiles;
    int32_t numSequencePoints;
    Il2CppSequencePointIndex* sequencePointIndexes;
    Il2CppSequencePoint** sequencePoints;
    int32_t numTypeSourceFileEntries;
    Il2CppTypeSourceFilePair* typeSourceFiles;
    const char** methodExecutionContextInfoStrings;
} Il2CppDebuggerMetadataRegistration;

typedef int32_t il2cpp_array_lower_bound_t;

typedef struct Il2CppArrayBounds
{
    il2cpp_array_size_t length;
    il2cpp_array_lower_bound_t lower_bound;
} Il2CppArrayBounds;

#endif  // IL2CPP_TINY
