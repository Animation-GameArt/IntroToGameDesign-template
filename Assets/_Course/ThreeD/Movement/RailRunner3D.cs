using CollinGame.Core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

namespace CollinGame.ThreeD
{
    /// <summary>
    /// Keeps the player on a rail: a spline the player runs along, which can
    /// wind, climb and loop. The player can't fall off, but can step a little
    /// to either side, within the rail's width, and jump.
    ///
    /// Three ways to control it:
    ///   Forward And Back   forward and back run along the rail; left and right step sideways.
    ///                      For a camera behind the player.
    ///   Left And Right     left and right run along the rail. For a camera at
    ///                      the side: a classic 2.5D side-scroller on a winding path.
    ///   Auto Run           the player runs forward by itself; left and right
    ///                      step sideways. For an endless runner.
    ///
    /// Setup: put this on the player's root object, with its pivot at the feet,
    /// and drag a Spline Container into Rail. When the game starts, the player
    /// jumps to the nearest point on the rail. A Character Controller is added,
    /// so pickups, checkpoints and hazards with trigger colliders still work.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Collin Game/3D/Rail Runner 3D")]
    public class RailRunner3D : MonoBehaviour
    {
        /// <summary>Which input runs along the rail.</summary>
        public enum RunControls
        {
            ForwardAndBack,
            LeftAndRight,
            AutoRun
        }

        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("The spline to run along: a GameObject with a Spline Container.")]
        [SerializeField] SplineContainer rail;

        [Tooltip("Which input runs along the rail.")]
        [SerializeField] RunControls controls = RunControls.ForwardAndBack;

        [Tooltip("Left And Right only: swap left and right, for a camera on the other side of the rail.")]
        [SerializeField] bool swapLeftAndRight;

        [Header("Running")]

        [Tooltip("Top speed along the rail, in units per second. Auto Run always runs at this speed.")]
        [SerializeField, Min(0f)] float runSpeed = 7f;

        [Tooltip("Forward And Back only: top speed running backward, in units per second.")]
        [SerializeField, Min(0f)] float backwardSpeed = 4f;

        [Tooltip("How quickly the player speeds up.")]
        [SerializeField, Min(0f)] float acceleration = 30f;

        [Tooltip("How quickly the player slows down when the stick is let go.")]
        [SerializeField, Min(0f)] float deceleration = 40f;

        [Header("Rail width")]

        [Tooltip("How wide the rail is, in units. The player can step sideways within it. 0 keeps the player exactly on the line.")]
        [SerializeField, Min(0f)] float railWidth = 1f;

        [Tooltip("How fast the player steps sideways, in units per second.")]
        [SerializeField, Min(0f)] float sideSpeed = 4f;

        [Tooltip("How far above the spline the player's feet are, in units. Raise it if the player sinks into the path's mesh.")]
        [SerializeField] float heightAboveRail = 0f;

        [Header("Facing")]

        [Tooltip("Ticked: the player turns around to face the way they run. Unticked: the player always faces forward along the rail, and backs up. Tick it for Left And Right.")]
        [SerializeField] bool faceRunDirection;

        [Tooltip("How fast the player turns to face the rail, in degrees per second.")]
        [SerializeField, Min(0f)] float turnSpeed = 720f;

        [Tooltip("Optional. A child object that always faces forward along the rail, even when the player turns around. Make it the Cinemachine Camera's Tracking Target for a side view, so the camera stays on one side.")]
        [SerializeField] Transform railFacingTarget;

        [Header("Jumping")]

        [Tooltip("How high a full jump goes, in units.")]
        [SerializeField, Min(0f)] float jumpHeight = 2f;

        [Tooltip("Extra jumps in the air. 1 gives a double jump.")]
        [SerializeField, Min(0)] int extraAirJumps = 0;

        [Tooltip("Share of the upward speed kept when Jump is let go early, for short hops.")]
        [SerializeField, Range(0f, 1f)] float shortHopSpeedKept = 0.5f;

        [Tooltip("Seconds before landing in which a Jump press still counts.")]
        [SerializeField, Min(0f)] float jumpBufferTime = 0.1f;

        [Tooltip("How strongly the player is pulled down, in units per second squared.")]
        [SerializeField, Min(0f)] float gravity = 25f;

        [Tooltip("Gravity is multiplied by this while falling.")]
        [SerializeField, Min(1f)] float fallGravityMultiplier = 1.5f;

        [Header("Events")]

        [Tooltip("Runs on every jump.")]
        [SerializeField] UnityEvent onJump;

