#pragma once

#include <stdint.h>

namespace ut {

class Image2DHelpers {
public:
    //static bool ConvertMemoryImageToRGBA8Pre(ManagerWorld& world, Entity e, Image2DLoadFromMemory& fspec,
                                             //std::vector<uint8_t>& temp, bool& hasalpha);
    //static bool ConvertMemoryImageToRGBA8(ManagerWorld& world, Entity e, Image2DLoadFromMemory& fspec,
                                          //std::vector<uint8_t>& temp, bool& hasalpha);
    //static int MemoryFormatToBytesPerPixel(Image2DMemoryFormat fmt);
    static const int sMaxImageSize = 2048;

    static bool PremultiplyAlphaAndCheckCopy(uint32_t* dest, const uint32_t* src, int w, int h);
    static void ExpandAlphaCopy(uint32_t* dest, const uint8_t* src, int w, int h);
    static void ExpandAlphaWhiteCopy(uint32_t* dest, const uint8_t* src, int w, int h);
    static void UnmultiplyAlpha(uint32_t* mem, int w, int h);
    static bool UnmultiplyAlphaAndCheckCopy(uint32_t* dest, const uint32_t* src, int w, int h);
    static void PremultiplyAlpha(uint32_t* mem, int w, int h);

    static uint32_t PremultiplyAlpha(uint32_t c);
    static uint32_t UnmultiplyAlpha(uint32_t c);

    //static NativeString FormatSourceName(Image2DLoadFromFile& fspec);
    //static bool CheckMemoryImage(ManagerWorld& world, Entity e, Image2DLoadFromMemory& fspec);
};

} // namespace ut
