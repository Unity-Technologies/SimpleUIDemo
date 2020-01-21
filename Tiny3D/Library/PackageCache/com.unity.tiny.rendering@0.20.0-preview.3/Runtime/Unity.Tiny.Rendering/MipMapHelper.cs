using Unity.Burst;
using Unity.Mathematics;
using Unity.Tiny.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Tiny.Rendering.CPU.Tests")]

namespace Unity.Tiny.Rendering
{
    [BurstCompile]
    static public class MipMapHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Average32(uint a, uint b)
        {
            uint rsum = (a & 0xff) + (b & 0xff);
            uint gsum = ((a>>8) & 0xff) + ((b>>8) & 0xff);
            uint bsum = ((a>>16) & 0xff) + ((b>>16) & 0xff);
            uint asum = ((a>>24) & 0xff) + ((b>>24) & 0xff);
            rsum >>= 1;
            gsum >>= 1;
            bsum >>= 1;
            asum >>= 1;
            return rsum | (gsum<<8) | (bsum<<16) | (asum<<24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Average32(uint a, uint b, uint c, uint d)
        {
            uint rsum = (a & 0xff) + (b & 0xff) + (c & 0xff) + (d & 0xff);
            uint gsum = ((a>>8) & 0xff) + ((b>>8) & 0xff) + ((c>>8) & 0xff) + ((d>>8) & 0xff);
            uint bsum = ((a>>16) & 0xff) + ((b>>16) & 0xff) + ((c>>16) & 0xff) + ((d>>16) & 0xff);
            uint asum = ((a>>24) & 0xff) + ((b>>24) & 0xff) + ((c>>24) & 0xff) + ((d>>24) & 0xff);
            rsum >>= 2;
            gsum >>= 2;
            bsum >>= 2;
            asum >>= 2;
            return rsum | (gsum<<8) | (bsum<<16) | (asum<<24);
        }

        [BurstCompile]
        internal unsafe static void DownSampleBox32(uint* src, int w, int h, uint* dest, int wdest, int hdest)
        {
            Assert.IsTrue(w > 1 || h > 1);
            if ((hdest == 1 && h == 1) || (wdest == 1 && w == 1)) { // x or y only
                int n = hdest > wdest ? hdest : wdest;
                for (int i = 0; i < n; i++) {
                    *dest = Average32(src[0], src[1]);
                    dest++;
                    src += 2;
                }
                return;
            }
            // regular 
            for (int y = 0; y < hdest; y++) {
                uint* srcl = src + y * 2 * w;
                for (int x = 0; x < wdest; x++) {
                    *dest = Average32(srcl[0], srcl[1], srcl[w], srcl[w + 1]);
                    dest++;
                    srcl += 2;
                }
            }
        }

        [BurstCompile]
        internal static unsafe void DownSampleBox(float4* src, int w, int h, float4* dest, int wdest, int hdest)
        {
            Assert.IsTrue(w > 1 || h > 1);
            if ((hdest == 1 && h == 1) || (wdest == 1 && w == 1)) { // x or y only
                int n = hdest > wdest ? hdest : wdest;
                for (int i = 0; i < n; i++) {
                    *dest = (src[0] + src[1]) * .5f;
                    dest++;
                    src += 2;
                }
                return;
            }
            // regular 
            for (int y = 0; y < hdest; y++) {
                float4* srcl = src + y * 2 * w;
                for (int x = 0; x < wdest; x++) {
                    *dest = (srcl[0] + srcl[1] + srcl[w] + srcl[w + 1]) * .25f;
                    dest++;
                    srcl += 2;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LinearToSRGB(float x)
        {
            if (x <= 0.0031308f) return x * 12.92f;
            return math.pow(x, 1.0f / 2.4f) * 1.055f - 0.055f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 LinearToSRGB(float3 rgb)
        {
            return new float3 (
                LinearToSRGB(rgb.x),
                LinearToSRGB(rgb.y),
                LinearToSRGB(rgb.z) );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SRGBToLinear(float x)
        {
            if (x < 0.04045f) return x * (1.0f / 12.92f);
            return math.pow((x + 0.055f) / 1.055f, 2.4f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 SRGBToLinear(float3 rgb)
        {
            return new float3(
                SRGBToLinear(rgb.x),
                SRGBToLinear(rgb.y),
                SRGBToLinear(rgb.z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte LinearToSRGB_Table(float x)
        {
            Assert.IsTrue(x>=0.0f && x<=1.0f);
            return LinearToSRGBTable[(int)(x *(LinearToSRGBTableSize-1))];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float SRGBToLinear_Table(byte x)
        {
            return SRGBToLinearTable[x];
        }

        internal static void InitLinearToSRGBTable()
        {
            if (LinearToSRGBTable.Length != 0)
                return;
            LinearToSRGBTable = new NativeArray<byte>(LinearToSRGBTableSize, Allocator.Persistent);
            for (int i = 0; i < LinearToSRGBTableSize ; i++) {
                float f = (float)i / (float)(LinearToSRGBTableSize-1);
                float y = LinearToSRGB(f) + .5f / 255.0f;
                if (y < 0.0f) y = 0.0f;
                if (y > 1.0f) y = 1.0f;
                LinearToSRGBTable[i] = (byte)(y * 255.0f);
            }
        }
     
        internal static void InitSRGBToLinearTable()
        {
            if (SRGBToLinearTable.Length != 0)
                return;
            SRGBToLinearTable = new NativeArray<float>(256, Allocator.Persistent);
            for (int i = 0; i < 256; i++) {
                float y = SRGBToLinear((float)i / (float)255);
                if (y > 1.0f) y = 1.0f;
                SRGBToLinearTable[i] = y;
            }
        }

        internal static void Shutdown()
        {
            if (LinearToSRGBTable.IsCreated)
                LinearToSRGBTable.Dispose();
            if (SRGBToLinearTable.IsCreated)
                SRGBToLinearTable.Dispose();
        }

        private const int LinearToSRGBTableSize = 8192;
        private static NativeArray<byte> LinearToSRGBTable;
        private static NativeArray<float> SRGBToLinearTable;

        [BurstCompile]
        private static unsafe void UnpackSRGBToLinear_Table(float4* dest, uint* src, int count, float *table)
        {
            float* destf = (float*)dest;
            for (int i = 0, i4 = 0; i < count; i++) {
                uint si = src[i];
                destf[i4++] = table[si & 0xff];
                destf[i4++] = table[(si >> 8) & 0xff];
                destf[i4++] = table[(si >> 16) & 0xff];
                destf[i4++] = (float)(si >> 24);
            }
        }

        [BurstCompile]
        private static unsafe void UnpackSRGBToLinear(float4* dest, uint* src, int count)
        {
            float4 smallscale = new float4(1.0f / 12.92f, 1.0f / 12.92f, 1.0f / 12.92f, 1.0f) * 1.0f / 255.0f;
            float4 bigoffset = new float4(0.055f / 1.055f, 0.055f / 1.055f, 0.055f / 1.055f, 0);
            float4 bigscale = new float4(1.0f / 1.055f, 1.0f / 1.055f, 1.0f / 1.055f, 1.0f) * 1.0f / 255.0f;
            float4 exponentbig = new float4(2.4f, 2.4f, 2.4f, 1.0f);
            float4 threshold = new float4(0.04045f, 0.04045f, 0.04045f, 1.0f) * 255.0f;
            for (int i = 0; i < count; i++) {
                float4 c = new float4( // srgb [0..255]
                    (src[i] & 0xff),
                    ((src[i] >> 8) & 0xff),
                    ((src[i] >> 16) & 0xff),
                    (src[i] >> 24));
                float4 csmall = c * smallscale;
                bool4 selector = c < threshold;
                float4 cbig = math.pow(math.mad(c, bigscale, bigoffset), exponentbig);
                c = math.select(cbig, csmall, selector); // linear [0..1]
                dest[i] = c;
            }
        }

        [BurstCompile]
        private static unsafe void PackLinearToSRGB(uint* dest, float4* src, int count)
        {
            float4 threshold = new float4(0.0031308f, 0.0031308f, 0.0031308f, 1.0f);
            float4 smallscale = new float4(12.92f, 12.92f, 12.92f, 1.0f) * 255.0f;
            float4 exponentbig = new float4(1.0f / 2.4f, 1.0f / 2.4f, 1.0f / 2.4f, 1.0f);
            float4 bigoffset = new float4(-0.055f, -0.055f, -0.055f, 0) * 255.0f;
            float4 bigscale = new float4(1.055f, 1.055f, 1.055f, 1.0f) * 255.0f;
            for (int i = 0; i < count; i++) {
                float4 c = math.saturate(src[i]); // linear [0..1]
                bool4 selector = c < threshold;
                float4 csmall = c * smallscale;
                float4 cbig = math.mad(math.pow(c, exponentbig), bigscale, bigoffset);
                c = math.select(cbig, csmall, selector); // srgb [0..255]
                dest[i] = ((uint)c.x) | (((uint)c.y) << 8) | (((uint)c.z) << 16) | (((uint)c.w) << 24);
            }
        }

        [BurstCompile]
        private static unsafe void PackLinearToSRGB_Table(uint* dest, float4* src, int count, byte *table)
        {
            float* srcf = (float*)src;
            for (int i = 0, i4 = 0; i < count; i++) {
                Assert.IsTrue(srcf[i4]>=0.0f && srcf[i4]<=1.0f);
                uint tr = (uint)table[(int)(srcf[i4++] * (LinearToSRGBTableSize-1))];
                Assert.IsTrue(srcf[i4]>=0.0f && srcf[i4]<=1.0f);
                tr |= (uint)(table[(int)(srcf[i4++] * (LinearToSRGBTableSize-1))] << 8);
                Assert.IsTrue(srcf[i4]>=0.0f && srcf[i4]<=1.0f);
                tr |= (uint)(table[(int)(srcf[i4++] * (LinearToSRGBTableSize-1))] << 16);
                Assert.IsTrue(srcf[i4]>=0.0f && srcf[i4]<=255.0f);
                tr |= (uint)(srcf[i4++]) << 24;
                dest[i] = tr;
            }
        }

        internal static unsafe void FillMipMapChain32(int w, int h, uint* dest, bool srgb)
        {
            uint* src = dest;
            dest += w * h;
            if (!srgb) {
                for (; ; ) {
                    if (w == 1 && h == 1) break;
                    int wdest = w == 1 ? 1 : w >> 1;
                    int hdest = h == 1 ? 1 : h >> 1;
                    DownSampleBox32(src, w, h, dest, wdest, hdest);
                    src += w * h;
                    dest += wdest * hdest;
                    w = wdest;
                    h = hdest;
                }
            } else {
                InitSRGBToLinearTable();
                InitLinearToSRGBTable();
                var buf = new NativeArray<float4>(w * h * 2, Allocator.TempJob);
                float4* srcf4 = (float4*)buf.GetUnsafePtr();
                float4* destf4 = srcf4 + w * h;
                float* tableSRGBToLinearTable = (float*)SRGBToLinearTable.GetUnsafePtr<float>();
                UnpackSRGBToLinear_Table(srcf4, src, w * h, tableSRGBToLinearTable);
                byte* tableLinearToSRGB = (byte*)LinearToSRGBTable.GetUnsafePtr<byte>();
                for (; ; )
                {
                    if (w == 1 && h == 1) break;
                    int wdest = w == 1 ? 1 : w >> 1;
                    int hdest = h == 1 ? 1 : h >> 1;
                    DownSampleBox(srcf4, w, h, destf4, wdest, hdest);
                    PackLinearToSRGB_Table(dest, destf4, wdest * hdest, tableLinearToSRGB);
                    src += w * h;
                    dest += wdest * hdest;
                    w = wdest;
                    h = hdest;
                    var t = srcf4; srcf4 = destf4; destf4 = t; // swap buffers
                }
                buf.Dispose();
            }
        }
    }
}