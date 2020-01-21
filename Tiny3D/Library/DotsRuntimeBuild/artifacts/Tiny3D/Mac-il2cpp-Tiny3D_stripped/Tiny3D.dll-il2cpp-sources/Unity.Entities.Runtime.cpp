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


// System.Byte[]
struct ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E;
// System.String
struct String_t;

IL2CPP_EXTERN_C const RuntimeMethod GuidUtility_NewGuid_mA6DC31F46897B3DC9548798A2A8F85BCFDFE7E98_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod Guid__ctor_m788D5E27384FE5B478CD3E384F705AE8FC6FB72E_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod MurmurHash3_ComputeHash128_m8E3A9DFB5B99FDE7CE97D0C5F656923092861CFF_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod MurmurHash3_MurmurHash3_x64_128_m590C9368E8E1F1C97DC2A638E82EFF9AD183B6B9_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod MurmurHash3_fmix64_m9DBDA328965DE090DB08B27E1D45D1DF27BF2F0C_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod String_get_Chars_mD46069ADC2F1285DCD7D07D65E8C9FC002DAFA1E_RuntimeMethod_var;

struct ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E;

IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif

// <Module>
struct  U3CModuleU3E_tF120708CE748695D9A299360F7E7D2B0D0F2D6EA 
{
public:

public:
};


// System.Object

struct Il2CppArrayBounds;

// System.Array


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

// Unity.Entities.Runtime.Hashing.GuidUtility
struct  GuidUtility_t4D2C0B7A9636F9FD1AB34E8A34858D838052AF9F  : public RuntimeObject
{
public:

public:
};


// Unity.Entities.Runtime.Hashing.MurmurHash3
struct  MurmurHash3_tF7D9D3976D859A6D3A163A7C2EF8D22056935915  : public RuntimeObject
{
public:

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


// System.Char
struct  Char_tA8ACF9D1EB321BD6AE88B61C6A30C783B2B06BCE 
{
public:
	// System.Char System.Char::m_value
	Il2CppChar ___m_value_0;

public:
	inline Il2CppChar get_m_value_0() const { return ___m_value_0; }
	inline Il2CppChar* get_address_of_m_value_0() { return &___m_value_0; }
	inline void set_m_value_0(Il2CppChar value)
	{
		___m_value_0 = value;
	}
};


// System.Guid
struct  Guid_t 
{
public:
	// System.Int32 System.Guid::_a
	int32_t ____a_1;
	// System.Int16 System.Guid::_b
	int16_t ____b_2;
	// System.Int16 System.Guid::_c
	int16_t ____c_3;
	// System.Byte System.Guid::_d
	uint8_t ____d_4;
	// System.Byte System.Guid::_e
	uint8_t ____e_5;
	// System.Byte System.Guid::_f
	uint8_t ____f_6;
	// System.Byte System.Guid::_g
	uint8_t ____g_7;
	// System.Byte System.Guid::_h
	uint8_t ____h_8;
	// System.Byte System.Guid::_i
	uint8_t ____i_9;
	// System.Byte System.Guid::_j
	uint8_t ____j_10;
	// System.Byte System.Guid::_k
	uint8_t ____k_11;

public:
	inline int32_t get__a_1() const { return ____a_1; }
	inline int32_t* get_address_of__a_1() { return &____a_1; }
	inline void set__a_1(int32_t value)
	{
		____a_1 = value;
	}

	inline int16_t get__b_2() const { return ____b_2; }
	inline int16_t* get_address_of__b_2() { return &____b_2; }
	inline void set__b_2(int16_t value)
	{
		____b_2 = value;
	}

	inline int16_t get__c_3() const { return ____c_3; }
	inline int16_t* get_address_of__c_3() { return &____c_3; }
	inline void set__c_3(int16_t value)
	{
		____c_3 = value;
	}

	inline uint8_t get__d_4() const { return ____d_4; }
	inline uint8_t* get_address_of__d_4() { return &____d_4; }
	inline void set__d_4(uint8_t value)
	{
		____d_4 = value;
	}

	inline uint8_t get__e_5() const { return ____e_5; }
	inline uint8_t* get_address_of__e_5() { return &____e_5; }
	inline void set__e_5(uint8_t value)
	{
		____e_5 = value;
	}

	inline uint8_t get__f_6() const { return ____f_6; }
	inline uint8_t* get_address_of__f_6() { return &____f_6; }
	inline void set__f_6(uint8_t value)
	{
		____f_6 = value;
	}

	inline uint8_t get__g_7() const { return ____g_7; }
	inline uint8_t* get_address_of__g_7() { return &____g_7; }
	inline void set__g_7(uint8_t value)
	{
		____g_7 = value;
	}

