using CollinGame.Core;
using UnityEngine;

namespace CollinGame.TwoD
{
    /// <summary>
    /// A side-view enemy that walks back and forth, and can chase the player.
    ///
    /// It is always in one of two states:
    ///   Patrol   walks in one direction and turns around at walls and ledges.
    ///   Chase    walks toward the player while the player is within Chase Range.
    ///
    /// Setup: add this to a GameObject that has a 2D collider. A Rigidbody 2D is
    /// added for you. To make it hurt the player, add Damage On Touch as well.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/2D/Patrol Enemy 2D")]
    public class PatrolEnemy2D : MonoBehaviour
    {
        /// <summary>What the enemy is doing right now.</summary>
        public enum State
        {
            Patrol,
            Chase
        }

        [Header("Patrol")]

        [Tooltip("Walking speed, in units per second.")]
        [SerializeField, Min(0f)] float patrolSpeed = 2f;

        [Tooltip("Ticked: starts by walking right. Unticked: starts by walking left.")]
        [SerializeField] bool startMovingRight = true;

        [Tooltip("Turn around when there is no ground ahead.")]
        [SerializeField] bool turnAtLedges = true;

        [Tooltip("Turn around when something solid is in the way.")]
        [SerializeField] bool turnAtWalls = true;

        [Tooltip("Turn around after walking this far from the starting point. 0 means no limit.")]
        [SerializeField, Min(0f)] float maxDistanceFromStart = 0f;

        [Header("Chase")]

        [Tooltip("The tag of the object to chase.")]
        [SerializeField] string chaseTag = "Player";

        [Tooltip("Chase when the target is closer than this, in units. 0 means never chase.")]
        [SerializeField, Min(0f)] float chaseRange = 0f;

        [Tooltip("Speed while chasing, in units per second.")]
        [SerializeField, Min(0f)] float chaseSpeed = 3.5f;

        [Header("Sensing")]

        [Tooltip("Which layers count as ground and walls. Everything works to start with.")]
        [SerializeField] LayerMask solidLayers = ~0;

        [Header("Looks")]

        [Tooltip("Optional. This sprite is flipped to face the way the enemy moves. Leave empty to use the first sprite found on this object.")]
        [SerializeField] SpriteRenderer spriteToFlip;

        const float TargetSearchInterval = 0.5f;

        Rigidbody2D body;
        Collider2D bodyCollider;
        readonly Collider2D[] hits = new Collider2D[8];

        int direction = 1;
        float startX;
        Transform target;
        float nextTargetSearch;
        bool tagProblemReported;

        /// <summary>What the enemy is doing right now.</summary>
        public State CurrentState { get; private set; } = State.Patrol;

        /// <summary>1 when facing right, -1 when facing left.</summary>
        public int FacingDirection => direction;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();

            if (spriteToFlip == null)
            {
                spriteToFlip = GetComponentInChildren<SpriteRenderer>();
            }

            body.freezeRotation = true;

            if (bodyCollider == null)
            {
                Debug.LogError($"Patrol Enemy 2D on \"{name}\": this GameObject has no 2D collider. Add a Box Collider 2D.", this);
            }
        }

        void OnEnable()
        {
            direction = startMovingRight ? 1 : -1;
            startX = transform.position.x;
            CurrentState = State.Patrol;
        }

        void FixedUpdate()
        {
            if (bodyCollider == null)
            {
                return;
            }

            FindTarget();

            int moveDirection;
            float speed;

            bool targetInRange = chaseRange > 0f && target != null
                && Vector2.Distance(target.position, transform.position) <= chaseRange;

            if (targetInRange)
            {
                // ----- Chase: walk toward the target, but never off a ledge or into a wall.
                CurrentState = State.Chase;
                speed = chaseSpeed;

                float offset = target.position.x - transform.position.x;
                moveDirection = Mathf.Abs(offset) < 0.1f ? 0 : (offset > 0f ? 1 : -1);

                if (moveDirection != 0)
                {
                    direction = moveDirection;

                    if (Blocked(moveDirection))
                    {
                        moveDirection = 0;
                    }
                }
            }
            else
            {
                // ----- Patrol: keep walking, and turn around when the way is blocked.
                CurrentState = State.Patrol;
                speed = patrolSpeed;

                bool tooFar = maxDistanceFromStart > 0f
                    && (transform.position.x - startX) * direction > maxDistanceFromStart;

                if (tooFar || Blocked(direction))
                {
                    direction = -direction;
                }

                moveDirection = direction;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = moveDirection * speed;
            body.linearVelocity = velocity;

            if (spriteToFlip != null)
            {
                spriteToFlip.flipX = direction < 0;
            }
        }

        bool Blocked(int towards)
        {
            return (turnAtWalls && WallAhead(towards)) || (turnAtLedges && !GroundAhead(towards));
        }

        // Is something solid directly in front, at body height?
        bool WallAhead(int towards)
        {
            Bounds bounds = bodyCollider.bounds;
            Vector2 center = new Vector2(bounds.center.x + towards * (bounds.extents.x + 0.06f), bounds.center.y);
            Vector2 size = new Vector2(0.06f, bounds.size.y * 0.6f);
            return AnythingSolid(center, size);
        }

        // Is there ground just past the front foot?
        bool GroundAhead(int towards)
        {
            Bounds bounds = bodyCollider.bounds;
            Vector2 center = new Vector2(bounds.center.x + towards * (bounds.extents.x + 0.1f), bounds.min.y - 0.15f);
            Vector2 size = new Vector2(0.1f, 0.2f);
            return AnythingSolid(center, size);
        }

        bool AnythingSolid(Vector2 center, Vector2 size)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(solidLayers);

            int count = Physics2D.OverlapBox(center, size, 0f, filter, hits);

            for (int i = 0; i < count; i++)
            {
                if (hits[i] != null && hits[i].attachedRigidbody != body)
                {
                    return true;
                }
            }

            return false;
        }

        // Looks for the object to chase, a couple of times a second.
        void FindTarget()
        {
            if (chaseRange <= 0f || string.IsNullOrEmpty(chaseTag) || tagProblemReported)
            {
                return;
            }

            if (target != null && target.gameObject.activeInHierarchy)
            {
                return;
            }

            if (Time.time < nextTargetSearch)
            {
                return;
            }

            nextTargetSearch = Time.time + TargetSearchInterval;

            try
            {
                GameObject found = GameObject.FindGameObjectWithTag(chaseTag);
                target = found != null ? found.transform : null;
            }
            catch (UnityException)
            {
                tagProblemReported = true;
                Debug.LogWarning($"Patrol Enemy 2D on \"{name}\": there is no tag named \"{chaseTag}\", so it can't chase anything. Check the spelling of Chase Tag.", this);
            }
        }

        // Shows the chase range in the Scene view while this object is selected.
        void OnDrawGizmosSelected()
        {
            if (chaseRange > 0f)
            {
                Gizmos.color = CurrentState == State.Chase ? Color.red : Color.yellow;
                Gizmos.DrawWireSphere(transform.position, chaseRange);
            }
        }
    }
}
