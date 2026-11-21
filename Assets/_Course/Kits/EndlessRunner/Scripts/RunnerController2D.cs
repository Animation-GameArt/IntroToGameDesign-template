using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.EndlessRunner
{
    /// <summary>
    /// A player that runs to the right by itself, faster and faster. The player
    /// only decides when to jump (or fly, or change lanes). Three styles:
    ///
    ///   Jump      jump over gaps and obstacles, and slide under them by holding
    ///             down. Canabalt, Alto's Adventure.
    ///   Jetpack   hold Jump to fly up, let go to fall. Jetpack Joyride.
    ///   Lanes     up and down switch between lanes, as in a top-down road or
    ///             a 2D take on Subway Surfers. No gravity.
    ///
    /// It also measures the distance run, for the score.
    ///
    /// Setup: a sprite with a Box Collider 2D or Capsule Collider 2D. A
    /// Rigidbody 2D is added and set up for you. With Health on the same
    /// object, the runner stops when its health runs out.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/Endless Runner Kit/Runner Controller 2D")]
    public class RunnerController2D : MonoBehaviour
    {
        /// <summary>How the player avoids things.</summary>
        public enum Style
        {
            Jump,
            Jetpack,
            Lanes
        }

        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("How the player avoids things.")]
        [SerializeField] Style style = Style.Jump;

        [Header("Speed")]

        [Tooltip("Running speed at the start, in units per second.")]
        [SerializeField, Min(0f)] float startSpeed = 6f;

        [Tooltip("The fastest the runner gets, in units per second.")]
        [SerializeField, Min(0f)] float maxSpeed = 14f;

        [Tooltip("Speed added every second. This is the difficulty ramp: from 6 to 14 at 0.1 takes 80 seconds.")]
        [SerializeField, Min(0f)] float speedGainPerSecond = 0.1f;

        [Tooltip("Seconds before running starts, so the player can get ready.")]
        [SerializeField, Min(0f)] float startDelay = 1f;

        [Header("Score")]

        [Tooltip("Optional. A number variable set to the distance run, for the HUD and a High Score.")]
        [SerializeField] IntVariable distance;

        [Tooltip("How many units make one point of distance. 1 counts every unit; 2 makes the numbers half as big.")]
        [SerializeField, Min(0.01f)] float unitsPerPoint = 1f;

        [Header("Jump style")]

        [Tooltip("How high a full jump goes, in units.")]
        [SerializeField, Min(0f)] float jumpHeight = 3.2f;

        [Tooltip("Extra jumps in the air. 1 gives a double jump.")]
        [SerializeField, Min(0)] int extraAirJumps = 0;

        [Tooltip("Share of the upward speed kept when Jump is let go early, for short hops.")]
        [SerializeField, Range(0f, 1f)] float shortHopSpeedKept = 0.5f;

        [Tooltip("Seconds after running off a ledge in which a jump still works.")]
        [SerializeField, Min(0f)] float coyoteTime = 0.1f;

        [Tooltip("Seconds before landing in which a Jump press still counts.")]
        [SerializeField, Min(0f)] float jumpBufferTime = 0.15f;

        [Tooltip("Ticked: holding down slides, making the player shorter.")]
        [SerializeField] bool canSlide = true;

        [Tooltip("The player's height while sliding, as a share of standing height.")]
        [SerializeField, Range(0.2f, 1f)] float slideHeight = 0.5f;

        [Tooltip("Optional. The sprite shown while sliding.")]
        [SerializeField] Sprite slideSprite;

        [Header("Jetpack style")]

        [Tooltip("How strongly the jetpack pushes up while Jump is held.")]
        [SerializeField, Min(0f)] float thrust = 45f;

        [Tooltip("The fastest the jetpack can climb, in units per second.")]
        [SerializeField, Min(0f)] float maxRiseSpeed = 8f;

        [Header("Lanes style")]

        [Tooltip("The height of each lane's middle, in world units, from bottom to top.")]
        [SerializeField] float[] laneHeights = { -2.5f, 0f, 2.5f };

        [Tooltip("The lane to start in, counting from 0 at the bottom.")]
        [SerializeField, Min(0)] int startLane = 1;

        [Tooltip("Seconds it takes to move to the next lane.")]
        [SerializeField, Min(0.01f)] float laneChangeTime = 0.12f;

        [Header("Falling")]

        [Tooltip("Gravity, as a multiple of the project's gravity. Not used in the Lanes style.")]
        [SerializeField, Min(0f)] float gravityScale = 3.5f;

        [Tooltip("Gravity is multiplied by this while falling.")]
        [SerializeField, Min(1f)] float fallGravityMultiplier = 1.5f;

        [Tooltip("The fastest the player can fall, in units per second.")]
        [SerializeField, Min(0f)] float maxFallSpeed = 25f;

        [Tooltip("Which layers count as ground.")]
        [SerializeField] LayerMask groundLayers = ~0;

        [Header("Events")]

        [Tooltip("Runs on every jump.")]
        [SerializeField] UnityEvent onJump;

        [Tooltip("Runs when the player lands.")]
        [SerializeField] UnityEvent onLand;

        [Tooltip("Runs when the player changes lane.")]
        [SerializeField] UnityEvent onLaneChange;

        [Tooltip("Runs when the runner stops, such as when its health runs out.")]
        [SerializeField] UnityEvent onStopped;

        const float GroundCheckDistance = 0.08f;

        Rigidbody2D body;
        Collider2D ownCollider;
        SpriteRenderer sprite;
        Sprite standingSprite;
        Health health;
        readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

        float speed;
        float startX;
        float runFrom;
        bool running;
        bool stopped;
        bool grounded;
        float lastGroundedTime = -1f;
        float jumpPressedTime = -1f;
        int airJumpsLeft;
        bool jumping;
        bool sliding;
        int lane;
        bool laneStickCentered = true;
        Vector2 standingSize;
        Vector2 standingOffset;

        /// <summary>The current running speed, in units per second.</summary>
        public float Speed => speed;

        /// <summary>How far the speed has ramped, from 0 at the start to 1 at Max Speed. Track Spawner 2D uses it.</summary>
        public float Difficulty => maxSpeed > startSpeed ? Mathf.Clamp01((speed - startSpeed) / (maxSpeed - startSpeed)) : 1f;

        /// <summary>The distance run, in units.</summary>
        public float DistanceRun => transform.position.x - startX;

        /// <summary>True while running.</summary>
        public bool IsRunning => running;

        /// <summary>True while standing on the ground (Jump style).</summary>
        public bool IsGrounded => grounded;

        /// <summary>True while sliding.</summary>
        public bool IsSliding => sliding;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.gravityScale = style == Style.Lanes ? 0f : gravityScale;

            ownCollider = GetComponent<Collider2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            standingSprite = sprite != null ? sprite.sprite : null;
            health = GetComponent<Health>();

            if (ownCollider is BoxCollider2D box)
            {
                standingSize = box.size;
                standingOffset = box.offset;
            }
            else if (ownCollider is CapsuleCollider2D capsule)
            {
                standingSize = capsule.size;
                standingOffset = capsule.offset;
            }

            if (input == null)
            {
                Debug.LogError($"Runner Controller 2D on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
            }
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.JumpPressed += OnJumpPressed;
                input.JumpReleased += OnJumpReleased;
            }

            if (health != null)
            {
                health.Died += Stop;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.JumpPressed -= OnJumpPressed;
                input.JumpReleased -= OnJumpReleased;
            }

            if (health != null)
            {
                health.Died -= Stop;
            }
        }

        void Start()
        {
            startX = transform.position.x;
            speed = startSpeed;
            runFrom = Time.time + startDelay;
            lane = Mathf.Clamp(startLane, 0, Mathf.Max(0, laneHeights.Length - 1));

            if (style == Style.Lanes && laneHeights.Length > 0)
            {
                Vector2 position = body.position;
                position.y = laneHeights[lane];
                body.position = position;
            }
        }

        void OnJumpPressed()
        {
            if (!PauseController.GamePaused && !stopped)
            {
                jumpPressedTime = Time.time;
            }
        }

        void OnJumpReleased()
        {
            if (style == Style.Jump && jumping && body.linearVelocity.y > 0f)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * shortHopSpeedKept);
                jumping = false;
            }
        }

        void Update()
        {
            if (stopped || PauseController.GamePaused)
            {
                return;
            }

            if (!running && Time.time >= runFrom)
            {
                running = true;
            }

            Vector2 move = input != null ? input.Move : Vector2.zero;

            if (style == Style.Lanes)
            {
                ReadLaneInput(move.y);
            }
            else if (style == Style.Jump)
            {
                SetSliding(canSlide && grounded && move.y < -0.5f);
            }

            if (distance != null && running)
            {
                distance.SetValue(Mathf.Max(0, Mathf.FloorToInt(DistanceRun / unitsPerPoint)));
            }
        }

        void FixedUpdate()
        {
            CheckGround();

            if (stopped)
            {
                return;
            }

            if (running)
            {
                speed = Mathf.Min(maxSpeed, speed + speedGainPerSecond * Time.fixedDeltaTime);
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = running ? speed : 0f;

            switch (style)
            {
                case Style.Jump:
                    velocity = JumpStyle(velocity);
                    break;

                case Style.Jetpack:
                    velocity = JetpackStyle(velocity);
                    break;

                case Style.Lanes:
                    velocity = LaneStyle(velocity);
                    break;
            }

            body.linearVelocity = velocity;
        }

        Vector2 JumpStyle(Vector2 velocity)
        {
            bool buffered = jumpPressedTime >= 0f && Time.time - jumpPressedTime <= jumpBufferTime;

            if (buffered)
            {
                bool canGroundJump = !jumping && (grounded || Time.time - lastGroundedTime <= coyoteTime);

                if (canGroundJump || airJumpsLeft > 0)
                {
                    if (!canGroundJump)
                    {
                        airJumpsLeft--;
                    }

                    SetSliding(false);
                    jumpPressedTime = -1f;
                    lastGroundedTime = -1f;
                    jumping = true;
                    velocity.y = Mathf.Sqrt(2f * gravityScale * Mathf.Abs(Physics2D.gravity.y) * jumpHeight);
                    onJump?.Invoke();
                }
            }

            body.gravityScale = gravityScale * (velocity.y < 0f ? fallGravityMultiplier : 1f);
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
            return velocity;
        }

        Vector2 JetpackStyle(Vector2 velocity)
        {
            body.gravityScale = gravityScale;

            if (!stopped && input != null && input.JumpHeld && !PauseController.GamePaused)
            {
                velocity.y = Mathf.Min(velocity.y + thrust * Time.fixedDeltaTime, maxRiseSpeed);
            }

            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
            return velocity;
        }

        Vector2 LaneStyle(Vector2 velocity)
        {
            body.gravityScale = 0f;

            if (laneHeights.Length == 0)
            {
                velocity.y = 0f;
                return velocity;
            }

            float target = laneHeights[lane];
            float laneGap = laneHeights.Length > 1 ? Mathf.Abs(laneHeights[1] - laneHeights[0]) : 1f;
            float laneSpeed = laneGap / laneChangeTime;
            float step = Mathf.MoveTowards(body.position.y, target, laneSpeed * Time.fixedDeltaTime) - body.position.y;
            velocity.y = step / Time.fixedDeltaTime;
            return velocity;
        }

        void ReadLaneInput(float vertical)
        {
            // One lane per push of the stick or key, not one lane per frame.
            if (Mathf.Abs(vertical) < 0.3f)
            {
                laneStickCentered = true;
                return;
            }

            if (!laneStickCentered)
            {
                return;
            }

            laneStickCentered = false;
            int next = Mathf.Clamp(lane + (vertical > 0f ? 1 : -1), 0, laneHeights.Length - 1);

            if (next != lane)
            {
                lane = next;
                onLaneChange?.Invoke();
            }
        }

        /// <summary>Moves to a lane, counting from 0 at the bottom. Lanes style only.</summary>
        public void GoToLane(int newLane)
        {
            lane = Mathf.Clamp(newLane, 0, Mathf.Max(0, laneHeights.Length - 1));
        }

        void CheckGround()
        {
            if (style == Style.Lanes || ownCollider == null)
            {
                grounded = false;
                return;
            }

            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(groundLayers);

            bool wasGrounded = grounded;
            grounded = body.linearVelocity.y <= 0.01f && ownCollider.Cast(Vector2.down, filter, groundHits, GroundCheckDistance) > 0;

            if (grounded)
            {
                lastGroundedTime = Time.time;
                airJumpsLeft = extraAirJumps;

                if (!wasGrounded)
                {
                    jumping = false;
                    onLand?.Invoke();
                }
            }
        }

        void SetSliding(bool value)
        {
            if (sliding == value || ownCollider == null || standingSize == Vector2.zero)
            {
                return;
            }

            sliding = value;

            // Shrink from the top, keeping the feet where they are.
            Vector2 size = standingSize;
            Vector2 offset = standingOffset;

            if (sliding)
            {
                size.y = standingSize.y * slideHeight;
                offset.y = standingOffset.y - (standingSize.y - size.y) / 2f;
            }

            if (ownCollider is BoxCollider2D box)
            {
                box.size = size;
                box.offset = offset;
            }
            else if (ownCollider is CapsuleCollider2D capsule)
            {
                capsule.size = size;
                capsule.offset = offset;
            }

            if (sprite != null && slideSprite != null)
            {
                sprite.sprite = sliding ? slideSprite : standingSprite;
            }
        }

        /// <summary>Stops running for good, such as when the player's health runs out.</summary>
        public void Stop()
        {
            if (stopped)
            {
                return;
            }

            stopped = true;
            running = false;
            SetSliding(false);
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
            onStopped?.Invoke();
        }

        void OnDrawGizmosSelected()
        {
            if (style != Style.Lanes || laneHeights == null)
            {
                return;
            }

            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.7f);
            float x = transform.position.x;

            foreach (float height in laneHeights)
            {
                Gizmos.DrawLine(new Vector3(x - 3f, height, 0f), new Vector3(x + 20f, height, 0f));
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
