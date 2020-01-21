#pragma once

#include <string>
#include "miniaudio/miniaudio.h"

class SoundClip
{
public:
    enum SoundClipStatus {
        WORKING,
        OK,
        FAIL
    };

    SoundClip(std::string filename) : m_fileName(filename) {}
    // Passes in memory *by ownership*, needs to allocated with platform_aligned_malloc
    SoundClip(void* memory, size_t memSize) : m_memory(memory), m_memorySize(memSize) {}
    ~SoundClip();

    const std::string& FileName() const { return m_fileName; }

    // The refcounts are on the main thread. Once the status of this object is 'OK',
    // then decoding will happen on the audio thread. This SoundClip object is locked
    // until the ref goes to zero, and nothing is using it as a source.
    void addRef() { ++m_refCount; }
    void releaseRef() { --m_refCount; }
    int refCount() const { return m_refCount; }

    // No deletion until the refCount is zero.
    void queueDeletion()        { m_queuedForDelete = true; }
    bool isQueuedForDeletion()  { return m_queuedForDelete; }

    SoundClipStatus checkLoad();

    bool okay() const { return m_status == OK; }            // Called from decoding thread
    const int16_t* frames() const { return m_frames; }      // Called from decoding thread
    uint64_t numFrames() const { return m_nFrames; }        // Called from decoding thread

    static void* constructWAV(int nFrames, int channels, int bitsPerSample, int frequency, size_t* nBytes);

private:
    std::string m_fileName;
    void* m_memory = 0;
    size_t m_memorySize = 0;

    int m_refCount = 0;
    bool m_queuedForDelete = false;
    SoundClipStatus m_status = WORKING;
    int16_t* m_frames = 0;
    uint64_t m_nFrames = 0;
};


