using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// Fires a Weapon. On the player it fires with the Attack button, where Aim 2D
    /// points. On an enemy it fires by itself, at the player or straight ahead.
    ///
    /// A power-up can swap the weapon for a while (Set Weapon), after which the
    /// starting weapon comes back.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Weapon Shooter 2D")]
    public class WeaponShooter2D : MonoBehaviour
    {
        /// <summary>When the weapon fires.</summary>
        public enum Trigger
        {
            /// <summary>With the Attack button. For the player.</summary>
            AttackButton,

            /// <summary>By itself, every Auto Fire Interval seconds. For enemies.</summary>
            Automatic
        }

        /// <summary>Which way the weapon fires.</summary>
        public enum FireDirection
        {
            /// <summary>Where an Aim 2D on this object points.</summary>
            Aim,

            /// <summary>At the object with Target Tag, such as the player.</summary>
            AtTarget,

            /// <summary>The way this object's top points (its up arrow in the Scene view).</summary>
            Forward
        }

        [Tooltip("The weapon to start with.")]
        [SerializeField] WeaponData weapon;

        [Tooltip("When the weapon fires.")]
        [SerializeField] Trigger fireWhen = Trigger.AttackButton;

        [Tooltip("Attack Button only: the Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("Which way the weapon fires.")]
        [SerializeField] FireDirection direction = FireDirection.Aim;

        [Tooltip("At Target only: the tag of the object to fire at.")]
        [SerializeField] string targetTag = "Player";

        [Tooltip("Optional. Where shots start, such as the tip of a gun. Leave empty to use this object.")]
        [SerializeField] Transform muzzle;

        [Header("Automatic")]

        [Tooltip("Seconds between bursts.")]
        [SerializeField, Min(0.05f)] float autoFireInterval = 1.5f;

        [Tooltip("Seconds before the first burst, so enemies don't all fire the moment they appear.")]
        [SerializeField, Min(0f)] float firstShotDelay = 1f;

        [Tooltip("Only fires while the target is closer than this, in units. 0 means any distance.")]
        [SerializeField, Min(0f)] float range = 0f;

        [Header("Display and events")]

        [Tooltip("Optional. A text variable to show the current weapon's name in the HUD.")]
        [SerializeField] StringVariable weaponName;

        [Tooltip("Runs on every shot. Use it for a muzzle flash or a camera shake.")]
        [SerializeField] UnityEvent onShoot;

        Aim2D aim;
        Transform target;
        WeaponData startingWeapon;
        float nextShotTime;
        float weaponRunsOutAt = -1f;
        bool tagProblemReported;

        /// <summary>The weapon being fired now.</summary>
        public WeaponData Weapon => weapon;

        void Awake()
        {
            aim = GetComponent<Aim2D>();
            startingWeapon = weapon;

            if (muzzle == null)
            {
                muzzle = transform;
            }

            if (weapon == null)
            {
                Debug.LogWarning($"Weapon Shooter 2D on \"{name}\": Weapon is empty, so it can't fire. Drag in a Weapon asset.", this);
            }
        }

        void OnEnable()
        {
            nextShotTime = Time.time + (fireWhen == Trigger.Automatic ? firstShotDelay : 0f);

            if (fireWhen == Trigger.AttackButton && input != null)
            {
                input.AttackPressed += OnAttackPressed;
            }

            ShowWeaponName();
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.AttackPressed -= OnAttackPressed;
            }
        }

        void OnAttackPressed()
        {
            if (weapon != null && !weapon.HoldToFire)
            {
                TryFire();
            }
        }

        void Update()
        {
            // A power-up weapon runs out.
            if (weaponRunsOutAt > 0f && Time.time >= weaponRunsOutAt)
            {
                weaponRunsOutAt = -1f;
                SetWeapon(startingWeapon);
            }

            if (weapon == null || PauseController.GamePaused)
            {
                return;
            }

            if (fireWhen == Trigger.AttackButton)
            {
                if (weapon.HoldToFire && input != null && input.AttackHeld)
                {
                    TryFire();
                }

                return;
            }

            // Automatic
            if (Time.time >= nextShotTime && TargetInRange())
            {
                Fire();
                nextShotTime = Time.time + autoFireInterval;
            }
        }

        /// <summary>Fires now if the weapon is ready.</summary>
        public void TryFire()
        {
            if (weapon == null || PauseController.GamePaused || Time.time < nextShotTime)
            {
                return;
            }

            Fire();
            nextShotTime = Time.time + weapon.SecondsBetweenShots;
        }

        void Fire()
        {
            if (weapon.ProjectilePrefab == null)
            {
                Debug.LogWarning($"Weapon Shooter 2D on \"{name}\": the weapon \"{weapon.name}\" has no Projectile Prefab.", this);
                nextShotTime = Time.time + 1f;
                return;
            }

            Vector2 aimDirection = CurrentDirection();
            int count = weapon.ProjectilesPerShot;
            float spread = count > 1 ? weapon.SpreadAngle : 0f;

            for (int i = 0; i < count; i++)
            {
                // Spread the shots evenly across the spread angle, centred on the aim.
                float offset = count > 1 ? Mathf.Lerp(-spread / 2f, spread / 2f, i / (float)(count - 1)) : 0f;
                offset += Random.Range(-weapon.Inaccuracy, weapon.Inaccuracy);
                Vector2 shotDirection = Quaternion.Euler(0f, 0f, offset) * aimDirection;

                Projectile2D.Fire(weapon.ProjectilePrefab, muzzle.position, shotDirection, weapon.ProjectileSpeed, weapon.Damage, gameObject);
            }

            if (weapon.Sound != null)
            {
                weapon.Sound.Play();
            }

            onShoot?.Invoke();
        }

        Vector2 CurrentDirection()
        {
            switch (direction)
            {
                case FireDirection.AtTarget:
                    FindTarget();
                    return target != null ? ((Vector2)(target.position - muzzle.position)).normalized : (Vector2)transform.up;

                case FireDirection.Forward:
                    return transform.up;

                default:
                    return aim != null ? aim.Direction : (Vector2)transform.up;
            }
        }

        bool TargetInRange()
        {
            if (range <= 0f && direction != FireDirection.AtTarget)
            {
                return true;
            }

            FindTarget();

            if (target == null)
            {
                return false;
            }

            return range <= 0f || Vector2.Distance(transform.position, target.position) <= range;
        }

        void FindTarget()
        {
            if (target != null && target.gameObject.activeInHierarchy)
            {
                return;
            }

            target = null;

            if (string.IsNullOrEmpty(targetTag))
            {
                return;
            }

            try
            {
                GameObject found = GameObject.FindWithTag(targetTag);
                target = found != null ? found.transform : null;
            }
            catch (UnityException)
            {
                if (!tagProblemReported)
                {
                    tagProblemReported = true;
                    Debug.LogWarning($"Weapon Shooter 2D on \"{name}\": there is no tag called \"{targetTag}\".", this);
                }
            }
        }

        /// <summary>Switches to another weapon for good. Use it from a shop or an event.</summary>
        public void SetWeapon(WeaponData newWeapon)
        {
            SetWeapon(newWeapon, 0f);
        }

        /// <summary>Switches to another weapon for some seconds (0 means for good), then back to the starting weapon.</summary>
        public void SetWeapon(WeaponData newWeapon, float seconds)
        {
            if (newWeapon == null)
            {
                return;
            }

            weapon = newWeapon;
            weaponRunsOutAt = seconds > 0f ? Time.time + seconds : -1f;

            if (seconds <= 0f)
            {
                startingWeapon = newWeapon;
            }

            ShowWeaponName();
        }

        void ShowWeaponName()
        {
            if (weaponName != null && weapon != null)
            {
                weaponName.SetValue(weapon.DisplayName);
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