	inline uint8_t get__h_8() const { return ____h_8; }
	inline uint8_t* get_address_of__h_8() { return &____h_8; }
	inline void set__h_8(uint8_t value)
	{
		____h_8 = value;
	}

	inline uint8_t get__i_9() const { return ____i_9; }
	inline uint8_t* get_address_of__i_9() { return &____i_9; }
	inline void set__i_9(uint8_t value)
	{
		____i_9 = value;
	}

	inline uint8_t get__j_10() const { return ____j_10; }
	inline uint8_t* get_address_of__j_10() { return &____j_10; }
	inline void set__j_10(uint8_t value)
	{
		____j_10 = value;
	}

	inline uint8_t get__k_11() const { return ____k_11; }
	inline uint8_t* get_address_of__k_11() { return &____k_11; }
	inline void set__k_11(uint8_t value)
	{
		____k_11 = value;
	}
};

extern void* Guid_t_StaticFields_Storage;
struct Guid_t_StaticFields
{
public:
	// System.Guid System.Guid::Empty
	Guid_t  ___Empty_0;

public:
	inline Guid_t  get_Empty_0() const { return ___Empty_0; }
	inline Guid_t * get_address_of_Empty_0() { return &___Empty_0; }
	inline void set_Empty_0(Guid_t  value)
	{
		___Empty_0 = value;
	}
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


// System.UInt32
struct  UInt32_t23DAB7EB6DE6B45CAEB462CA072B1694A24D7B7D 
{
public:
	// System.UInt32 System.UInt32::m_value
	uint32_t ___m_value_0;

public:
	inline uint32_t get_m_value_0() const { return ___m_value_0; }
	inline uint32_t* get_address_of_m_value_0() { return &___m_value_0; }
	inline void set_m_value_0(uint32_t value)
	{
		___m_value_0 = value;
	}
};


// System.UInt64
struct  UInt64_t43F1115A4857914A5475842725AA384F8B7000B6 
{
public:
	// System.UInt64 System.UInt64::m_value
	uint64_t ___m_value_0;

public:
	inline uint64_t get_m_value_0() const { return ___m_value_0; }
	inline uint64_t* get_address_of_m_value_0() { return &___m_value_0; }
	inline void set_m_value_0(uint64_t value)
	{
		___m_value_0 = value;
	}
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

#ifdef __clang__
#pragma clang diagnostic pop
#endif
// System.Byte[]
struct ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E  : public RuntimeArray
{
public:
	ALIGN_FIELD (8) uint8_t m_Items[1];

public:
	inline uint8_t GetAt(il2cpp_array_size_t index) const
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items[index];
	}
	inline uint8_t* GetAddressAt(il2cpp_array_size_t index)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		return m_Items + index;
	}
	inline void SetAt(il2cpp_array_size_t index, uint8_t value)
	{
		IL2CPP_ARRAY_BOUNDS_CHECK(index, (uint32_t)(this)->max_length);
		m_Items[index] = value;
	}
	inline uint8_t GetAtUnchecked(il2cpp_array_size_t index) const
	{
		return m_Items[index];
	}
	inline uint8_t* GetAddressAtUnchecked(il2cpp_array_size_t index)
	{
		return m_Items + index;
	}
	inline void SetAtUnchecked(il2cpp_array_size_t index, uint8_t value)
	{
		m_Items[index] = value;
	}
};



// System.Byte[] Unity.Entities.Runtime.Hashing.MurmurHash3::ComputeHash128(System.Byte[],System.UInt32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* MurmurHash3_ComputeHash128_m8E3A9DFB5B99FDE7CE97D0C5F656923092861CFF (ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* ___bytes0, uint32_t ___seed1);
// System.Void System.Guid::.ctor(System.Byte[])
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Guid__ctor_m788D5E27384FE5B478CD3E384F705AE8FC6FB72E (Guid_t * __this, ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* ___bytes0);
// System.Char System.String::get_Chars(System.Int32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Il2CppChar String_get_Chars_mD46069ADC2F1285DCD7D07D65E8C9FC002DAFA1E (String_t* __this, int32_t ___index0);
// System.Guid Unity.Entities.Runtime.Hashing.GuidUtility::NewGuid(System.Byte[])
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Guid_t  GuidUtility_NewGuid_mA6DC31F46897B3DC9548798A2A8F85BCFDFE7E98 (ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* ___data0);
// System.Byte[] Unity.Entities.Runtime.Hashing.MurmurHash3::MurmurHash3_x64_128(System.Void*,System.Int32,System.UInt32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* MurmurHash3_MurmurHash3_x64_128_m590C9368E8E1F1C97DC2A638E82EFF9AD183B6B9 (void* ___key0, int32_t ___len1, uint32_t ___seed2);
// System.UInt64 Unity.Entities.Runtime.Hashing.MurmurHash3::rotl64(System.UInt64,System.Byte)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint64_t MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9 (uint64_t ___x0, uint8_t ___r1);
// System.UInt64 Unity.Entities.Runtime.Hashing.MurmurHash3::fmix64(System.UInt64)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint64_t MurmurHash3_fmix64_m9DBDA328965DE090DB08B27E1D45D1DF27BF2F0C (uint64_t ___k0);
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
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_GuidUtility_NewGuid_mA6DC31F46897B3DC9548798A2A8F85BCFDFE7E98()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Guid Unity.Entities.Runtime.Hashing.GuidUtility::NewGuid(System.Byte[])", "it does not have the [MonoPInvokeCallback] attribute.");
}
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_GuidUtility_NewGuid_mAC25DB98A1AE682C5AA6BBEFB05CFA273A0CF0A0()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Guid Unity.Entities.Runtime.Hashing.GuidUtility::NewGuid(System.String)", "it does not have the [MonoPInvokeCallback] attribute.");
}
// System.Guid Unity.Entities.Runtime.Hashing.GuidUtility::NewGuid(System.Byte[])
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Guid_t  GuidUtility_NewGuid_mA6DC31F46897B3DC9548798A2A8F85BCFDFE7E98 (ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* ___data0)
{
	{
		// return new Guid(MurmurHash3.ComputeHash128(data));
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_0 = ___data0;
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_1 = MurmurHash3_ComputeHash128_m8E3A9DFB5B99FDE7CE97D0C5F656923092861CFF(L_0, 0);
		Guid_t  L_2;
		il2cpp::utils::MemoryUtils::MemorySet((&L_2), 0, sizeof(L_2));
		Guid__ctor_m788D5E27384FE5B478CD3E384F705AE8FC6FB72E((&L_2), L_1);
		return L_2;
	}
}
// System.Guid Unity.Entities.Runtime.Hashing.GuidUtility::NewGuid(System.String)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR Guid_t  GuidUtility_NewGuid_mAC25DB98A1AE682C5AA6BBEFB05CFA273A0CF0A0 (String_t* ___data0)
{
	ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* V_0 = NULL;
	int32_t V_1 = 0;
	{
		String_t* L_0 = ___data0;
		int32_t L_1 = L_0->get__length_0();
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_2 = (ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E*)(ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E*)SZArrayNew<ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E*>(LookupTypeInfoFromCursor(IL2CPP_SIZEOF_VOID_P == 4 ? 14248 : 28384), sizeof(uint8_t), (uint32_t)L_1);
		V_0 = L_2;
		V_1 = 0;
		goto IL_001f;
	}

IL_0010:
	{
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_3 = V_0;
		int32_t L_4 = V_1;
		String_t* L_5 = ___data0;
		int32_t L_6 = V_1;
		Il2CppChar L_7 = String_get_Chars_mD46069ADC2F1285DCD7D07D65E8C9FC002DAFA1E(L_5, L_6);
		(L_3)->SetAt(static_cast<il2cpp_array_size_t>(L_4), (uint8_t)(((int32_t)((uint8_t)L_7))));
		int32_t L_8 = V_1;
		V_1 = ((int32_t)il2cpp_codegen_add((int32_t)L_8, (int32_t)1));
	}

IL_001f:
	{
		int32_t L_9 = V_1;
		String_t* L_10 = ___data0;
		int32_t L_11 = L_10->get__length_0();
		if ((((int32_t)L_9) < ((int32_t)L_11)))
		{
			goto IL_0010;
		}
	}
	{
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_12 = V_0;
		Guid_t  L_13 = GuidUtility_NewGuid_mA6DC31F46897B3DC9548798A2A8F85BCFDFE7E98(L_12);
		return L_13;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_MurmurHash3_ComputeHash128_m8E3A9DFB5B99FDE7CE97D0C5F656923092861CFF()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Byte[] Unity.Entities.Runtime.Hashing.MurmurHash3::ComputeHash128(System.Byte[],System.UInt32)", "it does not have the [MonoPInvokeCallback] attribute.");
}
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.UInt64 Unity.Entities.Runtime.Hashing.MurmurHash3::rotl64(System.UInt64,System.Byte)", "it does not have the [MonoPInvokeCallback] attribute.");
}
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_MurmurHash3_fmix64_m9DBDA328965DE090DB08B27E1D45D1DF27BF2F0C()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.UInt64 Unity.Entities.Runtime.Hashing.MurmurHash3::fmix64(System.UInt64)", "it does not have the [MonoPInvokeCallback] attribute.");
}
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_MurmurHash3_MurmurHash3_x64_128_m590C9368E8E1F1C97DC2A638E82EFF9AD183B6B9()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Byte[] Unity.Entities.Runtime.Hashing.MurmurHash3::MurmurHash3_x64_128(System.Void*,System.Int32,System.UInt32)", "it does not have the [MonoPInvokeCallback] attribute.");
}
// System.Byte[] Unity.Entities.Runtime.Hashing.MurmurHash3::ComputeHash128(System.Byte[],System.UInt32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* MurmurHash3_ComputeHash128_m8E3A9DFB5B99FDE7CE97D0C5F656923092861CFF (ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* ___bytes0, uint32_t ___seed1)
{
	void* V_0 = NULL;
	ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* V_1 = NULL;
	void* G_B6_0 = NULL;
	void* G_B5_0 = NULL;
	int32_t G_B7_0 = 0;
	void* G_B7_1 = NULL;
	{
		// fixed (void* src = bytes)
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_0 = ___bytes0;
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_1 = L_0;
		V_1 = L_1;
		if (!L_1)
		{
			goto IL_000a;
		}
	}
	{
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_2 = V_1;
		if ((((int32_t)((int32_t)(((RuntimeArray*)L_2)->max_length)))))
		{
			goto IL_000f;
		}
	}

IL_000a:
	{
		V_0 = (void*)(((uintptr_t)0));
		goto IL_0018;
	}

IL_000f:
	{
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_3 = V_1;
		V_0 = (void*)(((uintptr_t)((L_3)->GetAddressAt(static_cast<il2cpp_array_size_t>(0)))));
	}

IL_0018:
	{
		// return MurmurHash3_x64_128(src, bytes?.Length ?? 0, seed);
		void* L_4 = V_0;
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_5 = ___bytes0;
		G_B5_0 = L_4;
		if (L_5)
		{
			G_B6_0 = L_4;
			goto IL_001f;
		}
	}
	{
		G_B7_0 = 0;
		G_B7_1 = G_B5_0;
		goto IL_0022;
	}

IL_001f:
	{
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_6 = ___bytes0;
		G_B7_0 = (((int32_t)((int32_t)(((RuntimeArray*)L_6)->max_length))));
		G_B7_1 = G_B6_0;
	}

IL_0022:
	{
		uint32_t L_7 = ___seed1;
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_8 = MurmurHash3_MurmurHash3_x64_128_m590C9368E8E1F1C97DC2A638E82EFF9AD183B6B9((void*)(void*)G_B7_1, G_B7_0, L_7);
		return L_8;
	}
}
// System.UInt64 Unity.Entities.Runtime.Hashing.MurmurHash3::rotl64(System.UInt64,System.Byte)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint64_t MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9 (uint64_t ___x0, uint8_t ___r1)
{
	{
		// return (x << r) | (x >> (64 - r));
		uint64_t L_0 = ___x0;
		uint8_t L_1 = ___r1;
		uint64_t L_2 = ___x0;
		uint8_t L_3 = ___r1;
		return ((int64_t)((int64_t)((int64_t)((int64_t)L_0<<(int32_t)((int32_t)((int32_t)L_1&(int32_t)((int32_t)63)))))|(int64_t)((int64_t)((uint64_t)L_2>>((int32_t)((int32_t)((int32_t)il2cpp_codegen_subtract((int32_t)((int32_t)64), (int32_t)L_3))&(int32_t)((int32_t)63)))))));
	}
}
// System.UInt64 Unity.Entities.Runtime.Hashing.MurmurHash3::fmix64(System.UInt64)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR uint64_t MurmurHash3_fmix64_m9DBDA328965DE090DB08B27E1D45D1DF27BF2F0C (uint64_t ___k0)
{
	{
		// k ^= k >> 33;
		uint64_t L_0 = ___k0;
		uint64_t L_1 = ___k0;
		___k0 = ((int64_t)((int64_t)L_0^(int64_t)((int64_t)((uint64_t)L_1>>((int32_t)33)))));
		// k *= 0xff51afd7ed558ccd;
		uint64_t L_2 = ___k0;
		___k0 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_2, (int64_t)((int64_t)-49064778989728563LL)));
		// k ^= k >> 33;
		uint64_t L_3 = ___k0;
		uint64_t L_4 = ___k0;
		___k0 = ((int64_t)((int64_t)L_3^(int64_t)((int64_t)((uint64_t)L_4>>((int32_t)33)))));
		// k *= 0xc4ceb9fe1a85ec53;
		uint64_t L_5 = ___k0;
		___k0 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_5, (int64_t)((int64_t)-4265267296055464877LL)));
		// k ^= k >> 33;
		uint64_t L_6 = ___k0;
		uint64_t L_7 = ___k0;
		___k0 = ((int64_t)((int64_t)L_6^(int64_t)((int64_t)((uint64_t)L_7>>((int32_t)33)))));
		// return k;
		uint64_t L_8 = ___k0;
		return L_8;
	}
}
// System.Byte[] Unity.Entities.Runtime.Hashing.MurmurHash3::MurmurHash3_x64_128(System.Void*,System.Int32,System.UInt32)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* MurmurHash3_MurmurHash3_x64_128_m590C9368E8E1F1C97DC2A638E82EFF9AD183B6B9 (void* ___key0, int32_t ___len1, uint32_t ___seed2)
{
	uint64_t V_0 = 0;
	uint64_t V_1 = 0;
	uint8_t* V_2 = NULL;
	int32_t V_3 = 0;
	uint64_t V_4 = 0;
	uint64_t V_5 = 0;
	uint64_t* V_6 = NULL;
	uint8_t* V_7 = NULL;
	int32_t V_8 = 0;
	int32_t V_9 = 0;
	uint8_t* V_10 = NULL;
	ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* V_11 = NULL;
	ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* G_B22_0 = NULL;
	ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* G_B21_0 = NULL;
	ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* G_B23_0 = NULL;
	ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* G_B24_0 = NULL;
	{
		// ulong h1 = seed;
		uint32_t L_0 = ___seed2;
		V_0 = (((int64_t)((uint64_t)L_0)));
		// ulong h2 = seed;
		uint32_t L_1 = ___seed2;
		V_1 = (((int64_t)((uint64_t)L_1)));
		// byte* data = (byte*)key;
		void* L_2 = ___key0;
		V_2 = (uint8_t*)L_2;
		// int nblocks = len / 16;
		int32_t L_3 = ___len1;
		V_3 = ((int32_t)((int32_t)L_3/(int32_t)((int32_t)16)));
		// ulong k1 = 0;
		V_4 = (((int64_t)((int64_t)0)));
		// ulong k2 = 0;
		V_5 = (((int64_t)((int64_t)0)));
		// ulong* blocks = (ulong*)data;
		uint8_t* L_4 = V_2;
		V_6 = (uint64_t*)L_4;
		// for (int i = 0; i < nblocks; i++)
		V_8 = 0;
		goto IL_00cc;
	}

IL_0020:
	{
		// k1 = blocks[i * 2 + 0];
		uint64_t* L_5 = V_6;
		int32_t L_6 = V_8;
		int64_t L_7 = *((int64_t*)((uint64_t*)il2cpp_codegen_add((intptr_t)L_5, (intptr_t)((intptr_t)il2cpp_codegen_multiply((intptr_t)(((intptr_t)((int32_t)il2cpp_codegen_multiply((int32_t)L_6, (int32_t)2)))), (int32_t)8)))));
		V_4 = L_7;
		// k2 = blocks[i * 2 + 1];
		uint64_t* L_8 = V_6;
		int32_t L_9 = V_8;
		int64_t L_10 = *((int64_t*)((uint64_t*)il2cpp_codegen_add((intptr_t)L_8, (intptr_t)((intptr_t)il2cpp_codegen_multiply((intptr_t)(((intptr_t)((int32_t)il2cpp_codegen_add((int32_t)((int32_t)il2cpp_codegen_multiply((int32_t)L_9, (int32_t)2)), (int32_t)1)))), (int32_t)8)))));
		V_5 = L_10;
		// k1 *= c1;
		uint64_t L_11 = V_4;
		V_4 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_11, (int64_t)((int64_t)-8663945395140668459LL)));
		// k1 = rotl64(k1, 31);
		uint64_t L_12 = V_4;
		uint64_t L_13 = MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9(L_12, (uint8_t)((int32_t)31));
		V_4 = L_13;
		// k1 *= c2;
		uint64_t L_14 = V_4;
		V_4 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_14, (int64_t)((int64_t)5545529020109919103LL)));
		// h1 ^= k1;
		uint64_t L_15 = V_0;
		uint64_t L_16 = V_4;
		V_0 = ((int64_t)((int64_t)L_15^(int64_t)L_16));
		// h1 = rotl64(h1, 27);
		uint64_t L_17 = V_0;
		uint64_t L_18 = MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9(L_17, (uint8_t)((int32_t)27));
		V_0 = L_18;
		// h1 += h2;
		uint64_t L_19 = V_0;
		uint64_t L_20 = V_1;
		V_0 = ((int64_t)il2cpp_codegen_add((int64_t)L_19, (int64_t)L_20));
		// h1 = h1 * 5 + 0x52dce729;
		uint64_t L_21 = V_0;
		V_0 = ((int64_t)il2cpp_codegen_add((int64_t)((int64_t)il2cpp_codegen_multiply((int64_t)L_21, (int64_t)(((int64_t)((int64_t)5))))), (int64_t)(((int64_t)((int64_t)((int32_t)1390208809))))));
		// k2 *= c2;
		uint64_t L_22 = V_5;
		V_5 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_22, (int64_t)((int64_t)5545529020109919103LL)));
		// k2 = rotl64(k2, 33);
		uint64_t L_23 = V_5;
		uint64_t L_24 = MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9(L_23, (uint8_t)((int32_t)33));
		V_5 = L_24;
		// k2 *= c1;
		uint64_t L_25 = V_5;
		V_5 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_25, (int64_t)((int64_t)-8663945395140668459LL)));
		// h2 ^= k2;
		uint64_t L_26 = V_1;
		uint64_t L_27 = V_5;
		V_1 = ((int64_t)((int64_t)L_26^(int64_t)L_27));
		// h2 = rotl64(h2, 31);
		uint64_t L_28 = V_1;
		uint64_t L_29 = MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9(L_28, (uint8_t)((int32_t)31));
		V_1 = L_29;
		// h2 += h1;
		uint64_t L_30 = V_1;
		uint64_t L_31 = V_0;
		V_1 = ((int64_t)il2cpp_codegen_add((int64_t)L_30, (int64_t)L_31));
		// h2 = h2 * 5 + 0x38495ab5;
		uint64_t L_32 = V_1;
		V_1 = ((int64_t)il2cpp_codegen_add((int64_t)((int64_t)il2cpp_codegen_multiply((int64_t)L_32, (int64_t)(((int64_t)((int64_t)5))))), (int64_t)(((int64_t)((int64_t)((int32_t)944331445))))));
		// for (int i = 0; i < nblocks; i++)
		int32_t L_33 = V_8;
		V_8 = ((int32_t)il2cpp_codegen_add((int32_t)L_33, (int32_t)1));
	}

