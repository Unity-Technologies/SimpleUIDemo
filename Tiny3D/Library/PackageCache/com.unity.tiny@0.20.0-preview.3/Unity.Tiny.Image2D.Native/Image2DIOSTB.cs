using System;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Tiny;
using Unity.Tiny.GenericAssetLoading;
using Unity.Tiny.Assertions;
using Unity.Collections;
using System.Runtime.InteropServices;

/**
 * @module
 * @name Unity.Tiny
 */
namespace Unity.Tiny.STB
{
    public struct Image2DSTB : ISystemStateComponentData
    {
        public int imageHandle;
    }

    public struct Image2DSTBLoading : ISystemStateComponentData
    {
        public long internalId;
    }

    public static class ImageIOSTBNativeCalls
    {
        [DllImport("lib_unity_tiny_image2d_native", EntryPoint = "startload_stb", CharSet = CharSet.Ansi)]
        public static extern long StartLoad([MarshalAs(UnmanagedType.LPStr)]string imageFile, [MarshalAs(UnmanagedType.LPStr)]string maskFile); // returns loadId

        [DllImport("lib_unity_tiny_image2d_native", EntryPoint = "freeimage_stb")]
        public static extern void FreeNative(int imageHandle);

        [DllImport("lib_unity_tiny_image2d_native", EntryPoint = "abortload_stb")]
        public static extern void AbortLoad(long loadId);

        [DllImport("lib_unity_tiny_image2d_native", EntryPoint = "checkload_stb")]
        public static extern int CheckLoading(long loadId, ref int imageHandle); // 0=still working, 1=ok, 2=fail, imageHandle set when ok

        [DllImport("lib_unity_tiny_image2d_native", EntryPoint = "finishload_stb")]
        public static extern void FinishLoading();

        [DllImport("lib_unity_tiny_image2d_native", EntryPoint = "initmask_stb")]
        public static extern unsafe void InitImage2DMask(int imageHandle, byte* buffer);

        [DllImport("lib_unity_tiny_image2d_native", EntryPoint = "getimage_stb")] 
        public static extern unsafe byte *GetImageFromHandle(int imageHandle, ref int sizeX, ref int sizeY);

        [DllImport("lib_unity_tiny_image2d_native", EntryPoint = "freeimagemem_stb")] 
        public static extern void FreeBackingMemory(int imageHandle);
    }

    class ImageIOSTBSystemLoadFromFile : IGenericAssetLoader<Image2D, Image2DSTB, Image2DLoadFromFile, Image2DSTBLoading>
    {
        public void StartLoad(EntityManager man, Entity e, ref Image2D image, ref Image2DSTB imgSTB, ref Image2DLoadFromFile fspec, ref Image2DSTBLoading loading)
        {
            // if there are async still loading, but set to new file stop job
            if (loading.internalId!=0) {
                ImageIOSTBNativeCalls.AbortLoad (loading.internalId);
            }

            image.status =  ImageStatus.Loading;
            image.imagePixelHeight = 0;
            image.imagePixelWidth = 0;

            string fnImage = "", fnMask = "";

            if (man.HasComponent<Image2DLoadFromFileGuids>(e))
            {
                var guids = man.GetComponentData<Image2DLoadFromFileGuids>(e);
                // TODO -- call an asset service to actually get some kind of stream from a guid
                if (!guids.imageAsset.Equals(Guid.Empty))
                    fnImage = "Data/" + guids.imageAsset.ToString("N");
                if (!guids.maskAsset.Equals(Guid.Empty))
                    fnMask = "Data/" + guids.maskAsset.ToString("N");
            }
            else
            {
                fnImage = man.GetBufferAsString<Image2DLoadFromFileImageFile>(e);
                if (man.HasComponent<Image2DLoadFromFileImageFile>(e) && fnImage.Length <= 0)
                    Debug.LogFormat("The file one entity {1} contains an empty Image2DLoadFromFileImageFile string.", e);
                fnMask = man.GetBufferAsString<Image2DLoadFromFileMaskFile>(e);
                if (man.HasComponent<Image2DLoadFromFileMaskFile>(e) && fnMask.Length <= 0)
                    Debug.LogFormat("The file one entity {1} contains an empty Image2DLoadFromFileMaskFile string.", e);
             }

            loading.internalId = ImageIOSTBNativeCalls.StartLoad(fnImage, fnMask);
        }

        public LoadResult CheckLoading(IntPtr cppwrapper, EntityManager man, Entity e, ref Image2D image, ref Image2DSTB imgSTB, ref Image2DLoadFromFile unused, ref Image2DSTBLoading loading)
        {
            int newHandle = 0;
            int r = ImageIOSTBNativeCalls.CheckLoading(loading.internalId, ref newHandle);
            if (r==0)
                return LoadResult.stillWorking;
            FreeNative(man, e, ref imgSTB);
            imgSTB.imageHandle = newHandle;

            var fnLog = string.Empty;
            fnLog += man.GetBufferAsString<Image2DLoadFromFileImageFile>(e);
            if (man.HasComponent<Image2DLoadFromFileMaskFile>(e)) {
                fnLog += " alpha=";
                fnLog += man.GetBufferAsString<Image2DLoadFromFileMaskFile>(e);
            }

            if (r == 2) {
                image.status = ImageStatus.LoadError;
                image.imagePixelHeight = 0;
                image.imagePixelWidth = 0;
                Debug.LogFormat("Failed to load {0}",fnLog);
                return LoadResult.failed;
            }
            Assert.IsTrue(newHandle > 0);

            int w = 0, h = 0;
            unsafe
            {
                ImageIOSTBNativeCalls.GetImageFromHandle(imgSTB.imageHandle, ref w, ref h);
                Assert.IsTrue(w > 0 && h > 0);
                image.imagePixelWidth = w;
                image.imagePixelHeight = h;
            }
#if IO_ENABLE_TRACE
            Debug.LogFormat("Loaded image: {0} Handle {4} Size: {1},{2}",fnLog, w, h, imgSTB.imageHandle);
#endif
            image.status = ImageStatus.Loaded;
            return LoadResult.success;
        }

        public void FreeNative(EntityManager man, Entity e, ref Image2DSTB imgSTB)
        {
            ImageIOSTBNativeCalls.FreeNative(imgSTB.imageHandle);
        }

        public void FinishLoading(EntityManager man, Entity e, ref Image2D img, ref Image2DSTB imgSTB, ref Image2DSTBLoading loading)
        {
            ImageIOSTBNativeCalls.FinishLoading();
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class Image2DIOSTBSystem : GenericAssetLoader< Image2D, Image2DSTB, Image2DLoadFromFile, Image2DSTBLoading >
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            c = new ImageIOSTBSystemLoadFromFile();
        }

        protected override void OnUpdate()
        {
            // loading
            base.OnUpdate();
        }
    }

}
