#pragma once

#include "il2cpp-object-internals.h"
#include "il2cpp-debug-metadata.h"
#include "os/Memory.h"
#include "vm/Exception.h"
#include "vm/Object.h"
#include "vm/PlatformInvoke.h"
#include "vm/ScopedThreadAttacher.h"
#include "vm/String.h"
#include "vm/Runtime.h"
#include "vm/Type.h"
#include "vm/TypeUniverse.h"
#include "utils/LeaveTargetStack.h"
#include "utils/MemoryUtils.h"
#include "utils/StringUtils.h"
#include "utils/StringView.h"
#include <string>
#include "gc/gc_wrapper.h"
#include "gc/GarbageCollector.h"
#include "icalls/mscorlib/System.Threading/Interlocked.h"

struct Exception_t;
struct Delegate_t;
struct MulticastDelegate_t;
struct String_t;
struct Type_t;

typedef Il2CppObject RuntimeObject;
typedef Il2CppArray RuntimeArray;
typedef Il2CppMethodPointer VirtualInvokeData;

struct RuntimeMethod;

#if IL2CPP_COMPILER_MSVC
#define DEFAULT_CALL STDCALL
#else
#define DEFAULT_CALL
#endif

NORETURN inline void il2cpp_codegen_raise_exception(Exception_t* ex);

inline void il2cpp_codegen_memcpy(void* dest, const void* src, size_t count)
{
    memcpy(dest, src, count);
}

inline void il2cpp_codegen_memset(void* ptr, int value, size_t num)
{
    memset(ptr, value, num);
}

inline void il2cpp_codegen_initobj(void* value, size_t size)
{
    il2cpp::utils::MemoryUtils::MemorySet(value, 0, size);
}

inline RuntimeObject* il2cpp_codegen_object_new(size_t size, TinyType* typeInfo)
{
    RuntimeObject* result = static_cast<RuntimeObject*>(il2cpp::gc::GarbageCollector::Allocate(size));
    IL2CPP_ASSUME(result > NULL);
    result->klass = typeInfo;
    return result;
}

template<typename T>
inline Il2CppObject* Box(TinyType* type, T* value)
{
    COMPILE_TIME_CONST size_t alignedObjectSize = (sizeof(Il2CppObject) + ALIGN_OF(T) - 1) & ~(ALIGN_OF(T) - 1);
    Il2CppObject* obj = il2cpp_codegen_object_new(sizeof(T) + alignedObjectSize, type);
    il2cpp::utils::MemoryUtils::MemoryCopy(reinterpret_cast<uint8_t*>(obj) + alignedObjectSize, value, sizeof(T));
    return obj;
}

template<typename NullableType, typename ArgumentType>
inline Il2CppObject* BoxNullable(TinyType* type, NullableType* value)
{
    /*
    From ECMA-335, I.8.2.4 Boxing and unboxing of values:

    All value types have an operation called box. Boxing a value of any value type produces its boxed value;
    i.e., a value of the corresponding boxed type containing a bitwise copy of the original value. If the
    value type is a nullable type defined as an instantiation of the value type System.Nullable<T> the result
    is a null reference or bitwise copy of its Value property of type T, depending on its HasValue property
    (false and true, respectively).
    */

    uint32_t valueSize = sizeof(ArgumentType);
    bool hasValue = *reinterpret_cast<bool*>(reinterpret_cast<uint8_t*>(value) + valueSize);
    if (!hasValue)
        return NULL;

    return Box(type, value);
}

inline void* UnBox(Il2CppObject* obj)
{
    return tiny::vm::Object::Unbox(obj);
}

template<typename T>
inline void* UnBox(Il2CppObject* obj, TinyType* expectedBoxedType)
{
    COMPILE_TIME_CONST size_t alignedObjectSize = (sizeof(Il2CppObject) + ALIGN_OF(T) - 1) & ~(ALIGN_OF(T) - 1);
    if (obj->klass == expectedBoxedType)
        return reinterpret_cast<uint8_t*>(obj) + alignedObjectSize;

    tiny::vm::Exception::RaiseInvalidCastException(obj, expectedBoxedType);
    return NULL;
}

