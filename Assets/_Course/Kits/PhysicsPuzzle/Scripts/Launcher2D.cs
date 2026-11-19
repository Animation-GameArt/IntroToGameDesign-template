using System.Collections;
using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.PhysicsPuzzle
{
    /// <summary>
    /// A slingshot. The player pulls back and lets go, and a projectile flies
    /// off under physics. Dots show where it will go.
    ///
    /// Mouse or touch: press near the launch point, drag back, release.
    /// Keyboard or gamepad: hold Attack (Enter / left face button), aim with Move,
    /// let go of Attack.
    ///
    /// After each shot the launcher waits until the projectile comes to rest (or a
    /// time limit passes), raises Shot Finished, and is ready again.
    /// </summary>
    [AddComponentMenu("Collin Game/Physics Puzzle Kit/Launcher 2D")]
    public class Launcher2D : MonoBehaviour
    {
        [Header("Input")]

        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input. Used for keyboard and gamepad aiming.")]
        [SerializeField] InputReader input;

        [Tooltip("How close to the launch point, in units, a mouse press or touch must be to start aiming.")]
        [SerializeField, Min(0.1f)] float grabRadius = 1.5f;

        [Header("Launching")]

        [Tooltip("The prefab to launch. It needs a Rigidbody 2D and a collider.")]
        [SerializeField] Rigidbody2D projectilePrefab;

        [Tooltip("Optional. Where projectiles start. Leave empty to use this object's position.")]
        [SerializeField] Transform launchPoint;

        [Tooltip("The furthest the player can pull back, in units.")]
        [SerializeField, Min(0.1f)] float maxPull = 2.5f;

        [Tooltip("Launch speed for each unit of pull. Higher values launch harder.")]
        [SerializeField, Min(0f)] float power = 7f;

        [Header("Shots")]

        [Tooltip("Optional. A number of shots left. Each launch takes one; at 0 the launcher stops.")]
        [SerializeField] IntVariable shotsLeft;

        [Tooltip("A shot is finished when the projectile has been nearly still for this long, in seconds.")]
        [SerializeField, Min(0f)] float restTime = 0.75f;

        [Tooltip("A shot is finished after this many seconds, even if the projectile is still moving.")]
        [SerializeField, Min(0.5f)] float maxShotTime = 8f;

        [Tooltip("Remove each projectile when its shot is finished.")]
        [SerializeField] bool removeProjectileAfterShot;

        [Header("Aiming dots")]

        [Tooltip("Optional. A small sprite for the aiming dots. Leave empty for no dots.")]
        [SerializeField] Sprite dotSprite;

        [Tooltip("How many dots to show.")]
        [SerializeField, Range(0, 40)] int dotCount = 15;

        [Tooltip("Seconds of flight between one dot and the next.")]
        [SerializeField, Min(0.01f)] float dotSpacing = 0.06f;

        [Header("Events")]

        [Tooltip("Optional. Raised when a shot is finished: the projectile has come to rest.")]
        [SerializeField] VoidEventChannel raiseOnShotFinished;

        [Tooltip("Runs when a projectile is launched.")]
        [SerializeField] UnityEvent onLaunched;

        [Tooltip("Runs when a shot is finished.")]
        [SerializeField] UnityEvent onShotFinished;

        readonly List<SpriteRenderer> dots = new List<SpriteRenderer>();

        bool aimingWithPointer;
        bool aimingWithButton;
        Vector2 pull;
        bool shotInFlight;

        /// <summary>True when a projectile can be launched.</summary>
        public bool Ready => !shotInFlight && projectilePrefab != null && (shotsLeft == null || shotsLeft.Value > 0);

        Vector2 Origin => launchPoint != null ? (Vector2)launchPoint.position : (Vector2)transform.position;

        void Awake()
        {
            if (projectilePrefab == null)
            {
                Debug.LogError($"Launcher 2D on \"{name}\": Projectile Prefab is empty. Drag a prefab with a Rigidbody 2D into it.", this);
            }

            CreateDots();
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.AttackPressed += OnAttackPressed;
                input.AttackReleased += OnAttackReleased;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.AttackPressed -= OnAttackPressed;
                input.AttackReleased -= OnAttackReleased;
            }

            CancelAim();
        }

        void Update()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }

            UpdatePointerAim();

            if (aimingWithButton && input != null)
            {
                // The stick or keys point the way to pull back; the shot goes the opposite way.
                pull = -input.Move * maxPull;
            }

            ShowDots(aimingWithPointer || aimingWithButton);
        }

        // ----- Mouse and touch -----

        void UpdatePointerAim()
        {
            if (!aimingWithPointer)
            {
                if (Ready && PointerUtility.PressedThisFrame
                    && PointerUtility.TryGetWorldPosition(out Vector2 pressAt)
                    && Vector2.Distance(pressAt, Origin) <= grabRadius)
                {
                    aimingWithPointer = true;
                }

                return;
            }

            if (PointerUtility.TryGetWorldPosition(out Vector2 at))
            {
                pull = Vector2.ClampMagnitude(Origin - at, maxPull);
            }

            if (PointerUtility.ReleasedThisFrame || !PointerUtility.IsPressed)
            {
                aimingWithPointer = false;
                Launch();
            }
        }

        // ----- Keyboard and gamepad -----

        void OnAttackPressed()
        {
            if (Ready && !aimingWithPointer && Time.timeScale > 0f)
            {
                aimingWithButton = true;
                pull = Vector2.zero;
            }
        }

        void OnAttackReleased()
        {
            if (aimingWithButton)
            {
                aimingWithButton = false;
                Launch();
            }
        }

        void CancelAim()
        {
            aimingWithPointer = false;
            aimingWithButton = false;
            ShowDots(false);
        }

        // ----- Launching -----

        /// <summary>Launches with the current pull. A very small pull is ignored, so a click doesn't waste a shot.</summary>
        void Launch()
        {
            ShowDots(false);

            if (!Ready || pull.magnitude < maxPull * 0.1f)
            {
                return;
            }

            Rigidbody2D projectile = Instantiate(projectilePrefab, Origin, Quaternion.identity);
            projectile.linearVelocity = pull * power;

            if (shotsLeft != null)
            {
                shotsLeft.Add(-1);
            }

            onLaunched?.Invoke();
            StartCoroutine(WatchShot(projectile));
        }

        IEnumerator WatchShot(Rigidbody2D projectile)
        {
            shotInFlight = true;
            float elapsed = 0f;
            float stillFor = 0f;

            // Give it a moment to leave the launcher before checking whether it has stopped.
            yield return new WaitForSeconds(0.3f);

            while (projectile != null && projectile.gameObject.activeInHierarchy && elapsed < maxShotTime)
            {
                elapsed += Time.deltaTime;
                stillFor = projectile.linearVelocity.sqrMagnitude < 0.04f ? stillFor + Time.deltaTime : 0f;

                if (stillFor >= restTime)
                {
                    break;
                }

                yield return null;
            }

            if (removeProjectileAfterShot && projectile != null)
            {
                Destroy(projectile.gameObject);
            }

            shotInFlight = false;

            if (raiseOnShotFinished != null)
            {
                raiseOnShotFinished.Raise();
            }

            onShotFinished?.Invoke();
        }

        // ----- Aiming dots -----

        void CreateDots()
        {
            if (dotSprite == null)
            {
                return;
            }

            for (int i = 0; i < dotCount; i++)
            {
                GameObject dot = new GameObject("Aim Dot");
                dot.transform.SetParent(transform, false);
                dot.transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 0.25f, i / (float)Mathf.Max(1, dotCount - 1));

                SpriteRenderer renderer = dot.AddComponent<SpriteRenderer>();
                renderer.sprite = dotSprite;
                renderer.sortingOrder = 50;
                renderer.enabled = false;
                dots.Add(renderer);
            }
        }

        void ShowDots(bool show)
        {
            if (dots.Count == 0)
            {
                return;
            }

            bool visible = show && pull.magnitude >= maxPull * 0.1f && projectilePrefab != null;
            Vector2 velocity = pull * power;
            Vector2 gravity = Physics2D.gravity * (projectilePrefab != null ? projectilePrefab.gravityScale : 1f);

            for (int i = 0; i < dots.Count; i++)
            {
                dots[i].enabled = visible;

                if (!visible)
                {
                    continue;
                }

                // Where the projectile will be after this much time: start + speed × time + ½ × gravity × time².
                float t = (i + 1) * dotSpacing;
                Vector2 position = Origin + velocity * t + 0.5f * gravity * t * t;
                dots[i].transform.position = new Vector3(position.x, position.y, 0f);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(launchPoint != null ? launchPoint.position : transform.position, grabRadius);
        }

#if UNITY_EDITOR
        // Runs when the component is first added: fills in the Input field for you.
        void Reset()
        {
            input = InputReader.FindInProject();
        }
#endif
    }
}
