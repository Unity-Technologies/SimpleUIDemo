#include "il2cpp-config.h"
#include "il2cpp-class-internals.h"
#include "GCHandle.h"
#include "gc/GCHandle.h"
#include "vm/Exception.h"

namespace tiny
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
namespace Runtime
{
namespace InteropServices
{
    void GCHandle::FreeHandle(int32_t handle)
    {
        il2cpp::gc::GCHandle::Free(handle);
    }

    Il2CppObject* GCHandle::GetTarget(int32_t handle)
    {
        return il2cpp::gc::GCHandle::GetTarget(handle);
    }

    int32_t GCHandle::GetTargetHandle(Il2CppObject* obj, int32_t handle, int32_t type)
    {
        return il2cpp::gc::GCHandle::GetTargetHandle(obj, handle, type);
    }
} /* namespace InteropServices */
} /* namespace Runtime */
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace tiny */
