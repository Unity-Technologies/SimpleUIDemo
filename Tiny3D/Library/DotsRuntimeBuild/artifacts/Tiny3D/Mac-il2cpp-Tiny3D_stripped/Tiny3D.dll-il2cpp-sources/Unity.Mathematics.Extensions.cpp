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


// System.String
struct String_t;

IL2CPP_EXTERN_C const RuntimeMethod AABB_ToString_mF99D24B9478C79AEEFD9CA4281643665AA831893_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod AABB_get_Min_m14DB6B87037B67B40993018B048951F8CE3BEED9_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod AABB_get_Size_m0CD6EB7A2E8CC022E3E1551E0421ADE0E5519436_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod MinMaxAABB_Equals_m05287DA1C456B0AC777F0CDE61F2627B48368D83_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod String_Format_mF7392EB22ED374B060A5C27ADF3BB5DDFCDC987D_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod float3_Equals_mD907D4D448B5C8F48E8A80990F482F77A57DF520_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod float3__ctor_m1A3F42DBA1AB9B4ACE8AB2DE6073E98A89597757_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod float3_op_Multiply_m439CAC4B3EE081F6126F2994CA01E171C6978FB1_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod float3_op_Subtraction_mB767105FEAF2403E8597BF6867D214BEE08C3751_RuntimeMethod_var;


IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif

// <Module>
struct  U3CModuleU3E_tC6C38364FD80623B1CFE7B1593B7097BA8B90AAE 
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


// System.Single
struct  Single_t721917D4A79F44210B3A1BFD48DC52627933C8EA 
{
public:
	// System.Single System.Single::m_value
	float ___m_value_0;

public:
	inline float get_m_value_0() const { return ___m_value_0; }
	inline float* get_address_of_m_value_0() { return &___m_value_0; }
	inline void set_m_value_0(float value)
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


// Unity.Mathematics.float3
struct  float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 
{
public:
	// System.Single Unity.Mathematics.float3::x
	float ___x_0;
	// System.Single Unity.Mathematics.float3::y
	float ___y_1;
	// System.Single Unity.Mathematics.float3::z
	float ___z_2;

public:
	inline float get_x_0() const { return ___x_0; }
	inline float* get_address_of_x_0() { return &___x_0; }
	inline void set_x_0(float value)
	{
		___x_0 = value;
	}

	inline float get_y_1() const { return ___y_1; }
	inline float* get_address_of_y_1() { return &___y_1; }
	inline void set_y_1(float value)
	{
		___y_1 = value;
	}