IL_00cc:
	{
		// for (int i = 0; i < nblocks; i++)
		int32_t L_34 = V_8;
		int32_t L_35 = V_3;
		if ((((int32_t)L_34) < ((int32_t)L_35)))
		{
			goto IL_0020;
		}
	}
	{
		// k1 = 0;
		V_4 = (((int64_t)((int64_t)0)));
		// k2 = 0;
		V_5 = (((int64_t)((int64_t)0)));
		// byte* tail = data + nblocks * 16;
		uint8_t* L_36 = V_2;
		int32_t L_37 = V_3;
		V_7 = (uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_36, (int32_t)((int32_t)il2cpp_codegen_multiply((int32_t)L_37, (int32_t)((int32_t)16)))));
		// switch (len & 15)
		int32_t L_38 = ___len1;
		V_9 = ((int32_t)((int32_t)L_38&(int32_t)((int32_t)15)));
		int32_t L_39 = V_9;
		switch (((int32_t)il2cpp_codegen_subtract((int32_t)L_39, (int32_t)1)))
		{
			case 0:
			{
				goto IL_0225;
			}
			case 1:
			{
				goto IL_0218;
			}
			case 2:
			{
				goto IL_020a;
			}
			case 3:
			{
				goto IL_01fc;
			}
			case 4:
			{
				goto IL_01ee;
			}
			case 5:
			{
				goto IL_01e0;
			}
			case 6:
			{
				goto IL_01d2;
			}
			case 7:
			{
				goto IL_01c4;
			}
			case 8:
			{
				goto IL_018d;
			}
			case 9:
			{
				goto IL_017f;
			}
			case 10:
			{
				goto IL_0170;
			}
			case 11:
			{
				goto IL_0161;
			}
			case 12:
			{
				goto IL_0152;
			}
			case 13:
			{
				goto IL_0143;
			}
			case 14:
			{
				goto IL_0134;
			}
		}
	}
	{
		goto IL_025a;
	}

