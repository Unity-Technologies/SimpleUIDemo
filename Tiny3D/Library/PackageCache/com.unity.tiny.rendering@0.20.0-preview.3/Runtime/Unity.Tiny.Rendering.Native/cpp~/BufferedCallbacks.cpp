/*
We need to register a whole bunch of callbacks with BGFX, but they will be called from the BGFX rendering thread.
That's why we can not call directly into c# with delegates, as now we have managed c# code running on a non-main thread, which is not supported by il2cpp.

So, we need to make these c++ callbacks and gather events in buffers that we can then grab and read from via the main thread.

Don't care about multiple instances, it's all a single one and static anyway
*/

#include <mutex>
#include <vector>
#include <chrono>
#include <memory>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>

#include <Unity/Runtime.h>

//#define BGFX_CALLBACK_DO_ABORT abort();
//#define BGFX_CALLBACK_PRINTF(...) printf(__VA_ARGS__);
#define BGFX_CALLBACK_DO_ABORT
#define BGFX_CALLBACK_PRINTF(...)
#define BGFX_CALLBACK_WRITESCREENSHOT

typedef enum bgfx_fatal
{
    BGFX_FATAL_DEBUG_CHECK,                   /** ( 0)                                */
    BGFX_FATAL_INVALID_SHADER,                /** ( 1)                                */
    BGFX_FATAL_UNABLE_TO_INITIALIZE,          /** ( 2)                                */
    BGFX_FATAL_UNABLE_TO_CREATE_TEXTURE,      /** ( 3)                                */
    BGFX_FATAL_DEVICE_LOST,                   /** ( 4)                                */

    BGFX_FATAL_COUNT

} bgfx_fatal_t;

