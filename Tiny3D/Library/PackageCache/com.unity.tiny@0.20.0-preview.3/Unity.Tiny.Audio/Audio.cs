using Unity.Collections;
using Unity.Entities;
using Unity.Tiny;

namespace Unity.Tiny.Audio
{
    /// <summary>
    ///  An enum listing the possible states an audio clip can be in during loading.
    ///  Used by the AudioClip component to track loading status.
    /// </summary>
    public enum AudioClipStatus
    {
        /// <summary>The clip is not loaded in memory.</summary>
        Unloaded,

        /// <summary>The clip has begun loading but is not ready to begin playback.</summary>
        Loading,

        /// <summary>The clip is fully decoded, loaded in memory, and ready for playback.</summary>
        Loaded,

        /// <summary>The clip cannot be loaded in memory.</summary>
        LoadError
    }

    /// <summary>
    ///  An AudioClip represents a single audio resource that can play back on
    ///  one or more AudioSource components.
    /// </summary>
    /// <remarks>
    ///  If only one AudioSource component references the AudioClip, you can attach the
    ///  AudioClip component to the same entity as that AudioSource component.
    ///
    ///  If multiple AudioSource components reference the audio clip, it's recommended
    ///  that you add the AudioClip component to a separate entity.
    ///
    ///  To perform the load of the audio resource, the AudioClip must have:
    ///  - An AudioClipLoadFromFileAudioFile to specify the location of the resource.
    ///  - An AudioClipLoadFromFile which initiates the load.
    ///
    ///  Note that this is a System, so that the actual loading will not be synchronous.
    ///
    /// <example>
    /// Minimal code load a file:
    /// <code>
    ///     mgr.AddComponentData(eClip, new AudioClip());
    ///     mgr.AddBufferFromString&lt;AudioClipLoadFromFileAudioFile&gt;(eClip, "path/to/file.wav");
    ///     mgr.AddComponent(eClip, typeof(AudioClipLoadFromFile));
    /// </code>
    /// </example>
    /// </remarks>
    public struct AudioClip : IComponentData
    {
        /// <summary>
        ///  The AudioClip load status. The AudioClipStatus enum defines the possible states.
        /// </summary>
        public AudioClipStatus status;
    }

    /// <summary>
    /// Location of the audio file. <seealso cref="AudioClip"/>
    /// </summary>
    public struct AudioClipLoadFromFileAudioFile: IBufferElementData
    {
        public char s;
    }

