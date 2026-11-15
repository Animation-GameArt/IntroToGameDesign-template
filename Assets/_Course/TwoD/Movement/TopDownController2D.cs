using CollinGame.Core;
using UnityEngine;

namespace CollinGame.TwoD
{
    /// <summary>
    /// A character seen from above that moves in any direction: the player in a
    /// top-down adventure or shooter.
    ///
    /// Setup: add this to a GameObject that has a 2D collider. A Rigidbody 2D is
    /// added for you, and gravity is switched off, because in a top-down game
    /// "down" is into the screen.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/2D/Top Down Controller 2D")]
    public class TopDownController2D : MonoBehaviour
    {
        /// <summary>How the character shows which way it is going.</summary>
        public enum FacingMode
        {
            /// <summary>The character never turns.</summary>
            None,

            /// <summary>The sprite flips to face left or right.</summary>
            FlipSprite,

            /// <summary>The whole character turns so the top of its sprite points the way it moves.</summary>
            RotateToMovement
        }

        [Header("Input")]

        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Header("Moving")]

        [Tooltip("Top speed, in units per second.")]
        [SerializeField, Min(0f)] float maxSpeed = 6f;

        [Tooltip("How quickly the character reaches top speed. Low values feel heavy or icy; high values feel sharp.")]
        [SerializeField, Min(0f)] float acceleration = 50f;

        [Tooltip("How quickly the character stops when there is no input. Low values slide; high values stop at once.")]
        [SerializeField, Min(0f)] float deceleration = 60f;

        [Header("Looks")]

        [Tooltip("How the character shows which way it is going.")]
        [SerializeField] FacingMode facing = FacingMode.FlipSprite;

        [Tooltip("Optional. The sprite to flip. Leave empty to use the first sprite found on this object.")]
        [SerializeField] SpriteRenderer spriteToFlip;

        Rigidbody2D body;
        Vector2 moveInput;

        /// <summary>The character's current speed and direction.</summary>
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

        /// <summary>The last direction the character moved in. It starts out pointing right.</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.right;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            if (spriteToFlip == null)
            {
                spriteToFlip = GetComponentInChildren<SpriteRenderer>();
            }

            // Set the Rigidbody up for a top-down character, so you don't have to.
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (GetComponent<Collider2D>() == null)
            {
                Debug.LogWarning($"Top Down Controller 2D on \"{name}\": this GameObject has no 2D collider, so it will pass through walls. Add a Circle Collider 2D or a Capsule Collider 2D.", this);
            }

            if (input == null)
            {
                Debug.LogError($"Top Down Controller 2D on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
            }
        }

        void OnDisable()
        {
            moveInput = Vector2.zero;
        }

        void Update()
        {
            moveInput = input != null ? input.Move : Vector2.zero;
        }

        void FixedUpdate()
        {
            bool hasInput = moveInput.sqrMagnitude > 0.0001f;

            Vector2 targetVelocity = moveInput * maxSpeed;
            float rate = hasInput ? acceleration : deceleration;

            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, targetVelocity, rate * Time.fixedDeltaTime);

            if (hasInput)
            {
                FacingDirection = moveInput.normalized;
                ShowFacing();
            }
        }

        void ShowFacing()
        {
            if (facing == FacingMode.FlipSprite)
            {
                if (spriteToFlip != null && Mathf.Abs(FacingDirection.x) > 0.01f)
                {
                    spriteToFlip.flipX = FacingDirection.x < 0f;
                }
            }
            else if (facing == FacingMode.RotateToMovement)
            {
                // Turn so that the top of the sprite points along the movement.
                float angle = Mathf.Atan2(FacingDirection.y, FacingDirection.x) * Mathf.Rad2Deg - 90f;
                body.SetRotation(angle);
            }
        }

#if UNITY_EDITOR
        // Runs when the component is first added: fills in the Input field for you.
        void Reset()
        {
            input = InputReader.FindInProject();
        }
#endif
    }
}