typedef enum bgfx_texture_format
{
    BGFX_TEXTURE_FORMAT_BC1,                  /** ( 0) DXT1 R5G6B5A1                  */
    BGFX_TEXTURE_FORMAT_BC2,                  /** ( 1) DXT3 R5G6B5A4                  */
    BGFX_TEXTURE_FORMAT_BC3,                  /** ( 2) DXT5 R5G6B5A8                  */
    BGFX_TEXTURE_FORMAT_BC4,                  /** ( 3) LATC1/ATI1 R8                  */
    BGFX_TEXTURE_FORMAT_BC5,                  /** ( 4) LATC2/ATI2 RG8                 */
    BGFX_TEXTURE_FORMAT_BC6H,                 /** ( 5) BC6H RGB16F                    */
    BGFX_TEXTURE_FORMAT_BC7,                  /** ( 6) BC7 RGB 4-7 bits per color channel, 0-8 bits alpha */
    BGFX_TEXTURE_FORMAT_ETC1,                 /** ( 7) ETC1 RGB8                      */
    BGFX_TEXTURE_FORMAT_ETC2,                 /** ( 8) ETC2 RGB8                      */
    BGFX_TEXTURE_FORMAT_ETC2A,                /** ( 9) ETC2 RGBA8                     */
    BGFX_TEXTURE_FORMAT_ETC2A1,               /** (10) ETC2 RGB8A1                    */
    BGFX_TEXTURE_FORMAT_PTC12,                /** (11) PVRTC1 RGB 2BPP                */
    BGFX_TEXTURE_FORMAT_PTC14,                /** (12) PVRTC1 RGB 4BPP                */
    BGFX_TEXTURE_FORMAT_PTC12A,               /** (13) PVRTC1 RGBA 2BPP               */
    BGFX_TEXTURE_FORMAT_PTC14A,               /** (14) PVRTC1 RGBA 4BPP               */
    BGFX_TEXTURE_FORMAT_PTC22,                /** (15) PVRTC2 RGBA 2BPP               */
    BGFX_TEXTURE_FORMAT_PTC24,                /** (16) PVRTC2 RGBA 4BPP               */
    BGFX_TEXTURE_FORMAT_ATC,                  /** (17) ATC RGB 4BPP                   */
    BGFX_TEXTURE_FORMAT_ATCE,                 /** (18) ATCE RGBA 8 BPP explicit alpha */
    BGFX_TEXTURE_FORMAT_ATCI,                 /** (19) ATCI RGBA 8 BPP interpolated alpha */
    BGFX_TEXTURE_FORMAT_ASTC4X4,              /** (20) ASTC 4x4 8.0 BPP               */
    BGFX_TEXTURE_FORMAT_ASTC5X5,              /** (21) ASTC 5x5 5.12 BPP              */
    BGFX_TEXTURE_FORMAT_ASTC6X6,              /** (22) ASTC 6x6 3.56 BPP              */
    BGFX_TEXTURE_FORMAT_ASTC8X5,              /** (23) ASTC 8x5 3.20 BPP              */
    BGFX_TEXTURE_FORMAT_ASTC8X6,              /** (24) ASTC 8x6 2.67 BPP              */
    BGFX_TEXTURE_FORMAT_ASTC10X5,             /** (25) ASTC 10x5 2.56 BPP             */
    BGFX_TEXTURE_FORMAT_UNKNOWN,              /** (26) Compressed formats above.      */
    BGFX_TEXTURE_FORMAT_R1,                   /** (27)                                */
    BGFX_TEXTURE_FORMAT_A8,                   /** (28)                                */
    BGFX_TEXTURE_FORMAT_R8,                   /** (29)                                */
    BGFX_TEXTURE_FORMAT_R8I,                  /** (30)                                */
    BGFX_TEXTURE_FORMAT_R8U,                  /** (31)                                */
    BGFX_TEXTURE_FORMAT_R8S,                  /** (32)                                */
    BGFX_TEXTURE_FORMAT_R16,                  /** (33)                                */
    BGFX_TEXTURE_FORMAT_R16I,                 /** (34)                                */
    BGFX_TEXTURE_FORMAT_R16U,                 /** (35)                                */
    BGFX_TEXTURE_FORMAT_R16F,                 /** (36)                                */
    BGFX_TEXTURE_FORMAT_R16S,                 /** (37)                                */
    BGFX_TEXTURE_FORMAT_R32I,                 /** (38)                                */
    BGFX_TEXTURE_FORMAT_R32U,                 /** (39)                                */
    BGFX_TEXTURE_FORMAT_R32F,                 /** (40)                                */
    BGFX_TEXTURE_FORMAT_RG8,                  /** (41)                                */
    BGFX_TEXTURE_FORMAT_RG8I,                 /** (42)                                */
    BGFX_TEXTURE_FORMAT_RG8U,                 /** (43)                                */
    BGFX_TEXTURE_FORMAT_RG8S,                 /** (44)                                */
    BGFX_TEXTURE_FORMAT_RG16,                 /** (45)                                */
    BGFX_TEXTURE_FORMAT_RG16I,                /** (46)                                */
    BGFX_TEXTURE_FORMAT_RG16U,                /** (47)                                */
    BGFX_TEXTURE_FORMAT_RG16F,                /** (48)                                */
    BGFX_TEXTURE_FORMAT_RG16S,                /** (49)                                */
    BGFX_TEXTURE_FORMAT_RG32I,                /** (50)                                */
    BGFX_TEXTURE_FORMAT_RG32U,                /** (51)                                */
    BGFX_TEXTURE_FORMAT_RG32F,                /** (52)                                */
    BGFX_TEXTURE_FORMAT_RGB8,                 /** (53)                                */
    BGFX_TEXTURE_FORMAT_RGB8I,                /** (54)                                */
    BGFX_TEXTURE_FORMAT_RGB8U,                /** (55)                                */
    BGFX_TEXTURE_FORMAT_RGB8S,                /** (56)                                */
    BGFX_TEXTURE_FORMAT_RGB9E5F,              /** (57)                                */
    BGFX_TEXTURE_FORMAT_BGRA8,                /** (58)                                */
    BGFX_TEXTURE_FORMAT_RGBA8,                /** (59)                                */
    BGFX_TEXTURE_FORMAT_RGBA8I,               /** (60)                                */
    BGFX_TEXTURE_FORMAT_RGBA8U,               /** (61)                                */
    BGFX_TEXTURE_FORMAT_RGBA8S,               /** (62)                                */
    BGFX_TEXTURE_FORMAT_RGBA16,               /** (63)                                */
    BGFX_TEXTURE_FORMAT_RGBA16I,              /** (64)                                */
    BGFX_TEXTURE_FORMAT_RGBA16U,              /** (65)                                */
    BGFX_TEXTURE_FORMAT_RGBA16F,              /** (66)                                */
    BGFX_TEXTURE_FORMAT_RGBA16S,              /** (67)                                */
    BGFX_TEXTURE_FORMAT_RGBA32I,              /** (68)                                */
    BGFX_TEXTURE_FORMAT_RGBA32U,              /** (69)                                */
    BGFX_TEXTURE_FORMAT_RGBA32F,              /** (70)                                */
    BGFX_TEXTURE_FORMAT_R5G6B5,               /** (71)                                */
    BGFX_TEXTURE_FORMAT_RGBA4,                /** (72)                                */
    BGFX_TEXTURE_FORMAT_RGB5A1,               /** (73)                                */
    BGFX_TEXTURE_FORMAT_RGB10A2,              /** (74)                                */
    BGFX_TEXTURE_FORMAT_RG11B10F,             /** (75)                                */
    BGFX_TEXTURE_FORMAT_UNKNOWNDEPTH,         /** (76) Depth formats below.           */
    BGFX_TEXTURE_FORMAT_D16,                  /** (77)                                */
    BGFX_TEXTURE_FORMAT_D24,                  /** (78)                                */
    BGFX_TEXTURE_FORMAT_D24S8,                /** (79)                                */
    BGFX_TEXTURE_FORMAT_D32,                  /** (80)                                */
    BGFX_TEXTURE_FORMAT_D16F,                 /** (81)                                */
    BGFX_TEXTURE_FORMAT_D24F,                 /** (82)                                */
    BGFX_TEXTURE_FORMAT_D32F,                 /** (83)                                */
    BGFX_TEXTURE_FORMAT_D0S8,                 /** (84)                                */

    BGFX_TEXTURE_FORMAT_COUNT

} bgfx_texture_format_t;

