using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// A pickup that gives the player a weapon for a while, such as a spread
    /// shot for 10 seconds. For health and points, use the Core Collectible instead.
    ///
    /// Setup: a sprite with a 2D collider, Is Trigger ticked. The player needs a
    /// Weapon Shooter 2D.
    /// </summary>
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Power Up 2D")]
    public class PowerUp2D : MonoBehaviour
    {
        [Tooltip("Only objects with this tag can pick it up.")]
        [SerializeField] string collectorTag = "Player";

        [Tooltip("The weapon it gives.")]
        [SerializeField] WeaponData weapon;

        [Tooltip("How long the weapon lasts, in seconds. 0 means for good.")]
        [SerializeField, Min(0f)] float duration = 10f;

        [Tooltip("Seconds before it disappears if nobody picks it up. 0 means it stays.")]
        [SerializeField, Min(0f)] float disappearAfter = 0f;

        [Tooltip("Runs when it's picked up. Use it for a sound.")]
        [SerializeField] UnityEvent onCollected;

        float disappearAt;

        void OnEnable()
        {
            disappearAt = disappearAfter > 0f ? Time.time + disappearAfter : -1f;
        }

        void Update()
        {
            if (disappearAt > 0f && Time.time >= disappearAt)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            GameObject collector = TouchUtility.OwnerOf(other);

            if (!TouchUtility.HasTag(collector, collectorTag))
            {
                return;
            }

            WeaponShooter2D shooter = collector.GetComponentInChildren<WeaponShooter2D>();

            if (shooter == null)
            {
                Debug.LogWarning($"Power Up 2D on \"{name}\": \"{collector.name}\" has no Weapon Shooter 2D to give the weapon to.", this);
                return;
            }

            shooter.SetWeapon(weapon, duration);
            onCollected?.Invoke();
            Destroy(gameObject);
        }
    }
}