IL_0134:
	{
		// k2 ^= ((ulong)tail[14]) << 48;
		uint64_t L_40 = V_5;
		uint8_t* L_41 = V_7;
		int32_t L_42 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_41, (int32_t)((int32_t)14))));
		V_5 = ((int64_t)((int64_t)L_40^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_42))))))<<(int32_t)((int32_t)48)))));
	}

IL_0143:
	{
		// k2 ^= ((ulong)tail[13]) << 40;
		uint64_t L_43 = V_5;
		uint8_t* L_44 = V_7;
		int32_t L_45 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_44, (int32_t)((int32_t)13))));
		V_5 = ((int64_t)((int64_t)L_43^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_45))))))<<(int32_t)((int32_t)40)))));
	}

IL_0152:
	{
		// k2 ^= ((ulong)tail[12]) << 32;
		uint64_t L_46 = V_5;
		uint8_t* L_47 = V_7;
		int32_t L_48 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_47, (int32_t)((int32_t)12))));
		V_5 = ((int64_t)((int64_t)L_46^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_48))))))<<(int32_t)((int32_t)32)))));
	}

IL_0161:
	{
		// k2 ^= ((ulong)tail[11]) << 24;
		uint64_t L_49 = V_5;
		uint8_t* L_50 = V_7;
		int32_t L_51 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_50, (int32_t)((int32_t)11))));
		V_5 = ((int64_t)((int64_t)L_49^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_51))))))<<(int32_t)((int32_t)24)))));
	}

