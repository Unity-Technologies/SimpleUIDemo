#pragma once

#if IL2CPP_TINY

#include "il2cpp-class-internals.h"
#include "il2cpp-runtime-metadata.h"
#ifndef __cplusplus
#include <stdbool.h>
#endif

typedef struct TinyType TinyType;


typedef struct Il2CppObject
{
    TinyType* klass;
} Il2CppObject;

#ifdef __cplusplus
typedef struct Il2CppArray : public Il2CppObject
{
#else
typedef struct Il2CppArray
{
    Il2CppObject obj;
#endif
    il2cpp_array_size_t max_length;
} Il2CppArray;

#ifdef __cplusplus
template<size_t N>
struct Il2CppMultidimensionalArray : public Il2CppObject
{
    il2cpp_array_size_t bounds[N];
};
#endif

#if IL2CPP_COMPILER_MSVC
#pragma warning( push )
#pragma warning( disable : 4200 )
#elif defined(__clang__)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#endif

#ifdef __cplusplus
typedef struct Il2CppArraySize : public Il2CppArray
{
#else
//mono code has no inheritance, so its members must be available from this type
typedef struct Il2CppArraySize
{
    Il2CppObject obj;
    Il2CppArrayBounds *bounds;
    il2cpp_array_size_t max_length;
#endif //__cplusplus
    ALIGN_TYPE(8) void* vector[IL2CPP_ZERO_LEN_ARRAY];
} Il2CppArraySize;

typedef uint32_t Il2CppMethodSlot;

#if IL2CPP_COMPILER_MSVC
#pragma warning( push )
#pragma warning( disable : 4200 )
#elif defined(__clang__)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#endif

#ifdef __cplusplus
typedef struct Il2CppString : public Il2CppObject
{
#else
typedef struct Il2CppString
{
    Il2CppObject obj;
#endif
    int32_t length;                             ///< Length of string *excluding* the trailing null (which is included in 'chars').
    Il2CppChar chars[IL2CPP_ZERO_LEN_ARRAY];
} Il2CppString;

#if IL2CPP_COMPILER_MSVC
#pragma warning( pop )
#elif defined(__clang__)
#pragma clang diagnostic pop
#endif

// Interface offsets are 16 bits wide, so we pack two into each entry in the tiny type universe
// for both 32-bit builds and four entries for 64-bit builds.
#if IL2CPP_SIZEOF_VOID_P == 8
const int NumberOfPackedInterfaceOffsetsPerElement = 4;
#else
const int NumberOfPackedInterfaceOffsetsPerElement = 2;
#endif

typedef uint16_t packed_iterface_offset_t;

#ifdef __cplusplus
typedef struct TinyType : Il2CppObject
{
#else
typedef struct TinyType
{
    Il2CppObject obj;
#endif
    uint16_t vtableSize;
    uint8_t typeHierarchySize;
    uint8_t interfacesSize;
#ifdef __cplusplus
    inline const Il2CppMethodPointer* GetVTable() const
    {
        return reinterpret_cast<const Il2CppMethodPointer*>(this + 1);
    }

    inline const TinyType* const* GetTypeHierarchy() const
    {
        return reinterpret_cast<const TinyType* const*>(GetVTable() + vtableSize);
    }

    inline const TinyType* const* GetInterfaces() const
    {
        return reinterpret_cast<const TinyType* const*>(GetTypeHierarchy() + typeHierarchySize);
    }

    inline const packed_iterface_offset_t* GetInterfaceOffsets() const
    {
        return reinterpret_cast<const packed_iterface_offset_t*>(GetInterfaces() + interfacesSize);
    }

    // This is the number of elements (each of size uintptr_t) in the tiny type universe we used
    // for the packed interface offsets for this type.
    inline size_t NumberOfPackedInterfaceOffsetElements() const
    {
        return interfacesSize / NumberOfPackedInterfaceOffsetsPerElement + (interfacesSize % NumberOfPackedInterfaceOffsetsPerElement != 0 ? 1 : 0);
    }

    int GetId() const
    {
        return (int) * reinterpret_cast<const intptr_t*>(GetInterfaceOffsets() + NumberOfPackedInterfaceOffsetElements());
    }

#endif
} TinyType;

#ifdef __cplusplus
typedef struct Il2CppDelegate : Il2CppObject
{
#else
typedef struct Il2CppDelegate
{
    Il2CppObject obj;
#endif
    void* method_ptr;
    Il2CppObject* m_target;
    void* m_ReversePInvokeWrapperPtr;
    bool m_IsDelegateOpen;
} Il2CppDelegate;

#ifdef __cplusplus
typedef struct Il2CppMulticastDelegate : Il2CppDelegate
{
#else
typedef struct Il2CppMulticastDelegate
{
    Il2CppDelegate delegate;
#endif
    Il2CppArray* delegates;
    int delegateCount;
} Il2CppMulticastDelegate;

typedef struct Il2CppReflectionType
{
    Il2CppObject object;
    const TinyType* typeHandle;
} Il2CppReflectionType;

typedef struct Il2CppInternalThread
{
    Il2CppObject obj;
    int lock_thread_id;
#ifdef __cplusplus
    /*il2cpp::os::Thread*/ void* handle;
#else
    void* handle;
#endif //__cplusplus
    void* native_handle;
    Il2CppArray* cached_culture_info;
    Il2CppChar* name;
    int name_len;
    uint32_t state;
    Il2CppObject* abort_exc;
    int abort_state_handle;
    uint64_t tid;
    intptr_t debugger_thread;
    void** static_data;
    void* runtime_thread_info;
    Il2CppObject* current_appcontext;
    Il2CppObject* root_domain_thread;
    Il2CppArray* _serialized_principal;
    int _serialized_principal_version;
    void* appdomain_refs;
    int32_t interruption_requested;
#ifdef __cplusplus
    /*il2cpp::os::FastMutex*/ void* synch_cs;
#else
    void* synch_cs;
#endif //__cplusplus
    uint8_t threadpool_thread;
    uint8_t thread_interrupt_requested;
    int stack_size;
    uint8_t apartment_state;
    int critical_region_level;
    int managed_id;
    uint32_t small_id;
    void* manage_callback;
    void* interrupt_on_stop;
    intptr_t flags;
    void* thread_pinning_ref;
    void* abort_protected_block_count;
    int32_t priority;
    void* owned_mutexes;
    void * suspended;
    int32_t self_suspended;
    size_t thread_state;
    size_t unused2;
    void* last;
} Il2CppInternalThread;

// System.Threading.Thread
typedef struct Il2CppThread
{
    Il2CppObject  obj;
    Il2CppInternalThread* internal_thread;
    Il2CppObject* start_obj;
    Il2CppException* pending_exception;
    Il2CppObject* principal;
    int32_t principal_version;
    Il2CppDelegate* delegate;
    Il2CppObject* executionContext;
    /*bool*/ uint8_t executionContextBelongsToOuterScope;

#ifdef __cplusplus
    Il2CppInternalThread* GetInternalThread() const
    {
        return internal_thread;
    }

#endif //__cplusplus
} Il2CppThread;

typedef struct Il2CppReflectionAssembly
{
    Il2CppObject object;
    const Il2CppAssembly *assembly;
    Il2CppObject *resolve_event_holder;
    /* CAS related */
    Il2CppObject *evidence; /* Evidence */
    Il2CppObject *minimum;  /* PermissionSet - for SecurityAction.RequestMinimum */
    Il2CppObject *optional; /* PermissionSet - for SecurityAction.RequestOptional */
    Il2CppObject *refuse;   /* PermissionSet - for SecurityAction.RequestRefuse */
    Il2CppObject *granted;  /* PermissionSet - for the resolved assembly granted permissions */
    Il2CppObject *denied;   /* PermissionSet - for the resolved assembly denied permissions */
    /* */
    /*bool*/ int32_t from_byte_array;
    /*Il2CppString*/ void *name;
} Il2CppReflectionAssembly;


#ifdef __cplusplus
// System.Exception
typedef struct Il2CppException : public Il2CppObject
{
#else
typedef struct Il2CppException
{
    Il2CppObject object;
#endif //__cplusplus
    Il2CppString* message;
    Il2CppString* stack_trace;
} Il2CppException;

typedef struct Il2CppExceptionWrapper
{
    Il2CppException* ex;
#ifdef __cplusplus
    Il2CppExceptionWrapper(Il2CppException* ex) : ex(ex) {}
#endif //__cplusplus
} Il2CppExceptionWrapper;

#endif // IL2CPP_TINY
