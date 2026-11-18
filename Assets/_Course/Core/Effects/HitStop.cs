using System.Collections;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Freezes the game for a fraction of a second, so a hit feels heavy.
    /// Call Stop from any event, such as a stomp or a Health's On Damaged.
    ///
    /// Put it on an object that stays in the scene, such as the player. If the
    /// object is switched off during the freeze, the game still un-freezes.
    /// </summary>
    [AddComponentMenu("Collin Game/Effects/Hit Stop")]
    public class HitStop : MonoBehaviour
    {
        [Tooltip("How long the game freezes, in seconds. A few hundredths is plenty.")]
        [SerializeField, Min(0f)] float duration = 0.06f;

        static bool stopping;
        bool ownsStop;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            stopping = false;
        }

        /// <summary>Freezes the game for this component's duration.</summary>
        public void Stop()
        {
            StopFor(duration);
        }

        /// <summary>Freezes the game for a chosen number of seconds.</summary>
        public void StopFor(float seconds)
        {
            // Never stack freezes, and never interfere with a pause.
            if (!isActiveAndEnabled || stopping || seconds <= 0f || Time.timeScale == 0f)
            {
                return;
            }

            StartCoroutine(Freeze(seconds));
        }

        IEnumerator Freeze(float seconds)
        {
            stopping = true;
            ownsStop = true;
            Time.timeScale = 0f;

            yield return new WaitForSecondsRealtime(seconds);

            Release();
        }

        void OnDisable()
        {
            if (ownsStop)
            {
                Release();
            }
        }

        void Release()
        {
            ownsStop = false;
            stopping = false;

            if (!PauseController.GamePaused)
            {
                Time.timeScale = 1f;
            }
        }
    }
}