template<typename ArgumentType>
inline void UnBoxNullable(Il2CppObject* obj, TinyType* expectedBoxedClass, void* storage)
{
    // We only need to do type checks if obj is not null
    // Unboxing null nullable is perfectly valid and returns an instance that has no value
    if (obj != NULL)
    {
        if (obj->klass != expectedBoxedClass)
            tiny::vm::Exception::RaiseInvalidCastException(obj, expectedBoxedClass);
    }

    uint32_t valueSize = sizeof(ArgumentType);

    if (obj == NULL)
    {
        *(static_cast<uint8_t*>(storage) + valueSize) = false;
    }
    else
    {
        il2cpp::utils::MemoryUtils::MemoryCopy(storage, UnBox(obj), valueSize);
        *(static_cast<uint8_t*>(storage) + valueSize) = true;
    }
}

inline bool il2cpp_codegen_is_fake_boxed_object(RuntimeObject* object)
{
    return false;
}

template<typename TInput, typename TOutput, typename TFloat>
inline TOutput il2cpp_codegen_cast_floating_point(TFloat value)
{
#if IL2CPP_TARGET_ARM64 || IL2CPP_TARGET_ARMV7
    // On ARM, a cast from a floating point to integer value will use
    // the min or max value if the cast is out of range (instead of
    // overflowing like x86/x64). So first do a cast to the output
    // type (which is signed in .NET - the value stack does not have
    // unsigned types) to try to get the value into a range that will
    // actually be cast.
    if (value < 0)
        return (TOutput)((TInput)(TOutput)value);
#endif
    return (TOutput)((TInput)value);
}

// Exception support macros
#define IL2CPP_LEAVE(Offset, Target) \
    __leave_targets.push(Offset); \
    goto Target;

#define IL2CPP_END_FINALLY(Id) \
    goto __CLEANUP_ ## Id;

#define IL2CPP_CLEANUP(Id) \
    __CLEANUP_ ## Id:

#define IL2CPP_RETHROW_IF_UNHANDLED(ExcType) \
    if(__last_unhandled_exception) { \
        ExcType _tmp_exception_local = __last_unhandled_exception; \
        __last_unhandled_exception = 0; \
        il2cpp_codegen_raise_exception(_tmp_exception_local); \
        }

#define IL2CPP_JUMP_TBL(Offset, Target) \
    if(!__leave_targets.empty() && __leave_targets.top() == Offset) { \
        __leave_targets.pop(); \
        goto Target; \
        }

#define IL2CPP_END_CLEANUP(Offset, Target) \
    if(!__leave_targets.empty() && __leave_targets.top() == Offset) \
        goto Target;


template<typename T>
inline void Il2CppCodeGenWriteBarrier(T** targetAddress, T* object)
{
    // TODO
}

inline void il2cpp_codegen_memory_barrier()
{
    // The joy of singlethreading
}

template<bool, class T, class U>
struct pick_first;

template<class T, class U>
struct pick_first<true, T, U>
{
    typedef T type;
};

template<class T, class U>
struct pick_first<false, T, U>
{
    typedef U type;
};

template<class T, class U>
struct pick_bigger
{
    typedef typename pick_first<(sizeof(T) >= sizeof(U)), T, U>::type type;
};


template<typename T, typename U>
inline typename pick_bigger<T, U>::type il2cpp_codegen_multiply(T left, U right)
{
    return left * right;
}

template<typename T, typename U>
inline typename pick_bigger<T, U>::type il2cpp_codegen_add(T left, U right)
{
    return left + right;
}

template<typename T, typename U>
inline typename pick_bigger<T, U>::type il2cpp_codegen_subtract(T left, U right)
{
    return left - right;
}

inline TinyType* LookupTypeInfoFromCursor(uint32_t typeCursor)
{
    return reinterpret_cast<TinyType*>(Il2CppGetTinyTypeUniverse() + typeCursor);
}

inline String_t* LookupStringFromCursor(uint32_t stringCursor)
{
    return reinterpret_cast<String_t*>(Il2CppGetStringLiterals() + stringCursor);
}

