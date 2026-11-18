using Unity.Cinemachine;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Shakes the camera for a moment. Call Shake from any event: a Health's
    /// On Damaged, a stomp, an explosion.
    ///
    /// Setup:
    ///   1. Add this to any GameObject. A Cinemachine Impulse Source is added with it.
    ///   2. On the Cinemachine camera, click Add Extension and choose
    ///      Cinemachine Impulse Listener. Without the listener nothing shakes.
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    [AddComponentMenu("Collin Game/Effects/Camera Shake")]
    public class CameraShake : MonoBehaviour
    {
        [Tooltip("How strong the shake is. 1 is a normal hit; use less for small things and more for big ones.")]
        [SerializeField, Min(0f)] float strength = 1f;

        CinemachineImpulseSource source;

        void Awake()
        {
            source = GetComponent<CinemachineImpulseSource>();
        }

        /// <summary>Shakes the camera at this component's strength.</summary>
        public void Shake()
        {
            ShakeWithStrength(strength);
        }

        /// <summary>Shakes the camera at a chosen strength.</summary>
        public void ShakeWithStrength(float shakeStrength)
        {
            if (source != null && shakeStrength > 0f)
            {
                source.GenerateImpulseWithForce(shakeStrength);
            }
        }
    }
}
