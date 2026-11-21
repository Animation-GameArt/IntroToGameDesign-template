using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.EndlessRunner
{
    /// <summary>
    /// Pulls nearby collectibles, such as coins, to the player for a while.
    /// A Magnet Power-Up turns it on; it can also be on all the time.
    ///
    /// Setup: add to the player.
    /// </summary>
    [AddComponentMenu("Collin Game/Endless Runner Kit/Coin Magnet 2D")]
    public class CoinMagnet2D : MonoBehaviour
    {
        [Tooltip("How close a collectible has to be to be pulled, in units.")]
        [SerializeField, Min(0f)] float radius = 4f;

        [Tooltip("How fast collectibles fly to the player, in units per second.")]
        [SerializeField, Min(0f)] float pullSpeed = 18f;

        [Tooltip("Ticked: always on. Unticked: on only after Activate, such as from a power-up.")]
        [SerializeField] bool alwaysOn = false;

        [Tooltip("Optional. Shown while the magnet is on, such as a glow around the player.")]
        [SerializeField] GameObject activeEffect;

        static readonly Collider2D[] Nearby = new Collider2D[32];

        float onUntil = -1f;

        /// <summary>True while the magnet is pulling.</summary>
        public bool IsOn => alwaysOn || Time.time < onUntil;

        /// <summary>Turns the magnet on for some seconds. Calling it again adds to the time left.</summary>
        public void Activate(float seconds)
        {
            onUntil = Mathf.Max(onUntil, Time.time) + seconds;
        }

        void Update()
        {
            bool on = IsOn;

            if (activeEffect != null && activeEffect.activeSelf != on)
            {
                activeEffect.SetActive(on);
            }

            if (!on || PauseController.GamePaused)
            {
                return;
            }

            ContactFilter2D filter = ContactFilter2D.noFilter;
            int count = Physics2D.OverlapCircle(transform.position, radius, filter, Nearby);

            for (int i = 0; i < count; i++)
            {
                Collectible collectible = Nearby[i].GetComponentInParent<Collectible>();

                if (collectible == null)
                {
                    continue;
                }

                Transform item = collectible.transform;
                item.position = Vector3.MoveTowards(item.position, transform.position, pullSpeed * Time.deltaTime);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
