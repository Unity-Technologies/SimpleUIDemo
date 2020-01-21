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


// System.Void
struct Void_t8DA50A09EC87863B9856F55E2EC298E97345FA9C;
// Unity.Platforms.RunLoop/RunLoopDelegate
struct RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463;

IL2CPP_EXTERN_C const RuntimeMethod RunLoopDelegate_Invoke_mB498A417DD5ABD7B53FD64D45953F34DEA48E173_RuntimeMethod_var;
IL2CPP_EXTERN_C const RuntimeMethod RunLoopImpl_EnterMainLoop_mA64AA1079E3482F40E37AF7B54764DBF2CF2FCC2_RuntimeMethod_var;


IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif

// <Module>
struct  U3CModuleU3E_t199AFE1B216A41CC48EE70AC506DC9BB0C7B7426 
{
public:

public:
};


// System.Object

struct Il2CppArrayBounds;

// System.Array


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

// Unity.Platforms.RunLoop
struct  RunLoop_t24926B687D9329870B880C46CA7016DDD054D48C  : public RuntimeObject
{
public:

public:
};


// Unity.Platforms.RunLoopImpl
struct  RunLoopImpl_t3C9D968FA90AD7DE22EAA0F19826E307B83F47E3  : public RuntimeObject
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


// System.IntPtr
struct  IntPtr_t 
{
public:
	// System.Void* System.IntPtr::m_value
	void* ___m_value_0;

public:
	inline void* get_m_value_0() const { return ___m_value_0; }
	inline void** get_address_of_m_value_0() { return &___m_value_0; }
	inline void set_m_value_0(void* value)
	{
		___m_value_0 = value;
	}
};

