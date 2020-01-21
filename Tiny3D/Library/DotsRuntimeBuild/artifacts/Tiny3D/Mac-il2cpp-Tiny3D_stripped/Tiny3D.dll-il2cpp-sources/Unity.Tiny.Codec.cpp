#include "il2cpp-config.h"

#ifndef _MSC_VER
# include <alloca.h>
#else
# include <malloc.h>
#endif


#include <cstring>
#include <string.h>
#include <stdio.h>
#include <cmath>
#include <limits>
#include <assert.h>
#include <stdint.h>

#include "codegen/il2cpp-codegen.h"
#include "il2cpp-object-internals.h"


// System.ArgumentException
struct ArgumentException_tAFF8E8471BCD8973F9F203E79A9E3B6ED147348D;
// System.String
struct String_t;

IL2CPP_EXTERN_C const RuntimeMethod ArgumentException__ctor_mACA65098F10D84203B8DB2A6BBFD8FB7DC9A0807_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod CodecService_DecompressLZ4_m2CED01314262F71229FFADB4D223C521F63E37FD_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod CodecService_Decompress_mC0B9FFAE31F516085670CBC066F935EEB6473574_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod String_Format_mC81E9E03366A14A6873576BD0DBD2CCB14BBD8DF_RuntimeMethod_var;


IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif

// <Module>
struct  U3CModuleU3E_tCC37334A57DB1574081D65D5DBF1745B6762C5A3 
{
public:

public:
};


// System.Object

struct Il2CppArrayBounds;

// System.Array


// System.Exception
struct  Exception_t  : public RuntimeObject
{
public:
	// System.String System.Exception::<Message>k__BackingField
	String_t* ___U3CMessageU3Ek__BackingField_0;
	// System.String System.Exception::<StackTrace>k__BackingField
	String_t* ___U3CStackTraceU3Ek__BackingField_1;

public:
	inline String_t* get_U3CMessageU3Ek__BackingField_0() const { return ___U3CMessageU3Ek__BackingField_0; }
	inline String_t** get_address_of_U3CMessageU3Ek__BackingField_0() { return &___U3CMessageU3Ek__BackingField_0; }
	inline void set_U3CMessageU3Ek__BackingField_0(String_t* value)
	{
		___U3CMessageU3Ek__BackingField_0 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CMessageU3Ek__BackingField_0), (void*)value);
	}

	inline String_t* get_U3CStackTraceU3Ek__BackingField_1() const { return ___U3CStackTraceU3Ek__BackingField_1; }
	inline String_t** get_address_of_U3CStackTraceU3Ek__BackingField_1() { return &___U3CStackTraceU3Ek__BackingField_1; }
	inline void set_U3CStackTraceU3Ek__BackingField_1(String_t* value)
	{
		___U3CStackTraceU3Ek__BackingField_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___U3CStackTraceU3Ek__BackingField_1), (void*)value);
	}
};


// System.String
struct  String_t  : public RuntimeObject
{
public:
	// System.Int32 System.String::_length
	int32_t ____length_0;
	// System.Char System.String::_firstChar
	Il2CppChar ____firstChar_1;

public:
	inline int32_t get__length_0() const { return ____length_0; }
	inline int32_t* get_address_of__length_0() { return &____length_0; }
	inline void set__length_0(int32_t value)
	{
		____length_0 = value;
	}

	inline Il2CppChar get__firstChar_1() const { return ____firstChar_1; }
	inline Il2CppChar* get_address_of__firstChar_1() { return &____firstChar_1; }
	inline void set__firstChar_1(Il2CppChar value)
	{
		____firstChar_1 = value;
	}
};

extern void* String_t_StaticFields_Storage;
struct String_t_StaticFields
{
public:
	// System.String System.String::Empty
	String_t* ___Empty_2;

public:
	inline String_t* get_Empty_2() const { return ___Empty_2; }
	inline String_t** get_address_of_Empty_2() { return &___Empty_2; }
	inline void set_Empty_2(String_t* value)
	{
		___Empty_2 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___Empty_2), (void*)value);
	}
};


// System.ValueType
struct  ValueType_t64F15EF9CF2A81C067DEFA394EE6E92316BC80B5  : public RuntimeObject
{
public:

public:
};

// Native definition for P/Invoke marshalling of System.ValueType
struct ValueType_t64F15EF9CF2A81C067DEFA394EE6E92316BC80B5_marshaled_pinvoke
{
};

// Unity.Tiny.Codec.CodecService
struct  CodecService_t5C8C4FE08C452F0057CDFBF75CD18F7D69C429AB  : public RuntimeObject
{
public:

public:
};


// System.Boolean
struct  Boolean_t65DF67C4FFCA1C56BB2250E17E0BC1E268868230 
{
public:
	union
	{
		struct
		{
		};
		uint8_t Boolean_t65DF67C4FFCA1C56BB2250E17E0BC1E268868230__padding[1];
	};

public:
};


