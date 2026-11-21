using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// A weapon, stored as an asset: what it fires, how fast, and how many at
    /// once. The player, enemies and power-ups all use the same weapon assets,
    /// so a "Spread Shot" can be picked up by the player and fired by a boss.
    ///
    /// Create one with Create → Collin Game → Top-Down Shooter Kit → Weapon.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Top-Down Shooter Kit/Weapon", fileName = "New Weapon", order = 0)]
    public class WeaponData : ScriptableObject
    {
        [Tooltip("The name shown in the HUD, such as Blaster or Spread Shot. Leave empty to use the asset's name.")]
        [SerializeField] string displayName = "";

        [Tooltip("The projectile to fire. It needs a Projectile 2D.")]
        [SerializeField] Projectile2D projectilePrefab;

        [Tooltip("Projectile speed, in units per second.")]
        [SerializeField, Min(0f)] float projectileSpeed = 14f;

        [Tooltip("How much health each projectile takes away.")]
        [SerializeField, Min(1)] int damage = 1;

        [Tooltip("The most shots per second.")]
        [SerializeField, Min(0.1f)] float shotsPerSecond = 6f;

        [Tooltip("Projectiles fired at once. More than 1 makes a spread.")]
        [SerializeField, Min(1)] int projectilesPerShot = 1;

        [Tooltip("The angle the projectiles are spread across, in degrees. Used when Projectiles Per Shot is more than 1.")]
        [SerializeField, Range(0f, 360f)] float spreadAngle = 30f;

        [Tooltip("Each shot wobbles randomly by up to this many degrees. 0 is perfectly accurate.")]
        [SerializeField, Range(0f, 45f)] float inaccuracy = 0f;

        [Tooltip("Ticked: holding Attack keeps firing. Unticked: one shot per press.")]
        [SerializeField] bool holdToFire = true;

        [Tooltip("Optional. A sound to play on each shot.")]
        [SerializeField] SoundEffect sound;

        /// <summary>The name shown in the HUD.</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>The projectile to fire.</summary>
        public Projectile2D ProjectilePrefab => projectilePrefab;

        /// <summary>Projectile speed, in units per second.</summary>
        public float ProjectileSpeed => projectileSpeed;

        /// <summary>Damage per projectile.</summary>
        public int Damage => damage;

        /// <summary>Seconds between shots.</summary>
        public float SecondsBetweenShots => 1f / shotsPerSecond;

        /// <summary>Projectiles fired at once.</summary>
        public int ProjectilesPerShot => projectilesPerShot;

        /// <summary>The angle the projectiles are spread across.</summary>
        public float SpreadAngle => spreadAngle;

        /// <summary>Random wobble per shot, in degrees.</summary>
        public float Inaccuracy => inaccuracy;

        /// <summary>True if holding Attack keeps firing.</summary>
        public bool HoldToFire => holdToFire;

        /// <summary>The sound for each shot, or nothing.</summary>
        public SoundEffect Sound => sound;
    }
}