IL_0170:
	{
		// k2 ^= ((ulong)tail[10]) << 16;
		uint64_t L_52 = V_5;
		uint8_t* L_53 = V_7;
		int32_t L_54 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_53, (int32_t)((int32_t)10))));
		V_5 = ((int64_t)((int64_t)L_52^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_54))))))<<(int32_t)((int32_t)16)))));
	}

IL_017f:
	{
		// k2 ^= ((ulong)tail[9]) << 8;
		uint64_t L_55 = V_5;
		uint8_t* L_56 = V_7;
		int32_t L_57 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_56, (int32_t)((int32_t)9))));
		V_5 = ((int64_t)((int64_t)L_55^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_57))))))<<(int32_t)8))));
	}

IL_018d:
	{
		// k2 ^= ((ulong)tail[8]) << 0;
		uint64_t L_58 = V_5;
		uint8_t* L_59 = V_7;
		int32_t L_60 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_59, (int32_t)8)));
		V_5 = ((int64_t)((int64_t)L_58^(int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_60))))))));
		// k2 *= c2;
		uint64_t L_61 = V_5;
		V_5 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_61, (int64_t)((int64_t)5545529020109919103LL)));
		// k2 = rotl64(k2, 33);
		uint64_t L_62 = V_5;
		uint64_t L_63 = MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9(L_62, (uint8_t)((int32_t)33));
		V_5 = L_63;
		// k2 *= c1;
		uint64_t L_64 = V_5;
		V_5 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_64, (int64_t)((int64_t)-8663945395140668459LL)));
		// h2 ^= k2;
		uint64_t L_65 = V_1;
		uint64_t L_66 = V_5;
		V_1 = ((int64_t)((int64_t)L_65^(int64_t)L_66));
	}