// System.Byte
struct  Byte_t890F19AC4C0E036E44480A958D52DA1FEBBCA282 
{
public:
	// System.Byte System.Byte::m_value
	uint8_t ___m_value_0;

public:
	inline uint8_t get_m_value_0() const { return ___m_value_0; }
	inline uint8_t* get_address_of_m_value_0() { return &___m_value_0; }
	inline void set_m_value_0(uint8_t value)
	{
		___m_value_0 = value;
	}
};


// System.Enum
struct  Enum_tB7F86F1F9E78CB0C85D22C1802ADB7E32CB8D13B  : public ValueType_t64F15EF9CF2A81C067DEFA394EE6E92316BC80B5
{
public:

public:
};

// Native definition for P/Invoke marshalling of System.Enum
struct Enum_tB7F86F1F9E78CB0C85D22C1802ADB7E32CB8D13B_marshaled_pinvoke
{
};

// System.Int32
struct  Int32_t8267F923518B87EC4FD10CAAE71FFCB4F6871988 
{
public:
	// System.Int32 System.Int32::m_value
	int32_t ___m_value_0;

public:
	inline int32_t get_m_value_0() const { return ___m_value_0; }
	inline int32_t* get_address_of_m_value_0() { return &___m_value_0; }
	inline void set_m_value_0(int32_t value)
	{
		___m_value_0 = value;
	}
};


// System.SystemException
struct  SystemException_tCFBA544FDD18D7EE9F8F0037FA3C892ED03386CB  : public Exception_t
{
public:

public:
};


// System.Void
struct  Void_t8DA50A09EC87863B9856F55E2EC298E97345FA9C 
{
public:
	union
	{
		struct
		{
		};
		uint8_t Void_t8DA50A09EC87863B9856F55E2EC298E97345FA9C__padding[1];
	};

public:
};


// System.ArgumentException
struct  ArgumentException_tAFF8E8471BCD8973F9F203E79A9E3B6ED147348D  : public SystemException_tCFBA544FDD18D7EE9F8F0037FA3C892ED03386CB
{
public:

public:
};


// Unity.Tiny.Codec.Codec
struct  Codec_tC74A46E8A36F5F6221FBA076897BA24B6835137F 
{
public:
	// System.Int32 Unity.Tiny.Codec.Codec::value__
	int32_t ___value___0;

public:
	inline int32_t get_value___0() const { return ___value___0; }
	inline int32_t* get_address_of_value___0() { return &___value___0; }
	inline void set_value___0(int32_t value)
	{
		___value___0 = value;
	}
};

#ifdef __clang__
#pragma clang diagnostic pop
#endif



