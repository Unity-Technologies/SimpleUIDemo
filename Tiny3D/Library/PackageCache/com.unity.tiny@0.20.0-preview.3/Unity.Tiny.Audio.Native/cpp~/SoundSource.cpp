#include "SoundSource.h"
#include "NativeAudio.h"

#include <string.h>

#define CHECK_CLIP m_clip->addRef(); m_clip->releaseRef();

SoundSource::SoundSource(SoundClip* clip)
{
    m_clip = clip;
    m_clip->addRef();
    LOGE("SoundSource() %s", m_clip->FileName().c_str());
}


SoundSource::~SoundSource()
{
    LOGE("~SoundSource() %s", m_clip->FileName().c_str());
    m_clip->releaseRef();
}

void SoundSource::play()
{
    CHECK_CLIP
    if (m_status == NotYetStarted || m_status == Stopped) {
        m_framePos = 0;
        m_status = Playing;
    }
}

void SoundSource::stop()
{
    CHECK_CLIP
    m_status = Stopped;
}

const int16_t* SoundSource::fetch(uint32_t frameCount, uint32_t* delivered)
{
    CHECK_CLIP
    uint64_t read = 0;

    if (m_status == Playing && m_clip->okay()) {
        uint64_t framesRemaining = m_clip->numFrames() - m_framePos;
        const int16_t* src = m_clip->frames() + m_framePos * 2;

        if(frameCount <= framesRemaining) {
            m_framePos += frameCount;
            read = frameCount;
        }
        else {
            m_framePos += framesRemaining;
            read = framesRemaining;
        }

        if (m_framePos == m_clip->numFrames() && !loop()) {
            m_status = Stopped;
        }
        *delivered = (uint32_t)read;
        return src;
    }
    *delivered = 0;
    return 0;
}

