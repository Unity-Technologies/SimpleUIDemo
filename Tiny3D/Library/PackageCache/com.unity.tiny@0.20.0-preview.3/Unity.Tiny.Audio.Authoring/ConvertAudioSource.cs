using Unity.Entities;
using Unity.Tiny.Audio;

namespace Unity.TinyConversion
{
    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    class AudioClipDeclareAssets : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.AudioSource audioSource) =>
            {
                DeclareReferencedAsset(audioSource.clip);
            });
        }  
    }
    
    internal class ConvertAudioSource : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UnityEngine.AudioSource audioSource) =>
            {
                var primaryEntity = GetPrimaryEntity(audioSource);
                DstEntityManager.AddComponentData(primaryEntity, new AudioSource
                {
                    clip = GetPrimaryEntity(audioSource.clip),
                    volume = audioSource.volume,
                    loop =  audioSource.loop
                });
                if (audioSource.playOnAwake)
                {
                    DstEntityManager.AddComponentData(primaryEntity, new AudioSourceStart());
                }
            });
        }
    }
}