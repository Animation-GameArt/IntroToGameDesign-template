using System.Collections;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Plays the game's sound effects and music. There is always exactly one: the
    /// Bootstrapper creates it on the "[Course Services]" object, so you never
    /// add it yourself.
    ///
    /// Sound effects are played by Sound Effect assets, and music by a Music Player
    /// in each scene. Music and effects have separate volumes, which are saved on
    /// this computer and changed from the Settings screen.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class AudioService : MonoBehaviour
    {
        const string MusicVolumeKey = "CollinGame.MusicVolume";
        const string EffectsVolumeKey = "CollinGame.EffectsVolume";

        // How many sound effects can play at the same moment.
        const int EffectVoices = 16;

        /// <summary>The one Audio Service in the game.</summary>
        public static AudioService Instance { get; private set; }

        AudioSource[] voices;
        int nextVoice;

        AudioSource musicA;
        AudioSource musicB;
        AudioSource currentMusic;
        AudioSource fadingMusic;
        float trackVolume = 1f;
        Coroutine fadeRoutine;

        float musicVolume = 0.8f;
        float effectsVolume = 1f;

        /// <summary>The music volume from the settings, 0 to 1. Saved on this computer.</summary>
        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);

                if (fadeRoutine == null && currentMusic != null)
                {
                    currentMusic.volume = trackVolume * musicVolume;
                }
            }
        }

        /// <summary>The sound effects volume from the settings, 0 to 1. Saved on this computer.</summary>
        public float EffectsVolume
        {
            get => effectsVolume;
            set
            {
                effectsVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(EffectsVolumeKey, effectsVolume);
            }
        }

        /// <summary>The music playing now, or nothing.</summary>
        public AudioClip CurrentMusic => currentMusic != null && currentMusic.isPlaying ? currentMusic.clip : null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
            effectsVolume = PlayerPrefs.GetFloat(EffectsVolumeKey, 1f);

            voices = new AudioSource[EffectVoices];

            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = CreateSource(false);
            }

            musicA = CreateSource(true);
            musicB = CreateSource(true);
            currentMusic = musicA;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            PlayerPrefs.Save();
        }

        AudioSource CreateSource(bool loop)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D sound: the same volume wherever the camera is.
            return source;
        }

        /// <summary>Plays a sound effect once.</summary>
        public void PlayEffect(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || voices == null)
            {
                return;
            }

            // Use the voices in turn, so a new sound never cuts off the one just started.
            AudioSource voice = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;

            voice.Stop();
            voice.clip = clip;
            voice.volume = Mathf.Clamp01(volume) * effectsVolume;
            voice.pitch = pitch;
            voice.Play();
        }

        /// <summary>
        /// Starts a piece of music, fading from whatever was playing. If the same
        /// music is already playing, it carries on without restarting.
        /// </summary>
        public void PlayMusic(AudioClip clip, float volume = 0.7f, float fadeTime = 1f)
        {
            if (clip == null)
            {
                StopMusic(fadeTime);
                return;
            }

            if (currentMusic.clip == clip && currentMusic.isPlaying)
            {
                trackVolume = Mathf.Clamp01(volume);

                if (fadeRoutine == null)
                {
                    currentMusic.volume = trackVolume * musicVolume;
                }

                return;
            }

            AudioSource next = currentMusic == musicA ? musicB : musicA;
            next.clip = clip;
            next.volume = 0f;
            next.Play();

            fadingMusic = currentMusic;
            currentMusic = next;
            trackVolume = Mathf.Clamp01(volume);

            StartFade(fadeTime);
        }

        /// <summary>Fades the music out and stops it.</summary>
        public void StopMusic(float fadeTime = 1f)
        {
            fadingMusic = currentMusic;
            currentMusic = currentMusic == musicA ? musicB : musicA;
            currentMusic.Stop();
            currentMusic.clip = null;
            trackVolume = 0f;

            StartFade(fadeTime);
        }

        void StartFade(float fadeTime)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
            }

            fadeRoutine = StartCoroutine(Fade(Mathf.Max(0f, fadeTime)));
        }

        IEnumerator Fade(float fadeTime)
        {
            float startOut = fadingMusic != null ? fadingMusic.volume : 0f;
            float elapsed = 0f;

            // Music fades on real time, so it still fades while the game is paused.
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeTime);

                if (fadingMusic != null)
                {
                    fadingMusic.volume = Mathf.Lerp(startOut, 0f, progress);
                }

                currentMusic.volume = Mathf.Lerp(0f, trackVolume * musicVolume, progress);
                yield return null;
            }

            if (fadingMusic != null)
            {
                fadingMusic.Stop();
                fadingMusic.volume = 0f;
            }

            currentMusic.volume = trackVolume * musicVolume;
            fadeRoutine = null;
        }
    }
}
