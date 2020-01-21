#include "il2cpp-config.h"

#include "il2cpp-class-internals.h"
#include "GC.h"
#include "gc/GarbageCollector.h"

namespace tiny
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
    void GC::InternalCollect(int generation)
    {
        il2cpp::gc::GarbageCollector::Collect(generation);
    }
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace tiny */