inline bool HasParentOrIs(const TinyType* type, const TinyType* targetType)
{
    IL2CPP_ASSERT(type != NULL);
    IL2CPP_ASSERT(targetType != NULL);

    if (type == targetType)
        return true;

    uint16_t typeHierarchySize = type->typeHierarchySize;
    uint16_t targetTypeHierarchySize = targetType->typeHierarchySize;
    if (typeHierarchySize <= targetTypeHierarchySize)
        return false;

    if (type->GetTypeHierarchy()[targetTypeHierarchySize] == targetType)
        return true;

    return false;
}

inline bool IsAssignableFrom(const TinyType* klass, const TinyType* oklass)
{
    // Following checks are always going to fail for interfaces
    // if (!IsInterface(klass))
    {
        // Array
        /*if (klass->rank)
        {
            if (oklass->rank != klass->rank)
                return false;

            if (oklass->castClass->valuetype)
            {
                // Full array covariance is defined only for reference types.
                // For value types, array element reduced types must match
                return GetReducedType(klass->castClass) == GetReducedType(oklass->castClass);
            }

            return Class::IsAssignableFrom(klass->castClass, oklass->castClass);
        }
        // Left is System.Nullable<>
        else if (Class::IsNullable(klass))
        {
            if (Class::IsNullable(oklass))
                IL2CPP_NOT_IMPLEMENTED(Class::IsAssignableFrom);
            Il2CppClass* nullableArg = Class::GetNullableArgument(klass);
            return Class::IsAssignableFrom(nullableArg, oklass);
        }
        */

        /*if (klass->parent == il2cpp_defaults.multicastdelegate_class && klass->generic_class != NULL)
        {
            const Il2CppTypeDefinition* genericClass = MetadataCache::GetTypeDefinitionFromIndex(klass->generic_class->typeDefinitionIndex);
            const Il2CppGenericContainer* genericContainer = MetadataCache::GetGenericContainerFromIndex(genericClass->genericContainerIndex);

            if (IsGenericClassAssignableFrom(klass, oklass, genericContainer))
                return true;
        }*/

        if (HasParentOrIs(oklass, klass))
            return true;
    }

    /*if (klass->generic_class != NULL)
    {
        // checking for simple reference equality is not enough in this case because generic interface might have covariant and/or contravariant parameters

        const Il2CppTypeDefinition* genericInterface = MetadataCache::GetTypeDefinitionFromIndex(klass->generic_class->typeDefinitionIndex);
        const Il2CppGenericContainer* genericContainer = MetadataCache::GetGenericContainerFromIndex(genericInterface->genericContainerIndex);

        for (Il2CppClass* iter = oklass; iter != NULL; iter = iter->parent)
        {
            if (IsGenericClassAssignableFrom(klass, iter, genericContainer))
                return true;

            for (uint16_t i = 0; i < iter->interfaces_count; ++i)
            {
                if (IsGenericClassAssignableFrom(klass, iter->implementedInterfaces[i], genericContainer))
                    return true;
            }

            for (uint16_t i = 0; i < iter->interface_offsets_count; ++i)
            {
                if (IsGenericClassAssignableFrom(klass, iter->interfaceOffsets[i].interfaceType, genericContainer))
                    return true;
            }
        }
    }
    else
        */
    {
        const TinyType* const* interfaces = oklass->GetInterfaces();
        uint8_t size = oklass->interfacesSize;
        for (uint8_t i = 0; i != size; i++)
        {
            if (interfaces[i] == klass)
                return true;
        }
        return false;
    }
}

inline Il2CppObject* IsInst(Il2CppObject* obj, TinyType* klass)
{
    if (!obj)
        return NULL;

    TinyType* objClass = obj->klass;
    if (IsAssignableFrom(klass, objClass))
        return obj;

    return NULL;
}

inline RuntimeObject* IsInstClass(RuntimeObject* obj, TinyType* targetType)
{
    IL2CPP_ASSERT(targetType != NULL);

    if (!obj)
        return NULL;

    if (HasParentOrIs(obj->klass, targetType))
        return obj;

    return NULL;
}