IL_01c4:
	{
		// k1 ^= ((ulong)tail[7]) << 56;
		uint64_t L_67 = V_4;
		uint8_t* L_68 = V_7;
		int32_t L_69 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_68, (int32_t)7)));
		V_4 = ((int64_t)((int64_t)L_67^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_69))))))<<(int32_t)((int32_t)56)))));
	}

IL_01d2:
	{
		// k1 ^= ((ulong)tail[6]) << 48;
		uint64_t L_70 = V_4;
		uint8_t* L_71 = V_7;
		int32_t L_72 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_71, (int32_t)6)));
		V_4 = ((int64_t)((int64_t)L_70^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_72))))))<<(int32_t)((int32_t)48)))));
	}

IL_01e0:
	{
		// k1 ^= ((ulong)tail[5]) << 40;
		uint64_t L_73 = V_4;
		uint8_t* L_74 = V_7;
		int32_t L_75 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_74, (int32_t)5)));
		V_4 = ((int64_t)((int64_t)L_73^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_75))))))<<(int32_t)((int32_t)40)))));
	}

IL_01ee:
	{
		// k1 ^= ((ulong)tail[4]) << 32;
		uint64_t L_76 = V_4;
		uint8_t* L_77 = V_7;
		int32_t L_78 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_77, (int32_t)4)));
		V_4 = ((int64_t)((int64_t)L_76^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_78))))))<<(int32_t)((int32_t)32)))));
	}

