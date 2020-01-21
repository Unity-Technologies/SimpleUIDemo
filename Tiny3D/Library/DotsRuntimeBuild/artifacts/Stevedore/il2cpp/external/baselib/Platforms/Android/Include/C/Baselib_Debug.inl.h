#pragma once

#include <signal.h>
#define BASELIB_DEBUG_TRAP() raise(SIGTRAP)
