using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.ThreeD
{
    /// <summary>
    /// A 3D player controller with controls relative to the camera, as in most
    /// 3D platformers and third-person games: pushing up on the stick moves the
    /// player away from the camera, wherever it's looking.
    ///
    ///   Move     walks, relative to the camera
    ///   Sprint   (Left Shift) runs
    ///   Crouch   (C) crouches: slower, quieter and shorter
    ///   Jump     jumps, with coyote time, jump buffering and short hops
    ///
    /// Facing: Face Movement turns the player toward where they walk (third
    /// person, with an orbiting camera). Face Camera keeps the player facing
    /// where the camera looks (first person, or over the shoulder).
    ///
    /// The player rides Moving Platform 3D objects they stand on.
    ///
    /// Setup: put this on the player's root object, with its pivot at the feet.
    /// A Character Controller is added for you.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Collin Game/3D/Player Controller 3D")]
    public class PlayerController3D : MonoBehaviour
    {
        public enum FacingMode
        {
            FaceMovement,
            FaceCamera
        }

        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("Optional. The camera that movement is relative to. Empty: the Main Camera.")]
        [SerializeField] Transform cameraTransform;

        [Header("Moving")]

        [SerializeField, Min(0f)] float walkSpeed = 6f;

        [Tooltip("Speed while Sprint is held. Set it to the walk speed for no sprinting.")]
        [SerializeField, Min(0f)] float sprintSpeed = 9f;

        [SerializeField, Min(0f)] float acceleration = 40f;
        [SerializeField, Min(0f)] float deceleration = 50f;

        [Tooltip("How much control the player has in the air: 1 is full, 0 is none.")]
        [SerializeField, Range(0f, 1f)] float airControl = 0.6f;

        [Header("Facing")]

        [SerializeField] FacingMode facing = FacingMode.FaceMovement;

        [Tooltip("Degrees per second the player turns to face their movement.")]
        [SerializeField, Min(0f)] float turnSpeed = 720f;

        [Header("Crouching")]

        [Tooltip("Ticked: Crouch makes the player shorter, slower and quieter.")]
        [SerializeField] bool canCrouch = true;

        [SerializeField, Min(0f)] float crouchSpeed = 2.5f;

        [Tooltip("The Character Controller's height while crouching.")]
        [SerializeField, Min(0.2f)] float crouchHeight = 1f;

        [Header("Jumping")]

        [Tooltip("Unticked: Jump does nothing, as in many horror games.")]
        [SerializeField] bool canJump = true;

        [Tooltip("How high a full jump goes, in units.")]
        [SerializeField, Min(0f)] float jumpHeight = 2f;

        [Tooltip("Jumps allowed in the air: 1 for a double jump.")]
        [SerializeField, Min(0)] int extraAirJumps = 0;

        [Tooltip("Letting go of Jump early keeps this much of the upward speed, for short hops.")]
        [SerializeField, Range(0f, 1f)] float shortHopSpeedKept = 0.5f;

        [Tooltip("Seconds after walking off a ledge when a jump still works.")]
        [SerializeField, Min(0f)] float coyoteTime = 0.12f;

        [Tooltip("Seconds a Jump press is remembered before landing.")]
        [SerializeField, Min(0f)] float jumpBufferTime = 0.12f;

        [Header("Gravity")]

        [SerializeField, Min(0f)] float gravity = 25f;

        [Tooltip("Gravity is this many times stronger while falling, for a snappier jump.")]
        [SerializeField, Min(1f)] float fallGravityMultiplier = 1.6f;

        [SerializeField, Min(0f)] float maxFallSpeed = 30f;

        [Header("Events")]

        [SerializeField] UnityEvent onJump;
        [SerializeField] UnityEvent onLanded;

        const float GroundedStickSpeed = 2f;

        CharacterController controller;
        Vector3 horizontalVelocity;
        float verticalSpeed;
        Vector2 moveInput;
        bool grounded;
        bool jumping;
        bool crouching;
        float standingHeight;
        Vector3 standingCenter;
        float lastGroundedTime = -1f;
        float jumpPressedTime = -1f;
        int airJumpsLeft;
        Vector3 lastPosition;
        MovingPlatform3D platform;

        public bool IsGrounded => grounded;
        public bool IsCrouching => crouching;
        public bool IsSprinting => !crouching && input != null && input.SprintHeld && horizontalVelocity.sqrMagnitude > 0.1f;
        public Vector3 Velocity => horizontalVelocity + Vector3.up * verticalSpeed;
        public Vector2 MoveInput => moveInput;

        /// <summary>
        /// How much noise the player makes, from 0 (standing still) to 1
        /// (sprinting). Crouching is quiet. Guards in the Stealth kit use it.
        /// </summary>
        public float Loudness
        {
            get
            {
                float speed = new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;

                if (speed < 0.3f || !grounded)
                {
                    return 0f;
                }

                if (crouching)
                {
                    return 0.15f;
                }

                return IsSprinting ? 1f : 0.5f;
            }
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            standingHeight = controller.height;
            standingCenter = controller.center;
            lastPosition = transform.position;

            if (input == null)
            {
                input = InputReader.FindInProject();
            }

            if (input == null)
            {
                Debug.LogError($"Player Controller 3D on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
            }
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
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
            horizontalVelocity = Vector3.zero;
            verticalSpeed = 0f;
        }

        void OnJumpPressed()
        {
            if (canJump && !PauseController.GamePaused)
            {
                jumpPressedTime = Time.time;
            }
        }

        void OnJumpReleased()
        {
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

            // Something else, such as a Respawner, moved the player: tell the physics system, and stop.
            if ((transform.position - lastPosition).sqrMagnitude > 0.0001f)
            {
                Physics.SyncTransforms();
                horizontalVelocity = Vector3.zero;
                verticalSpeed = 0f;
                platform = null;
            }

            moveInput = input != null && !PauseController.GamePaused ? input.Move : Vector2.zero;

            UpdateCrouch();
            UpdateHorizontal(dt);
            TryJump();
            ApplyGravity(dt);
            Face(dt);

            Vector3 motion = (horizontalVelocity + Vector3.up * verticalSpeed) * dt;

            // Ride the platform underfoot.
            if (platform != null && grounded)
            {
                motion += platform.Delta;
            }

            platform = null;
            CollisionFlags hits = controller.Move(motion);

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

        // Remembers a moving platform the player is standing on.
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y > 0.5f)
            {
                MovingPlatform3D found = hit.collider.GetComponentInParent<MovingPlatform3D>();

                if (found != null)
                {
                    platform = found;
                }
            }
        }

        void UpdateCrouch()
        {
            bool wantCrouch = canCrouch && input != null && input.CrouchHeld && !PauseController.GamePaused;

            if (wantCrouch == crouching)
            {
                return;
            }

            if (!wantCrouch && !RoomToStand())
            {
                return;
            }

            crouching = wantCrouch;
            float height = crouching ? Mathf.Min(crouchHeight, standingHeight) : standingHeight;
            controller.height = height;
            controller.center = new Vector3(standingCenter.x, standingCenter.y - (standingHeight - height) * 0.5f, standingCenter.z);
        }

        bool RoomToStand()
        {
            float radius = controller.radius * 0.95f;
            Vector3 bottom = transform.position + Vector3.up * (radius + 0.05f);
            Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
            foreach (Collider hit in Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                // The player's own collider doesn't count.
                if (hit != controller && !hit.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        void UpdateHorizontal(float dt)
        {
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;

                if (forward.sqrMagnitude < 0.01f)
                {
                    forward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up).normalized;
                }

                right = Vector3.Cross(Vector3.up, forward);
            }

            float speed = crouching ? crouchSpeed : (input != null && input.SprintHeld ? sprintSpeed : walkSpeed);
            Vector3 target = (forward * moveInput.y + right * moveInput.x) * speed;
            bool speedingUp = target.sqrMagnitude > horizontalVelocity.sqrMagnitude;
            float rate = (speedingUp ? acceleration : deceleration) * (grounded ? 1f : airControl);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, rate * dt);
        }

        void Face(float dt)
        {
            Vector3 look;

            if (facing == FacingMode.FaceCamera && cameraTransform != null)
            {
                look = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            }
            else
            {
                look = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);

                if (look.sqrMagnitude < 0.05f)
                {
                    return;
                }
            }

            if (look.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion target = Quaternion.LookRotation(look.normalized, Vector3.up);
            float step = facing == FacingMode.FaceCamera ? 100000f : turnSpeed * dt;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, step);
        }

        void TryJump()
        {
            bool buffered = jumpPressedTime >= 0f && Time.time - jumpPressedTime <= jumpBufferTime;

            if (!buffered || crouching)
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

        /// <summary>Makes the player jump now, whatever the input. Bounce Pad 3D uses it.</summary>
        public void Bounce(float height)
        {
            verticalSpeed = Mathf.Sqrt(2f * gravity * Mathf.Max(0f, height));
            jumping = false;
            grounded = false;
            lastGroundedTime = -1f;
            airJumpsLeft = extraAirJumps;
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
