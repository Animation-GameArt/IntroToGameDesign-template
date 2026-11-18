using System;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// A sound effect: one or more audio clips, with a little random variation so
    /// a sound heard often doesn't become irritating.
    ///
    /// Play it from any event in the Inspector: click +, drag this asset into the
    /// object field, and choose SoundEffect > Play. For example, a Platformer
    /// Controller's On Jump, a Collectible's On Collected, or a Health's On Damaged.
    ///
    /// Create one with: Create > Collin Game > Audio > Sound Effect.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Audio/Sound Effect", fileName = "NewSound")]
    public class SoundEffect : ScriptableObject
    {
        [Tooltip("The sounds to play. With more than one, a different one is picked each time.")]
        [SerializeField] AudioClip[] clips;

        [Tooltip("How loud, from 0 to 1. The player's Sound Effects volume setting is applied on top.")]
        [SerializeField, Range(0f, 1f)] float volume = 1f;

        [Tooltip("How much the pitch changes at random each time. 0 is none; 0.1 is a little.")]
        [SerializeField, Range(0f, 0.5f)] float pitchVariation = 0.05f;

        [Tooltip("The shortest time between two plays, in seconds, so the same sound doesn't stack up when triggered many times at once.")]
        [SerializeField, Min(0f)] float minimumGap = 0.05f;

        [NonSerialized] float lastPlayedAt = -1f;
        [NonSerialized] int lastIndex = -1;

        /// <summary>Plays the sound once.</summary>
        public void Play()
        {
            if (!Application.isPlaying || AudioService.Instance == null || clips == null || clips.Length == 0)
            {
                return;
            }

            float now = Time.unscaledTime;

            if (lastPlayedAt >= 0f && now >= lastPlayedAt && now - lastPlayedAt < minimumGap)
            {
                return;
            }

            lastPlayedAt = now;

            int index = UnityEngine.Random.Range(0, clips.Length);

            // Avoid the same clip twice in a row when there is a choice.
            if (clips.Length > 1 && index == lastIndex)
            {
                index = (index + 1) % clips.Length;
            }

            lastIndex = index;

            float pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            AudioService.Instance.PlayEffect(clips[index], volume, pitch);
        }
    }
}
