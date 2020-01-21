#include "Image2DHelpers.h"

using namespace ut;

static bool
IsValidPremultiplied(const uint32_t* mem, int w, int h)
{
    for (int i = 0; i < w * h; i++) {
        uint32_t v = mem[i];
        uint32_t a = v >> 24;
        if (((v >> 0) & 0xff) > a || ((v >> 8) & 0xff) > a || ((v >> 16) & 0xff) > a)
            return false;
    }
    return true;
}

void
Image2DHelpers::ExpandAlphaCopy(uint32_t* dest, const uint8_t* src, int w, int h)
{
    for (int i = 0; i < w * h; i++) {
        uint32_t v = src[i];
        v = v | (v << 8);
        v = v | (v << 16);
        dest[i] = v;
    }
}

void
Image2DHelpers::ExpandAlphaWhiteCopy(uint32_t* dest, const uint8_t* src, int w, int h)
{
    for (int i = 0; i < w * h; i++) {
        uint32_t v = src[i];
        dest[i] = (v << 24) | 0xffffff;
    }
}

uint32_t
Image2DHelpers::PremultiplyAlpha(uint32_t c)
{
    int32_t a = c >> 24;
    if (a == 0)
        return 0;
    if (a == 0xff)
        return c;
    uint32_t r = ((c & 0xff) * a) / 255;
    uint32_t g = (((c >> 8) & 0xff) * a) / 255;
    uint32_t b = (((c >> 16) & 0xff) * a) / 255;
    return r | (g << 8) | (b << 16) | (a << 24);
}

void
Image2DHelpers::PremultiplyAlpha(uint32_t* mem, int w, int h)
{
     for (int i = 0; i < w * h; i++)
         mem[i] = PremultiplyAlpha(mem[i]);
}

uint32_t
Image2DHelpers::UnmultiplyAlpha(uint32_t c)
{
    int32_t a = c >> 24;
    if (a == 0)
        return 0;
    if (a == 0xff)
        return c;
    uint32_t r = ((c & 0xff)*255) / a;
    uint32_t g = (((c >> 8) & 0xff)*255) / a;
    uint32_t b = (((c >> 16) & 0xff)*255) / a;
    //Assert(r<=0xff && g<=0xff && b<=0xff);
    return r | (g << 8) | (b << 16) | (a << 24);
}

void 
Image2DHelpers::UnmultiplyAlpha(uint32_t* mem, int w, int h)
{
     for (int i = 0; i < w * h; i++)
         mem[i] = UnmultiplyAlpha(mem[i]);
}

bool
Image2DHelpers::UnmultiplyAlphaAndCheckCopy(uint32_t* dest, const uint32_t* src, int w, int h)
{
    bool r = false;
    for (int i = 0; i < w * h; i++) {
        uint32_t v = src[i];
        if ((v & 0xff000000) != 0xff000000) {
            v = UnmultiplyAlpha(v);
            r = true;
        }
        dest[i] = v;
    }
    return r;
}

bool
Image2DHelpers::PremultiplyAlphaAndCheckCopy(uint32_t* dest, const uint32_t* src, int w, int h)
{
    bool r = false;
    for (int i = 0; i < w * h; i++) {
        uint32_t v = src[i];
        if ((v & 0xff000000) != 0xff000000) {
            v = PremultiplyAlpha(v);
            r = true;
        }
        dest[i] = v;
    }
    return r;
}
