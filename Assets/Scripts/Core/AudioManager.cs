using System;
using UnityEngine;

namespace BubbleTeaShop
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;

        [Header("Background Music (Looping)")]
        [SerializeField] private AudioClip backgroundMusic;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.45f;
        [SerializeField] private bool autoPlayMusicOnStart = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Ensure AudioSources exist
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            if (autoPlayMusicOnStart && backgroundMusic != null)
            {
                PlayMusic(backgroundMusic, musicVolume);
            }
        }

        public void PlayMusic(AudioClip musicClip, float volume = 0.45f)
        {
            if (musicClip == null || bgmSource == null) return;

            backgroundMusic = musicClip;
            musicVolume = volume;
            bgmSource.clip = musicClip;
            bgmSource.volume = musicVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (bgmSource != null) bgmSource.volume = musicVolume;
        }

        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
            }
        }
    }
}
