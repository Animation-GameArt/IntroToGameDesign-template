using UnityEngine;

namespace CollinGame.Kits.TopDownAdventure
{
    /// <summary>
    /// A top-down enemy that wanders near its starting point, chases the player
    /// when the player comes close, and goes home when the player gets away.
    ///
    /// It is always in one of three states:
    ///   Wander   drifts around its home point, with short rests.
    ///   Chase    moves toward the player.
    ///   Return   walks back home after losing the player.
    ///
    /// Setup: add this to a GameObject with a 2D collider. A Rigidbody 2D is
    /// added for you, and gravity is switched off. Add Damage On Touch to make
    /// it hurt the player, and Health and Knockback 2D to make it fight back.
    ///
    /// Select it in the Scene view to see its ranges.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/Top-Down Adventure Kit/Top Down Chaser 2D")]
    public class TopDownChaser2D : MonoBehaviour
    {
        /// <summary>What the enemy is doing right now.</summary>
        public enum State
        {
            Wander,
            Chase,
            Return
        }

        [Header("Wander")]

        [Tooltip("Speed while wandering, in units per second. 0 means it stands still until it sees the player.")]
        [SerializeField, Min(0f)] float wanderSpeed = 1f;

        [Tooltip("How far from home it wanders, in units.")]
        [SerializeField, Min(0f)] float wanderRadius = 2f;

        [Tooltip("Seconds it rests between wandering moves.")]
        [SerializeField, Min(0f)] float restTime = 1f;

        [Header("Chase")]

        [Tooltip("The tag of the object to chase.")]
        [SerializeField] string chaseTag = "Player";

        [Tooltip("Starts chasing when the target is closer than this, in units.")]
        [SerializeField, Min(0f)] float noticeRange = 4f;

        [Tooltip("Gives up when the target is farther than this, in units.")]
        [SerializeField, Min(0f)] float giveUpRange = 7f;

        [Tooltip("Speed while chasing, in units per second.")]
        [SerializeField, Min(0f)] float chaseSpeed = 2.5f;

        [Tooltip("Ticked: walls block its view, so it only notices a target it can see. Walls are anything solid on the Sight Blockers layers.")]
        [SerializeField] bool needsLineOfSight = true;

        [Tooltip("Which layers block its view. Everything works to start with.")]
        [SerializeField] LayerMask sightBlockers = ~0;

        [Header("Moving")]

        [Tooltip("How quickly it changes speed and direction.")]
        [SerializeField, Min(0f)] float acceleration = 20f;

        [Header("Looks")]

        [Tooltip("Optional. This sprite is flipped to face the way it moves. Leave empty to use the first sprite found on this object.")]
        [SerializeField] SpriteRenderer spriteToFlip;

        const float TargetSearchInterval = 0.5f;
        const float ArriveDistance = 0.1f;

        Rigidbody2D body;
        Vector2 home;
        Vector2 wanderPoint;
        float restUntil;
        Transform target;
        float nextTargetSearch;
        bool tagProblemReported;
        readonly RaycastHit2D[] sightHits = new RaycastHit2D[8];

        /// <summary>What the enemy is doing right now.</summary>
        public State CurrentState { get; private set; } = State.Wander;

        /// <summary>The point it wanders around and returns to.</summary>
        public Vector2 Home => home;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (spriteToFlip == null)
            {
                spriteToFlip = GetComponentInChildren<SpriteRenderer>();
            }

            home = transform.position;
            wanderPoint = home;
        }

        void OnEnable()
        {
            CurrentState = State.Wander;
            restUntil = Time.time + restTime;
        }

        void OnDisable()
        {
            // Stop at once when switched off, unless something else, such as a knockback, is moving it.
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic && GetComponent<Knockback2D>() == null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        void FixedUpdate()
        {
            FindTarget();
            UpdateState();

            Vector2 desired = Vector2.zero;

            switch (CurrentState)
            {
                case State.Chase:
                    desired = DirectionTo(target.position) * chaseSpeed;
                    break;

                case State.Return:
                    desired = DirectionTo(home) * chaseSpeed;
                    break;

                case State.Wander:
                    desired = Wander();
                    break;
            }

            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, desired, acceleration * Time.fixedDeltaTime);

            if (spriteToFlip != null && Mathf.Abs(desired.x) > 0.01f)
            {
                spriteToFlip.flipX = desired.x < 0f;
            }
        }

        void UpdateState()
        {
            float distance = target != null ? Vector2.Distance(body.position, target.position) : float.MaxValue;

            switch (CurrentState)
            {
                case State.Wander:
                case State.Return:
                    if (target != null && distance <= noticeRange && CanSee(target))
                    {
                        CurrentState = State.Chase;
                    }
                    else if (CurrentState == State.Return && Vector2.Distance(body.position, home) <= ArriveDistance * 3f)
                    {
                        CurrentState = State.Wander;
                        wanderPoint = body.position;
                        restUntil = Time.time + restTime;
                    }
                    break;

                case State.Chase:
                    if (target == null || !target.gameObject.activeInHierarchy || distance > giveUpRange)
                    {
                        CurrentState = State.Return;
                    }
                    break;
            }
        }

        Vector2 Wander()
        {
            if (wanderSpeed <= 0f || Time.time < restUntil)
            {
                return Vector2.zero;
            }

            if (Vector2.Distance(body.position, wanderPoint) <= ArriveDistance)
            {
                // Arrived: rest, then pick a new point near home.
                wanderPoint = home + Random.insideUnitCircle * wanderRadius;
                restUntil = Time.time + restTime;
                return Vector2.zero;
            }

            return DirectionTo(wanderPoint) * wanderSpeed;
        }

        Vector2 DirectionTo(Vector2 point)
        {
            Vector2 offset = point - body.position;
            return offset.sqrMagnitude > ArriveDistance * ArriveDistance ? offset.normalized : Vector2.zero;
        }

        bool CanSee(Transform other)
        {
            if (!needsLineOfSight)
            {
                return true;
            }

            Vector2 from = body.position;
            Vector2 to = other.position;
            ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(sightBlockers);

            int count = Physics2D.Linecast(from, to, filter, sightHits);

            for (int i = 0; i < count; i++)
            {
                Rigidbody2D hitBody = sightHits[i].rigidbody;

                // Its own body and the target's body don't block the view.
                if (hitBody == body || sightHits[i].transform.IsChildOf(transform))
                {
                    continue;
                }

                if (sightHits[i].transform == other || sightHits[i].transform.IsChildOf(other))
                {
                    return true;
                }

                // Other moving things, such as other enemies, don't block the view; walls do.
                if (hitBody != null && hitBody.bodyType == RigidbodyType2D.Dynamic)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        void FindTarget()
        {
            if (target != null && target.gameObject.activeInHierarchy)
            {
                return;
            }

            if (Time.time < nextTargetSearch || string.IsNullOrEmpty(chaseTag))
            {
                return;
            }

            nextTargetSearch = Time.time + TargetSearchInterval;

            try
            {
                GameObject found = GameObject.FindWithTag(chaseTag);
                target = found != null ? found.transform : null;
            }
            catch (UnityException)
            {
                if (!tagProblemReported)
                {
                    tagProblemReported = true;
                    Debug.LogWarning($"Top Down Chaser 2D on \"{name}\": there is no tag called \"{chaseTag}\". Add it in Project Settings → Tags and Layers, or fix Chase Tag.", this);
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? (Vector3)home : transform.position;

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(center, wanderRadius);

            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, noticeRange);

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, giveUpRange);
        }
    }
}
