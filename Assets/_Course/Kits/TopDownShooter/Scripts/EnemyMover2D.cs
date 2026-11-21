using UnityEngine;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// Moves a shooter enemy in one of a few classic patterns:
    ///   Chase       heads straight for the player. Swarmers in an arena.
    ///   Straight    flies one way and never turns. Scrolling-shooter fodder.
    ///   Wave        flies one way, weaving from side to side.
    ///   Circle      circles the player at a distance, like a gunship.
    ///   Keep Away   stays at a distance from the player, backing off if too close.
    ///
    /// Setup: add to a sprite with a 2D collider. A Rigidbody 2D is added and
    /// set up for you (no gravity). Give it Health and Damage On Touch to make it fight.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Enemy Mover 2D")]
    public class EnemyMover2D : MonoBehaviour
    {
        /// <summary>How the enemy moves.</summary>
        public enum Pattern
        {
            Chase,
            Straight,
            Wave,
            Circle,
            KeepAway
        }

        [Tooltip("How the enemy moves.")]
        [SerializeField] Pattern pattern = Pattern.Chase;

        [Tooltip("Speed, in units per second.")]
        [SerializeField, Min(0f)] float speed = 2.5f;

        [Tooltip("The tag of the player, for Chase, Circle and Keep Away.")]
        [SerializeField] string targetTag = "Player";

        [Tooltip("Straight and Wave: the direction to fly, such as down for a scrolling shooter.")]
        [SerializeField] Vector2 flyDirection = Vector2.down;

        [Header("Wave")]

        [Tooltip("How far it weaves to each side, in units.")]
        [SerializeField, Min(0f)] float waveWidth = 1.5f;

        [Tooltip("Weaves per second.")]
        [SerializeField, Min(0f)] float wavesPerSecond = 0.5f;

        [Header("Circle and Keep Away")]

        [Tooltip("The distance it tries to keep from the player, in units.")]
        [SerializeField, Min(0f)] float preferredDistance = 4f;

        [Header("Looks")]

        [Tooltip("Ticked: the enemy turns to face the way it moves. Its sprite should point up.")]
        [SerializeField] bool faceMovement = false;

        Rigidbody2D body;
        Transform target;
        float startTime;
        float nextSearch;
        int circleDirection = 1;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = !faceMovement;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void OnEnable()
        {
            startTime = Time.time;
            circleDirection = Random.value < 0.5f ? 1 : -1;
        }

        void FixedUpdate()
        {
            Vector2 velocity = DesiredVelocity();
            body.linearVelocity = velocity;

            if (faceMovement && velocity.sqrMagnitude > 0.01f)
            {
                body.MoveRotation(Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f);
            }
        }

        Vector2 DesiredVelocity()
        {
            Vector2 fly = flyDirection.sqrMagnitude > 0.0001f ? flyDirection.normalized : Vector2.down;

            switch (pattern)
            {
                case Pattern.Straight:
                    return fly * speed;

                case Pattern.Wave:
                {
                    // Forward, plus a side-to-side swing whose speed follows a cosine.
                    Vector2 side = new Vector2(-fly.y, fly.x);
                    float t = (Time.time - startTime) * wavesPerSecond * Mathf.PI * 2f;
                    float sideSpeed = Mathf.Cos(t) * waveWidth * wavesPerSecond * Mathf.PI * 2f;
                    return fly * speed + side * sideSpeed;
                }

                case Pattern.Chase:
                {
                    Vector2? toTarget = ToTarget();
                    return toTarget.HasValue ? toTarget.Value.normalized * speed : Vector2.zero;
                }

                case Pattern.Circle:
                case Pattern.KeepAway:
                {
                    Vector2? toTarget = ToTarget();

                    if (!toTarget.HasValue)
                    {
                        return Vector2.zero;
                    }

                    Vector2 offset = toTarget.Value;
                    float distance = offset.magnitude;
                    Vector2 toward = distance > 0.001f ? offset / distance : Vector2.up;

                    // Move in or out to hold the preferred distance.
                    float inOut = Mathf.Clamp(distance - preferredDistance, -1f, 1f);
                    Vector2 velocity = toward * inOut;

                    if (pattern == Pattern.Circle)
                    {
                        velocity += new Vector2(-toward.y, toward.x) * circleDirection;
                    }

                    return velocity.sqrMagnitude > 1f ? velocity.normalized * speed : velocity * speed;
                }
            }

            return Vector2.zero;
        }

        Vector2? ToTarget()
        {
            if ((target == null || !target.gameObject.activeInHierarchy) && Time.time >= nextSearch && !string.IsNullOrEmpty(targetTag))
            {
                nextSearch = Time.time + 0.5f;

                try
                {
                    GameObject found = GameObject.FindWithTag(targetTag);
                    target = found != null ? found.transform : null;
                }
                catch (UnityException)
                {
                    target = null;
                }
            }

            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return null;
            }

            return (Vector2)target.position - body.position;
        }

        void OnDrawGizmosSelected()
        {
            if (pattern == Pattern.Circle || pattern == Pattern.KeepAway)
            {
                Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
                Gizmos.DrawWireSphere(transform.position, preferredDistance);
            }
            else if (pattern == Pattern.Straight || pattern == Pattern.Wave)
            {
                Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.9f);
                Gizmos.DrawLine(transform.position, (Vector2)transform.position + flyDirection.normalized * 2f);
            }
        }
    }
}
