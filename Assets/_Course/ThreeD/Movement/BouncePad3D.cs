using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.ThreeD
{
    /// <summary>
    /// Launches a Player Controller 3D upward when it touches this trigger,
    /// such as a spring or mushroom.
    ///
    /// Setup: a collider with Is Trigger ticked, on top of the pad.
    /// </summary>
    [AddComponentMenu("Collin Game/3D/Bounce Pad 3D")]
    public class BouncePad3D : MonoBehaviour
    {
        [Tooltip("How high it launches the player, in units.")]
        [SerializeField, Min(0f)] float height = 6f;

        [Tooltip("Runs on each bounce, such as for a sound.")]
        [SerializeField] UnityEvent onBounce;

        void OnTriggerEnter(Collider other)
        {
            PlayerController3D player = TouchUtility.OwnerOf(other).GetComponentInParent<PlayerController3D>();

            if (player != null)
            {
                player.Bounce(height);
                onBounce?.Invoke();
            }
        }
    }
}
