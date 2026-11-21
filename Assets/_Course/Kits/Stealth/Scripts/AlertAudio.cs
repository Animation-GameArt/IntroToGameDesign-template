using UnityEngine;

namespace CollinGame.Kits.Stealth
{
    /// <summary>
    /// Fades a looping sound in as the guards grow alert, such as a heartbeat
    /// or tense music, and speeds it up during a chase.
    ///
    /// Setup: an Audio Source with a looping clip, Play On Awake ticked and
    /// Loop ticked.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Collin Game/Stealth Kit/Alert Audio")]
    public class AlertAudio : MonoBehaviour
    {
        [Tooltip("Volume when every guard is calm.")]
        [SerializeField, Range(0f, 1f)] float calmVolume = 0f;

        [Tooltip("Volume when a guard is fully alert.")]
        [SerializeField, Range(0f, 1f)] float alertVolume = 0.8f;

        [Tooltip("Pitch during a chase. 1 is normal.")]
        [SerializeField, Range(0.5f, 2f)] float chasePitch = 1.25f;

        [Tooltip("How quickly the volume and pitch follow the alert.")]
        [SerializeField, Min(0.1f)] float smoothing = 3f;

        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.volume = calmVolume;
        }

        void Update()
        {
            float alert = Guard3D.HighestAlert;
            float volume = Mathf.Lerp(calmVolume, alertVolume, alert);
            float pitch = Guard3D.AnyChasing ? chasePitch : 1f;
            float t = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            source.volume = Mathf.Lerp(source.volume, volume, t);
            source.pitch = Mathf.Lerp(source.pitch, pitch, t);
        }
    }
}
