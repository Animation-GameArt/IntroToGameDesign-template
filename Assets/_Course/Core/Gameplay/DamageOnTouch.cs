using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Hurts anything with Health that touches this: spikes, an enemy, a falling rock.
    ///
    /// Setup: this GameObject needs a collider. It can be solid or a trigger.
    /// The object being hurt needs a Health component. Works in 2D and 3D.
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Damage On Touch")]
    public class DamageOnTouch : MonoBehaviour
    {
        [Tooltip("Only objects with this tag are hurt. Leave empty to hurt anything with Health.")]
        [SerializeField] string targetTag = "Player";

        [Tooltip("How much health each hit takes away.")]
        [SerializeField, Min(1)] int damage = 1;

        [Tooltip("While still touching, hit again every this many seconds. 0 hits only at the moment of touching.")]
        [SerializeField, Min(0f)] float repeatEvery = 0.5f;

        [Tooltip("Ticked: this object disappears after it hits something. Use it for rocks and projectiles.")]
        [SerializeField] bool removeSelfAfterHit;

        [Tooltip("Optional. Things to do each time this hits something, such as playing a sound.")]
        [SerializeField] UnityEvent onHit;

        float nextRepeatTime;

        void OnTriggerEnter2D(Collider2D other) { Hit(TouchUtility.OwnerOf(other)); }
        void OnCollisionEnter2D(Collision2D collision) { Hit(TouchUtility.OwnerOf(collision)); }
        void OnTriggerEnter(Collider other) { Hit(TouchUtility.OwnerOf(other)); }
        void OnCollisionEnter(Collision collision) { Hit(TouchUtility.OwnerOf(collision)); }

        void OnTriggerStay2D(Collider2D other) { Repeat(TouchUtility.OwnerOf(other)); }
        void OnCollisionStay2D(Collision2D collision) { Repeat(TouchUtility.OwnerOf(collision)); }
        void OnTriggerStay(Collider other) { Repeat(TouchUtility.OwnerOf(other)); }
        void OnCollisionStay(Collision collision) { Repeat(TouchUtility.OwnerOf(collision)); }

        void Repeat(GameObject target)
        {
            if (repeatEvery > 0f && Time.time >= nextRepeatTime)
            {
                Hit(target);
            }
        }

        void Hit(GameObject target)
        {
            if (!TouchUtility.HasTag(target, targetTag))
            {
                return;
            }

            Health health = target.GetComponent<Health>();

            if (health == null || health.IsDead)
            {
                return;
            }

            nextRepeatTime = Time.time + repeatEvery;
            health.TakeDamage(damage);
            onHit?.Invoke();

            if (removeSelfAfterHit)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