inline RuntimeObject* IsInstSealed(RuntimeObject* obj, TinyType* targetType)
{
    if (!obj)
        return NULL;

    // optimized version to compare sealed classes
    return (obj->klass == targetType ? obj : NULL);
}

inline RuntimeObject* Castclass(RuntimeObject* obj, TinyType* targetType)
{
    if (!obj)
        return NULL;

    RuntimeObject* result = IsInst(obj, targetType);
    if (result)
        return result;

    tiny::vm::Exception::RaiseInvalidCastException(obj, targetType);
    return NULL;
}

inline RuntimeObject* CastclassSealed(RuntimeObject *obj, TinyType* targetType)
{
    if (!obj)
        return NULL;

    RuntimeObject* result = IsInstSealed(obj, targetType);
    if (result)
        return result;

    tiny::vm::Exception::RaiseInvalidCastException(obj, targetType);
    return NULL;
}

inline RuntimeObject* CastclassClass(RuntimeObject *obj, TinyType* targetType)
{
    if (!obj)
        return NULL;

    RuntimeObject* result = IsInstClass(obj, targetType);
    if (result)
        return result;

    tiny::vm::Exception::RaiseInvalidCastException(obj, targetType);
    return NULL;
}

inline bool il2cpp_codegen_is_assignable_from(Type_t* left, Type_t* right)
{
    if (right == NULL)
        return false;

    return IsAssignableFrom(reinterpret_cast<Il2CppReflectionType*>(left)->typeHandle, reinterpret_cast<Il2CppReflectionType*>(right)->typeHandle);
}

// il2cpp generates direct calls to this specific name
inline bool il2cpp_codegen_class_is_assignable_from(TinyType* left, TinyType* right)
{
    if (right == NULL)
        return false;
    return IsAssignableFrom(left, right);
}

inline TinyType* il2cpp_codegen_object_class(RuntimeObject *obj)
{
    return obj->klass;
}

inline int il2cpp_codegen_get_offset_to_string_data()
{
    return offsetof(Il2CppString, chars);
}

inline String_t* il2cpp_codegen_string_new_length(int length)
{
    return reinterpret_cast<String_t*>(tiny::vm::String::NewLen(length));
}

inline String_t* il2cpp_codegen_string_new_utf16(const il2cpp::utils::StringView<Il2CppChar>& str)
{
    return (String_t*)tiny::vm::String::NewLen(str.Str(), static_cast<uint32_t>(str.Length()));
}

inline String_t* il2cpp_codegen_string_new_from_char_array(Il2CppArray* characterArray, int32_t startIndex, int32_t length)
{
    il2cpp_array_size_t arraySize = characterArray->max_length;
    if (startIndex + length > ARRAY_LENGTH_AS_INT32(arraySize) || startIndex < 0)
        il2cpp_codegen_raise_exception(NULL);

    return il2cpp_codegen_string_new_utf16(il2cpp::utils::StringView<Il2CppChar>(reinterpret_cast<Il2CppChar*>(characterArray + 1), startIndex, length));
}

template<typename T>
inline Il2CppArray* SZArrayNew(TinyType* arrayType, uint32_t elementSize, uint32_t arrayLength)
{
    COMPILE_TIME_CONST size_t alignedArraySize = (sizeof(Il2CppArray) + ALIGN_OF(T) - 1) & ~(ALIGN_OF(T) - 1);
    Il2CppArray* szArray = static_cast<Il2CppArray*>(il2cpp_codegen_object_new(alignedArraySize + elementSize * arrayLength, arrayType));
    szArray->max_length = arrayLength;
    return szArray;
}

template<size_t N>
inline Il2CppMultidimensionalArray<N>* GenArrayNew(TinyType* arrayType, uint32_t elementSize, il2cpp_array_size_t(&dimensions)[N])
{
    il2cpp_array_size_t arrayLength = elementSize;
    for (uint32_t i = 0; i < N; i++)
        arrayLength *= dimensions[i];

    Il2CppMultidimensionalArray<N>* genArray = static_cast<Il2CppMultidimensionalArray<N>*>(il2cpp_codegen_object_new(sizeof(Il2CppMultidimensionalArray<N>) + elementSize * arrayLength, arrayType));
    for (uint32_t i = 0; i < N; i++)
        genArray->bounds[i] = dimensions[i];

    return genArray;
}