#pragma pack(push)
#pragma pack(1)
typedef struct {
    char  idlength;
    char  colourmaptype;
    char  datatypecode;
    short int colourmaporigin;
    short int colourmaplength;
    char  colourmapdepth;
    short int x_origin;
    short int y_origin;
    short width;
    short height;
    char  bitsperpixel;
    char  imagedescriptor;
} TGA_HEADER;
#pragma pack(pop)

/**/
typedef struct bgfx_interface_vtbl bgfx_interface_vtbl_t;

/**/
typedef struct bgfx_callback_interface_s
{
    const struct bgfx_callback_vtbl_s* vtbl;
} bgfx_callback_interface_t;

/**/
typedef struct bgfx_callback_vtbl_s
{
    void(*fatal)(bgfx_callback_interface_t* _this, const char* _filePath, uint16_t _line, bgfx_fatal_t _code, const char* _str);
    void(*trace_vargs)(bgfx_callback_interface_t* _this, const char* _filePath, uint16_t _line, const char* _format, va_list _argList);
    void(*profiler_begin)(bgfx_callback_interface_t* _this, const char* _name, uint32_t _abgr, const char* _filePath, uint16_t _line);
    void(*profiler_begin_literal)(bgfx_callback_interface_t* _this, const char* _name, uint32_t _abgr, const char* _filePath, uint16_t _line);
    void(*profiler_end)(bgfx_callback_interface_t* _this);
    uint32_t(*cache_read_size)(bgfx_callback_interface_t* _this, uint64_t _id);
    bool(*cache_read)(bgfx_callback_interface_t* _this, uint64_t _id, void* _data, uint32_t _size);
    void(*cache_write)(bgfx_callback_interface_t* _this, uint64_t _id, const void* _data, uint32_t _size);
    void(*screen_shot)(bgfx_callback_interface_t* _this, const char* _filePath, uint32_t _width, uint32_t _height, uint32_t _pitch, const void* _data, uint32_t _size, bool _yflip);
    void(*capture_begin)(bgfx_callback_interface_t* _this, uint32_t _width, uint32_t _height, uint32_t _pitch, bgfx_texture_format_t _format, bool _yflip);
    void(*capture_end)(bgfx_callback_interface_t* _this);
    void(*capture_frame)(bgfx_callback_interface_t* _this, const void* _data, uint32_t _size);

} bgfx_callback_vtbl_t;

// must match c# 
enum class BGFXCallbackEntryType {
    Fatal = 0,
    Trace = 1,
    ProfilerBegin = 2,
    ProfilerBeginLiteral = 3,
    ProfilerEnd = 4,
    ScreenShot = 5,
    ScreenShotFilename = 6,
    ScreenShotDesc = 7
};

struct BGFXScreenShotDesc {
    int width;
    int height;
    int pitch;
    int size;
    int yflip;
};

struct BGFXCallbackEntry {
    uint64_t time;
    union {
        BGFXCallbackEntryType callbacktype;
        int callbacktypei;
    };
    int additionalAllocatedDataStart;
    int additionalAllocatedDataLen;
};

static bgfx_callback_interface_s cb_interface;
static bgfx_callback_vtbl_s cb_vtbl;
static std::vector<char> logbuffer;
static std::vector<BGFXCallbackEntry> calllog;
static std::mutex mutex;

