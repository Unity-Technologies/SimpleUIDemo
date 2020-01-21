#pragma once
#include "gc/GarbageCollector.h"

namespace tiny
{
namespace vm
{
    struct ScopedThreadAttacher
    {
        ScopedThreadAttacher()
        {
#if IL2CPP_GC_BUMP
            m_MemoryCursor = gc::GarbageCollector::GetPosition();
#endif
        }

        ~ScopedThreadAttacher()
        {
#if IL2CPP_GC_BUMP
            gc::GarbageCollector::SetPosition(m_MemoryCursor);
#endif
        }

    private:
#if IL2CPP_GC_BUMP
        void* m_MemoryCursor;
#endif
    };
} /* namespace vm */
} /* namespace il2cpp */
