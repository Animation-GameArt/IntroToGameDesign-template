using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.EndlessRunner
{
    /// <summary>
    /// A pickup that turns on the player's Coin Magnet 2D for a while.
    ///
    /// Setup: a sprite with a 2D collider, Is Trigger ticked. The player needs a
    /// Coin Magnet 2D.
    /// </summary>
    [AddComponentMenu("Collin Game/Endless Runner Kit/Magnet Power-Up 2D")]
    public class MagnetPowerUp2D : MonoBehaviour
    {
        [Tooltip("Only objects with this tag can pick it up.")]
        [SerializeField] string collectorTag = "Player";

        [Tooltip("How long the magnet stays on, in seconds.")]
        [SerializeField, Min(0f)] float duration = 8f;

        [Tooltip("Runs when it's picked up. Use it for a sound.")]
        [SerializeField] UnityEvent onCollected;

        void OnTriggerEnter2D(Collider2D other)
        {
            GameObject collector = TouchUtility.OwnerOf(other);

            if (!TouchUtility.HasTag(collector, collectorTag))
            {
                return;
            }

            CoinMagnet2D magnet = collector.GetComponentInChildren<CoinMagnet2D>();

            if (magnet == null)
            {
                Debug.LogWarning($"Magnet Power-Up 2D on \"{name}\": \"{collector.name}\" has no Coin Magnet 2D.", this);
                return;
            }

            magnet.Activate(duration);
            onCollected?.Invoke();
            Destroy(gameObject);
        }
    }
}