static uint64_t getHighResTime() {
    return (uint64_t)std::chrono::high_resolution_clock::now().time_since_epoch().count();
}

static void addEntry(BGFXCallbackEntryType t, const char* mem, int memLen) {
    BGFXCallbackEntry e;
    e.time = getHighResTime();
    e.callbacktype = t;
    e.additionalAllocatedDataLen = memLen;
    if (!mem || memLen <= 0) {
        e.additionalAllocatedDataStart = -1;
        e.additionalAllocatedDataLen = 0;
    } else {
        int s = (int)logbuffer.size();
        e.additionalAllocatedDataStart = s;
        logbuffer.resize(s + memLen);
        char* dest = logbuffer.data() + s;
        memcpy(dest, mem, memLen);
    }
    calllog.push_back(e);
}

static void addEntryString(BGFXCallbackEntryType t, const char* str) {
    int s = str ? (int)strlen(str) + 1 : 0;
    addEntry(t, str, s);
}

static bool endsWith(const char *path, const char *ext) {
    int pl = (int)strlen(path);
    int l = (int)strlen(ext);
    if (pl < l)
        return false;
    return strcmp(path + pl - l, ext) == 0;
}

static const char *stripPath(const char *inPath) {
    if (!inPath)
        return 0;
    int idx = (int)strlen(inPath);
    do {
        idx--;
    } while (idx >= 0 && inPath[idx] != '/' && inPath[idx] != '\\');
    return inPath + idx + 1;
}

static void stripTrailing(char *buf, char c) {
    int idx = (int)strlen(buf);
    if (idx <= 0)
        return;
    if (buf[idx - 1] == c)
        buf[idx - 1] = 0;
}

static void writeTGA(FILE *f, int w, int h, int pitch, const char *data, bool yflip) {
    TGA_HEADER hdr = { 0 };
    hdr.datatypecode = 2; // RGB uncompressed
    hdr.width = (short)w;
    hdr.height = (short)h;
    hdr.bitsperpixel = 32;
    fwrite(&hdr, sizeof(TGA_HEADER), 1, f);
    for (int y = 0; y < h; y++) {
        int y2 = yflip ? h - 1 - y : y;
        const char *srcLine = y2 * pitch + data;
        fwrite(srcLine, 4, w, f);
    }
}

// callbacks from bgfx, any thread
static void fatal(bgfx_callback_interface_t* _this, const char* _filePath, uint16_t _line, bgfx_fatal_t _code, const char* _str) {
    { // don't hold mutex for abort
        std::lock_guard<std::mutex> lock(mutex);
        _filePath = stripPath(_filePath);
        char buf[4096] = { 0 };
        snprintf(buf, sizeof(buf), "FATAL: %x %s at %s:%i", (int)_code, _str, _filePath, _line);
        addEntryString(BGFXCallbackEntryType::Fatal, buf);
        BGFX_CALLBACK_PRINTF("%s\n", buf);
    }
    BGFX_CALLBACK_DO_ABORT;
}

static void trace_vargs(bgfx_callback_interface_t* _this, const char* _filePath, uint16_t _line, const char* _format, va_list _argList) {
    std::lock_guard<std::mutex> lock(mutex);
    _filePath = stripPath(_filePath);
    char buf[4096] = { 0 };
    char buf2[4096] = { 0 };
    vsnprintf(buf, sizeof(buf), _format, _argList);
    stripTrailing(buf, '\n');
    snprintf(buf2, sizeof(buf2), "%s (at %s:%i)", buf, _filePath, (int)_line);
    addEntryString(BGFXCallbackEntryType::Trace, buf2);
    BGFX_CALLBACK_PRINTF("%s\n", buf);
}

static void profiler_begin(bgfx_callback_interface_t* _this, const char* _name, uint32_t _abgr, const char* _filePath, uint16_t _line) {
    std::lock_guard<std::mutex> lock(mutex);
    _filePath = stripPath(_filePath);
    char buf[4096] = { 0 };
    snprintf(buf, sizeof(buf), "%s (at %s:%i)", _name, _filePath, (int)_line);
    addEntryString(BGFXCallbackEntryType::ProfilerBegin, buf);
}

static void profiler_begin_literal(bgfx_callback_interface_t* _this, const char* _name, uint32_t _abgr, const char* _filePath, uint16_t _line) {
    std::lock_guard<std::mutex> lock(mutex);
    _filePath = stripPath(_filePath);
    char buf[4096] = { 0 };
    snprintf(buf, sizeof(buf), "%s (at %s:%i)", _name, _filePath, (int)_line);
    addEntryString(BGFXCallbackEntryType::ProfilerBeginLiteral, buf);
}

