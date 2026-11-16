using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Something that can be picked up: a coin, a key, a gem.
    ///
    /// When an object with the right tag touches it, the collectible can add to a
    /// variable (such as the score), raise an event (such as "key collected"),
    /// and run its own response (such as playing a sound). Then it disappears.
    ///
    /// Setup: this GameObject needs a collider with Is Trigger ticked, and the
    /// object that collects it needs a Rigidbody. Works in 2D and 3D.
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Collectible")]
    public class Collectible : MonoBehaviour
    {
        [Tooltip("Only objects with this tag can collect it. Leave empty to let anything collect it.")]
        [SerializeField] string collectorTag = "Player";

        [Header("When collected")]

        [Tooltip("Optional. A number to add to, such as the score.")]
        [SerializeField] IntVariable addTo;

        [Tooltip("How much to add.")]
        [SerializeField] int amount = 1;

        [Tooltip("Health to give back to whoever collects this. 0 means none. Use it for hearts and health packs.")]
        [SerializeField, Min(0)] int heal = 0;

        [Tooltip("Optional. An event to raise, so other objects can react.")]
        [SerializeField] VoidEventChannel raiseEvent;

        [Tooltip("Optional. Extra things to do on this object, such as playing a sound.")]
        [SerializeField] UnityEvent onCollected;

        [Tooltip("Ticked: the GameObject is destroyed. Unticked: it is only hidden, so it can be shown again.")]
        [SerializeField] bool destroyWhenCollected = true;

        bool collected;

        void OnEnable()
        {
            collected = false;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // Use the object that owns the Rigidbody, so a collider on a child
            // of the player still counts as the player.
            GameObject collector = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            TryCollect(collector);
        }

        void OnTriggerEnter(Collider other)
        {
            GameObject collector = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            TryCollect(collector);
        }

        void TryCollect(GameObject collector)
        {
            if (!string.IsNullOrEmpty(collectorTag) && !collector.CompareTag(collectorTag))
            {
                return;
            }

            Collect(collector);
        }

        /// <summary>Collects this right now, without anything having to touch it.</summary>
        public void Collect()
        {
            Collect(null);
        }

        void Collect(GameObject collector)
        {
            if (collected)
            {
                return;
            }

            collected = true;

            if (heal > 0 && collector != null)
            {
                Health collectorHealth = collector.GetComponent<Health>();

                if (collectorHealth != null)
                {
                    collectorHealth.Heal(heal);
                }
            }

            if (addTo != null)
            {
                addTo.Add(amount);
            }

            if (raiseEvent != null)
            {
                raiseEvent.Raise();
            }

            onCollected?.Invoke();

            if (destroyWhenCollected)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
