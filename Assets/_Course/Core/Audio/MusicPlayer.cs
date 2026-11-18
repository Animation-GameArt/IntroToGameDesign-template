using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Plays a piece of music in this scene. Put one in each scene that has music.
    ///
    /// When a scene starts with the same music as the scene before, the music
    /// carries on without restarting. Different music fades in over the old.
    /// </summary>
    [AddComponentMenu("Collin Game/Audio/Music Player")]
    public class MusicPlayer : MonoBehaviour
    {
        [Tooltip("The music to play. It loops.")]
        [SerializeField] AudioClip music;

        [Tooltip("How loud, from 0 to 1. The player's Music volume setting is applied on top.")]
        [SerializeField, Range(0f, 1f)] float volume = 0.7f;

        [Tooltip("How long the change from the previous music takes, in seconds.")]
        [SerializeField, Min(0f)] float fadeTime = 1f;

        [Tooltip("Start the music when the scene starts.")]
        [SerializeField] bool playOnStart = true;

        void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        /// <summary>Plays this object's music.</summary>
        public void Play()
        {
            PlayClip(music);
        }

        /// <summary>Plays a different piece of music, such as a boss theme.</summary>
        public void PlayClip(AudioClip clip)
        {
            if (AudioService.Instance != null)
            {
                AudioService.Instance.PlayMusic(clip, volume, fadeTime);
            }
        }

        /// <summary>Fades the music out.</summary>
        public void Stop()
        {
            if (AudioService.Instance != null)
            {
                AudioService.Instance.StopMusic(fadeTime);
            }
        }
    }
}
