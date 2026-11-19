using CollinGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// A paddle that slides along one line: left and right at the bottom of the
    /// screen for Breakout, or up and down at the side for Pong.
    ///
    ///   Player     moves with the Move input (keys, stick), or follows the mouse.
    ///   Computer   follows the ball, a little slower than the ball moves.
    ///
    /// Setup: add this to a sprite with a Box Collider 2D. A Rigidbody 2D is
    /// added for you and set to Kinematic, so the ball can't push the paddle.
    /// Select it in the Scene view to see how far it can move.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Paddle 2D")]
    public class Paddle2D : MonoBehaviour
    {
        /// <summary>The line the paddle moves along.</summary>
        public enum Axis
        {
            /// <summary>Left and right, as in Breakout.</summary>
            Horizontal,

            /// <summary>Up and down, as in Pong.</summary>
            Vertical
        }

        /// <summary>Who moves the paddle.</summary>
        public enum Controller
        {
            Player,
            Computer
        }

        [Tooltip("The line the paddle moves along.")]
        [SerializeField] Axis movesAlong = Axis.Horizontal;

        [Tooltip("Who moves the paddle.")]
        [SerializeField] Controller controlledBy = Controller.Player;

        [Tooltip("Needed when Controlled By is Player. The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("Ticked: the paddle also follows the mouse, whenever the mouse moves.")]
        [SerializeField] bool followMouse = true;

        [Header("Moving")]

        [Tooltip("Top speed, in units per second. The computer moves at this speed times Computer Speed.")]
        [SerializeField, Min(0f)] float speed = 12f;

        [Tooltip("The lowest position of the paddle's middle along its line (left or bottom), in world units.")]
        [SerializeField] float minPosition = -7f;

        [Tooltip("The highest position of the paddle's middle along its line (right or top), in world units.")]
        [SerializeField] float maxPosition = 7f;

        [Header("Computer")]

        [Tooltip("The computer's speed, as a share of Speed. Lower makes it easier to beat.")]
        [SerializeField, Range(0.1f, 1.5f)] float computerSpeed = 0.6f;

        [Tooltip("The computer doesn't move when the ball is this close to its middle, in units. Higher makes it lazier.")]
        [SerializeField, Min(0f)] float computerDeadZone = 0.3f;

        Rigidbody2D body;
        Collider2D paddleCollider;
        Ball2D ball;
        Camera mainCamera;

        /// <summary>The line the paddle moves along.</summary>
        public Axis MovesAlong => movesAlong;

        /// <summary>Half the paddle's length along its line, in units. The ball uses it to choose its bounce angle.</summary>
        public float HalfLength
        {
            get
            {
                if (paddleCollider == null)
                {
                    return 0.5f;
                }

                Vector3 size = paddleCollider.bounds.size;
                return Mathf.Max(0.01f, (movesAlong == Axis.Horizontal ? size.x : size.y) * 0.5f);
            }
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            paddleCollider = GetComponent<Collider2D>();
            mainCamera = Camera.main;

            if (paddleCollider == null)
            {
                Debug.LogWarning($"Paddle 2D on \"{name}\": there is no 2D collider, so the ball will pass through. Add a Box Collider 2D.", this);
            }

            if (controlledBy == Controller.Player && input == null)
            {
                Debug.LogError($"Paddle 2D on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
            }
        }

        void FixedUpdate()
        {
            float current = Along(body.position);
            float target = controlledBy == Controller.Player ? PlayerTarget(current) : ComputerTarget(current);
            float maxStep = (controlledBy == Controller.Player ? speed : speed * computerSpeed) * Time.fixedDeltaTime;

            float next = Mathf.Clamp(Mathf.MoveTowards(current, target, maxStep), minPosition, maxPosition);
            body.MovePosition(WithAlong(body.position, next));
        }

        float PlayerTarget(float current)
        {
            if (PauseController.GamePaused)
            {
                return current;
            }

            if (followMouse && Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f && mainCamera != null)
            {
                Vector3 world = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                return Along(world);
            }

            if (input == null)
            {
                return current;
            }

            Vector2 move = input.Move;
            float amount = movesAlong == Axis.Horizontal ? move.x : move.y;

            // Far enough that MoveTowards moves at full speed this step.
            return current + amount * speed;
        }

        float ComputerTarget(float current)
        {
            if (ball == null || !ball.isActiveAndEnabled)
            {
                ball = FindFirstObjectByType<Ball2D>();
            }

            if (ball == null || !ball.InPlay)
            {
                return current;
            }

            float ballPosition = Along(ball.transform.position);
            return Mathf.Abs(ballPosition - current) <= computerDeadZone ? current : ballPosition;
        }

        float Along(Vector2 point) => movesAlong == Axis.Horizontal ? point.x : point.y;

        Vector2 WithAlong(Vector2 point, float value)
        {
            if (movesAlong == Axis.Horizontal)
            {
                point.x = value;
            }
            else
            {
                point.y = value;
            }

            return point;
        }

        void OnDrawGizmosSelected()
        {
            Vector3 from = transform.position;
            Vector3 to = transform.position;

            if (movesAlong == Axis.Horizontal)
            {
                from.x = minPosition;
                to.x = maxPosition;
            }
            else
            {
                from.y = minPosition;
                to.y = maxPosition;
            }

            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(from, 0.1f);
            Gizmos.DrawWireSphere(to, 0.1f);
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