IL_01fc:
	{
		// k1 ^= ((ulong)tail[3]) << 24;
		uint64_t L_79 = V_4;
		uint8_t* L_80 = V_7;
		int32_t L_81 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_80, (int32_t)3)));
		V_4 = ((int64_t)((int64_t)L_79^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_81))))))<<(int32_t)((int32_t)24)))));
	}

IL_020a:
	{
		// k1 ^= ((ulong)tail[2]) << 16;
		uint64_t L_82 = V_4;
		uint8_t* L_83 = V_7;
		int32_t L_84 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_83, (int32_t)2)));
		V_4 = ((int64_t)((int64_t)L_82^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_84))))))<<(int32_t)((int32_t)16)))));
	}

IL_0218:
	{
		// k1 ^= ((ulong)tail[1]) << 8;
		uint64_t L_85 = V_4;
		uint8_t* L_86 = V_7;
		int32_t L_87 = *((uint8_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_86, (int32_t)1)));
		V_4 = ((int64_t)((int64_t)L_85^(int64_t)((int64_t)((int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_87))))))<<(int32_t)8))));
	}

IL_0225:
	{
		// k1 ^= ((ulong)tail[0]) << 0;
		uint64_t L_88 = V_4;
		uint8_t* L_89 = V_7;
		int32_t L_90 = *((uint8_t*)L_89);
		V_4 = ((int64_t)((int64_t)L_88^(int64_t)(((int64_t)((uint64_t)(((uint32_t)((uint32_t)L_90))))))));
		// k1 *= c1;
		uint64_t L_91 = V_4;
		V_4 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_91, (int64_t)((int64_t)-8663945395140668459LL)));
		// k1 = rotl64(k1, 31);
		uint64_t L_92 = V_4;
		uint64_t L_93 = MurmurHash3_rotl64_m8F41CB63EC8E2283C734938702F9EF73AC21B6E9(L_92, (uint8_t)((int32_t)31));
		V_4 = L_93;
		// k1 *= c2;
		uint64_t L_94 = V_4;
		V_4 = ((int64_t)il2cpp_codegen_multiply((int64_t)L_94, (int64_t)((int64_t)5545529020109919103LL)));
		// h1 ^= k1;
		uint64_t L_95 = V_0;
		uint64_t L_96 = V_4;
		V_0 = ((int64_t)((int64_t)L_95^(int64_t)L_96));
	}