extern void* IntPtr_t_StaticFields_Storage;
struct IntPtr_t_StaticFields
{
public:
	// System.IntPtr System.IntPtr::Zero
	intptr_t ___Zero_1;

public:
	inline intptr_t get_Zero_1() const { return ___Zero_1; }
	inline intptr_t* get_address_of_Zero_1() { return &___Zero_1; }
	inline void set_Zero_1(intptr_t value)
	{
		___Zero_1 = value;
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


// System.Delegate
struct  Delegate_t  : public RuntimeObject
{
public:
	// System.IntPtr System.Delegate::method_ptr
	intptr_t ___method_ptr_0;
	// System.Object System.Delegate::m_target
	RuntimeObject * ___m_target_1;
	// System.Void* System.Delegate::m_ReversePInvokeWrapperPtr
	void* ___m_ReversePInvokeWrapperPtr_2;
	// System.Boolean System.Delegate::m_IsDelegateOpen
	bool ___m_IsDelegateOpen_3;

public:
	inline intptr_t get_method_ptr_0() const { return ___method_ptr_0; }
	inline intptr_t* get_address_of_method_ptr_0() { return &___method_ptr_0; }
	inline void set_method_ptr_0(intptr_t value)
	{
		___method_ptr_0 = value;
	}

	inline RuntimeObject * get_m_target_1() const { return ___m_target_1; }
	inline RuntimeObject ** get_address_of_m_target_1() { return &___m_target_1; }
	inline void set_m_target_1(RuntimeObject * value)
	{
		___m_target_1 = value;
		Il2CppCodeGenWriteBarrier((void**)(&___m_target_1), (void*)value);
	}

	inline void* get_m_ReversePInvokeWrapperPtr_2() const { return ___m_ReversePInvokeWrapperPtr_2; }
	inline void** get_address_of_m_ReversePInvokeWrapperPtr_2() { return &___m_ReversePInvokeWrapperPtr_2; }
	inline void set_m_ReversePInvokeWrapperPtr_2(void* value)
	{
		___m_ReversePInvokeWrapperPtr_2 = value;
	}

	inline bool get_m_IsDelegateOpen_3() const { return ___m_IsDelegateOpen_3; }
	inline bool* get_address_of_m_IsDelegateOpen_3() { return &___m_IsDelegateOpen_3; }
	inline void set_m_IsDelegateOpen_3(bool value)
	{
		___m_IsDelegateOpen_3 = value;
	}
};


// System.MulticastDelegate
struct  MulticastDelegate_t  : public Delegate_t
{
public:

public:
};


// Unity.Platforms.RunLoop_RunLoopDelegate
struct  RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463  : public MulticastDelegate_t
{
public:

public:
};

#ifdef __clang__
#pragma clang diagnostic pop
#endif



// System.Void Unity.Platforms.RunLoopImpl::EnterMainLoop(Unity.Platforms.RunLoop/RunLoopDelegate)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RunLoopImpl_EnterMainLoop_mA64AA1079E3482F40E37AF7B54764DBF2CF2FCC2 (RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 * ___runLoopDelegate0);
// System.Boolean Unity.Platforms.RunLoop/RunLoopDelegate::Invoke()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool RunLoopDelegate_Invoke_mB498A417DD5ABD7B53FD64D45953F34DEA48E173 (RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 * __this);
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
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_RunLoop_EnterMainLoop_m5E04E5799EAEB94CF41036F171B95E67A7E18F14()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Void Unity.Platforms.RunLoop::EnterMainLoop(Unity.Platforms.RunLoop/RunLoopDelegate)", "it does not have the [MonoPInvokeCallback] attribute.");
}
// System.Void Unity.Platforms.RunLoop::EnterMainLoop(Unity.Platforms.RunLoop_RunLoopDelegate)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RunLoop_EnterMainLoop_m5E04E5799EAEB94CF41036F171B95E67A7E18F14 (RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 * ___runLoopDelegate0)
{
	{
		// RunLoopImpl.EnterMainLoop(runLoopDelegate);
		RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 * L_0 = ___runLoopDelegate0;
		RunLoopImpl_EnterMainLoop_mA64AA1079E3482F40E37AF7B54764DBF2CF2FCC2(L_0);
		// }
		return;
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
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_RunLoopDelegate__ctor_m48B199708FDADA6D247F09325DFD4CB269B13E51()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Void Unity.Platforms.RunLoop/RunLoopDelegate::.ctor(System.Object,System.IntPtr)", "it is an instance method. Only static methods can be called back from native code.");
}
IL2CPP_EXTERN_C  bool DelegatePInvokeWrapper_RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 (RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 * __this)
{
	typedef int32_t (DEFAULT_CALL *PInvokeFunc)();
	PInvokeFunc il2cppPInvokeFunc = reinterpret_cast<PInvokeFunc>(__this);

	// Native function invocation
	int32_t returnValue = il2cppPInvokeFunc();

	return static_cast<bool>(returnValue);
}
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_RunLoopDelegate_Invoke_mB498A417DD5ABD7B53FD64D45953F34DEA48E173()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Boolean Unity.Platforms.RunLoop/RunLoopDelegate::Invoke()", "it is an instance method. Only static methods can be called back from native code.");
}
// System.Void Unity.Platforms.RunLoop_RunLoopDelegate::.ctor(System.Object,System.IntPtr)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RunLoopDelegate__ctor_m48B199708FDADA6D247F09325DFD4CB269B13E51 (RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 * __this, RuntimeObject * ___object0, intptr_t ___method1)
{
	__this->set_method_ptr_0(___method1);
	__this->set_m_target_1(___object0);
}
// System.Boolean Unity.Platforms.RunLoop_RunLoopDelegate::Invoke()
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool RunLoopDelegate_Invoke_mB498A417DD5ABD7B53FD64D45953F34DEA48E173 (RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 * __this)
{
	bool result = false;
	intptr_t targetMethodPointer = __this->get_method_ptr_0();
	RuntimeObject* targetThis = __this->get_m_target_1();
	if (__this->get_m_IsDelegateOpen_3())
	{
		typedef bool (*FunctionPointerType) ();
		result = ((FunctionPointerType)targetMethodPointer)();
	}
	else
	{
		typedef bool (*FunctionPointerType) (void*);
		result = ((FunctionPointerType)targetMethodPointer)(targetThis);
	}
	return result;
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
extern "C" void DEFAULT_CALL ReversePInvokeWrapper_RunLoopImpl_EnterMainLoop_mA64AA1079E3482F40E37AF7B54764DBF2CF2FCC2()
{
	il2cpp_codegen_no_reverse_pinvoke_wrapper("System.Void Unity.Platforms.RunLoopImpl::EnterMainLoop(Unity.Platforms.RunLoop/RunLoopDelegate)", "it does not have the [MonoPInvokeCallback] attribute.");
}
// System.Void Unity.Platforms.RunLoopImpl::EnterMainLoop(Unity.Platforms.RunLoop_RunLoopDelegate)
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void RunLoopImpl_EnterMainLoop_mA64AA1079E3482F40E37AF7B54764DBF2CF2FCC2 (RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 * ___runLoopDelegate0)
{

IL_0000:
	{
		// if (runLoopDelegate() == false)
		RunLoopDelegate_t4C13F5029C590818101896FAAEC1DC569B1A7463 * L_0 = ___runLoopDelegate0;
		bool L_1 = RunLoopDelegate_Invoke_mB498A417DD5ABD7B53FD64D45953F34DEA48E173(L_0);
		if (L_1)
		{
			goto IL_0000;
		}
	}
	{
		// }
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