static void profiler_end(bgfx_callback_interface_t* _this) {
    std::lock_guard<std::mutex> lock(mutex);
    addEntryString(BGFXCallbackEntryType::ProfilerEnd, 0);
}

static uint32_t cache_read_size(bgfx_callback_interface_t* _this, uint64_t _id) {
    return 0;
}

static bool cache_read(bgfx_callback_interface_t* _this, uint64_t _id, void* _data, uint32_t _size) {
    return false;
}

static void cache_write(bgfx_callback_interface_t* _this, uint64_t _id, const void* _data, uint32_t _size) {
}

static void screen_shot(bgfx_callback_interface_t* _this, const char* _filePath, uint32_t _width, uint32_t _height, uint32_t _pitch, const void* _data, uint32_t _size, bool _yflip) {
    std::lock_guard<std::mutex> lock(mutex);
    BGFXScreenShotDesc ds;
    ds.width = (int)_width;
    ds.height = (int)_height;
    ds.pitch = (int)_pitch;
    ds.size = (int)_size;
    ds.yflip = _yflip ? 1 : 0;
    addEntry(BGFXCallbackEntryType::ScreenShotDesc, (const char*)&ds, sizeof(BGFXScreenShotDesc));
    addEntryString(BGFXCallbackEntryType::ScreenShotFilename, _filePath);
    addEntry(BGFXCallbackEntryType::ScreenShot, (const char*)_data, _size);
    BGFX_CALLBACK_PRINTF("SCREENSHOT: %s (%i*%i)\n", _filePath, (int)_width, (int)_height);

#ifdef BGFX_CALLBACK_WRITESCREENSHOT
    if (!_filePath || !_filePath[0])
        return;
    char path2[4096] = { 0 };
    if (!endsWith(_filePath, ".tga")) {
        snprintf(path2, sizeof(path2), "%s.tga", _filePath);
        _filePath = path2;
    }
    FILE *f = fopen(_filePath, "wb");
    if (f) {
        writeTGA(f, (int)_width, (int)_height, (int)_pitch, (const char*)_data, !_yflip);
        fclose(f);
        BGFX_CALLBACK_PRINTF("  wrote file %s\n", _filePath);
    }
    else {
        BGFX_CALLBACK_PRINTF("  could not write file.\n");
    }
#endif
}

static void capture_begin(bgfx_callback_interface_t* _this, uint32_t _width, uint32_t _height, uint32_t _pitch, bgfx_texture_format_t _format, bool _yflip) {
    BGFX_CALLBACK_DO_ABORT
}

static void capture_end(bgfx_callback_interface_t* _this) {
    BGFX_CALLBACK_DO_ABORT
}

static void capture_frame(bgfx_callback_interface_t* _this, const void* _data, uint32_t _size) {
    BGFX_CALLBACK_DO_ABORT
}

// called from c#, main thread
DOTS_EXPORT(void*) BGFXCB_Init() {
    std::lock_guard<std::mutex> lock(mutex);
    cb_vtbl.fatal = &fatal;
    cb_vtbl.trace_vargs = &trace_vargs;
    cb_vtbl.profiler_begin = &profiler_begin;
    cb_vtbl.profiler_begin_literal = &profiler_begin_literal;
    cb_vtbl.profiler_end = &profiler_end;
    cb_vtbl.cache_read_size = &cache_read_size;
    cb_vtbl.cache_read = &cache_read;
    cb_vtbl.cache_write = &cache_write;
    cb_vtbl.screen_shot = &screen_shot;
    cb_vtbl.capture_begin = &capture_begin;
    cb_vtbl.capture_end = &capture_end;
    cb_vtbl.capture_frame = &capture_frame;
    cb_interface.vtbl = &cb_vtbl;
    return &cb_interface;
}

DOTS_EXPORT(void) BGFXCB_DeInit() {
    std::lock_guard<std::mutex> lock(mutex);
    memset(&cb_vtbl, 0, sizeof(cb_vtbl));
    logbuffer.clear();
    calllog.clear();
}

DOTS_EXPORT(int) BGFXCB_Lock(char **text, BGFXCallbackEntry **log) {
    mutex.lock();
    if (!calllog.empty()) {
        *log = calllog.data();
        *text = logbuffer.data();
    } else {
        *log = 0;
        *text = 0;
    }
    return (int)calllog.size();
}

DOTS_EXPORT(void) BGFXCB_UnlockAndClear() {
    calllog.clear();
    logbuffer.clear();
    mutex.unlock();
}