        [Tooltip("Runs when the player lands back on the rail.")]
        [SerializeField] UnityEvent onLanded;

        [Tooltip("Runs when the player reaches the end of a rail that doesn't loop. Use it to finish the level.")]
        [SerializeField] UnityEvent onReachedEnd;

        [Tooltip("Runs when the player runs back to the start of a rail that doesn't loop.")]
        [SerializeField] UnityEvent onReachedStart;

        float t;
        float speed;
        float side;
        float height;
        float verticalSpeed;
        float jumpPressedTime = -1f;
        int airJumpsLeft;
        bool jumping;
        bool atEnd;
        bool atStart;
        bool snapped;
        Vector3 lastPosition;
        bool railMissingReported;

        /// <summary>How far along the rail the player is, from 0 at the start to 1 at the end.</summary>
        public float Progress => t;

        /// <summary>Speed along the rail. Negative while running back.</summary>
        public float RunSpeed => speed;

        /// <summary>How far the player has stepped from the middle of the rail. Negative is left.</summary>
        public float SideOffset => side;

        /// <summary>True while on the rail, not in the air.</summary>
        public bool IsGrounded => height <= 0f && verticalSpeed <= 0f;

        /// <summary>The rail the player runs along.</summary>
        public SplineContainer Rail => rail;

