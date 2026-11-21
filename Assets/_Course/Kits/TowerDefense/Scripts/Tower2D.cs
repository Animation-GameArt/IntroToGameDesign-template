using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TowerDefense
{
    /// <summary>
    /// A built tower. It finds enemies within range, picks one, turns to it,
    /// and fires. Its Tower asset says how far, how hard and how fast.
    ///
    /// Setup: a prefab with a sprite for the base, and optionally a child
    /// "Turret" sprite pointing right that turns to aim. Drag the prefab into
    /// a Tower asset's Prefab. Towers are built on Build Spots, not placed by hand.
    /// </summary>
    [AddComponentMenu("Collin Game/Tower Defense Kit/Tower 2D")]
    public class Tower2D : MonoBehaviour
    {
        /// <summary>Which enemy in range the tower shoots at.</summary>
        public enum Targeting
        {
            /// <summary>The one furthest along the path: the most dangerous.</summary>
            First,

            /// <summary>The one nearest the tower.</summary>
            Closest,

            /// <summary>The one with the most health left.</summary>
            Strongest
        }

        [Tooltip("Which enemy in range the tower shoots at.")]
        [SerializeField] Targeting target = Targeting.First;

        [Tooltip("Optional. A child that turns to face the target. Its sprite should point right.")]
        [SerializeField] Transform turret;

        [Tooltip("Optional. Where projectiles start, such as the end of a barrel. Leave empty to use the turret or the tower.")]
        [SerializeField] Transform muzzle;

        [Tooltip("Optional. A circle sprite, 1 unit across, shown when the tower is selected to show its range.")]
        [SerializeField] GameObject rangeIndicator;

        [Tooltip("Runs on every shot.")]
        [SerializeField] UnityEvent onShoot;

        float nextShot;

        /// <summary>The tower's current level.</summary>
        public TowerData Data { get; private set; }

        /// <summary>All the money spent on this tower, for selling.</summary>
        public int Spent { get; private set; }

        /// <summary>The spot it's built on.</summary>
        public BuildSpot2D Spot { get; private set; }

        /// <summary>Sets the tower's level. The Build Spot calls this when it builds or upgrades.</summary>
        public void Setup(TowerData data, BuildSpot2D spot, int spent)
        {
            Data = data;
            Spot = spot;
            Spent = spent;
            ShowRange(false);
        }

        /// <summary>Shows or hides the range circle.</summary>
        public void ShowRange(bool show)
        {
            if (rangeIndicator == null)
            {
                return;
            }

            rangeIndicator.SetActive(show);

            if (show && Data != null)
            {
                // The circle is 1 unit across, so scale it to the range's diameter.
                Vector3 parentScale = transform.lossyScale;
                float diameter = Data.Range * 2f;
                rangeIndicator.transform.localScale = new Vector3(diameter / parentScale.x, diameter / parentScale.y, 1f);
            }
        }

        void Update()
        {
            if (Data == null)
            {
                return;
            }

            PathEnemy2D enemy = FindTarget();

            if (enemy == null)
            {
                return;
            }

            Vector2 toEnemy = enemy.transform.position - transform.position;

            if (turret != null)
            {
                turret.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(toEnemy.y, toEnemy.x) * Mathf.Rad2Deg);
            }

            if (Time.time >= nextShot)
            {
                nextShot = Time.time + Data.SecondsBetweenShots;
                Fire(enemy);
            }
        }

        PathEnemy2D FindTarget()
        {
            PathEnemy2D best = null;
            float bestScore = float.MinValue;
            float range = Data.Range;
            Vector2 position = transform.position;

            foreach (PathEnemy2D enemy in PathEnemy2D.All)
            {
                if (enemy == null || enemy.Health.IsDead)
                {
                    continue;
                }

                float distance = Vector2.Distance(position, enemy.transform.position);

                if (distance > range)
                {
                    continue;
                }

                float score = target switch
                {
                    Targeting.Closest => -distance,
                    Targeting.Strongest => enemy.Health.Current,
                    _ => enemy.Progress
                };

                if (score > bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        void Fire(PathEnemy2D enemy)
        {
            if (Data.Projectile == null)
            {
                Data.HitAt(enemy, enemy.transform.position);
            }
            else
            {
                Transform from = muzzle != null ? muzzle : (turret != null ? turret : transform);
                TowerProjectile2D shot = Instantiate(Data.Projectile, from.position, from.rotation);
                shot.Launch(Data, enemy, Data.ProjectileSpeed);
            }

            if (Data.Sound != null)
            {
                Data.Sound.Play();
            }

            onShoot?.Invoke();
        }

        void OnDrawGizmosSelected()
        {
            if (Data != null)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
                Gizmos.DrawWireSphere(transform.position, Data.Range);
            }
        }
    }
}
