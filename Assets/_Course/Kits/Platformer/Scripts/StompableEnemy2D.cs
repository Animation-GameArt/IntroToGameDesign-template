using CollinGame.Core;
using CollinGame.TwoD;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.Platformer
{
    /// <summary>
    /// An enemy the player can defeat by landing on it.
    ///
    /// If the player touches it from above, the enemy is hurt and the player
    /// bounces off. If the player touches it any other way, the player is hurt.
    ///
    /// Setup: this GameObject needs a solid 2D collider (not a trigger). Use this
    /// in place of Damage On Touch. Give the enemy a Health component if it
    /// should take more than one stomp; without Health, one stomp removes it.
    /// </summary>
    [AddComponentMenu("Collin Game/Platformer Kit/Stompable Enemy 2D")]
    public class StompableEnemy2D : MonoBehaviour
    {
        [Tooltip("The tag of the object that can stomp this enemy, and that it hurts.")]
        [SerializeField] string playerTag = "Player";

        [Header("When the player lands on top")]

        [Tooltip("How much health a stomp takes from this enemy, if it has a Health component.")]
        [SerializeField, Min(1)] int stompDamage = 1;

        [Tooltip("How high the player bounces after a stomp, in units.")]
        [SerializeField, Min(0f)] float bounceHeight = 2f;

        [Tooltip("Optional. A number to add to, such as the score.")]
        [SerializeField] IntVariable addTo;

        [Tooltip("How much to add.")]
        [SerializeField] int amount = 1;

        [Tooltip("Optional. Things to do when stomped, such as playing a sound.")]
        [SerializeField] UnityEvent onStomped;

        [Header("When the player touches it any other way")]

        [Tooltip("How much health a touch takes from the player.")]
        [SerializeField, Min(0)] int touchDamage = 1;

        [Tooltip("While still touching, hurt again every this many seconds. 0 hurts only at the moment of touching.")]
        [SerializeField, Min(0f)] float repeatEvery = 0.5f;

        // The player's feet must be at least this far above the enemy's middle to count as a stomp.
        const float StompMargin = 0.05f;

        Collider2D ownCollider;
        Health ownHealth;
        float nextTouchTime;
        float nextStompTime;

        void Awake()
        {
            ownCollider = GetComponent<Collider2D>();
            ownHealth = GetComponent<Health>();

            if (ownCollider == null)
            {
                Debug.LogError($"Stompable Enemy 2D on \"{name}\": this GameObject has no 2D collider. Add a Box Collider 2D.", this);
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            Touch(TouchUtility.OwnerOf(collision), true);
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            Touch(TouchUtility.OwnerOf(collision), false);
        }

        void Touch(GameObject other, bool justTouched)
        {
            if (ownCollider == null || !TouchUtility.HasTag(other, playerTag))
            {
                return;
            }

            Collider2D playerCollider = other.GetComponentInChildren<Collider2D>();

            if (playerCollider == null)
            {
                return;
            }

            bool fromAbove = playerCollider.bounds.min.y >= ownCollider.bounds.center.y + StompMargin;

            if (fromAbove)
            {
                if (Time.time >= nextStompTime)
                {
                    nextStompTime = Time.time + 0.2f;
                    Stomp(other);
                }

                return;
            }

            if (touchDamage <= 0)
            {
                return;
            }

            if (!justTouched && (repeatEvery <= 0f || Time.time < nextTouchTime))
            {
                return;
            }

            Health playerHealth = other.GetComponent<Health>();

            if (playerHealth != null)
            {
                nextTouchTime = Time.time + repeatEvery;
                playerHealth.TakeDamage(touchDamage);
            }
        }

        void Stomp(GameObject player)
        {
            // Bounce the player first, while this enemy is still in the scene.
            PlatformerController2D controller = player.GetComponent<PlatformerController2D>();

            if (controller != null)
            {
                controller.Bounce(bounceHeight);
            }
            else
            {
                Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();

                if (playerBody != null)
                {
                    float gravity = Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.01f, playerBody.gravityScale);
                    Vector2 velocity = playerBody.linearVelocity;
                    velocity.y = Mathf.Sqrt(2f * gravity * bounceHeight);
                    playerBody.linearVelocity = velocity;
                }
            }

            if (addTo != null)
            {
                addTo.Add(amount);
            }

            onStomped?.Invoke();

            if (ownHealth != null)
            {
                ownHealth.TakeDamage(stompDamage);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
