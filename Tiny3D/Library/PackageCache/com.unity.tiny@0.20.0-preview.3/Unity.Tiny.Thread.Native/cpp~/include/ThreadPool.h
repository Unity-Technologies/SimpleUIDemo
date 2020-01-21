#pragma once

#include <stdint.h>
#include <thread>
#include <atomic>
#include <mutex>
#include <vector>
#include <condition_variable>

#include <Unity/Runtime.h>

#define BUILD_UNITY_TINY_THREADNATIVE

#if defined(BUILD_UNITY_TINY_THREADNATIVE)
#define THREADNATIVE_EXPORT DOTS_CPP_EXPORT
#else
#define THREADNATIVE_EXPORT DOTS_CPP_IMPORT
#endif

namespace ut {
namespace ThreadPool {

// Base class of asynchronous work items.
class Job {
public:
    Job()
        : progress(0)
        , abort(false)
        , returnvalue(false)
        , id(-1)
    {
    }
    virtual ~Job() {}
    // Implement Do function to do all the work.
    virtual bool Do() = 0;
    // Value returned from Do
    bool GetReturnValue() const { return returnvalue; }

protected:
    // Set this in Do() implementation, if there is some easy progress indication.
    // Range is 0..100.
    // Not required, but nice to set at least to 50 at the start if there is no finer
    // grained values.
    std::atomic<int> progress;
    // Read this in Do() implementation, and abort if set in case there are
    // easy checkpoints when doing work. Not required to pay attention to though.
    std::atomic<bool> abort;
    friend class Thread;
    friend class Pool;

private:
    bool returnvalue;
    uint64_t id;
};

// Single worker thread. Can be used to dispatch Job objects
class Thread {
public:
    THREADNATIVE_EXPORT ~Thread();
    void Start(int poolidx);
    void Stop();
    std::unique_ptr<Job> CheckAndRemove(uint64_t id);
    bool GetProgress(uint64_t id, int& progress);
    void Enqueue(std::unique_ptr<Job> p, uint64_t id);
    void Abort(uint64_t id);

private:
    static std::unique_ptr<Job> RemoveFromList(uint64_t id, std::vector<std::unique_ptr<Job>>& list);
    static void StaticThreadFun(Thread* self);
    void ThreadFun();
    void WakeUp();

    // index of thread in pool
    int poolIdx;
    std::unique_ptr<std::thread> thread;
    std::atomic<bool> quit;
    std::mutex mutexLists;
    // lists of work stages. Moving entries between stages is guarded by mutexLists
    std::vector<std::unique_ptr<Job>> todolist;
    std::unique_ptr<Job> doing;
    std::vector<std::unique_ptr<Job>> donelist;
    // event to wake up a thread after it went to sleep because of an empty todo list
    std::mutex mutexWake;
    std::condition_variable waitForWork;
};

// Worker thread pool. Defaults to number of cpus threads.
// Interchangeable with a single thread.
class Pool {
public:
    static THREADNATIVE_EXPORT Pool* GetInstance();
    THREADNATIVE_EXPORT uint64_t Enqueue(std::unique_ptr<Job> p);
    THREADNATIVE_EXPORT void Abort(uint64_t id);
    THREADNATIVE_EXPORT std::unique_ptr<Job> CheckAndRemove(uint64_t id);
    THREADNATIVE_EXPORT bool GetProgress(uint64_t id, int& progress);

private:
    Pool();
    static const int sMaxThreads = 16;
    int nThreads;
    Thread threads[sMaxThreads];
    uint64_t nextId;
};

} // namespace ThreadPool
} // namespace ut

