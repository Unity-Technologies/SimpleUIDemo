using System;
using Unity.Entities;
using Unity.Tiny;
using Unity.Tiny.GenericAssetLoading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.Collections;

[assembly: InternalsVisibleTo("Unity.Tiny.Audio.Tests")]
namespace Unity.Tiny.Audio
{
    struct AudioNativeClip : ISystemStateComponentData
    {
        public uint clipID;
    }

    struct AudioNativeLoading : ISystemStateComponentData
    {
    }

    struct AudioNativeSource : IComponentData
    {
        public uint sourceID;
    }

    static class AudioNativeCalls
    {
        private const string DLL = "lib_unity_tiny_audio_native";

        // Mixer
        [DllImport(DLL, EntryPoint = "initAudio")]
        public static extern void InitAudio();

        [DllImport(DLL, EntryPoint = "destroyAudio")]
        public static extern void DestroyAudio();

        // Clip
        [DllImport(DLL, EntryPoint = "startLoad", CharSet = CharSet.Ansi)]
        public static extern uint StartLoad([MarshalAs(UnmanagedType.LPStr)]string imageFile);    // returns clipID

        [DllImport(DLL, EntryPoint = "freeAudio")]
        public static extern void FreeAudio(uint clipID);

        [DllImport(DLL, EntryPoint = "abortLoad")]
        public static extern void AbortLoad(uint clipID);

        [DllImport(DLL, EntryPoint = "checkLoading")]
        public static extern int CheckLoading(uint clipID );    // 0=still working, 1=ok, 2=fail

        [DllImport(DLL, EntryPoint = "finishedLoading")]
        public static extern void FinishedLoading(uint clipID);

        // Source
        [DllImport(DLL, EntryPoint = "playSource")]
        public static extern uint Play(uint clipID, float volume, bool loop);    // returns sourceID (>0) or 0 or failure.

