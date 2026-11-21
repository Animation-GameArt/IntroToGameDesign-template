using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.ThreeD
{
    /// <summary>
    /// A 3D player controller with controls relative to the player, often
    /// called "tank controls":
    ///   Left and right   turn the player.
    ///   Forward          moves the player the way they face.
    ///   Back             moves the player backward, more slowly.
    ///   Jump             jumps, with coyote time, jump buffering and short hops.
    ///
    /// Pair it with a Cinemachine Camera that follows from behind, so "forward"
    /// on the stick is always "into the screen".
    ///
    /// Setup: put this on the player's root object, with its pivot at the feet.
    /// A Character Controller is added for you; set its Center to half its
    /// Height (for example Height 2, Center 0, 1, 0).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Collin Game/3D/Tank Controller 3D")]
    public class TankController3D : MonoBehaviour
    {
        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Header("Turning")]

        [Tooltip("How fast left and right turn the player, in degrees per second.")]
        [SerializeField, Min(0f)] float turnSpeed = 180f;

        [Tooltip("Turning speed in the air, as a share of Turn Speed.")]
        [SerializeField, Range(0f, 1f)] float airTurning = 0.75f;

        [Header("Moving")]

        [Tooltip("Top speed going forward, in units per second.")]
        [SerializeField, Min(0f)] float forwardSpeed = 6f;

        [Tooltip("Top speed going backward, in units per second.")]
        [SerializeField, Min(0f)] float backwardSpeed = 3f;

        [Tooltip("How quickly the player speeds up.")]
        [SerializeField, Min(0f)] float acceleration = 30f;

        [Tooltip("How quickly the player slows down when the stick is let go.")]
        [SerializeField, Min(0f)] float deceleration = 40f;

        [Tooltip("How much speed can change in the air, as a share of on the ground. 0 keeps the speed the player jumped with.")]
        [SerializeField, Range(0f, 1f)] float airControl = 0.5f;

        [Header("Jumping")]

        [Tooltip("How high a full jump goes, in units.")]
        [SerializeField, Min(0f)] float jumpHeight = 2f;

        [Tooltip("Extra jumps in the air. 1 gives a double jump.")]
        [SerializeField, Min(0)] int extraAirJumps = 0;

        [Tooltip("Share of the upward speed kept when Jump is let go early, for short hops. 1 means every jump is full height.")]
        [SerializeField, Range(0f, 1f)] float shortHopSpeedKept = 0.5f;

        [Tooltip("Seconds after walking off a ledge in which the player can still jump.")]
        [SerializeField, Min(0f)] float coyoteTime = 0.1f;

        [Tooltip("Seconds before landing in which a Jump press still counts.")]
        [SerializeField, Min(0f)] float jumpBufferTime = 0.1f;

        [Header("Gravity")]

        [Tooltip("How strongly the player is pulled down, in units per second squared.")]
        [SerializeField, Min(0f)] float gravity = 25f;

        [Tooltip("Gravity is multiplied by this while falling, so jumps feel snappy.")]
        [SerializeField, Min(1f)] float fallGravityMultiplier = 1.5f;

        [Tooltip("The fastest the player can fall, in units per second.")]
        [SerializeField, Min(0f)] float maxFallSpeed = 30f;

        [Header("Events")]

        [Tooltip("Runs on every jump. Use it for a sound.")]
        [SerializeField] UnityEvent onJump;

        [Tooltip("Runs when the player lands. Use it for a sound or dust.")]
        [SerializeField] UnityEvent onLanded;

        // A small downward speed while grounded keeps the controller on slopes and steps.
        const float GroundedStickSpeed = 2f;

        CharacterController controller;
        Vector2 moveInput;
        float speed;
        float verticalSpeed;
        float lastGroundedTime = -1f;
        float jumpPressedTime = -1f;
        int airJumpsLeft;
        bool grounded;
        bool jumping;
        Vector3 lastPosition;

        /// <summary>True while standing on something.</summary>
        public bool IsGrounded => grounded;

        /// <summary>Speed along the way the player faces. Negative while backing up.</summary>
        public float ForwardSpeed => speed;

        /// <summary>The player's velocity this frame, in units per second.</summary>
        public Vector3 Velocity => transform.forward * speed + Vector3.up * verticalSpeed;

        /// <summary>The move input this frame, after pausing is taken into account.</summary>
        public Vector2 MoveInput => moveInput;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            lastPosition = transform.position;

            if (input == null)
            {
                Debug.LogError($"Tank Controller 3D on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
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

            moveInput = Vector2.zero;
            speed = 0f;
            verticalSpeed = 0f;
        }

        void OnJumpPressed()
        {
            if (!PauseController.GamePaused)
            {
                jumpPressedTime = Time.time;
            }
        }

        void OnJumpReleased()
        {
            // Letting go early cuts the jump short.
            if (jumping && verticalSpeed > 0f)
            {
                verticalSpeed *= shortHopSpeedKept;
                jumping = false;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (dt <= 0f)
            {
                return;
            }

            // Something else, such as a Respawner, moved the player: tell the physics system.
            if ((transform.position - lastPosition).sqrMagnitude > 0.0001f)
            {
                Physics.SyncTransforms();
                verticalSpeed = 0f;
                speed = 0f;
            }

            moveInput = input != null && !PauseController.GamePaused ? input.Move : Vector2.zero;

            Turn(dt);
            UpdateSpeed(dt);
            TryJump();
            ApplyGravity(dt);

            Vector3 motion = transform.forward * speed + Vector3.up * verticalSpeed;
            CollisionFlags hits = controller.Move(motion * dt);

            // Bumping a ceiling stops the jump.
            if ((hits & CollisionFlags.Above) != 0 && verticalSpeed > 0f)
            {
                verticalSpeed = 0f;
                jumping = false;
            }

            bool wasGrounded = grounded;
            grounded = controller.isGrounded;

            if (grounded)
            {
                lastGroundedTime = Time.time;
                airJumpsLeft = extraAirJumps;

                if (!wasGrounded)
                {
                    jumping = false;
                    onLanded?.Invoke();
                }
            }

            lastPosition = transform.position;
        }

        void Turn(float dt)
        {
            float rate = turnSpeed * (grounded ? 1f : airTurning);
            transform.Rotate(0f, moveInput.x * rate * dt, 0f, Space.World);
        }

        void UpdateSpeed(float dt)
        {
            float target = moveInput.y >= 0f ? moveInput.y * forwardSpeed : moveInput.y * backwardSpeed;
            bool speedingUp = Mathf.Abs(target) > Mathf.Abs(speed) && target * speed >= 0f;
            float rate = (speedingUp ? acceleration : deceleration) * (grounded ? 1f : airControl);
            speed = Mathf.MoveTowards(speed, target, rate * dt);
        }

        void TryJump()
        {
            bool buffered = jumpPressedTime >= 0f && Time.time - jumpPressedTime <= jumpBufferTime;

            if (!buffered)
            {
                return;
            }

            bool canGroundJump = grounded || Time.time - lastGroundedTime <= coyoteTime;

            if (canGroundJump && !jumping)
            {
                Jump();
            }
            else if (!grounded && airJumpsLeft > 0)
            {
                airJumpsLeft--;
                Jump();
            }
        }

        void Jump()
        {
            jumpPressedTime = -1f;
            lastGroundedTime = -1f;
            verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
            jumping = true;
            grounded = false;
            onJump?.Invoke();
        }

        void ApplyGravity(float dt)
        {
            if (grounded && verticalSpeed <= 0f)
            {
                verticalSpeed = -GroundedStickSpeed;
                return;
            }

            float pull = gravity * (verticalSpeed < 0f ? fallGravityMultiplier : 1f);
            verticalSpeed = Mathf.Max(verticalSpeed - pull * dt, -maxFallSpeed);
        }

        /// <summary>Makes the player jump now, whatever the input. Use it for bounce pads.</summary>
        public void Bounce(float height)
        {
            verticalSpeed = Mathf.Sqrt(2f * gravity * Mathf.Max(0f, height));
            jumping = false;
            grounded = false;
            lastGroundedTime = -1f;
        }

#if UNITY_EDITOR
        // Runs when the component is first added: fills in the Input field and sizes the controller.
        void Reset()
        {
            input = InputReader.FindInProject();

            CharacterController body = GetComponent<CharacterController>();

            if (body != null)
            {
                body.height = 2f;
                body.radius = 0.4f;
                body.center = new Vector3(0f, 1f, 0f);
            }
        }
#endif
    }
}
