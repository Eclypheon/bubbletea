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

            // Ensure AudioListener is active in scene
            if (FindFirstObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
            AudioListener.pause = false;
            AudioListener.volume = 1f;

            // Ensure AudioSources exist and are explicitly 2D
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 100% 2D
            sfxSource.bypassEffects = true;
            sfxSource.bypassListenerEffects = true;
            sfxSource.mute = false;

            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f; // 100% 2D
            bgmSource.bypassEffects = true;
            bgmSource.bypassListenerEffects = true;
            bgmSource.mute = false;
            bgmSource.volume = musicVolume;
        }

        private bool hasUnlockedMobileAudio = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void ResumeWebAudioContext();
#endif

        private void Start()
        {
            UnlockMobileAudio();
            if (autoPlayMusicOnStart && backgroundMusic != null)
            {
                PlayMusic(backgroundMusic, musicVolume);
            }
        }

        private void Update()
        {
            if (!hasUnlockedMobileAudio)
            {
                bool pressed = false;
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                {
                    pressed = true;
                }
                else if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
                {
                    pressed = true;
                }
                else if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    pressed = true;
                }
#elif ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
                {
                    pressed = true;
                }
#else
                try
                {
                    if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                    {
                        pressed = true;
                    }
                }
                catch
                {
                    // Fallback
                }
#endif
                if (pressed)
                {
                    hasUnlockedMobileAudio = true;
                    UnlockMobileAudio();
                }
            }
        }

        public void UnlockMobileAudio()
        {
            AudioListener.pause = false;
            AudioListener.volume = 1f;

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                ResumeWebAudioContext();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AudioManager] WebAudio unlock hook: {ex.Message}");
            }
#endif
            if (bgmSource != null && backgroundMusic != null && !bgmSource.isPlaying)
            {
                bgmSource.volume = musicVolume;
                bgmSource.Play();
            }
        }

        public void PlayMusic(AudioClip musicClip, float volume = -1f)
        {
            if (musicClip == null || bgmSource == null) return;

            backgroundMusic = musicClip;
            if (volume >= 0f) musicVolume = volume;

            bgmSource.clip = musicClip;
            bgmSource.spatialBlend = 0f;
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

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
            }

            float finalVol = Mathf.Clamp01(volume * sfxVolume);
            if (finalVol <= 0.001f) return;

            if (sfxSource != null)
            {
                sfxSource.spatialBlend = 0f;
                sfxSource.PlayOneShot(clip, finalVol);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, Vector3.zero, finalVol);
            }
        }
    }
}