inline int32_t il2cpp_codegen_get_array_length(Il2CppArray* szArray)
{
    return static_cast<int32_t>(szArray->max_length);
}

inline int32_t il2cpp_codegen_get_array_length(Il2CppArray* genArray, int32_t dimension)
{
    return static_cast<int32_t>(reinterpret_cast<Il2CppMultidimensionalArray<1>*>(genArray)->bounds[dimension]);
}

inline Type_t* il2cpp_codegen_get_type(Il2CppObject* obj)
{
    return reinterpret_cast<Type_t*>(tiny::vm::Type::GetTypeFromHandle((intptr_t)obj->klass));
}

inline Type_t* il2cpp_codegen_get_base_type(const Type_t* t)
{
    const Il2CppReflectionType* type = reinterpret_cast<const Il2CppReflectionType*>(t);
    const TinyType* tinyType = type->typeHandle;
    uint8_t typeHierarchySize = tinyType->typeHierarchySize;
    if (typeHierarchySize == 0)
        return NULL;

    return const_cast<Type_t*>(reinterpret_cast<const Type_t*>(tiny::vm::Type::GetTypeFromHandle((intptr_t)(tinyType->GetTypeHierarchy()[typeHierarchySize - 1]))));
}

inline int il2cpp_codegen_double_to_string(double value, uint8_t* format, uint8_t* buffer, int bufferLength)
{
    // return number of characters written to the buffer. if the return value greater than bufferLength
    // means the number of characters would be written to the buffer if there is enough space
    return snprintf(reinterpret_cast<char*>(buffer), bufferLength, reinterpret_cast<char*>(format), value);
}

inline MulticastDelegate_t* il2cpp_codegen_create_combined_delegate(Type_t* type, Il2CppArray* delegates, int delegateCount)
{
    Il2CppReflectionType* reflectionType = tiny::vm::Type::GetTypeFromHandle((intptr_t) reinterpret_cast<Il2CppReflectionType*>(type)->typeHandle);
    Il2CppMulticastDelegate* result = static_cast<Il2CppMulticastDelegate*>(il2cpp_codegen_object_new(sizeof(Il2CppMulticastDelegate), const_cast<TinyType*>(reflectionType->typeHandle)));
    result->delegates = delegates;
    result->delegateCount = delegateCount;
    return reinterpret_cast<MulticastDelegate_t*>(result);
}

inline const VirtualInvokeData& il2cpp_codegen_get_virtual_invoke_data(Il2CppMethodSlot slot, const RuntimeObject* obj)
{
    Assert(slot != kInvalidIl2CppMethodSlot && "il2cpp_codegen_get_virtual_invoke_data got called on a non-virtual method");
    return obj->klass->GetVTable()[slot];
}

inline const VirtualInvokeData& il2cpp_codegen_get_interface_invoke_data(Il2CppMethodSlot slot, const Il2CppObject* obj, TinyType* declaringInterface)
{
    for (int i = 0; i < obj->klass->interfacesSize; ++i)
    {
        if (obj->klass->GetInterfaces()[i] == declaringInterface)
            return il2cpp_codegen_get_virtual_invoke_data((Il2CppMethodSlot)obj->klass->GetInterfaceOffsets()[i] + slot, obj);
    }

    tiny::vm::Exception::Raise(NULL);
    IL2CPP_UNREACHABLE;
}

inline Exception_t* il2cpp_codegen_get_overflow_exception()
{
    return NULL;
}

inline Exception_t* il2cpp_codegen_get_argument_exception(const char* param, const char* msg)
{
    return NULL;
}

inline Exception_t* il2cpp_codegen_get_missing_method_exception(const char* msg)
{
    return NULL;
}

