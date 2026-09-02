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

        [Header("Audio Settings")]
        [SerializeField] private AudioClip backgroundMusic;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1.0f;
        [SerializeField] private bool autoPlayMusicOnStart = true;

        public const string PREFS_MUSIC_VOL = "BT_MusicVolume";
        public const string PREFS_SFX_VOL = "BT_SFXVolume";

        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Load saved audio preferences
            musicVolume = PlayerPrefs.GetFloat(PREFS_MUSIC_VOL, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(PREFS_SFX_VOL, sfxVolume);

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
            bgmSource.volume = musicVolume;
        }

        private void Start()
        {
            if (autoPlayMusicOnStart && backgroundMusic != null)
            {
                PlayMusic(backgroundMusic, musicVolume);
            }
        }

        public void PlayMusic(AudioClip musicClip, float volume = -1f)
        {
            if (musicClip == null || bgmSource == null) return;

            backgroundMusic = musicClip;
            if (volume >= 0f) musicVolume = volume;

            bgmSource.clip = musicClip;
            bgmSource.volume = musicVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (bgmSource != null) bgmSource.volume = musicVolume;
            PlayerPrefs.SetFloat(PREFS_MUSIC_VOL, musicVolume);
            PlayerPrefs.Save();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(PREFS_SFX_VOL, sfxVolume);
            PlayerPrefs.Save();
        }

        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            float finalVol = Mathf.Clamp01(volume * sfxVolume);
            if (finalVol <= 0.001f) return;

            if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, finalVol);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, finalVol);
            }
        }
    }
}