    /// <summary>
    ///  Attach this component to an entity with an AudioClip and AudioClipLoadFromFileAudioFile
    ///  component to begin loading an audio clip.
    /// </summary>
    /// <remarks>
    ///  Loading is performed by the AudioSystem.
    ///  Once loading is complete the AudioSystem removes the
    ///  AudioClipLoadFromFile component.
    /// </remarks>
    public struct AudioClipLoadFromFile : IComponentData
    {
    }


    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public abstract class AudioSystem : ComponentSystem
    {
        protected abstract void InitAudioSystem();
        protected abstract void DestroyAudioSystem();

        protected abstract bool PlaySource(Entity e);
        protected abstract void StopSource(Entity e);
        protected abstract bool IsPlaying(Entity e);

        protected override void OnCreate()
        {
            InitAudioSystem();
        }

        protected override void OnDestroy()
        {
            DestroyAudioSystem();
        }

        protected override void OnUpdate()
        {
            var mgr = EntityManager;
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                Entities
                    .WithAll<AudioSource>()
                    .ForEach((Entity e, ref AudioSourceStop tag) =>
                    {
                        StopSource(e);
                        PostUpdateCommands.RemoveComponent<AudioSourceStop>(e);
                    });
                Entities
                   .WithAll<AudioSource>()
                   .ForEach((Entity e, ref Disabled tag) =>
                   {
                       StopSource(e);
                   });
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                Entities
                    .WithAll<AudioSource>()
                    .ForEach((Entity e, ref AudioSourceStart tag) =>
                    {
                        if(PlaySource(e))
                            PostUpdateCommands.RemoveComponent<AudioSourceStart>(e);
                    });
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                Entities
                    .ForEach((Entity e, ref AudioSource tag) =>
                    {
                        tag.isPlaying = IsPlaying(e);
                    });
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }
        }
    }

    /// <summary>
    ///  Configures the global audio state, which you can access via TinyEnvironment.GetConfigData
    ///  This component is attached to the Config entity.
    /// </summary>
    public struct AudioConfig : IComponentData
    {
        /// <summary>
        ///  True if the audio context is initialized.
        /// </summary>
        /// <remarks>
        ///  After you export and launch the project, and the AudioSystem updates
        ///  for the first time, the AudioConfig component attempts to initialize
        ///  audio. If successful, it sets this value to true.
        ///
        ///  Once audio is initialized successfully the AudioConfig component does
        ///  not re-attempt to initialize it on subsequent AudioSystem updates.
        /// </remarks>
        public bool initialized;

        /// <summary>
        ///  If true, pauses the audio context. Set this at any time to pause or
        ///  resume audio.
        /// </summary>
        public bool paused;

        /// <summary>
        ///  True if the audio context is unlocked in the browser.
        /// </summary>
        /// <remarks>
        ///  Some browsers require a user interaction, for example a touch interaction
        ///  or key input, to unlock the audio context. If the context is locked
        ///  no audio plays.
        /// </remarks>
        public bool unlocked;
    }

    /// <summary>
    ///  Attach this component to an entity with an AudioSource component to start
    ///  playback the next time the AudioSystem updates.
    /// </summary>
    /// <remarks>
    ///  Once playback starts, the
    ///  AudioSystem removes this component.
    ///  Attaching an AudioSourceStart component to an already playing source re-starts
    ///  playback from the beginning.
    ///  To stop a playing source, use the AudioSourceStop component.
    /// <example>
    /// Minimal code to play an AudioClip:
    /// <code>
    ///     var eSource = mgr.CreateEntity();
    ///     AudioSource source = new AudioSource();
    ///     source.clip = eClip;
    ///     mgr.AddComponentData(eSource, source);
    ///     mgr.AddComponent(eSource, typeof(AudioSourceStart));
    /// </code>
    /// </example>
    /// </remarks>
    public struct AudioSourceStart : IComponentData
    {
    }

    /// <summary>
    ///  Attach this component to an entity with an AudioSource component to stop
    ///  playback the next time the AudioSystem updates.
    /// </summary>
    /// <remarks>
    ///  Once playback stops, the
    ///  AudioSystem removes this component.
    ///  Attaching an AudioSourceStop component to an already stopped source has no effect.
    ///  To start playing a source, use the AudioSourceStart component.
    /// </remarks>
    public struct AudioSourceStop : IComponentData
    {
    }

    /// <summary>
    ///  An AudioSource component plays back one audio clip at a time.
    /// </summary>
    /// <remarks>
    ///  Multiple audio sources can play at the same time.
    ///  To start playback use the AudioSourceStart component.
    ///  To stop playback use the AudioSourceStop component.
    ///
    ///  `clip`, `volume`, and `loop` are read when the audio source
    ///  starts as a result of AudioSourceStart being added. They will
    ///  not change audio that is already playing.
    ///
    ///  `isPlaying` is updated with every tick of the world.
    /// </remarks>
    public struct AudioSource : IComponentData
    {
        /// <summary>
        ///  Specifies the audio clip that plays when this source starts playing.
        /// </summary>
        //[EntityWithComponents(typeof(AudioClip))]
        public Entity clip;

        /// <summary>
        ///  Specifies the audio clip's playback volume. Values can range from 0..1.
        /// </summary>
        public float volume;

        /// <summary>
        ///  If true, replays the audio clip when it reaches end.
        /// </summary>
        public bool loop;

        /// <summary>
        ///  True if the audio clip is currently playing.
        /// </summary>
        /// <remarks>
        ///  `isPlaying` will start false, and will be false until the AudioSourceStart tag
        ///  is removed by the Audio system.
        /// </remarks>
        public bool isPlaying { get; internal set; }
    }
}
