using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TowerDefense
{
    /// <summary>
    /// An enemy that walks along an Enemy Path 2D. If it reaches the end, the
    /// player loses lives; if its Health runs out first, the player earns money.
    /// Towers can slow it down.
    ///
    /// Setup: a sprite with a Health component (When Health Runs Out: Destroy).
    /// No collider is needed for towers to find it. The Wave Spawner sets the path.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [AddComponentMenu("Collin Game/Tower Defense Kit/Path Enemy 2D")]
    public class PathEnemy2D : MonoBehaviour
    {
        [Tooltip("Speed along the path, in units per second.")]
        [SerializeField, Min(0f)] float speed = 2f;

        [Tooltip("Money earned when it's defeated.")]
        [SerializeField, Min(0)] int reward = 5;

        [Tooltip("Lives the player loses if it reaches the end.")]
        [SerializeField, Min(0)] int livesTaken = 1;

        [Tooltip("Optional. Set by the Wave Spawner; set it by hand for an enemy placed in the scene.")]
        [SerializeField] EnemyPath2D path;

        [Header("Looks")]

        [Tooltip("Ticked: the sprite turns to face along the path (it should point right). Unticked: it flips left and right.")]
        [SerializeField] bool rotateToPath = false;

        [Tooltip("Runs when it reaches the end of the path.")]
        [SerializeField] UnityEvent onReachedEnd;

        static readonly List<PathEnemy2D> all = new List<PathEnemy2D>();

        Health health;
        SpriteRenderer sprite;
        IntVariable money;
        IntVariable lives;
        int nextPoint = 1;
        float slowFactor = 1f;
        float slowUntil;

        /// <summary>Every enemy on the map. Towers search this list.</summary>
        public static IReadOnlyList<PathEnemy2D> All => all;

        /// <summary>How far along the path it has walked, in units. Towers use it to aim at the leader.</summary>
        public float Progress { get; private set; }

        /// <summary>This enemy's Health.</summary>
        public Health Health => health;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
        }

        void Awake()
        {
            health = GetComponent<Health>();
            sprite = GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable()
        {
            all.Add(this);
            health.Died += OnDied;
        }

        void OnDisable()
        {
            all.Remove(this);
            health.Died -= OnDied;
        }

        /// <summary>Starts the enemy on a path. The Wave Spawner calls this.</summary>
        public void Begin(EnemyPath2D newPath, IntVariable moneyVariable, IntVariable livesVariable)
        {
            path = newPath;
            money = moneyVariable;
            lives = livesVariable;
            nextPoint = 1;
            Progress = 0f;

            if (path != null && path.Count > 0)
            {
                transform.position = path.Point(0);
            }
        }

        /// <summary>Slows the enemy to a share of its speed for some seconds, such as 0.5 for half speed.</summary>
        public void Slow(float factor, float seconds)
        {
            // The strongest slow wins; a new slow refreshes the time.
            if (factor <= slowFactor || Time.time >= slowUntil)
            {
                slowFactor = Mathf.Clamp(factor, 0.05f, 1f);
            }

            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
        }

        void Update()
        {
            if (path == null || path.Count < 2 || health.IsDead)
            {
                return;
            }

            if (Time.time >= slowUntil)
            {
                slowFactor = 1f;
            }

            float step = speed * slowFactor * Time.deltaTime;

            while (step > 0f && nextPoint < path.Count)
            {
                Vector2 target = path.Point(nextPoint);
                Vector2 position = transform.position;
                Vector2 toTarget = target - position;
                float distance = toTarget.magnitude;

                if (distance <= step)
                {
                    transform.position = target;
                    Progress += distance;
                    step -= distance;
                    nextPoint++;
                    continue;
                }

                Vector2 direction = toTarget / distance;
                transform.position = position + direction * step;
                Progress += step;
                Face(direction);
                step = 0f;
            }

            if (nextPoint >= path.Count)
            {
                ReachEnd();
            }
        }

        void Face(Vector2 direction)
        {
            if (rotateToPath)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
            else if (sprite != null && Mathf.Abs(direction.x) > 0.01f)
            {
                sprite.flipX = direction.x < 0f;
            }
        }

        void ReachEnd()
        {
            if (lives != null)
            {
                lives.Add(-livesTaken);
            }

            onReachedEnd?.Invoke();
            Destroy(gameObject);
        }

        void OnDied()
        {
            if (money != null)
            {
                money.Add(reward);
            }
        }
    }
}