        void Awake()
        {
            if (input == null && controls != RunControls.AutoRun)
            {
                Debug.LogError($"Rail Runner 3D on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
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
        }

        void Start()
        {
            SnapToNearest();
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
            if (jumping && verticalSpeed > 0f)
            {
                verticalSpeed *= shortHopSpeedKept;
                jumping = false;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (dt <= 0f || !HasRail())
            {
                return;
            }

            // Something else, such as a Respawner, moved the player: find the rail again from there.
            if (!snapped || (transform.position - lastPosition).sqrMagnitude > 0.0001f)
            {
                SnapToNearest();
            }

            ReadInput(out float along, out float sideways);
            RunAlong(along, dt);
            StepSideways(sideways, dt);
            JumpAndFall(dt);
            Place(dt);
        }

        void ReadInput(out float along, out float sideways)
        {
            Vector2 move = input != null && !PauseController.GamePaused ? input.Move : Vector2.zero;

            switch (controls)
            {
                case RunControls.LeftAndRight:
                    along = swapLeftAndRight ? -move.x : move.x;
                    sideways = 0f;
                    break;

                case RunControls.AutoRun:
                    along = PauseController.GamePaused ? 0f : 1f;
                    sideways = move.x;
                    break;

                default:
                    along = move.y;
                    sideways = move.x;
                    break;
            }
        }

        void RunAlong(float along, float dt)
        {
            float back = controls == RunControls.ForwardAndBack ? backwardSpeed : runSpeed;
            float target = along >= 0f ? along * runSpeed : along * back;
            bool speedingUp = Mathf.Abs(target) > Mathf.Abs(speed) && target * speed >= 0f;
            speed = Mathf.MoveTowards(speed, target, (speedingUp ? acceleration : deceleration) * dt);

            // The spline's own pace varies along its length, so convert units to a share of the rail.
            float3 tangent = rail.EvaluateTangent(t);
            float pace = math.length(tangent);

            if (pace > 0.0001f)
            {
                t += speed * dt / pace;
            }

            if (rail.Spline.Closed)
            {
                t = Mathf.Repeat(t, 1f);
                return;
            }

            if (t >= 1f)
            {
                t = 1f;
                speed = Mathf.Min(speed, 0f);

                if (!atEnd)
                {
                    atEnd = true;
                    onReachedEnd?.Invoke();
                }
            }
            else if (t <= 0f)
            {
                t = 0f;
                speed = Mathf.Max(speed, 0f);

                if (!atStart)
                {
                    atStart = true;
                    onReachedStart?.Invoke();
                }
            }
            else
            {
                atEnd = t >= 0.999f && atEnd;
                atStart = t <= 0.001f && atStart;
            }
        }

        void StepSideways(float sideways, float dt)
        {
            float half = railWidth * 0.5f;
            side = Mathf.Clamp(side + sideways * sideSpeed * dt, -half, half);
        }

        void JumpAndFall(float dt)
        {
            bool grounded = IsGrounded;

            if (grounded)
            {
                airJumpsLeft = extraAirJumps;
            }

            bool buffered = jumpPressedTime >= 0f && Time.time - jumpPressedTime <= jumpBufferTime;

            if (buffered && (grounded || airJumpsLeft > 0))
            {
                if (!grounded)
                {
                    airJumpsLeft--;
                }

                jumpPressedTime = -1f;
                verticalSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
                jumping = true;
                onJump?.Invoke();
            }

            if (height <= 0f && verticalSpeed <= 0f)
            {
                height = 0f;
                verticalSpeed = 0f;
                return;
            }

            float pull = gravity * (verticalSpeed < 0f ? fallGravityMultiplier : 1f);
            verticalSpeed -= pull * dt;
            height += verticalSpeed * dt;

            if (height <= 0f)
            {
                height = 0f;
                verticalSpeed = 0f;
                jumping = false;
                onLanded?.Invoke();
            }
        }

        void Place(float dt)
        {
            Vector3 point = rail.EvaluatePosition(t);
            Vector3 forward = Flat((Vector3)(float3)rail.EvaluateTangent(t));
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            transform.position = point + right * side + Vector3.up * (height + heightAboveRail);

            Vector3 facing = forward;

            if (faceRunDirection && speed < -0.1f)
            {
                facing = -forward;
            }

            if (!faceRunDirection || Mathf.Abs(speed) > 0.1f)
            {
                Quaternion goal = Quaternion.LookRotation(facing, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, turnSpeed * dt);
            }

            if (railFacingTarget != null)
            {
                railFacingTarget.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }

            lastPosition = transform.position;
        }

        /// <summary>
        /// Moves the player to the point on the rail nearest to where they are now,
        /// in the middle of the rail and standing still. Runs at the start, and
        /// by itself after a Respawner moves the player.
        /// </summary>
        public void SnapToNearest()
        {
            if (!HasRail())
            {
                return;
            }

            float3 local = rail.transform.InverseTransformPoint(transform.position);
            SplineUtility.GetNearestPoint(rail.Spline, local, out float3 _, out float nearestT);

            t = nearestT;
            speed = 0f;
            side = 0f;
            height = 0f;
            verticalSpeed = 0f;
            jumping = false;
            atEnd = false;
            atStart = false;
            snapped = true;

            Vector3 forward = Flat((Vector3)(float3)rail.EvaluateTangent(t));
            Vector3 start = (Vector3)rail.EvaluatePosition(t) + Vector3.up * heightAboveRail;
            transform.SetPositionAndRotation(start, Quaternion.LookRotation(forward, Vector3.up));

            if (railFacingTarget != null)
            {
                railFacingTarget.rotation = transform.rotation;
            }

            lastPosition = transform.position;
            Physics.SyncTransforms();
        }

        /// <summary>Makes the player jump now, whatever the input. Use it for bounce pads.</summary>
        public void Bounce(float bounceHeight)
        {
            verticalSpeed = Mathf.Sqrt(2f * gravity * Mathf.Max(0f, bounceHeight));
            height = Mathf.Max(height, 0.001f);
            jumping = false;
        }

        bool HasRail()
        {
            if (rail != null && rail.Spline != null && rail.Spline.Count >= 2)
            {
                return true;
            }

            if (!railMissingReported)
            {
                railMissingReported = true;
                Debug.LogWarning($"Rail Runner 3D on \"{name}\": Rail is empty, or its spline has fewer than two knots. Drag in a GameObject with a Spline Container.", this);
            }

            return false;
        }

        // The direction along the ground, ignoring slope, so the player stays upright on hills.
        static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        void OnDrawGizmosSelected()
        {
            if (rail == null || rail.Spline == null || railWidth <= 0f)
            {
                return;
            }

            // Draw the edges of the rail.
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            const int steps = 64;
            Vector3 previousLeft = Vector3.zero;
            Vector3 previousRight = Vector3.zero;

            for (int i = 0; i <= steps; i++)
            {
                float step = i / (float)steps;
                Vector3 point = rail.EvaluatePosition(step);
                Vector3 right = Vector3.Cross(Vector3.up, Flat((Vector3)(float3)rail.EvaluateTangent(step))) * railWidth * 0.5f;

                if (i > 0)
                {
                    Gizmos.DrawLine(previousLeft, point - right);
                    Gizmos.DrawLine(previousRight, point + right);
                }

                previousLeft = point - right;
                previousRight = point + right;
            }
        }

#if UNITY_EDITOR
        // Runs when the component is first added: fills in the fields and sizes the controller.
        void Reset()
        {
            input = InputReader.FindInProject();
            rail = FindFirstObjectByType<SplineContainer>();

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