	inline float get_z_2() const { return ___z_2; }
	inline float* get_address_of_z_2() { return &___z_2; }
	inline void set_z_2(float value)
	{
		___z_2 = value;
	}
};

extern void* float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7_StaticFields_Storage;
struct float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7_StaticFields
{
public:
	// Unity.Mathematics.float3 Unity.Mathematics.float3::zero
	float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___zero_3;

public:
	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  get_zero_3() const { return ___zero_3; }
	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * get_address_of_zero_3() { return &___zero_3; }
	inline void set_zero_3(float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  value)
	{
		___zero_3 = value;
	}
};


// Unity.Mathematics.AABB
struct  AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 
{
public:
	// Unity.Mathematics.float3 Unity.Mathematics.AABB::Center
	float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___Center_0;
	// Unity.Mathematics.float3 Unity.Mathematics.AABB::Extents
	float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___Extents_1;

public:
	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  get_Center_0() const { return ___Center_0; }
	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * get_address_of_Center_0() { return &___Center_0; }
	inline void set_Center_0(float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  value)
	{
		___Center_0 = value;
	}

	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  get_Extents_1() const { return ___Extents_1; }
	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * get_address_of_Extents_1() { return &___Extents_1; }
	inline void set_Extents_1(float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  value)
	{
		___Extents_1 = value;
	}
};


// Unity.Mathematics.MinMaxAABB
struct  MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE 
{
public:
	// Unity.Mathematics.float3 Unity.Mathematics.MinMaxAABB::Min
	float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___Min_0;
	// Unity.Mathematics.float3 Unity.Mathematics.MinMaxAABB::Max
	float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___Max_1;

public:
	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  get_Min_0() const { return ___Min_0; }
	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * get_address_of_Min_0() { return &___Min_0; }
	inline void set_Min_0(float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  value)
	{
		___Min_0 = value;
	}

	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  get_Max_1() const { return ___Max_1; }
	inline float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * get_address_of_Max_1() { return &___Max_1; }
	inline void set_Max_1(float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  value)
	{
		___Max_1 = value;
	}
};

#ifdef __clang__
#pragma clang diagnostic pop
#endif



// Unity.Mathematics.float3 Unity.Mathematics.float3::op_Multiply(Unity.Mathematics.float3,System.Single)
IL2CPP_EXTERN_C inline  IL2CPP_METHOD_ATTR float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  float3_op_Multiply_m439CAC4B3EE081F6126F2994CA01E171C6978FB1_inline (float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___lhs0, float ___rhs1);
// Unity.Mathematics.float3 Unity.Mathematics.AABB::get_Size()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  AABB_get_Size_m0CD6EB7A2E8CC022E3E1551E0421ADE0E5519436 (AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 * __this);
// Unity.Mathematics.float3 Unity.Mathematics.float3::op_Subtraction(Unity.Mathematics.float3,Unity.Mathematics.float3)
IL2CPP_EXTERN_C inline  IL2CPP_METHOD_ATTR float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  float3_op_Subtraction_mB767105FEAF2403E8597BF6867D214BEE08C3751_inline (float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___lhs0, float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___rhs1);
// Unity.Mathematics.float3 Unity.Mathematics.AABB::get_Min()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  AABB_get_Min_m14DB6B87037B67B40993018B048951F8CE3BEED9 (AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 * __this);
// System.String System.String::Format(System.String,System.Object,System.Object)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* String_Format_mF7392EB22ED374B060A5C27ADF3BB5DDFCDC987D (String_t* ___format0, RuntimeObject * ___arg11, RuntimeObject * ___arg22);
// System.String Unity.Mathematics.AABB::ToString()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* AABB_ToString_mF99D24B9478C79AEEFD9CA4281643665AA831893 (AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 * __this);
// System.Boolean Unity.Mathematics.float3::Equals(Unity.Mathematics.float3)
IL2CPP_EXTERN_C inline  IL2CPP_METHOD_ATTR bool float3_Equals_mD907D4D448B5C8F48E8A80990F482F77A57DF520_inline (float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * __this, float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___rhs0);
// System.Boolean Unity.Mathematics.MinMaxAABB::Equals(Unity.Mathematics.MinMaxAABB)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool MinMaxAABB_Equals_m05287DA1C456B0AC777F0CDE61F2627B48368D83 (MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE * __this, MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE  ___other0);
// System.Void Unity.Mathematics.float3::.ctor(System.Single,System.Single,System.Single)
IL2CPP_EXTERN_C inline  IL2CPP_METHOD_ATTR void float3__ctor_m1A3F42DBA1AB9B4ACE8AB2DE6073E98A89597757_inline (float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * __this, float ___x0, float ___y1, float ___z2);
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
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_AABB_get_Size_m0CD6EB7A2E8CC022E3E1551E0421ADE0E5519436()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("Unity.Mathematics.float3 Unity.Mathematics.AABB::get_Size()", "it is an instance method. Only static methods can be called back from native code.");
}
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_AABB_get_Min_m14DB6B87037B67B40993018B048951F8CE3BEED9()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("Unity.Mathematics.float3 Unity.Mathematics.AABB::get_Min()", "it is an instance method. Only static methods can be called back from native code.");
}
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_AABB_ToString_mF99D24B9478C79AEEFD9CA4281643665AA831893()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.String Unity.Mathematics.AABB::ToString()", "it is an instance method. Only static methods can be called back from native code.");
}
// Unity.Mathematics.float3 Unity.Mathematics.AABB::get_Size()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  AABB_get_Size_m0CD6EB7A2E8CC022E3E1551E0421ADE0E5519436 (AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 * __this)
{
	{
		// public float3 Size { get { return Extents * 2; } }
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_0 = __this->get_Extents_1();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_1 = float3_op_Multiply_m439CAC4B3EE081F6126F2994CA01E171C6978FB1_inline(L_0, (2.0f));
		return L_1;
	}
}
IL2CPP_EXTERN_C  float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  AABB_get_Size_m0CD6EB7A2E8CC022E3E1551E0421ADE0E5519436_AdjustorThunk (RuntimeObject * __this)
{
	int32_t _offset = ((sizeof(RuntimeObject) + ALIGN_OF(AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 ) - 1) & ~(ALIGN_OF(AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 ) - 1)) / sizeof(void*);
	AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 * _thisAdjusted = reinterpret_cast<AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 *>(__this + _offset);
	return AABB_get_Size_m0CD6EB7A2E8CC022E3E1551E0421ADE0E5519436(_thisAdjusted);
}
// Unity.Mathematics.float3 Unity.Mathematics.AABB::get_Min()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  AABB_get_Min_m14DB6B87037B67B40993018B048951F8CE3BEED9 (AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 * __this)
{
	{
		// public float3 Min { get { return Center - Extents; } }
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_0 = __this->get_Center_0();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_1 = __this->get_Extents_1();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_2 = float3_op_Subtraction_mB767105FEAF2403E8597BF6867D214BEE08C3751_inline(L_0, L_1);
		return L_2;
	}
}
IL2CPP_EXTERN_C  float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  AABB_get_Min_m14DB6B87037B67B40993018B048951F8CE3BEED9_AdjustorThunk (RuntimeObject * __this)
{
	int32_t _offset = ((sizeof(RuntimeObject) + ALIGN_OF(AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 ) - 1) & ~(ALIGN_OF(AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 ) - 1)) / sizeof(void*);
	AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 * _thisAdjusted = reinterpret_cast<AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 *>(__this + _offset);
	return AABB_get_Min_m14DB6B87037B67B40993018B048951F8CE3BEED9(_thisAdjusted);
}
// System.String Unity.Mathematics.AABB::ToString()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* AABB_ToString_mF99D24B9478C79AEEFD9CA4281643665AA831893 (AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 * __this)
{
	{
		// return $"AABB(Center:{Center}, Extents:{Extents}";
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_0 = __this->get_Center_0();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_1 = L_0;
		RuntimeObject * L_2 = Box(LookupTypeInfoFromCursor(IL2CPP_SIZEOF_VOID_P == 4 ? 1796 : 3504), &L_1);
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_3 = __this->get_Extents_1();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_4 = L_3;
		RuntimeObject * L_5 = Box(LookupTypeInfoFromCursor(IL2CPP_SIZEOF_VOID_P == 4 ? 1796 : 3504), &L_4);
		String_t* L_6 = String_Format_mF7392EB22ED374B060A5C27ADF3BB5DDFCDC987D(LookupStringFromCursor(IL2CPP_SIZEOF_VOID_P == 4 ? 9368 : 10208), L_2, L_5);
		return L_6;
	}
}
IL2CPP_EXTERN_C  String_t* AABB_ToString_mF99D24B9478C79AEEFD9CA4281643665AA831893_AdjustorThunk (RuntimeObject * __this)
{
	int32_t _offset = ((sizeof(RuntimeObject) + ALIGN_OF(AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 ) - 1) & ~(ALIGN_OF(AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 ) - 1)) / sizeof(void*);
	AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 * _thisAdjusted = reinterpret_cast<AABB_t7CE417F9B45D325719CAF041893718E617AA0AD4 *>(__this + _offset);
	return AABB_ToString_mF99D24B9478C79AEEFD9CA4281643665AA831893(_thisAdjusted);
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_MinMaxAABB_Equals_m05287DA1C456B0AC777F0CDE61F2627B48368D83()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Boolean Unity.Mathematics.MinMaxAABB::Equals(Unity.Mathematics.MinMaxAABB)", "it is an instance method. Only static methods can be called back from native code.");
}
// System.Boolean Unity.Mathematics.MinMaxAABB::Equals(Unity.Mathematics.MinMaxAABB)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool MinMaxAABB_Equals_m05287DA1C456B0AC777F0CDE61F2627B48368D83 (MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE * __this, MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE  ___other0)
{
	{
		// return Min.Equals(Min) && Max.Equals(other.Max);
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * L_0 = __this->get_address_of_Min_0();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_1 = __this->get_Min_0();
		bool L_2 = float3_Equals_mD907D4D448B5C8F48E8A80990F482F77A57DF520_inline((float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 *)L_0, L_1);
		if (!L_2)
		{
			goto IL_0025;
		}
	}
	{
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * L_3 = __this->get_address_of_Max_1();
		MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE  L_4 = ___other0;
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_5 = L_4.get_Max_1();
		bool L_6 = float3_Equals_mD907D4D448B5C8F48E8A80990F482F77A57DF520_inline((float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 *)L_3, L_5);
		return L_6;
	}

IL_0025:
	{
		return (bool)0;
	}
}
IL2CPP_EXTERN_C  bool MinMaxAABB_Equals_m05287DA1C456B0AC777F0CDE61F2627B48368D83_AdjustorThunk (RuntimeObject * __this, MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE  ___other0)
{
	int32_t _offset = ((sizeof(RuntimeObject) + ALIGN_OF(MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE ) - 1) & ~(ALIGN_OF(MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE ) - 1)) / sizeof(void*);
	MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE * _thisAdjusted = reinterpret_cast<MinMaxAABB_t1914A5FA22F5F68686B332A026EED2FAF7C173CE *>(__this + _offset);
	return MinMaxAABB_Equals_m05287DA1C456B0AC777F0CDE61F2627B48368D83(_thisAdjusted, ___other0);
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
IL2CPP_EXTERN_C inline  IL2CPP_METHOD_ATTR float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  float3_op_Multiply_m439CAC4B3EE081F6126F2994CA01E171C6978FB1_inline (float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___lhs0, float ___rhs1)
{
	{
		// public static float3 operator * (float3 lhs, float rhs) { return new float3 (lhs.x * rhs, lhs.y * rhs, lhs.z * rhs); }
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_0 = ___lhs0;
		float L_1 = L_0.get_x_0();
		float L_2 = ___rhs1;
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_3 = ___lhs0;
		float L_4 = L_3.get_y_1();
		float L_5 = ___rhs1;
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_6 = ___lhs0;
		float L_7 = L_6.get_z_2();
		float L_8 = ___rhs1;
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_9;
		il2cpp::utils::MemoryUtils::MemorySet((&L_9), 0, sizeof(L_9));
		float3__ctor_m1A3F42DBA1AB9B4ACE8AB2DE6073E98A89597757_inline((&L_9), ((float)il2cpp_codegen_multiply((float)L_1, (float)L_2)), ((float)il2cpp_codegen_multiply((float)L_4, (float)L_5)), ((float)il2cpp_codegen_multiply((float)L_7, (float)L_8)));
		return L_9;
	}
}
IL2CPP_EXTERN_C inline  IL2CPP_METHOD_ATTR float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  float3_op_Subtraction_mB767105FEAF2403E8597BF6867D214BEE08C3751_inline (float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___lhs0, float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___rhs1)
{
	{
		// public static float3 operator - (float3 lhs, float3 rhs) { return new float3 (lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z); }
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_0 = ___lhs0;
		float L_1 = L_0.get_x_0();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_2 = ___rhs1;
		float L_3 = L_2.get_x_0();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_4 = ___lhs0;
		float L_5 = L_4.get_y_1();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_6 = ___rhs1;
		float L_7 = L_6.get_y_1();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_8 = ___lhs0;
		float L_9 = L_8.get_z_2();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_10 = ___rhs1;
		float L_11 = L_10.get_z_2();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_12;
		il2cpp::utils::MemoryUtils::MemorySet((&L_12), 0, sizeof(L_12));
		float3__ctor_m1A3F42DBA1AB9B4ACE8AB2DE6073E98A89597757_inline((&L_12), ((float)il2cpp_codegen_subtract((float)L_1, (float)L_3)), ((float)il2cpp_codegen_subtract((float)L_5, (float)L_7)), ((float)il2cpp_codegen_subtract((float)L_9, (float)L_11)));
		return L_12;
	}
}
IL2CPP_EXTERN_C inline  IL2CPP_METHOD_ATTR bool float3_Equals_mD907D4D448B5C8F48E8A80990F482F77A57DF520_inline (float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * __this, float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  ___rhs0)
{
	{
		// public bool Equals(float3 rhs) { return x == rhs.x && y == rhs.y && z == rhs.z; }
		float L_0 = __this->get_x_0();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_1 = ___rhs0;
		float L_2 = L_1.get_x_0();
		if ((!(((float)L_0) == ((float)L_2))))
		{
			goto IL_002b;
		}
	}
	{
		float L_3 = __this->get_y_1();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_4 = ___rhs0;
		float L_5 = L_4.get_y_1();
		if ((!(((float)L_3) == ((float)L_5))))
		{
			goto IL_002b;
		}
	}
	{
		float L_6 = __this->get_z_2();
		float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7  L_7 = ___rhs0;
		float L_8 = L_7.get_z_2();
		return (bool)((((float)L_6) == ((float)L_8))? 1 : 0);
	}

IL_002b:
	{
		return (bool)0;
	}
}
IL2CPP_EXTERN_C inline  IL2CPP_METHOD_ATTR void float3__ctor_m1A3F42DBA1AB9B4ACE8AB2DE6073E98A89597757_inline (float3_tB3DB6E304B40D8C4DA63622603E1671D83A2FDF7 * __this, float ___x0, float ___y1, float ___z2)
{
	{
		// this.x = x;
		float L_0 = ___x0;
		__this->set_x_0(L_0);
		// this.y = y;
		float L_1 = ___y1;
		__this->set_y_1(L_1);
		// this.z = z;
		float L_2 = ___z2;
		__this->set_z_2(L_2);
		// }
		return;
	}
}
