#include "il2cpp-config.h"
#include "vm/StackTrace.h"
#include "il2cpp-debug-metadata.h"
#include "os/StackTrace.h"
#include "os/ThreadLocalValue.h"
#include "vm-utils/NativeSymbol.h"

namespace tiny
{
namespace vm
{
#if IL2CPP_ENABLE_STACKTRACES

    class MethodStack
    {
    protected:
        il2cpp::os::ThreadLocalValue s_StackFrames;

        inline StackFrames* GetStackFramesRaw()
        {
            StackFrames* stackFrames = NULL;

            il2cpp::os::ErrorCode result = s_StackFrames.GetValue(reinterpret_cast<void**>(&stackFrames));
            Assert(result == il2cpp::os::kErrorCodeSuccess);

            return stackFrames;
        }

    public:
        inline void InitializeForCurrentThread()
        {
            if (GetStackFramesRaw() != NULL)
                return;

            StackFrames* stackFrames = new StackFrames();
            stackFrames->reserve(64);

            il2cpp::os::ErrorCode result = s_StackFrames.SetValue(stackFrames);
            Assert(result == il2cpp::os::kErrorCodeSuccess);
        }

        inline void CleanupForCurrentThread()
        {
            StackFrames* frames = GetStackFramesRaw();

            if (frames == NULL)
                return;

            delete frames;

            il2cpp::os::ErrorCode result = s_StackFrames.SetValue(NULL);
            Assert(result == il2cpp::os::kErrorCodeSuccess);
        }
    };

#if IL2CPP_ENABLE_STACKTRACE_SENTRIES

    class StacktraceSentryMethodStack : public MethodStack
    {
    public:
        inline const StackFrames* GetStackFrames()
        {
            return GetStackFramesRaw();
        }

        inline bool GetStackFrameAt(int32_t depth, TinyStackFrameInfo& frame)
        {
            const StackFrames& frames = *GetStackFramesRaw();

            if (static_cast<int>(frames.size()) + depth < 1)
                return false;

            frame = frames[frames.size() - 1 + depth];
            return true;
        }

        inline void PushFrame(TinyStackFrameInfo& frame)
        {
            GetStackFramesRaw()->push_back(frame);
        }

        inline void PopFrame()
        {
            StackFrames* stackFrames = GetStackFramesRaw();
            stackFrames->pop_back();
        }
    };

#endif // IL2CPP_ENABLE_STACKTRACE_SENTRIES

#if IL2CPP_ENABLE_NATIVE_STACKTRACES

    class NativeMethodStack : public MethodStack
    {
        static bool GetStackFramesCallback(Il2CppMethodPointer frame, void* context)
        {
            const TinyMethod* method = il2cpp::utils::NativeSymbol::GetMethodFromNativeSymbol(frame);
            StackFrames* stackFrames = static_cast<StackFrames*>(context);

            if (method != NULL)
            {
                TinyStackFrameInfo frameInfo = { 0 };
                frameInfo.method = method;
                stackFrames->push_back(frameInfo);
            }

            return true;
        }

        struct GetStackFrameAtContext
        {
            int32_t currentDepth;
            const TinyMethod* method;
        };

        static bool GetStackFrameAtCallback(Il2CppMethodPointer frame, void* context)
        {
            const TinyMethod* method = il2cpp::utils::NativeSymbol::GetMethodFromNativeSymbol(frame);
            GetStackFrameAtContext* ctx = static_cast<GetStackFrameAtContext*>(context);

            if (method != NULL)
            {
                if (ctx->currentDepth == 0)
                {
                    ctx->method = method;
                    return false;
                }

                ctx->currentDepth++;
            }

            return true;
        }

    public:
        inline const StackFrames* GetStackFrames()
        {
            StackFrames* stackFrames = GetStackFramesRaw();
            if (stackFrames == NULL)
                return stackFrames;
            stackFrames->clear();

            il2cpp::os::StackTrace::WalkStack(&NativeMethodStack::GetStackFramesCallback, stackFrames, il2cpp::os::StackTrace::kFirstCalledToLastCalled);

            return stackFrames;
        }

        inline bool GetStackFrameAt(int32_t depth, TinyStackFrameInfo& frame)
        {
            GetStackFrameAtContext context = { depth, NULL };

            il2cpp::os::StackTrace::WalkStack(&NativeMethodStack::GetStackFrameAtCallback, &context, il2cpp::os::StackTrace::kLastCalledToFirstCalled);

            if (context.method != NULL)
            {
                frame.method = context.method;
                return true;
            }

            return false;
        }

        inline void PushFrame(TinyStackFrameInfo& frame)
        {
        }

        inline void PopFrame()
        {
        }
    };

#endif // IL2CPP_ENABLE_NATIVE_STACKTRACES

#else

    static StackFrames s_EmptyStack;

    class NoOpMethodStack
    {
    public:
        inline void InitializeForCurrentThread()
        {
        }

        inline void CleanupForCurrentThread()
        {
        }

        inline const StackFrames* GetStackFrames()
        {
            return &s_EmptyStack;
        }

        inline bool GetStackFrameAt(int32_t depth, TinyStackFrameInfo& frame)
        {
            return false;
        }

        inline void PushFrame(TinyStackFrameInfo& frame)
        {
        }

        inline void PopFrame()
        {
        }
    };

#endif // IL2CPP_ENABLE_STACKTRACES

#if IL2CPP_ENABLE_STACKTRACES

#if IL2CPP_ENABLE_STACKTRACE_SENTRIES

    StacktraceSentryMethodStack s_MethodStack;

#elif IL2CPP_ENABLE_NATIVE_STACKTRACES

    NativeMethodStack s_MethodStack;

#endif

#else

    NoOpMethodStack s_MethodStack;

#endif // IL2CPP_ENABLE_STACKTRACES

    void StackTrace::InitializeStackTracesForCurrentThread()
    {
        s_MethodStack.InitializeForCurrentThread();
    }

    void StackTrace::CleanupStackTracesForCurrentThread()
    {
        s_MethodStack.CleanupForCurrentThread();
    }

    std::string StackTrace::GetStackTrace()
    {
        const StackFrames* frames = s_MethodStack.GetStackFrames();

        const size_t numberOfFramesToSkip = 1;
        int startFrame = (int)frames->size() - 1 - numberOfFramesToSkip;

        std::string stackTrace;
        for (int i = startFrame; i > 0; i--)
        {
            if (i == startFrame)
                stackTrace += "at ";
            else
                stackTrace += "  at ";
            TinyStackFrameInfo test = (*frames)[i];
            stackTrace += std::string((*frames)[i].method->fullName);
            if (i != 1)
                stackTrace += "\n";
        }

        return stackTrace;
    }

    void StackTrace::PushFrame(TinyStackFrameInfo& frame)
    {
        s_MethodStack.PushFrame(frame);
    }

    void StackTrace::PopFrame()
    {
        s_MethodStack.PopFrame();
    }
}
}
