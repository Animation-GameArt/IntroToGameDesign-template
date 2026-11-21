using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// A bullet, laser or arrow fired by a Weapon Shooter 2D. It flies straight,
    /// damages the first thing it hits that has Health and the Target Tag, and
    /// disappears after a while.
    ///
    /// Projectiles are reused instead of created and destroyed for every shot,
    /// which keeps a game with hundreds of bullets running smoothly.
    ///
    /// Setup: a sprite pointing up, with a 2D collider (Is Trigger ticked). A
    /// Rigidbody 2D is added and set up for you. Make it a prefab, and drag the
    /// prefab into a Weapon's Projectile Prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Projectile 2D")]
    public class Projectile2D : MonoBehaviour
    {
        [Tooltip("Only objects with this tag are damaged: Enemy for the player's shots, Player for enemy shots.")]
        [SerializeField] string targetTag = "Enemy";

        [Tooltip("Seconds before it disappears if it hits nothing.")]
        [SerializeField, Min(0.1f)] float lifetime = 2f;

        [Tooltip("Ticked: it disappears when it hits a wall (a solid collider without Health).")]
        [SerializeField] bool stoppedByWalls = true;

        [Tooltip("Optional. A prefab to create where it hits, such as a small spark.")]
        [SerializeField] GameObject hitEffect;

        // One pool of reusable projectiles per prefab.
        static readonly Dictionary<Projectile2D, ObjectPool<Projectile2D>> Pools = new Dictionary<Projectile2D, ObjectPool<Projectile2D>>();

        Rigidbody2D body;
        ObjectPool<Projectile2D> pool;
        GameObject owner;
        int damage = 1;
        float despawnAt;
        bool released;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Pools.Clear();

            // Loading a new scene destroys every projectile, so start the pools again.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                Pools.Clear();
            }
        }

        /// <summary>Takes a projectile from the prefab's pool, or makes a new one, and launches it.</summary>
        public static Projectile2D Fire(Projectile2D prefab, Vector2 position, Vector2 direction, float speed, int damage, GameObject owner)
        {
            if (!Pools.TryGetValue(prefab, out ObjectPool<Projectile2D> pool) || pool == null)
            {
                ObjectPool<Projectile2D> newPool = null;
                newPool = new ObjectPool<Projectile2D>(
                    () =>
                    {
                        Projectile2D made = Instantiate(prefab);
                        made.pool = newPool;
                        return made;
                    },
                    shot => shot.gameObject.SetActive(true),
                    shot => shot.gameObject.SetActive(false),
                    shot =>
                    {
                        if (shot != null)
                        {
                            Destroy(shot.gameObject);
                        }
                    },
                    collectionCheck: false,
                    defaultCapacity: 32);
                pool = newPool;
                Pools[prefab] = pool;
            }

            Projectile2D projectile = pool.Get();
            projectile.Launch(position, direction, speed, damage, owner);
            return projectile;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        void Launch(Vector2 position, Vector2 direction, float speed, int amount, GameObject shooter)
        {
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            body.position = position;
            body.rotation = angle;
            body.linearVelocity = direction * speed;

            owner = shooter;
            damage = amount;
            despawnAt = Time.time + lifetime;
            released = false;
        }

        void Update()
        {
            if (Time.time >= despawnAt)
            {
                Release();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Hit(other);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            Hit(collision.collider);
        }

        void Hit(Collider2D other)
        {
            if (released)
            {
                return;
            }

            GameObject target = TouchUtility.OwnerOf(other);

            // Never hit the shooter, or other projectiles.
            if (target == null || target == owner || (owner != null && target.transform.IsChildOf(owner.transform))
                || target.GetComponent<Projectile2D>() != null)
            {
                return;
            }

            Health health = target.GetComponent<Health>();

            if (health != null && TouchUtility.HasTag(target, targetTag))
            {
                if (!health.IsDead)
                {
                    health.TakeDamage(damage);
                }

                Burst();
                Release();
                return;
            }

            if (stoppedByWalls && !other.isTrigger && health == null)
            {
                Burst();
                Release();
            }
        }

        void Burst()
        {
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
        }

        /// <summary>Puts the projectile away, ready to be fired again.</summary>
        public void Release()
        {
            if (released)
            {
                return;
            }

            released = true;
            body.linearVelocity = Vector2.zero;

            if (pool != null)
            {
                pool.Release(this);
            }
            else
            {
                // Placed in the scene by hand rather than fired: just remove it.
                Destroy(gameObject);
            }
        }
    }
}
