#include "il2cpp-config.h"
#include "os/Time.h"

extern "C" int64_t STDCALL Stopwatch_GetTimestamp()
{
    return il2cpp::os::Time::GetTicks100NanosecondsMonotonic();
}
