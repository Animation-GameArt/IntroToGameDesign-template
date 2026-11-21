using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;

namespace CollinGame.ThreeD
{
    /// <summary>
    /// A platform that moves between points, carrying a Player Controller 3D
    /// standing on it. It can wait at each point, go back and forth or around
    /// in a loop, and start only when something calls StartMoving (such as a
    /// Trigger Zone the player steps on).
    ///
    /// Setup: give it a solid collider. Add empty child objects for the
    /// points, then drag them into Points; the platform starts at the first.
    /// Their positions are remembered when the game starts, so it doesn't
    /// matter that, as children, they move with the platform.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("Collin Game/3D/Moving Platform 3D")]
    public class MovingPlatform3D : MonoBehaviour
    {
        public enum PathMode
        {
            BackAndForth,
            Loop
        }

        [Tooltip("The points to visit, in order.")]
        [SerializeField] List<Transform> points = new List<Transform>();

        [SerializeField] PathMode mode = PathMode.BackAndForth;

        [Tooltip("Units per second.")]
        [SerializeField, Min(0.01f)] float speed = 3f;

        [Tooltip("Seconds to wait at each point.")]
        [SerializeField, Min(0f)] float waitTime = 0.5f;

        [Tooltip("Ticked: it waits at the first point until StartMoving is called.")]
        [SerializeField] bool waitForStart;

        readonly List<Vector3> positions = new List<Vector3>();
        int target = 1;
        int direction = 1;
        float waitUntil;
        bool moving;

        /// <summary>How far the platform moved this frame. The player controller adds it to the player.</summary>
        public Vector3 Delta { get; private set; }

        void Start()
        {
            foreach (Transform point in points)
            {
                if (point != null)
                {
                    positions.Add(point.position);
                }
            }

            if (positions.Count > 0)
            {
                transform.position = positions[0];
            }

            moving = !waitForStart;
            waitUntil = Time.time + waitTime;
        }

        /// <summary>Starts a platform set to Wait For Start.</summary>
        public void StartMoving()
        {
            moving = true;
        }

        /// <summary>Stops the platform where it is.</summary>
        public void StopMoving()
        {
            moving = false;
        }

        void Update()
        {
            Delta = Vector3.zero;

            if (!moving || positions.Count < 2 || Time.time < waitUntil || PauseController.GamePaused)
            {
                return;
            }

            Vector3 before = transform.position;
            Vector3 goal = positions[target];
            transform.position = Vector3.MoveTowards(before, goal, speed * Time.deltaTime);
            Delta = transform.position - before;

            if ((transform.position - goal).sqrMagnitude < 0.0001f)
            {
                waitUntil = Time.time + waitTime;
                NextTarget();
            }
        }

        void NextTarget()
        {
            if (mode == PathMode.Loop)
            {
                target = (target + 1) % positions.Count;
                return;
            }

            if (target + direction >= positions.Count || target + direction < 0)
            {
                direction = -direction;
            }

            target += direction;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;

            for (int i = 0; i + 1 < points.Count; i++)
            {
                if (points[i] != null && points[i + 1] != null)
                {
                    Gizmos.DrawLine(points[i].position, points[i + 1].position);
                }
            }

            if (mode == PathMode.Loop && points.Count > 2 && points[0] != null && points[points.Count - 1] != null)
            {
                Gizmos.DrawLine(points[points.Count - 1].position, points[0].position);
            }
        }
    }
}
