using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.TwoD;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// Fires a projectile prefab when the Attack button is pressed, or held.
    ///
    /// The projectile prefab needs a Rigidbody 2D (Gravity Scale 0) and a 2D
    /// collider. Give it Damage On Touch (Remove Self After Hit ticked) to hurt
    /// what it hits, and Lifetime so it disappears if it hits nothing.
    /// The projectile never hits the shooter.
    /// </summary>
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Shooter 2D")]
    public class Shooter2D : MonoBehaviour
    {
        /// <summary>Which way the projectiles go.</summary>
        public enum AimMode
        {
            /// <summary>The way the top of the Muzzle points. Use it for ships that turn.</summary>
            MuzzleUp,

            /// <summary>The way the right side of the Muzzle points.</summary>
            MuzzleRight,

            /// <summary>The way a Top Down Controller 2D last moved.</summary>
            MoveDirection
        }

        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input. Leave empty if something else calls Shoot, such as an enemy timer.")]
        [SerializeField] InputReader input;

        [Tooltip("The projectile to fire.")]
        [SerializeField] GameObject projectilePrefab;

        [Tooltip("Optional. Where projectiles start, such as a child at the ship's nose. Leave empty to use this object.")]
        [SerializeField] Transform muzzle;

        [Tooltip("Which way the projectiles go.")]
        [SerializeField] AimMode aim = AimMode.MuzzleUp;

        [Header("Firing")]

        [Tooltip("Projectile speed, in units per second.")]
        [SerializeField, Min(0f)] float projectileSpeed = 14f;

        [Tooltip("The most shots per second.")]
        [SerializeField, Min(0.1f)] float shotsPerSecond = 5f;

        [Tooltip("Ticked: holding Attack keeps firing. Unticked: one shot per press.")]
        [SerializeField] bool holdToFire = false;

        [Tooltip("The most of this shooter's projectiles allowed at once. 0 means no limit. The original Asteroids allowed 4.")]
        [SerializeField, Min(0)] int maxOnScreen = 4;

        [Tooltip("Ticked: projectiles also carry the shooter's own speed.")]
        [SerializeField] bool addShooterVelocity = true;

        [Tooltip("Runs on every shot. Use it for a sound.")]
        [SerializeField] UnityEvent onShoot;

        readonly List<GameObject> live = new List<GameObject>();
        Rigidbody2D ownBody;
        Collider2D[] ownColliders;
        TopDownController2D topDown;
        float nextShotTime;

        void Awake()
        {
            ownBody = GetComponentInParent<Rigidbody2D>();
            ownColliders = GetComponentsInChildren<Collider2D>();
            topDown = GetComponentInParent<TopDownController2D>();

            if (muzzle == null)
            {
                muzzle = transform;
            }
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.AttackPressed += Shoot;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.AttackPressed -= Shoot;
            }
        }

        void Update()
        {
            if (holdToFire && input != null && input.AttackHeld)
            {
                Shoot();
            }
        }

        /// <summary>Fires one projectile, unless it's too soon after the last or too many are out.</summary>
        public void Shoot()
        {
            if (!isActiveAndEnabled || PauseController.GamePaused || Time.time < nextShotTime)
            {
                return;
            }

            if (projectilePrefab == null)
            {
                Debug.LogWarning($"Shooter 2D on \"{name}\": Projectile Prefab is empty, so nothing is fired.", this);
                nextShotTime = Time.time + 1f;
                return;
            }

            live.RemoveAll(shot => shot == null || !shot.activeInHierarchy);

            if (maxOnScreen > 0 && live.Count >= maxOnScreen)
            {
                return;
            }

            nextShotTime = Time.time + 1f / shotsPerSecond;

            Vector2 direction = Direction();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            GameObject shot = Instantiate(projectilePrefab, muzzle.position, Quaternion.Euler(0f, 0f, angle));
            live.Add(shot);

            // The shot never hits the shooter.
            Collider2D[] shotColliders = shot.GetComponentsInChildren<Collider2D>();

            foreach (Collider2D mine in ownColliders)
            {
                foreach (Collider2D theirs in shotColliders)
                {
                    if (mine != null && theirs != null)
                    {
                        Physics2D.IgnoreCollision(mine, theirs);
                    }
                }
            }

            Rigidbody2D shotBody = shot.GetComponent<Rigidbody2D>();

            if (shotBody != null)
            {
                Vector2 velocity = direction * projectileSpeed;

                if (addShooterVelocity && ownBody != null)
                {
                    velocity += ownBody.linearVelocity;
                }

                shotBody.gravityScale = 0f;
                shotBody.linearVelocity = velocity;
            }
            else
            {
                Debug.LogWarning($"Shooter 2D on \"{name}\": the projectile prefab has no Rigidbody 2D, so it can't move.", this);
            }

            onShoot?.Invoke();
        }

        Vector2 Direction()
        {
            switch (aim)
            {
                case AimMode.MuzzleRight:
                    return muzzle.right;

                case AimMode.MoveDirection:
                    return topDown != null ? topDown.FacingDirection : (Vector2)muzzle.up;

                default:
                    return muzzle.up;
            }
        }

#if UNITY_EDITOR
        // Runs when the component is first added: fills in the Input field on the player.
        void Reset()
        {
            if (CompareTag("Player"))
            {
                input = InputReader.FindInProject();
            }
        }
#endif
    }
}
