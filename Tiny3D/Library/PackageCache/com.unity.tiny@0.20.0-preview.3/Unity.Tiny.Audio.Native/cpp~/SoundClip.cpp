#include "SoundClip.h"
#include "NativeAudio.h"
#include <assert.h>
#include <stdlib.h>
#include <string.h>


SoundClip::SoundClipStatus SoundClip::checkLoad()
{
    ma_decoder_config config;
    memset(&config, 0, sizeof(config));
    config.format = ma_format_s16;
    config.channels = 2;
    config.sampleRate = 44100;
    void* mem = 0;

    if (this->m_status == WORKING) {
        if (m_memory) {
            auto result = ma_decode_memory(m_memory, m_memorySize, &config, &m_nFrames, &mem);
            if (result != MA_SUCCESS) {
                free(m_memory);
                m_memory = 0;
                m_memorySize = 0;
                
                LOGE("Error decoding memory (in SoundClip::checkLoad())");
                this->m_status = FAIL;
                return this->m_status;
            }
        }
        else {
            int rc = ma_decode_file(m_fileName.c_str(), &config, &m_nFrames, &mem);
            if (rc != MA_SUCCESS) {
                LOGE("Error decoding file `%s` (in SoundClip::checkLoad()). Error=%d", m_fileName.c_str(), rc);
                this->m_status = FAIL;
                return this->m_status;
            }
        }
        if (config.channels != 2 || config.sampleRate != 44100) {
            LOGE("Error bad config (in SoundClip::checkLoad())");
            this->m_status = FAIL;
            return this->m_status;
        }
        m_frames = (int16_t*) mem;
        m_status = (m_frames && m_nFrames > 0) ? OK : FAIL;    // once status goes OK the buffer will be decoded() on a separate thread!

        LOGE("Decoded: %s status=%d nFrames=%d config: format=%d channels=%d sampleRate=%d", m_fileName.c_str(), m_status, m_nFrames, 
            config.format, config.channels, config.sampleRate);
    }
    return m_status;
}


SoundClip::~SoundClip()
{
    if (m_frames) {
        ma_free(m_frames);
    }
    if (m_memory) {
        free(m_memory);
    }
}


struct WAV
{
    char chunID[4]          = { 'R', 'I', 'F', 'F' };
    uint32_t chunkSize      = 0;             // filesize - 8
    char format[4]          = { 'W', 'A', 'V', 'E' };
    char subChunk1D[4]      = { 'f', 'm', 't', ' ' };
    uint32_t subChunk1Size  = 16;
    uint16_t audioFormat    = 1;
    uint16_t numChannels    = 2;            // 1 or 2
    uint32_t sampleRate     = 44100;        // 44100, 22050
    uint32_t byteRate       = 0;            // == SampleRate * NumChannels * BitsPerSample/8
    uint16_t blockAlign     = 0;            // == NumChannels * BitsPerSample/8
    uint16_t bitsPerSample  = 16;           // 8 or 16
    char subChunk2ID[4]     = { 'd', 'a', 't', 'a' };
    uint32_t subChunk2Size  = 0;            // == filesize - sizeof(WAV)
};

static_assert(sizeof(WAV) == 44, "WAV structure must be 44 bytes.");    // In theory there are other wav header sizes, but I've never seen one.

// Generates silence with the specified parameters. Used for testing.
void* SoundClip::constructWAV(int nFrames, int nChannels, int bitsPerSample, int frequency, size_t* nBytes)
{
    if (bitsPerSample == 8 || bitsPerSample == 16) {}
    else {
        assert(false);
        return 0;
    }

    if (frequency == 44100 || frequency == 22050) {}
    else {
        assert(false);
        return 0;
    }

    if (nChannels == 1 || nChannels == 2) {}
    else {
        assert(false);
        return 0;
    }

    int bytesPerSample = bitsPerSample / 8;
    int bytesPerFrame = nChannels * bytesPerSample;
    int fileSize = sizeof(WAV) + bytesPerFrame * nFrames;
    int nSamples = nFrames * nChannels;

    WAV wav;
    wav.chunkSize = fileSize - 8;
    wav.numChannels = nChannels;
    wav.sampleRate = frequency;
    wav.byteRate = frequency * bytesPerFrame;
    wav.blockAlign = bytesPerFrame;
    wav.bitsPerSample = bitsPerSample;
    wav.subChunk2Size = fileSize - sizeof(WAV);

    void* mem = malloc(fileSize);
    memset(mem, 0, fileSize);
    memcpy(mem, &wav, sizeof(WAV));
    *nBytes = fileSize;
    return mem;
}
