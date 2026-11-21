using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// A ball that bounces around at a steady speed, for Breakout and Pong.
    ///
    /// Where it hits a Paddle 2D decides where it goes: the middle sends it
    /// straight back, the ends send it off at an angle. It never travels so
    /// flat that it would bounce between two walls forever.
    ///
    /// Before each serve the ball waits: on a paddle (set Serve From), or at its
    /// starting point. It is served with the Jump or Attack button, or by
    /// itself after Auto Serve seconds.
    ///
    /// Setup: add this to a round sprite with a Circle Collider 2D. A Rigidbody 2D
    /// is added and set up for you.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Ball 2D")]
    public class Ball2D : MonoBehaviour
    {
        /// <summary>Which way the ball should mostly travel.</summary>
        public enum PlayDirection
        {
            /// <summary>Mostly up and down, as in Breakout.</summary>
            UpAndDown,

            /// <summary>Mostly side to side, as in Pong.</summary>
            SideToSide
        }

        [Tooltip("Which way the ball should mostly travel. It's never allowed to travel too close to the other way.")]
        [SerializeField] PlayDirection playDirection = PlayDirection.UpAndDown;

        [Header("Speed")]

        [Tooltip("Speed at each serve, in units per second.")]
        [SerializeField, Min(0.1f)] float startSpeed = 8f;

        [Tooltip("Speed added each time it hits a paddle.")]
        [SerializeField, Min(0f)] float speedUpPerPaddleHit = 0.3f;

        [Tooltip("The fastest it can go.")]
        [SerializeField, Min(0.1f)] float maxSpeed = 16f;

        [Header("Bouncing")]

        [Tooltip("The steepest angle it leaves a paddle at, when it hits the very end, in degrees.")]
        [SerializeField, Range(10f, 80f)] float maxBounceAngle = 60f;

        [Tooltip("The flattest angle it may travel at, in degrees. Keeps it from bouncing between two walls forever.")]
        [SerializeField, Range(0f, 45f)] float minAngle = 15f;

        [Header("Serving")]

        [Tooltip("Optional. The ball waits on this object, usually the paddle, until it is served. Leave empty to wait at its starting point.")]
        [SerializeField] Transform serveFrom;

        [Tooltip("Where the ball waits, relative to Serve From.")]
        [SerializeField] Vector2 serveOffset = new Vector2(0f, 0.5f);

        [Tooltip("The direction of each serve.")]
        [SerializeField] Vector2 serveDirection = new Vector2(0.3f, 1f);

        [Tooltip("Ticked: each serve randomly goes left or right (and up or down, for Side To Side).")]
        [SerializeField] bool randomServeSide = true;

        [Tooltip("The Input Reader asset. Jump or Attack serves the ball. Leave empty to serve only with Auto Serve.")]
        [SerializeField] InputReader input;

        [Tooltip("Serve by itself after this many seconds. 0 means wait for the player.")]
        [SerializeField, Min(0f)] float autoServe = 0f;

        [Header("Events")]

        [Tooltip("Runs at each serve.")]
        [SerializeField] UnityEvent onServe;

        [Tooltip("Runs when it hits a paddle. Use it for a sound.")]
        [SerializeField] UnityEvent onPaddleHit;

        [Tooltip("Runs when it hits anything else. Use it for a sound.")]
        [SerializeField] UnityEvent onBounce;

        Rigidbody2D body;
        Vector3 startPosition;
        Vector2 nextServeDirection;
        float speed;
        float serveAt;
        bool resetPending;
        Vector2 pendingDirection;

        /// <summary>True while the ball is moving, false while it waits to be served.</summary>
        public bool InPlay { get; private set; }

        /// <summary>The ball's current speed.</summary>
        public float Speed => speed;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // No friction and a perfect bounce, so the ball never slows or sticks.
            body.sharedMaterial = new PhysicsMaterial2D("Ball Bounce") { friction = 0f, bounciness = 1f };

            startPosition = transform.position;
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.JumpPressed += ServeFromInput;
                input.AttackPressed += ServeFromInput;
            }

            ApplyReset(Vector2.zero);
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.JumpPressed -= ServeFromInput;
                input.AttackPressed -= ServeFromInput;
            }
        }

        void Update()
        {
            ApplyPendingReset();

            if (!InPlay && autoServe > 0f && Time.time >= serveAt)
            {
                Serve();
            }
        }

        void FixedUpdate()
        {
            ApplyPendingReset();

            if (!InPlay)
            {
                FollowServePoint();
                return;
            }

            Vector2 direction = body.linearVelocity.sqrMagnitude > 0.0001f ? body.linearVelocity.normalized : nextServeDirection;
            body.linearVelocity = KeepOffFlat(direction) * speed;
        }

        void LateUpdate()
        {
            if (!InPlay)
            {
                FollowServePoint();
            }
        }

        void FollowServePoint()
        {
            Vector3 point = serveFrom != null ? serveFrom.position + (Vector3)serveOffset : startPosition;
            transform.position = point;
        }

        void ServeFromInput()
        {
            if (!InPlay && !PauseController.GamePaused)
            {
                Serve();
            }
        }

        /// <summary>Puts the ball back to wait for a serve, at its start speed.</summary>
        public void ResetBall()
        {
            ResetBall(Vector2.zero);
        }

        /// <summary>Puts the ball back to wait for a serve, which will go in this direction. Zero uses Serve Direction.</summary>
        public void ResetBall(Vector2 direction)
        {
            // Stops counting as in play at once, and moves back on the next step.
            // A Ball Goal 2D calls this from inside a physics callback, where the
            // Rigidbody shouldn't be switched off.
            InPlay = false;
            resetPending = true;
            pendingDirection = direction;
        }

        void ApplyPendingReset()
        {
            if (resetPending)
            {
                resetPending = false;
                ApplyReset(pendingDirection);
            }
        }

        void ApplyReset(Vector2 direction)
        {
            resetPending = false;
            InPlay = false;
            speed = startSpeed;
            body.simulated = false;
            body.linearVelocity = Vector2.zero;
            serveAt = Time.time + autoServe;

            Vector2 chosen = direction.sqrMagnitude > 0.0001f ? direction : serveDirection;

            if (randomServeSide && direction.sqrMagnitude <= 0.0001f)
            {
                if (Random.value < 0.5f)
                {
                    chosen.x = -chosen.x;
                }

                if (playDirection == PlayDirection.SideToSide && Random.value < 0.5f)
                {
                    chosen.y = -chosen.y;
                }
            }

            nextServeDirection = chosen.sqrMagnitude > 0.0001f ? chosen.normalized : Vector2.up;
            FollowServePoint();
        }

        /// <summary>Sends the ball off, if it is waiting.</summary>
        public void Serve()
        {
            if (InPlay)
            {
                return;
            }

            ApplyPendingReset();
            InPlay = true;
            body.simulated = true;
            body.linearVelocity = KeepOffFlat(nextServeDirection) * speed;
            onServe?.Invoke();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            Paddle2D paddle = collision.collider.GetComponentInParent<Paddle2D>();

            if (paddle == null)
            {
                onBounce?.Invoke();
                return;
            }

            speed = Mathf.Min(maxSpeed, speed + speedUpPerPaddleHit);
            body.linearVelocity = BounceOff(paddle) * speed;
            onPaddleHit?.Invoke();
        }

        // Where the ball hits the paddle decides the angle: the middle is straight, the ends are steep.
        Vector2 BounceOff(Paddle2D paddle)
        {
            Vector2 offset = body.position - (Vector2)paddle.transform.position;
            float angle;
            Vector2 direction;

            if (paddle.MovesAlong == Paddle2D.Axis.Horizontal)
            {
                angle = Mathf.Clamp(offset.x / paddle.HalfLength, -1f, 1f) * maxBounceAngle * Mathf.Deg2Rad;
                float away = offset.y >= 0f ? 1f : -1f;
                direction = new Vector2(Mathf.Sin(angle), away * Mathf.Cos(angle));
            }
            else
            {
                angle = Mathf.Clamp(offset.y / paddle.HalfLength, -1f, 1f) * maxBounceAngle * Mathf.Deg2Rad;
                float away = offset.x >= 0f ? 1f : -1f;
                direction = new Vector2(away * Mathf.Cos(angle), Mathf.Sin(angle));
            }

            return KeepOffFlat(direction);
        }

        // Keeps the direction at least Min Angle away from the flat direction.
        Vector2 KeepOffFlat(Vector2 direction)
        {
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
            float least = Mathf.Sin(minAngle * Mathf.Deg2Rad);

            if (playDirection == PlayDirection.UpAndDown && Mathf.Abs(direction.y) < least)
            {
                float signY = direction.y >= 0f ? 1f : -1f;
                float signX = direction.x >= 0f ? 1f : -1f;
                direction = new Vector2(signX * Mathf.Sqrt(1f - least * least), signY * least);
            }
            else if (playDirection == PlayDirection.SideToSide && Mathf.Abs(direction.x) < least)
            {
                float signX = direction.x >= 0f ? 1f : -1f;
                float signY = direction.y >= 0f ? 1f : -1f;
                direction = new Vector2(signX * least, signY * Mathf.Sqrt(1f - least * least));
            }

            return direction;
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
