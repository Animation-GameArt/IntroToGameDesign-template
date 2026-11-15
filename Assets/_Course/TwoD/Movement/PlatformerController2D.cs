using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.TwoD
{
    /// <summary>
    /// A side-view character that runs and jumps: the player in a platformer.
    ///
    /// Setup: add this to a GameObject that has a 2D collider (a Capsule Collider 2D
    /// works well). A Rigidbody 2D is added for you. The ground is anything with a
    /// 2D collider.
    ///
    /// The settings are grouped by what they change. Good "game feel" comes from
    /// tuning them, so change one at a time and play:
    ///   Coyote Time   lets the player still jump just after walking off a ledge.
    ///   Jump Buffer   remembers a jump pressed just before landing.
    ///   Short Hop     makes a quick tap jump lower than a held press.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/2D/Platformer Controller 2D")]
    public class PlatformerController2D : MonoBehaviour
    {
        [Header("Input")]

        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Header("Running")]

        [Tooltip("Top running speed, in units per second.")]
        [SerializeField, Min(0f)] float maxSpeed = 7f;

        [Tooltip("How quickly the character reaches top speed. Low values feel heavy or icy; high values feel sharp.")]
        [SerializeField, Min(0f)] float acceleration = 60f;

        [Tooltip("How quickly the character stops when there is no input. Low values slide; high values stop at once.")]
        [SerializeField, Min(0f)] float deceleration = 70f;

        [Tooltip("How much steering the player has in the air. 0 is none, 1 is the same as on the ground.")]
        [SerializeField, Range(0f, 1f)] float airControl = 0.6f;

        [Header("Jumping")]

        [Tooltip("How high a full jump goes, in units.")]
        [SerializeField, Min(0f)] float jumpHeight = 3f;

        [Tooltip("Extra jumps allowed in the air. 1 gives a double jump.")]
        [SerializeField, Min(0)] int extraAirJumps = 0;

        [Tooltip("Short Hop: how much upward speed is kept when Jump is let go early. 1 turns short hops off; 0.5 halves the speed.")]
        [SerializeField, Range(0f, 1f)] float shortHopSpeedKept = 0.5f;

        [Tooltip("Coyote Time: how long after leaving a ledge the player can still jump, in seconds.")]
        [SerializeField, Min(0f)] float coyoteTime = 0.1f;

        [Tooltip("Jump Buffer: how long a jump press is remembered before landing, in seconds.")]
        [SerializeField, Min(0f)] float jumpBufferTime = 0.1f;

        [Header("Gravity")]

        [Tooltip("How strong gravity is for this character. Higher values make jumps quicker and snappier.")]
        [SerializeField, Min(0f)] float gravityScale = 3f;

        [Tooltip("Extra gravity while falling. Values above 1 make the fall faster than the rise.")]
        [SerializeField, Min(1f)] float fallGravityMultiplier = 1.5f;

        [Tooltip("The fastest the character can fall, in units per second.")]
        [SerializeField, Min(0f)] float maxFallSpeed = 20f;

        [Header("Ground check")]

        [Tooltip("Which layers count as ground. Everything works to start with.")]
        [SerializeField] LayerMask groundLayers = ~0;

        [Tooltip("How far below the feet to look for ground, in units.")]
        [SerializeField, Min(0.01f)] float groundCheckDistance = 0.08f;

        [Header("Looks")]

        [Tooltip("Optional. This sprite is flipped to face the way the character moves. Leave empty to use the first sprite found on this object.")]
        [SerializeField] SpriteRenderer spriteToFlip;

        [Header("Events")]

        [Tooltip("Runs each time the character jumps. Use it for a sound or an effect.")]
        [SerializeField] UnityEvent onJump;

        [Tooltip("Runs each time the character lands.")]
        [SerializeField] UnityEvent onLand;

        // After a jump, ignore the ground for a moment. Otherwise the feet are
        // still touching it on the next step and the jump would count as a landing.
        const float GroundIgnoreAfterJump = 0.1f;

        Rigidbody2D body;
        Collider2D bodyCollider;
        readonly Collider2D[] groundHits = new Collider2D[8];

        float moveX;
        float coyoteTimer;
        float jumpBufferTimer;
        float groundIgnoreTimer;
        int airJumpsLeft;
        bool isJumping;
        bool shortHopRequested;
        bool groundCheckedOnce;

        /// <summary>True while the character is standing on ground.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>The character's current speed and direction.</summary>
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

        /// <summary>1 when facing right, -1 when facing left.</summary>
        public int FacingDirection { get; private set; } = 1;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();

            if (spriteToFlip == null)
            {
                spriteToFlip = GetComponentInChildren<SpriteRenderer>();
            }

            // Set the Rigidbody up for a character, so you don't have to.
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.gravityScale = gravityScale;

            if (bodyCollider == null)
            {
                Debug.LogError($"Platformer Controller 2D on \"{name}\": this GameObject has no 2D collider. Add a Capsule Collider 2D.", this);
            }
            else if (bodyCollider.sharedMaterial == null)
            {
                // With no friction, the character can't stick to walls in mid-air.
                // Stopping on the ground is handled by Deceleration instead.
                bodyCollider.sharedMaterial = new PhysicsMaterial2D("No Friction (Platformer Controller 2D)")
                {
                    friction = 0f,
                    bounciness = 0f
                };
            }

            if (input == null)
            {
                Debug.LogError($"Platformer Controller 2D on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
            }
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.JumpPressed += OnJumpPressed;
                input.JumpReleased += OnJumpReleased;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.JumpPressed -= OnJumpPressed;
                input.JumpReleased -= OnJumpReleased;
            }

            moveX = 0f;
            jumpBufferTimer = 0f;
            shortHopRequested = false;
        }

        void OnJumpPressed()
        {
            // Remember the press for a short time, in case the character is about to land.
            jumpBufferTimer = Mathf.Max(jumpBufferTime, Time.fixedDeltaTime);
        }

        void OnJumpReleased()
        {
            if (isJumping)
            {
                shortHopRequested = true;
            }
        }

        void Update()
        {
            moveX = input != null ? input.Move.x : 0f;
        }

        void FixedUpdate()
        {
            float step = Time.fixedDeltaTime;

            UpdateGrounded(step);

            Vector2 velocity = body.linearVelocity;

            // ----- Running -----
            float targetSpeed = moveX * maxSpeed;
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

            if (!IsGrounded)
            {
                rate *= airControl;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * step);

            // ----- Jumping -----
            if (jumpBufferTimer > 0f)
            {
                if (coyoteTimer > 0f)
                {
                    velocity.y = JumpSpeed();
                    StartJump();
                }
                else if (airJumpsLeft > 0)
                {
                    airJumpsLeft--;
                    velocity.y = JumpSpeed();
                    StartJump();
                }
            }

            jumpBufferTimer -= step;

            if (shortHopRequested)
            {
                shortHopRequested = false;

                if (velocity.y > 0f)
                {
                    velocity.y *= shortHopSpeedKept;
                }
            }

            if (velocity.y <= 0f)
            {
                isJumping = false;
            }

            // ----- Gravity -----
            body.gravityScale = velocity.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale;

            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
            }

            body.linearVelocity = velocity;

            // ----- Facing -----
            if (Mathf.Abs(moveX) > 0.01f)
            {
                FacingDirection = moveX > 0f ? 1 : -1;

                if (spriteToFlip != null)
                {
                    spriteToFlip.flipX = FacingDirection < 0;
                }
            }
        }

        void UpdateGrounded(float step)
        {
            bool wasGrounded = IsGrounded;

            groundIgnoreTimer -= step;
            IsGrounded = groundIgnoreTimer <= 0f && CheckGround();

            if (IsGrounded)
            {
                coyoteTimer = Mathf.Max(coyoteTime, step * 0.5f);
                airJumpsLeft = extraAirJumps;

                if (!wasGrounded && groundCheckedOnce)
                {
                    onLand?.Invoke();
                }
            }
            else
            {
                coyoteTimer -= step;
            }

            groundCheckedOnce = true;
        }

        // Looks for a collider in a thin box just below the feet.
        bool CheckGround()
        {
            if (bodyCollider == null)
            {
                return false;
            }

            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(groundLayers);

            int count = Physics2D.OverlapBox(GroundCheckCenter(), GroundCheckSize(), 0f, filter, groundHits);

            for (int i = 0; i < count; i++)
            {
                // Anything that isn't part of this character counts as ground.
                if (groundHits[i] != null && groundHits[i].attachedRigidbody != body)
                {
                    return true;
                }
            }

            return false;
        }

        Vector2 GroundCheckCenter()
        {
            Bounds bounds = bodyCollider.bounds;
            return new Vector2(bounds.center.x, bounds.min.y - groundCheckDistance * 0.5f);
        }

        Vector2 GroundCheckSize()
        {
            // A little narrower than the character, so walls don't count as ground.
            float width = Mathf.Max(0.05f, bodyCollider.bounds.size.x * 0.9f);
            return new Vector2(width, groundCheckDistance);
        }

        // The upward speed needed to reach Jump Height under this character's gravity.
        float JumpSpeed()
        {
            float gravity = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
            return Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        void StartJump()
        {
            isJumping = true;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            groundIgnoreTimer = GroundIgnoreAfterJump;
            IsGrounded = false;
            onJump?.Invoke();
        }

        // Shows the ground check box in the Scene view while this object is selected.
        void OnDrawGizmosSelected()
        {
            Collider2D shape = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();

            if (shape == null)
            {
                return;
            }

            Bounds bounds = shape.bounds;
            Vector3 center = new Vector3(bounds.center.x, bounds.min.y - groundCheckDistance * 0.5f, transform.position.z);
            Vector3 size = new Vector3(Mathf.Max(0.05f, bounds.size.x * 0.9f), groundCheckDistance, 0f);

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(center, size);
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
