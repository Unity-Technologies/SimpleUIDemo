#pragma once

#include <string>
#include "miniaudio/miniaudio.h"

#include "SoundClip.h"

class SoundSource
{
public:
    enum SoundStatus { 
        NotYetStarted, 
        Playing, 
        //Paused,     // Not supported per sound
        //Finished,   // Equivalent to stop in current code
        Stopped 
    };

    SoundSource(SoundClip* clip);
    ~SoundSource();
    
    void play();
    void stop();
    SoundStatus getStatus() const { return m_status; }
    bool isPlaying() const { return m_status == Playing && m_clip->okay(); }

    void setVolume(float v) { m_volume = v; }
    float volume() const { return m_volume; }
    void setLoop(bool enable) { m_loop = enable; }
    bool loop() const { return m_loop; }

    bool readyToDelete() {
        return m_status == Stopped;
    }

    // Returns the number of frames fetched.
    const int16_t* fetch(uint32_t frameCount, uint32_t* delivered);
    // Resets the decoding (used for looping)
    void rewind() { m_framePos = 0; m_status = Playing; }

private:
    SoundClip* m_clip;

    float m_volume = 1.0f;
    bool m_loop = false;
    SoundStatus m_status = NotYetStarted;
    uint64_t m_framePos = 0;
};
