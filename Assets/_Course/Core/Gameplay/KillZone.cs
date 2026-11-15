using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Anything that touches this is out at once: a bottomless pit, lava, a crusher.
    ///
    /// If the object has Health, all of it is taken away. If it has no Health but
    /// has a Respawner, it goes straight back to its respawn point.
    ///
    /// Setup: this GameObject needs a collider. Tick Is Trigger for something the
    /// player falls into; leave it unticked for something solid. Works in 2D and 3D.
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Kill Zone")]
    public class KillZone : MonoBehaviour
    {
        [Tooltip("Only objects with this tag are affected. Leave empty to affect anything.")]
        [SerializeField] string targetTag = "Player";

        void OnTriggerEnter2D(Collider2D other)
        {
            Kill(TouchUtility.OwnerOf(other));
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            Kill(TouchUtility.OwnerOf(collision));
        }

        void OnTriggerEnter(Collider other)
        {
            Kill(TouchUtility.OwnerOf(other));
        }

        void OnCollisionEnter(Collision collision)
        {
            Kill(TouchUtility.OwnerOf(collision));
        }

        void Kill(GameObject target)
        {
            if (!TouchUtility.HasTag(target, targetTag))
            {
                return;
            }

            Health health = target.GetComponent<Health>();

            if (health != null)
            {
                health.Kill();
                return;
            }

            Respawner respawner = target.GetComponent<Respawner>();

            if (respawner != null)
            {
                respawner.Respawn();
                return;
            }

            Debug.LogWarning($"Kill Zone \"{name}\": \"{target.name}\" touched it but has no Health and no Respawner, so nothing happened. Add a Respawner to \"{target.name}\".", this);
        }
    }
}
