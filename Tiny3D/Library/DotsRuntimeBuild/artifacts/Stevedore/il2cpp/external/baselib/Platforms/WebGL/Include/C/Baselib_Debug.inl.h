#pragma once

#ifdef __cplusplus
extern "C" {
#endif
// We don't want to include whole emscripten.h
int emscripten_asm_const_int(const char* code, ...);
#ifdef __cplusplus
}
#endif

// The debugger statement invokes any available debugging functionality, such as setting a breakpoint.
// If no debugging functionality is available, this statement has no effect.
// Also Using compilers __builtin_debugtrap generates:
// "WARNING:root:disabling asm.js validation due to use of non-supported features: llvm.debugtrap is used"
#define BASELIB_DEBUG_TRAP() ((void)emscripten_asm_const_int("debugger;"))
