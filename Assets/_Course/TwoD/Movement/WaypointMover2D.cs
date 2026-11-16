using UnityEngine;

namespace CollinGame.TwoD
{
    /// <summary>
    /// Moves an object along a path, over and over: a moving platform, a flying
    /// enemy, a saw blade.
    ///
    /// The path is a list of Stops. Each stop is a position measured from where
    /// the object starts, so a single stop of (4, 0) means "go 4 units to the
    /// right, then come back". Select the object to see its path in the Scene view.
    ///
    /// A character using the Platformer Controller 2D is carried along when it
    /// stands on one of these.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/2D/Waypoint Mover 2D")]
    public class WaypointMover2D : MonoBehaviour
    {
        /// <summary>What happens at the end of the path.</summary>
        public enum PathMode
        {
            /// <summary>Go to the last stop, then back the same way.</summary>
            BackAndForth,

            /// <summary>Go to the last stop, then straight to the start, in a circuit.</summary>
            Loop
        }

        [Tooltip("The places to visit, in order, measured from where the object starts.")]
        [SerializeField] Vector2[] stops = { new Vector2(4f, 0f) };

        [Tooltip("What happens at the end of the path.")]
        [SerializeField] PathMode pathMode = PathMode.BackAndForth;

        [Tooltip("How fast it moves, in units per second.")]
        [SerializeField, Min(0f)] float speed = 2f;

        [Tooltip("How long it waits at each stop, in seconds.")]
        [SerializeField, Min(0f)] float waitAtStops = 0.5f;

        Rigidbody2D body;
        Vector2 startPosition;
        int targetIndex;      // 0 is the start; 1 and up are the stops.
        int direction = 1;
        float waitTimer;
        bool started;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();

            // A kinematic body moves exactly as told and isn't pushed around,
            // which is what a platform or a patrolling hazard needs.
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void OnEnable()
        {
            startPosition = transform.position;
            targetIndex = stops != null && stops.Length > 0 ? 1 : 0;
            direction = 1;
            waitTimer = 0f;
            started = true;
        }

        void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        void FixedUpdate()
        {
            if (stops == null || stops.Length == 0 || speed <= 0f)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            float step = Time.fixedDeltaTime;

            if (waitTimer > 0f)
            {
                waitTimer -= step;
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 toTarget = PointAt(targetIndex) - body.position;
            float distance = toTarget.magnitude;

            if (distance <= speed * step)
            {
                // Close enough to arrive on this step: go exactly there, then pick the next stop.
                body.linearVelocity = toTarget / step;
                ChooseNextTarget();
                waitTimer = waitAtStops;
            }
            else
            {
                body.linearVelocity = toTarget / distance * speed;
            }
        }

        // Point 0 is the start; point 1 is the first stop, and so on.
        Vector2 PointAt(int index)
        {
            return index <= 0 ? startPosition : startPosition + stops[index - 1];
        }

        void ChooseNextTarget()
        {
            int lastIndex = stops.Length;

            if (pathMode == PathMode.Loop)
            {
                targetIndex = targetIndex >= lastIndex ? 0 : targetIndex + 1;
                return;
            }

            if (targetIndex >= lastIndex)
            {
                direction = -1;
            }
            else if (targetIndex <= 0)
            {
                direction = 1;
            }

            targetIndex += direction;
        }

        // Shows the path in the Scene view while this object is selected.
        void OnDrawGizmosSelected()
        {
            if (stops == null || stops.Length == 0)
            {
                return;
            }

            Vector2 origin = started ? startPosition : (Vector2)transform.position;
            Vector2 previous = origin;

            Gizmos.color = Color.cyan;

            for (int i = 0; i < stops.Length; i++)
            {
                Vector2 point = origin + stops[i];
                Gizmos.DrawLine(previous, point);
                Gizmos.DrawWireSphere(point, 0.15f);
                previous = point;
            }

            if (pathMode == PathMode.Loop)
            {
                Gizmos.DrawLine(previous, origin);
            }

            Gizmos.DrawWireSphere(origin, 0.15f);
        }
    }
}