// System.Int32 Unity.Tiny.Codec.CodecService::DecompressLZ4(System.Byte*,System.Byte*,System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t CodecService_DecompressLZ4_m2CED01314262F71229FFADB4D223C521F63E37FD (uint8_t* ___src0, uint8_t* ___dst1, int32_t ___compressedSize2, int32_t ___dstCapacity3);
// System.String System.String::Format(System.String,System.Object)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* String_Format_mC81E9E03366A14A6873576BD0DBD2CCB14BBD8DF (String_t* ___format0, RuntimeObject * ___arg11);
// System.Void System.ArgumentException::.ctor(System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void ArgumentException__ctor_mACA65098F10D84203B8DB2A6BBFD8FB7DC9A0807 (ArgumentException_tAFF8E8471BCD8973F9F203E79A9E3B6ED147348D * __this, String_t* ___arg0);
// System.Boolean Unity.Tiny.Codec.CodecService::Decompress(Unity.Tiny.Codec.Codec,System.Byte*&,System.Int32,System.Byte*,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool CodecService_Decompress_mC0B9FFAE31F516085670CBC066F935EEB6473574 (int32_t ___codec0, uint8_t** ___compressedData1, int32_t ___compressedSize2, uint8_t* ___decompressedData3, int32_t ___decompressedSize4);
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_CodecService_Decompress_mC0B9FFAE31F516085670CBC066F935EEB6473574()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Boolean Unity.Tiny.Codec.CodecService::Decompress(Unity.Tiny.Codec.Codec,System.Byte*&,System.Int32,System.Byte*,System.Int32)", "it does not have the [MonoPInvokeCallback] attribute.");
}
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_CodecService_DecompressLZ4_m2CED01314262F71229FFADB4D223C521F63E37FD()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Int32 Unity.Tiny.Codec.CodecService::DecompressLZ4(System.Byte*,System.Byte*,System.Int32,System.Int32)", "it does not have the [MonoPInvokeCallback] attribute.");
}
// System.Boolean Unity.Tiny.Codec.CodecService::Decompress(Unity.Tiny.Codec.Codec,System.Byte*&,System.Int32,System.Byte*,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool CodecService_Decompress_mC0B9FFAE31F516085670CBC066F935EEB6473574 (int32_t ___codec0, uint8_t** ___compressedData1, int32_t ___compressedSize2, uint8_t* ___decompressedData3, int32_t ___decompressedSize4)
{
	{
		// switch (codec)
		int32_t L_0 = ___codec0;
		if (!L_0)
		{
			goto IL_0016;
		}
	}
	{
		int32_t L_1 = ___codec0;
		if ((!(((uint32_t)L_1) == ((uint32_t)1))))
		{
			goto IL_0016;
		}
	}
	{
		// case Codec.LZ4: return DecompressLZ4(compressedData, decompressedData, compressedSize, decompressedSize) > 0;
		uint8_t** L_2 = ___compressedData1;
		uint8_t* L_3 = ___decompressedData3;
		int32_t L_4 = ___compressedSize2;
		int32_t L_5 = ___decompressedSize4;
		int32_t L_6 = CodecService_DecompressLZ4_m2CED01314262F71229FFADB4D223C521F63E37FD((uint8_t*)(uint8_t*)(*((intptr_t*)L_2)), (uint8_t*)(uint8_t*)L_3, L_4, L_5);
		return (bool)((((int32_t)L_6) > ((int32_t)0))? 1 : 0);
	}

IL_0016:
	{
		// default: throw new ArgumentException($"Invalid codec '{codec}' specified");
		int32_t L_7 = ___codec0;
		int32_t L_8 = L_7;
		RuntimeObject * L_9 = Box(LookupTypeInfoFromCursor(IL2CPP_SIZEOF_VOID_P == 4 ? 2456 : 4824), &L_8);
		String_t* L_10 = String_Format_mC81E9E03366A14A6873576BD0DBD2CCB14BBD8DF(LookupStringFromCursor(IL2CPP_SIZEOF_VOID_P == 4 ? 9436 : 10280), L_9);
		ArgumentException_tAFF8E8471BCD8973F9F203E79A9E3B6ED147348D * L_11 = (ArgumentException_tAFF8E8471BCD8973F9F203E79A9E3B6ED147348D *)il2cpp_codegen_object_new(sizeof(ArgumentException_tAFF8E8471BCD8973F9F203E79A9E3B6ED147348D), LookupTypeInfoFromCursor(IL2CPP_SIZEOF_VOID_P == 4 ? 364 : 720));
		ArgumentException__ctor_mACA65098F10D84203B8DB2A6BBFD8FB7DC9A0807(L_11, L_10);
		IL2CPP_RAISE_MANAGED_EXCEPTION(L_11, &CodecService_Decompress_mC0B9FFAE31F516085670CBC066F935EEB6473574_RuntimeMethod_var);
	}
}
#if FORCE_PINVOKE_INTERNAL
IL2CPP_EXTERN_C int32_t DEFAULT_CALL Decompress_LZ4(uint8_t*, uint8_t*, int32_t, int32_t);
#endif
// System.Int32 Unity.Tiny.Codec.CodecService::DecompressLZ4(System.Byte*,System.Byte*,System.Int32,System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR int32_t CodecService_DecompressLZ4_m2CED01314262F71229FFADB4D223C521F63E37FD (uint8_t* ___src0, uint8_t* ___dst1, int32_t ___compressedSize2, int32_t ___dstCapacity3)
{
	typedef int32_t (DEFAULT_CALL *PInvokeFunc) (uint8_t*, uint8_t*, int32_t, int32_t);
	#if !FORCE_PINVOKE_INTERNAL
	static PInvokeFunc il2cppPInvokeFunc;
	if (il2cppPInvokeFunc == NULL)
	{
		int parameterSize = sizeof(uint8_t*) + sizeof(uint8_t*) + sizeof(int32_t) + sizeof(int32_t);
		il2cppPInvokeFunc = il2cpp_codegen_resolve_pinvoke<PInvokeFunc>(IL2CPP_NATIVE_STRING("lib_unity_tiny_codec"), "Decompress_LZ4", IL2CPP_CALL_DEFAULT, CHARSET_NOT_SPECIFIED, parameterSize, false);
		IL2CPP_ASSERT(il2cppPInvokeFunc != NULL);
	}
	#endif

	// Native function invocation
	#if FORCE_PINVOKE_INTERNAL
	int32_t returnValue = reinterpret_cast<PInvokeFunc>(Decompress_LZ4)(___src0, ___dst1, ___compressedSize2, ___dstCapacity3);
	#else
	int32_t returnValue = il2cppPInvokeFunc(___src0, ___dst1, ___compressedSize2, ___dstCapacity3);
	#endif

	return returnValue;
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
