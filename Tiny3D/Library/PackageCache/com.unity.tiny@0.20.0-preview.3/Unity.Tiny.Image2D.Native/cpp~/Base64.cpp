#include "Base64.h"

static const char b64[64] = {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
                             'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
                             'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
                             'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/'};
static const uint8_t invb64[256] = {
    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0xff, 0xff, 0xff, 0x3e, 0xff, 0xff, 0xff, 0x3f, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 0x3c,
    0x3d, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9,  0xa,
    0xb,  0xc,  0xd,  0xe,  0xf,  0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a,
    0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30, 0x31, 0x32, 0x33, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
    0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff};

static inline void
EncodeBytesBase64(std::vector<char>& dest, uint8_t a, uint8_t b, uint8_t c)
{
    dest.push_back(b64[(a & 0xfc) >> 2]);
    dest.push_back(b64[((a & 0x3) << 4) | ((b & 0xf0) >> 4)]);
    dest.push_back(b64[((b & 0xf) << 2) | ((c & 0xc0) >> 6)]);
    dest.push_back(b64[(c & 0x3f)]);
}

static inline void
EncodeBytesBase64(std::vector<char>& dest, uint8_t a, uint8_t b, bool pad)
{
    dest.push_back(b64[(a & 0xfc) >> 2]);
    dest.push_back(b64[((a & 0x3) << 4) | ((b & 0xf0) >> 4)]);
    dest.push_back(b64[((b & 0xf) << 2)]);
    if (pad)
        dest.push_back('=');
}

static inline void
EncodeBytesBase64(std::vector<char>& dest, uint8_t a, bool pad)
{
    dest.push_back(b64[(a & 0xfc) >> 2]);
    dest.push_back(b64[((a & 0x3) << 4)]);
    if (pad) {
        dest.push_back('=');
        dest.push_back('=');
    }
}

static inline bool
DecodeCharsBase64(std::vector<uint8_t>& dest, char a, char b, char c, char d)
{
    uint8_t ia = invb64[a];
    uint8_t ib = invb64[b];
    uint8_t ic = invb64[c];
    uint8_t id = invb64[d];
    if ((ia | ib | ic | id) < 0)
        return false;
    dest.push_back((ia << 2) | (ib >> 4));
    dest.push_back((ib << 4) | (ic >> 2));
    dest.push_back((ic << 6) | id);
    return true;
}

static inline bool
DecodeCharsBase64(std::vector<uint8_t>& dest, char a, char b, char c)
{
    uint8_t ia = invb64[a];
    uint8_t ib = invb64[b];
    uint8_t ic = invb64[c];
    if ((ia | ib | ic) < 0)
        return false;
    dest.push_back((ia << 2) | (ib >> 4));
    dest.push_back((ib << 4) | (ic >> 2));
    return true;
}

static inline bool
DecodeCharsBase64(std::vector<uint8_t>& dest, char a, char b)
{
    uint8_t ia = invb64[a];
    uint8_t ib = invb64[b];
    if ((ia | ib) < 0)
        return false;
    dest.push_back((ia << 2) | (ib >> 4));
    return true;
}

static inline bool
DecodeCharsPaddedBase64(std::vector<uint8_t>& dest, char a, char b, char c, char d)
{
    if (d == '=') {
        if (c == '=')
            return DecodeCharsBase64(dest, a, b);
        else
            return DecodeCharsBase64(dest, a, b, c);
    }
    return DecodeCharsBase64(dest, a, b, c, d);
}

static void
Append(std::vector<char>& dest, const char* s)
{
    size_t si = strlen(s);
    size_t s0 = dest.size();
    dest.resize(s0 + si);
    memcpy(dest.data() + s0, s, si);
}

void
EncodeBase64(std::vector<char>& dest, const uint8_t* src, size_t srclen, bool pad)
{
    dest.reserve(dest.size() + (srclen * 4) / 3 + 4);
    while (srclen >= 3) {
        EncodeBytesBase64(dest, src[0], src[1], src[2]);
        src += 3;
        srclen -= 3;
    }
    if (srclen == 2)
        EncodeBytesBase64(dest, src[0], src[1], pad);
    else if (srclen == 1)
        EncodeBytesBase64(dest, src[0], pad);
}

void
EncodeDataURIBase64(std::vector<char>& dest, const char* mediaType, const uint8_t* src, size_t srclen)
{
    Append(dest, "data:");
    Append(dest, mediaType);
    Append(dest, ";base64,");
    EncodeBase64(dest, src, srclen, true);
    dest.push_back(0);
}

bool
DecodeBase64(std::vector<uint8_t>& dest, const char* src, size_t srclen)
{
    dest.reserve(dest.size() + (srclen * 3) / 4 + 4);
    while (srclen >= 8) {
        if (!DecodeCharsBase64(dest, src[0], src[1], src[2], src[3]))
            return false;
        src += 4;
        srclen -= 4;
    }
    if (srclen >= 4) {
        if (!DecodeCharsPaddedBase64(dest, src[0], src[1], src[2], src[3]))
            return false;
        src += 4;
        srclen -= 4;
    }
    if (srclen == 3)
        return DecodeCharsBase64(dest, src[0], src[1], src[2]);
    else if (srclen == 2)
        return DecodeCharsBase64(dest, src[0], src[1]);
    else if (srclen == 1)
        return *src == 0; // allow one trailing zero
    else
        return true;
}

bool
DecodeDataURIBase64(std::vector<uint8_t>& dest, std::string &mediaType, const char* src, size_t srclen)
{
    if (srclen<5 || memcmp(src,"data:",5))
        return false;
    src+=5; srclen-=5;
    while(srclen>0 && *src!=';' && *src) {
        mediaType.push_back(*src);
        srclen--; src++;
    }
    if (srclen<8 || memcmp(src,";base64,",8))
        return false;
    src+=8; srclen-=8;
    return DecodeBase64(dest,src,srclen);
}

