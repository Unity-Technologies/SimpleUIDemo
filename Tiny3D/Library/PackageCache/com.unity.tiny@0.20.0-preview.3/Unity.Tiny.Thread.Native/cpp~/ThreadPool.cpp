#include "ThreadPool.h"
//#include <stdio.h>

using namespace ut::ThreadPool;

// Thread ---------------------------------------------------------------

Thread::~Thread() { Stop(); }

void
Thread::Start(int poolidx)
{
    quit = false;
    poolIdx = poolidx;
    //Assert(thread.get() == 0);
    thread.reset(new std::thread(&StaticThreadFun, this));
    WakeUp();
}

void
Thread::Stop()
{
    if (thread.get() == 0)
        return;
    quit = true;
    WakeUp();
    {
        // set abort on all active jobs as well, might be quicker
        std::lock_guard<std::mutex> lock(mutexLists);
        if (doing)
            doing->abort = true;
    }
    // wait for last job to finish
    thread->join();
}

std::unique_ptr<Job>
Thread::CheckAndRemove(uint64_t id)
{
    std::lock_guard<std::mutex> lock(mutexLists);
    return RemoveFromList(id, donelist);
}

bool
Thread::GetProgress(uint64_t id, int& progress)
{
    // could be more ligthweight...
    std::lock_guard<std::mutex> lock(mutexLists);
    if (doing && doing->id == id) {
        progress = doing->progress;
        return true;
    }
    return false;
}

void
Thread::Enqueue(std::unique_ptr<Job> p, uint64_t id)
{
    std::lock_guard<std::mutex> lock(mutexLists);
    p->id = id;
    todolist.push_back(std::move(p));
    WakeUp();
}

void
Thread::Abort(uint64_t id)
{
    // things can only move forward todo->doing->done, so if it exists
    // it needs to be in one of them
    std::lock_guard<std::mutex> lock(mutexLists);
    std::unique_ptr<Job> notstarted = RemoveFromList(id, todolist);
    if (notstarted)
        return;
    if (doing) {
        // if abort was set the thread will get rid of the result when done
        doing->abort = true;
        return;
    }
    std::unique_ptr<Job> donealready = RemoveFromList(id, todolist);
    if (donealready)
        return;
    // this could happen with multiple consumers, but should not
    // in single consumer case
    //Assert(0);
}

std::unique_ptr<Job>
Thread::RemoveFromList(uint64_t id, std::vector<std::unique_ptr<Job>>& list)
{
    for (size_t i = 0; i < list.size(); i++) {
        if (list[i]->id == id) {
            std::unique_ptr<Job> r = std::move(list[i]);
            list[i] = std::move(list.back());
            list.pop_back();
            return r;
        }
    }
    return 0;
}

void
Thread::StaticThreadFun(Thread* self)
{
    self->ThreadFun();
}

void
Thread::ThreadFun()
{
    while (!quit) {
        // look for work (can wait on event here)
        // printf ( "Thread %i woke up, looking for work...\n", poolIdx);
        { // move from todo to doing
            mutexLists.lock();
            if (!todolist.empty()) {
                doing = std::move(todolist.back());
                todolist.pop_back();
                mutexLists.unlock();
            } else {
                //Assert(doing.get() == 0);
                mutexLists.unlock();
                // nothing to do
                // wait on event to check quit or todolist again
                // printf ( "Thread %i sleeps...\n", poolIdx);
                {
                    std::unique_lock<std::mutex> lock(mutexWake);
                    waitForWork.wait(lock);
                }
                continue;
            }
        }

        // doing the job
        // printf ( "Thread %i begining work...\n", poolIdx);
        doing->returnvalue = doing->Do();
        // printf ( "Thread %i job is done...\n", poolIdx);

        { // move from doing to done
            std::lock_guard<std::mutex> lock(mutexLists);
            if (!doing->abort)
                donelist.push_back(std::move(doing));
            doing = 0;
        }
    }
    // printf ( "Thread %i quit.\n", poolIdx);
}

void
Thread::WakeUp()
{
    std::lock_guard<std::mutex> lock(mutexWake);
    waitForWork.notify_one();
}

// Pool ---------------------------------------------------------------

Pool::Pool()
{
    int nhw = std::thread::hardware_concurrency();
    if (nhw <= 0)
        nhw = 2;
    nThreads = nhw < sMaxThreads ? nhw : sMaxThreads;
    nextId = 1;
    for (int i = 0; i < nThreads; i++)
        threads[i].Start(i);
}

uint64_t
Pool::Enqueue(std::unique_ptr<Job> p)
{
    // super basic round robin by id assignment
    // can get much fancier with work stealing etc later
    uint64_t id = nextId++;
    int threadidx = id % nThreads;
    threads[threadidx].Enqueue(std::move(p), id);
    //Assert(id != 0);
    return id;
}

void
Pool::Abort(uint64_t id)
{
    int threadidx = id % nThreads;
    threads[threadidx].Abort(id);
}

std::unique_ptr<Job>
Pool::CheckAndRemove(uint64_t id)
{
    int threadidx = id % nThreads;
    return threads[threadidx].CheckAndRemove(id);
}

bool
Pool::GetProgress(uint64_t id, int& progress)
{
    int threadidx = id % nThreads;
    return threads[threadidx].GetProgress(id, progress);
}

ut::ThreadPool::Pool* 
Pool::GetInstance()
{
    static ut::ThreadPool::Pool* threadPool = new ut::ThreadPool::Pool();
    return threadPool;
}
