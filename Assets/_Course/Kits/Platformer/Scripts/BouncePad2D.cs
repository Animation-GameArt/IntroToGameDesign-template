using System.Collections;
using CollinGame.Core;
using CollinGame.TwoD;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.Platformer
{
    /// <summary>
    /// A spring. Anything with the right tag that touches it is launched upward.
    ///
    /// Setup: this GameObject needs a 2D collider with Is Trigger ticked, placed
    /// over the top of the spring.
    /// </summary>
    [AddComponentMenu("Collin Game/Platformer Kit/Bounce Pad 2D")]
    public class BouncePad2D : MonoBehaviour
    {
        [Tooltip("Only objects with this tag are launched. Leave empty to launch anything with a Rigidbody 2D.")]
        [SerializeField] string targetTag = "Player";

        [Tooltip("How high it launches, in units.")]
        [SerializeField, Min(0f)] float bounceHeight = 6f;

        [Header("Looks")]

        [Tooltip("Optional. The sprite renderer to change for a moment when the spring fires. Leave empty to use the one on this object.")]
        [SerializeField] SpriteRenderer spriteToChange;

        [Tooltip("Optional. The sprite to show for a moment when the spring fires.")]
        [SerializeField] Sprite firedSprite;

        [Tooltip("How long the fired sprite is shown, in seconds.")]
        [SerializeField, Min(0f)] float firedTime = 0.2f;

        [Tooltip("Optional. Things to do when the spring fires, such as playing a sound.")]
        [SerializeField] UnityEvent onBounce;

        Sprite restingSprite;
        Coroutine spriteRoutine;

        void Awake()
        {
            if (spriteToChange == null)
            {
                spriteToChange = GetComponent<SpriteRenderer>();
            }

            if (spriteToChange != null)
            {
                restingSprite = spriteToChange.sprite;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            GameObject target = TouchUtility.OwnerOf(other);

            if (!TouchUtility.HasTag(target, targetTag))
            {
                return;
            }

            PlatformerController2D controller = target.GetComponent<PlatformerController2D>();

            if (controller != null)
            {
                controller.Bounce(bounceHeight);
            }
            else
            {
                Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();

                if (targetBody == null)
                {
                    return;
                }

                float gravity = Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.01f, targetBody.gravityScale);
                Vector2 velocity = targetBody.linearVelocity;
                velocity.y = Mathf.Sqrt(2f * gravity * bounceHeight);
                targetBody.linearVelocity = velocity;
            }

            onBounce?.Invoke();

            if (spriteToChange != null && firedSprite != null && isActiveAndEnabled)
            {
                if (spriteRoutine != null)
                {
                    StopCoroutine(spriteRoutine);
                }

                spriteRoutine = StartCoroutine(ShowFiredSprite());
            }
        }

        IEnumerator ShowFiredSprite()
        {
            spriteToChange.sprite = firedSprite;
            yield return new WaitForSeconds(firedTime);
            spriteToChange.sprite = restingSprite;
            spriteRoutine = null;
        }
    }
}