NORETURN inline void il2cpp_codegen_no_return()
{
    IL2CPP_UNREACHABLE;
}

NORETURN inline void il2cpp_codegen_raise_exception(Exception_t* ex)
{
    tiny::vm::Exception::Raise((Il2CppException*)ex);
    IL2CPP_UNREACHABLE;
}

NORETURN inline void il2cpp_codegen_raise_execution_engine_exception(const void* method)
{
    tiny::vm::Exception::Raise(NULL);
    IL2CPP_UNREACHABLE;
}

inline Exception_t* il2cpp_codegen_get_marshal_directive_exception(const char* msg)
{
    return NULL;
}

#define IL2CPP_RAISE_MANAGED_EXCEPTION(ex, lastManagedFrame) \
    do {\
        il2cpp_codegen_raise_exception(ex);\
        IL2CPP_UNREACHABLE;\
    } while (0)

#if _DEBUG
#define IL2CPP_ARRAY_BOUNDS_CHECK(index, length) \
    do { \
        if (((uint32_t)(index)) >= ((uint32_t)length)) tiny::vm::Exception::RaiseGetIndexOutOfRangeException(); \
    } while (0)
#else
#define IL2CPP_ARRAY_BOUNDS_CHECK(index, length)
#endif

inline void ArrayElementTypeCheck(Il2CppArray* array, void* value)
{
}

template<typename FunctionPointerType, size_t dynamicLibraryLength, size_t entryPointLength>
inline FunctionPointerType il2cpp_codegen_resolve_pinvoke(const Il2CppNativeChar(&nativeDynamicLibrary)[dynamicLibraryLength], const char(&entryPoint)[entryPointLength],
    Il2CppCallConvention callingConvention, Il2CppCharSet charSet, int parameterSize, bool isNoMangle)
{
    const PInvokeArguments pinvokeArgs =
    {
        il2cpp::utils::StringView<Il2CppNativeChar>(nativeDynamicLibrary),
        il2cpp::utils::StringView<char>(entryPoint),
        callingConvention,
        charSet,
        parameterSize,
        isNoMangle
    };

    return reinterpret_cast<FunctionPointerType>(tiny::vm::PlatformInvoke::Resolve(pinvokeArgs));
}

template<typename T>
inline T* il2cpp_codegen_marshal_allocate()
{
    return static_cast<T*>(tiny::vm::PlatformInvoke::MarshalAllocate(sizeof(T)));
}

inline char* il2cpp_codegen_marshal_string(String_t* string)
{
    if (string == NULL)
        return NULL;

    Il2CppString* managedString = ((Il2CppString*)string);
    return tiny::vm::PlatformInvoke::MarshalCSharpStringToCppString(managedString->chars, managedString->length);
}

inline void il2cpp_codegen_marshal_string_fixed(String_t* string, char* buffer, uint32_t numberOfCharacters)
{
    IL2CPP_ASSERT(numberOfCharacters > 0);

    if (string == NULL)
    {
        *buffer = '\0';
        return;
    }

    Il2CppString* managedString = ((Il2CppString*)string);
    tiny::vm::PlatformInvoke::MarshalCSharpStringToFixedCppStringBuffer(managedString->chars, managedString->length, buffer, numberOfCharacters);
}

inline intptr_t il2cpp_codegen_marshal_get_function_pointer_for_delegate(const Delegate_t* d)
{
    return reinterpret_cast<intptr_t>(reinterpret_cast<const Il2CppDelegate*>(d)->m_ReversePInvokeWrapperPtr);
}

inline Il2CppChar* il2cpp_codegen_marshal_wstring(String_t* string)
{
    if (string == NULL)
        return NULL;

    Il2CppString* managedString = ((Il2CppString*)string);
    return tiny::vm::PlatformInvoke::MarshalCSharpStringToCppWString(managedString->chars, managedString->length);
}