        [DllImport(DLL, EntryPoint = "isPlaying")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsPlaying(uint sourceID);

        [DllImport(DLL, EntryPoint = "stopSource")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool Stop(uint sourceID);    // returns success (or failure)

        [DllImport(DLL, EntryPoint = "pauseAudio")]
        public static extern void PauseAudio(bool doPause);    // returns success (or failure)

        [DllImport(DLL, EntryPoint = "numSourcesAllocated")]
        public static extern int NumSourcesAllocated();          // Testing: number of SoundSources allocated.

        [DllImport(DLL, EntryPoint = "numClipsAllocated")]
        public static extern int NumClipsAllocated();            // Testing: number of SoundClips allocated.

        [DllImport(DLL, EntryPoint = "sourcePoolID")]
        public static extern int SourcePoolID();                 // Testing: the next ID that will be assigned to a source. (Useful to tell if a source changed.)
    }

    class AudioNativeSystemLoadFromFile : IGenericAssetLoader< AudioClip, AudioNativeClip, AudioClipLoadFromFile, AudioNativeLoading >
    {
        public void StartLoad(
            EntityManager entityManager,
            Entity e,
            ref AudioClip audioClip,
            ref AudioNativeClip audioNativeClip,
            ref AudioClipLoadFromFile loader,
            ref AudioNativeLoading nativeLoading)
        {
            if (audioNativeClip.clipID != 0)
                AudioNativeCalls.AbortLoad(audioNativeClip.clipID);

            if (!entityManager.HasComponent<AudioClipLoadFromFileAudioFile>(e))
            {
                audioNativeClip.clipID = 0;
                audioClip.status = AudioClipStatus.LoadError;
                return;
            }

            string path = entityManager.GetBufferAsString<AudioClipLoadFromFileAudioFile>(e);

            audioNativeClip.clipID = AudioNativeCalls.StartLoad(path);
            audioClip.status = audioNativeClip.clipID > 0 ? AudioClipStatus.Loading : AudioClipStatus.LoadError;
        }

        public LoadResult CheckLoading(IntPtr wrapper,
            EntityManager man,
            Entity e,
            ref AudioClip audioClip, ref AudioNativeClip audioNativeClip, ref AudioClipLoadFromFile param, ref AudioNativeLoading nativeLoading)
        {
            LoadResult result = (LoadResult) AudioNativeCalls.CheckLoading(audioNativeClip.clipID);

            if (result == LoadResult.success)
                audioClip.status = AudioClipStatus.Loaded;
            else if (result == LoadResult.failed)
                audioClip.status = AudioClipStatus.LoadError;

            return result;
        }

        public void FreeNative(EntityManager man, Entity e, ref AudioNativeClip audioNativeClip)
        {
           	AudioNativeCalls.FreeAudio(audioNativeClip.clipID);
        }

        public void FinishLoading(EntityManager man, Entity e, ref AudioClip audioClip, ref AudioNativeClip audioNativeClip, ref AudioNativeLoading nativeLoading)
        {
            AudioNativeCalls.FinishedLoading(audioNativeClip.clipID);
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(AudioNativeSystem))]
    class AudioIONativeSystem : GenericAssetLoader< AudioClip, AudioNativeClip, AudioClipLoadFromFile, AudioNativeLoading >
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            c = new AudioNativeSystemLoadFromFile();
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    class AudioNativeSystem : AudioSystem
    {
        protected override void InitAudioSystem()
        {
            AudioNativeCalls.InitAudio();

            TinyEnvironment env = World.TinyEnvironment();
            AudioConfig ac = env.GetConfigData<AudioConfig>();
            ac.initialized = true;
            ac.unlocked = true;
            env.SetConfigData(ac);
        }

        protected override void DestroyAudioSystem()
        {
            AudioNativeCalls.DestroyAudio();
        }

        protected override bool PlaySource(Entity e)
        {
            var mgr = EntityManager;

            if (mgr.HasComponent<AudioSource>(e))
            {
                AudioSource audioSource = mgr.GetComponentData<AudioSource>(e);

                Entity clipEntity = audioSource.clip;
                if (mgr.HasComponent<AudioNativeClip>(clipEntity))
                {
                    AudioNativeClip clip = mgr.GetComponentData<AudioNativeClip>(clipEntity);
                    if (clip.clipID > 0)
                    {
                        // If there is an existing source, it should re-start.
                        // Do this with a Stop() and let it play below.
                        if (mgr.HasComponent<AudioNativeSource>(e))
                        {
                            AudioNativeSource ans = mgr.GetComponentData<AudioNativeSource>(e);
                            AudioNativeCalls.Stop(ans.sourceID);
                        }

                        uint sourceID = AudioNativeCalls.Play(clip.clipID, audioSource.volume, audioSource.loop);

                        AudioNativeSource audioNativeSource = new AudioNativeSource()
                        {
                            sourceID = sourceID
                        };
                        if (mgr.HasComponent<AudioNativeSource>(e))
                        {
                            mgr.SetComponentData(e, audioNativeSource);
                        }
                        else
                        {
                            PostUpdateCommands.AddComponent(e, audioNativeSource);
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void StopSource(Entity e)
        {
            if (EntityManager.HasComponent<AudioNativeSource>(e))
            {
                AudioNativeSource audioNativeSource = EntityManager.GetComponentData<AudioNativeSource>(e);
                if (audioNativeSource.sourceID > 0)
                {
                    AudioNativeCalls.Stop(audioNativeSource.sourceID);
                }
            }
        }

        protected override bool IsPlaying(Entity e)
        {
            if (EntityManager.HasComponent<AudioNativeSource>(e))
            {
                AudioNativeSource audioNativeSource = EntityManager.GetComponentData<AudioNativeSource>(e);
                if (audioNativeSource.sourceID > 0)
                {
                    return AudioNativeCalls.IsPlaying(audioNativeSource.sourceID);
                }
            }

            return false;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            TinyEnvironment env = World.TinyEnvironment();
            AudioConfig ac = env.GetConfigData<AudioConfig>();
            AudioNativeCalls.PauseAudio(ac.paused);

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities
                .WithNone<AudioClipLoadFromFileAudioFile>()
                .ForEach((Entity e, ref AudioNativeClip tag) =>
                {
                    AudioNativeCalls.FreeAudio(tag.clipID);
                    ecb.RemoveComponent<AudioNativeClip>(e);
                });
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

    }
}