IL_025a:
	{
		// h1 ^= (ulong)len;
		uint64_t L_97 = V_0;
		int32_t L_98 = ___len1;
		V_0 = ((int64_t)((int64_t)L_97^(int64_t)(((int64_t)((int64_t)L_98)))));
		// h2 ^= (ulong)len;
		uint64_t L_99 = V_1;
		int32_t L_100 = ___len1;
		V_1 = ((int64_t)((int64_t)L_99^(int64_t)(((int64_t)((int64_t)L_100)))));
		// h1 += h2;
		uint64_t L_101 = V_0;
		uint64_t L_102 = V_1;
		V_0 = ((int64_t)il2cpp_codegen_add((int64_t)L_101, (int64_t)L_102));
		// h2 += h1;
		uint64_t L_103 = V_1;
		uint64_t L_104 = V_0;
		V_1 = ((int64_t)il2cpp_codegen_add((int64_t)L_103, (int64_t)L_104));
		// h1 = fmix64(h1);
		uint64_t L_105 = V_0;
		uint64_t L_106 = MurmurHash3_fmix64_m9DBDA328965DE090DB08B27E1D45D1DF27BF2F0C(L_105);
		V_0 = L_106;
		// h2 = fmix64(h2);
		uint64_t L_107 = V_1;
		uint64_t L_108 = MurmurHash3_fmix64_m9DBDA328965DE090DB08B27E1D45D1DF27BF2F0C(L_107);
		V_1 = L_108;
		// h1 += h2;
		uint64_t L_109 = V_0;
		uint64_t L_110 = V_1;
		V_0 = ((int64_t)il2cpp_codegen_add((int64_t)L_109, (int64_t)L_110));
		// h2 += h1;
		uint64_t L_111 = V_1;
		uint64_t L_112 = V_0;
		V_1 = ((int64_t)il2cpp_codegen_add((int64_t)L_111, (int64_t)L_112));
		// var result = new byte[16];
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_113 = (ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E*)(ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E*)SZArrayNew<ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E*>(LookupTypeInfoFromCursor(IL2CPP_SIZEOF_VOID_P == 4 ? 14248 : 28384), sizeof(uint8_t), (uint32_t)((int32_t)16));
		// fixed (byte* ptr = result)
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_114 = L_113;
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_115 = L_114;
		V_11 = L_115;
		G_B21_0 = L_114;
		if (!L_115)
		{
			G_B22_0 = L_114;
			goto IL_0295;
		}
	}
	{
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_116 = V_11;
		G_B22_0 = G_B21_0;
		if ((((int32_t)((int32_t)(((RuntimeArray*)L_116)->max_length)))))
		{
			G_B23_0 = G_B21_0;
			goto IL_029b;
		}
	}

IL_0295:
	{
		V_10 = (uint8_t*)(((uintptr_t)0));
		G_B24_0 = G_B22_0;
		goto IL_02a6;
	}

IL_029b:
	{
		ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E* L_117 = V_11;
		V_10 = (uint8_t*)(((uintptr_t)((L_117)->GetAddressAt(static_cast<il2cpp_array_size_t>(0)))));
		G_B24_0 = G_B23_0;
	}

IL_02a6:
	{
		// ((ulong*)ptr)[0] = h1;
		uint8_t* L_118 = V_10;
		uint64_t L_119 = V_0;
		*((int64_t*)L_118) = (int64_t)L_119;
		// ((ulong*)ptr)[1] = h2;
		uint8_t* L_120 = V_10;
		uint64_t L_121 = V_1;
		*((int64_t*)((uint8_t*)il2cpp_codegen_add((intptr_t)L_120, (int32_t)8))) = (int64_t)L_121;
		V_11 = (ByteU5BU5D_tA471A8437AF09D90E2948BE24E0DA631750B725E*)NULL;
		// return result;
		return G_B24_0;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