inline void il2cpp_codegen_marshal_wstring_fixed(String_t* string, Il2CppChar* buffer, uint32_t numberOfCharacters)
{
    IL2CPP_ASSERT(numberOfCharacters > 0);

    if (string == NULL)
    {
        *buffer = '\0';
        return;
    }

    Il2CppString* managedString = ((Il2CppString*)string);
    tiny::vm::PlatformInvoke::MarshalCSharpStringToFixedCppWStringBuffer(managedString->chars, managedString->length, buffer, numberOfCharacters);
}

inline String_t* il2cpp_codegen_marshal_string_result(const char* value)
{
    if (value == NULL)
        return NULL;

    return reinterpret_cast<String_t*>(tiny::vm::PlatformInvoke::MarshalCppStringToCSharpStringResult(value));
}

inline String_t* il2cpp_codegen_marshal_wstring_result(const Il2CppChar* value)
{
    if (value == NULL)
        return NULL;

    return reinterpret_cast<String_t*>(tiny::vm::PlatformInvoke::MarshalCppWStringToCSharpStringResult(value));
}

template<typename T>
inline T* il2cpp_codegen_marshal_allocate_array(size_t length)
{
    return static_cast<T*>(tiny::vm::PlatformInvoke::MarshalAllocate(sizeof(T) * length));
}

inline void il2cpp_codegen_marshal_free(void* ptr)
{
    tiny::vm::PlatformInvoke::MarshalFree(ptr);
}

inline String_t* il2cpp_codegen_marshal_ptr_to_string_ansi(intptr_t ptr)
{
    return il2cpp_codegen_marshal_string_result(reinterpret_cast<const char*>(ptr));
}

inline intptr_t il2cpp_codegen_marshal_string_to_co_task_mem_ansi(String_t* ptr)
{
    return reinterpret_cast<intptr_t>(il2cpp_codegen_marshal_string(ptr));
}

inline void il2cpp_codegen_marshal_string_free_co_task_mem(intptr_t ptr)
{
    il2cpp_codegen_marshal_free(reinterpret_cast<void*>(ptr));
}

template<typename T>
struct Il2CppReversePInvokeMethodHolder
{
    Il2CppReversePInvokeMethodHolder(T** storageAddress) :
        m_LastValue(*storageAddress),
        m_StorageAddress(storageAddress)
    {
    }

    ~Il2CppReversePInvokeMethodHolder()
    {
        *m_StorageAddress = m_LastValue;
    }

private:
    T* const m_LastValue;
    T** const m_StorageAddress;
};

inline void* InterlockedExchangeImplRef(void** location, void* value)
{
    return tiny::icalls::mscorlib::System::Threading::Interlocked::ExchangePointer(location, value);
}

template<typename T>
inline T InterlockedCompareExchangeImpl(T* location, T value, T comparand)
{
    return (T)tiny::icalls::mscorlib::System::Threading::Interlocked::CompareExchange_T((void**)location, value, comparand);
}

template<typename T>
inline T InterlockedExchangeImpl(T* location, T value)
{
    return (T)InterlockedExchangeImplRef((void**)location, value);
}

void il2cpp_codegen_stacktrace_push_frame(TinyStackFrameInfo& frame);

void il2cpp_codegen_stacktrace_pop_frame();

struct StackTraceSentry
{
    StackTraceSentry(const RuntimeMethod* method) : m_method(method)
    {
        TinyStackFrameInfo frame_info;

        frame_info.method = (TinyMethod*)method;

        il2cpp_codegen_stacktrace_push_frame(frame_info);
    }

    ~StackTraceSentry()
    {
        il2cpp_codegen_stacktrace_pop_frame();
    }

private:
    const RuntimeMethod* m_method;
};

inline const RuntimeMethod* GetVirtualMethodInfo(RuntimeObject* pThis, Il2CppMethodSlot slot)
{
    if (!pThis)
        tiny::vm::Exception::Raise(NULL);

    return (const RuntimeMethod*)pThis->klass->GetVTable()[slot];
}

inline void il2cpp_codegen_no_reverse_pinvoke_wrapper(const char* methodName, const char* reason)
{
    std::string message = "No reverse pinvoke wrapper exists for method: '";
    message += methodName;
    message += "' because ";
    message += reason;
    tiny::vm::Runtime::FailFast(message.c_str());
}
