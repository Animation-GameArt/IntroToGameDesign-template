using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.TowerDefense
{
    /// <summary>
    /// A kind of tower, stored as an asset: what it costs, how far it reaches,
    /// how hard and fast it hits, and what it upgrades to.
    ///
    /// Create one with Create → Collin Game → Tower Defense Kit → Tower.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Tower Defense Kit/Tower", fileName = "New Tower", order = 0)]
    public class TowerData : ScriptableObject
    {
        [Tooltip("The name on the build button, such as Archer or Cannon. Leave empty to use the asset's name.")]
        [SerializeField] string displayName = "";

        [Tooltip("The tower prefab to build. It needs a Tower 2D.")]
        [SerializeField] Tower2D prefab;

        [Tooltip("What it costs to build (or to upgrade to this level).")]
        [SerializeField, Min(0)] int cost = 50;

        [Header("Attack")]

        [Tooltip("How far it reaches, in units.")]
        [SerializeField, Min(0.1f)] float range = 3f;

        [Tooltip("Health taken from each enemy hit.")]
        [SerializeField, Min(0)] int damage = 1;

        [Tooltip("Shots per second.")]
        [SerializeField, Min(0.05f)] float shotsPerSecond = 1f;

        [Tooltip("Optional. The projectile it fires. Leave empty to hit instantly, like a laser.")]
        [SerializeField] TowerProjectile2D projectile;

        [Tooltip("Projectile speed, in units per second.")]
        [SerializeField, Min(0.1f)] float projectileSpeed = 8f;

        [Tooltip("Damage also hits every enemy within this distance of the target, in units. 0 hits only the target.")]
        [SerializeField, Min(0f)] float splashRadius = 0f;

        [Tooltip("Hit enemies move at this share of their speed for a while. 1 means no slow; 0.5 is half speed.")]
        [SerializeField, Range(0.05f, 1f)] float slowTo = 1f;

        [Tooltip("How long the slow lasts, in seconds.")]
        [SerializeField, Min(0f)] float slowSeconds = 1.5f;

        [Header("Upgrade and selling")]

        [Tooltip("Optional. The tower it upgrades to. Its Cost is the upgrade price.")]
        [SerializeField] TowerData upgradesTo;

        [Tooltip("The share of the money spent that selling gives back.")]
        [SerializeField, Range(0f, 1f)] float sellShare = 0.6f;

        [Tooltip("Optional. A sound to play on each shot.")]
        [SerializeField] SoundEffect sound;

        /// <summary>The name on the build button.</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>The tower prefab.</summary>
        public Tower2D Prefab => prefab;

        /// <summary>What it costs to build or upgrade to.</summary>
        public int Cost => cost;

        /// <summary>Reach, in units.</summary>
        public float Range => range;

        /// <summary>Health taken per hit.</summary>
        public int Damage => damage;

        /// <summary>Seconds between shots.</summary>
        public float SecondsBetweenShots => 1f / shotsPerSecond;

        /// <summary>The projectile, or null for instant hits.</summary>
        public TowerProjectile2D Projectile => projectile;

        /// <summary>Projectile speed.</summary>
        public float ProjectileSpeed => projectileSpeed;

        /// <summary>Splash radius, or 0.</summary>
        public float SplashRadius => splashRadius;

        /// <summary>The speed share for slowed enemies; 1 means no slow.</summary>
        public float SlowTo => slowTo;

        /// <summary>How long a slow lasts.</summary>
        public float SlowSeconds => slowSeconds;

        /// <summary>The upgrade, or null.</summary>
        public TowerData UpgradesTo => upgradesTo;

        /// <summary>The share of money returned when sold.</summary>
        public float SellShare => sellShare;

        /// <summary>The shot sound, or null.</summary>
        public SoundEffect Sound => sound;

        /// <summary>Hurts the target (and anyone in the splash), and slows them. Towers and projectiles call this.</summary>
        public void HitAt(PathEnemy2D target, Vector2 point)
        {
            if (splashRadius <= 0f)
            {
                if (target != null)
                {
                    Hit(target);
                }

                return;
            }

            // Copy first: enemies can be destroyed while we hit them.
            PathEnemy2D[] everyone = new PathEnemy2D[PathEnemy2D.All.Count];

            for (int i = 0; i < everyone.Length; i++)
            {
                everyone[i] = PathEnemy2D.All[i];
            }

            foreach (PathEnemy2D enemy in everyone)
            {
                if (enemy != null && Vector2.Distance(enemy.transform.position, point) <= splashRadius)
                {
                    Hit(enemy);
                }
            }
        }

        void Hit(PathEnemy2D enemy)
        {
            if (slowTo < 1f)
            {
                enemy.Slow(slowTo, slowSeconds);
            }

            if (damage > 0 && !enemy.Health.IsDead)
            {
                enemy.Health.TakeDamage(damage);
            }
        }
    }
}
