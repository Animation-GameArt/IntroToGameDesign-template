using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// Moves the camera steadily through a level, as in a scrolling shooter
    /// such as Galaga's descendants, 1942 or Raiden. Put Keep On Screen 2D on
    /// the player so they're carried along.
    ///
    /// Setup: add to the Main Camera (no Cinemachine camera is needed). The
    /// camera stops when it reaches Stop After.
    /// </summary>
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Scroll Camera 2D")]
    public class ScrollCamera2D : MonoBehaviour
    {
        [Tooltip("The direction the camera moves, such as up.")]
        [SerializeField] Vector2 direction = Vector2.up;

        [Tooltip("Scroll speed, in units per second.")]
        [SerializeField, Min(0f)] float speed = 2f;

        [Tooltip("How far the camera travels before it stops, in units. 0 means it never stops.")]
        [SerializeField, Min(0f)] float stopAfter = 100f;

        [Tooltip("Seconds before scrolling starts.")]
        [SerializeField, Min(0f)] float startDelay = 1f;

        [Tooltip("Optional. An event to raise when the camera reaches the end, such as for a boss.")]
        [SerializeField] VoidEventChannel raiseAtEnd;

        [Tooltip("Runs when the camera reaches the end.")]
        [SerializeField] UnityEvent onReachedEnd;

        Vector3 start;
        float travelled;
        float startAt;
        bool finished;

        /// <summary>How far the camera has travelled, in units.</summary>
        public float Travelled => travelled;

        /// <summary>True while scrolling is paused by Pause Scrolling.</summary>
        public bool Paused { get; private set; }

        void Start()
        {
            start = transform.position;
            startAt = Time.time + startDelay;
        }

        void Update()
        {
            if (finished || Paused || Time.time < startAt)
            {
                return;
            }

            travelled += speed * Time.deltaTime;

            if (stopAfter > 0f && travelled >= stopAfter)
            {
                travelled = stopAfter;
                finished = true;

                if (raiseAtEnd != null)
                {
                    raiseAtEnd.Raise();
                }

                onReachedEnd?.Invoke();
            }

            Vector2 move = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
            transform.position = start + (Vector3)(move * travelled);
        }

        /// <summary>Stops scrolling, such as while a mid-level boss fights.</summary>
        public void PauseScrolling() => Paused = true;

        /// <summary>Carries on scrolling.</summary>
        public void ResumeScrolling() => Paused = false;

        void OnDrawGizmosSelected()
        {
            if (stopAfter <= 0f)
            {
                return;
            }

            Vector3 from = Application.isPlaying ? start : transform.position;
            Vector3 to = from + (Vector3)(direction.normalized * stopAfter);
            Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.9f);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireCube(to, new Vector3(1f, 1f, 0f));
        }
    }
}
