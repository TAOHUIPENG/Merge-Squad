using UnityEngine;

using static D2D.Utilities.CommonGameplayFacade;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] [Range(0f, 1f)] private float backgroundMusicVolume = 0.2f;

    private AudioSource backgroundMusicSource;

    private void Awake()
    {
        _audioManager = this;
        PlayBackgroundMusic();
    }

    public void PlayOneShot(AudioClip audioClip, float volume = 1f)
    {
        audioSource.PlayOneShot(audioClip, volume);
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusicClip == null || backgroundMusicSource != null)
            return;

        backgroundMusicSource = gameObject.AddComponent<AudioSource>();
        backgroundMusicSource.clip = backgroundMusicClip;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.playOnAwake = false;
        backgroundMusicSource.volume = backgroundMusicVolume;
        backgroundMusicSource.spatialBlend = 0f;
        backgroundMusicSource.ignoreListenerPause = true;

        if (audioSource != null)
            backgroundMusicSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;

        backgroundMusicSource.Play();
    }
}
