using CollinGame.Core;
using CollinGame.TwoD;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TopDownAdventure
{
    /// <summary>
    /// A close-range attack, like a sword swing. When the Attack button is
    /// pressed, everything with Health in a circle in front of the character
    /// takes damage, and is pushed back if it has a Knockback 2D.
    ///
    /// "In front" is the way the character last moved, from its Top Down
    /// Controller 2D. Without one, it is left or right, from the way the sprite faces.
    ///
    /// Select the character in the Scene view to see the circle the attack reaches.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Top-Down Adventure Kit/Melee Attack 2D")]
    public class MeleeAttack2D : MonoBehaviour
    {
        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input. Leave empty if something else calls Attack, such as an enemy's script.")]
        [SerializeField] InputReader input;

        [Header("Reach")]

        [Tooltip("How far in front of the character the middle of the hit circle is, in units.")]
        [SerializeField, Min(0f)] float reach = 0.8f;

        [Tooltip("The size of the hit circle, in units.")]
        [SerializeField, Min(0.05f)] float radius = 0.6f;

        [Header("Damage")]

        [Tooltip("Only objects with this tag are hurt. Leave empty to hurt anything with Health except this character.")]
        [SerializeField] string targetTag = "Enemy";

        [Tooltip("How much health each hit takes away.")]
        [SerializeField, Min(1)] int damage = 1;

        [Tooltip("How hard a hit pushes the target back. Only works on targets with a Knockback 2D.")]
        [SerializeField, Min(0f)] float knockbackForce = 8f;

        [Tooltip("Seconds before the character can attack again.")]
        [SerializeField, Min(0f)] float cooldown = 0.35f;

        [Header("Looks")]

        [Tooltip("Optional. A child object, such as a sword or slash sprite, shown briefly in front of the character during each attack.")]
        [SerializeField] GameObject swingVisual;

        [Tooltip("How long the swing visual is shown, in seconds.")]
        [SerializeField, Min(0f)] float swingVisualTime = 0.15f;

        [Header("Events")]

        [Tooltip("Runs on every attack, hit or miss. Use it for a sound.")]
        [SerializeField] UnityEvent onSwing;

        [Tooltip("Runs when an attack hits at least one target. Use it for a sound, a camera shake or hit stop.")]
        [SerializeField] UnityEvent onHit;

        static readonly Collider2D[] Hits = new Collider2D[16];
        static readonly Health[] AlreadyHit = new Health[16];

        TopDownController2D topDown;
        SpriteRenderer sprite;
        float nextAttackTime;
        float hideSwingAt;

        /// <summary>The direction the attack goes in right now.</summary>
        public Vector2 AttackDirection
        {
            get
            {
                if (topDown != null)
                {
                    return topDown.FacingDirection;
                }

                return sprite != null && sprite.flipX ? Vector2.left : Vector2.right;
            }
        }

        /// <summary>True when the cooldown has run out.</summary>
        public bool Ready => Time.time >= nextAttackTime;

        void Awake()
        {
            topDown = GetComponent<TopDownController2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();

            if (swingVisual != null)
            {
                swingVisual.SetActive(false);
            }
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.AttackPressed += Attack;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.AttackPressed -= Attack;
            }

            if (swingVisual != null)
            {
                swingVisual.SetActive(false);
            }
        }

        void Update()
        {
            if (swingVisual != null && swingVisual.activeSelf && Time.time >= hideSwingAt)
            {
                swingVisual.SetActive(false);
            }
        }

        /// <summary>Attacks now, unless the cooldown is still running or the game is paused.</summary>
        public void Attack()
        {
            if (!isActiveAndEnabled || !Ready || PauseController.GamePaused)
            {
                return;
            }

            nextAttackTime = Time.time + cooldown;

            Vector2 direction = AttackDirection;
            Vector2 center = (Vector2)transform.position + direction * reach;

            ShowSwing(direction);
            onSwing?.Invoke();

            int count = Physics2D.OverlapCircle(center, radius, ContactFilter2D.noFilter, Hits);
            int hitCount = 0;

            for (int i = 0; i < count && hitCount < AlreadyHit.Length; i++)
            {
                GameObject target = TouchUtility.OwnerOf(Hits[i]);

                if (target == null || target == gameObject || target.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!TouchUtility.HasTag(target, targetTag))
                {
                    continue;
                }

                Health health = target.GetComponent<Health>();

                if (health == null || health.IsDead || System.Array.IndexOf(AlreadyHit, health, 0, hitCount) >= 0)
                {
                    continue;
                }

                // An object with several colliders is only hit once.
                AlreadyHit[hitCount] = health;
                hitCount++;

                Knockback2D knockback = target.GetComponent<Knockback2D>();

                if (knockback != null && knockbackForce > 0f)
                {
                    knockback.Push(transform.position, knockbackForce);
                }

                health.TakeDamage(damage);
            }

            System.Array.Clear(AlreadyHit, 0, hitCount);

            if (hitCount > 0)
            {
                onHit?.Invoke();
            }
        }

        void ShowSwing(Vector2 direction)
        {
            if (swingVisual == null)
            {
                return;
            }

            swingVisual.transform.position = (Vector2)transform.position + direction * reach;

            // The swing visual's sprite should point right; it is turned to face the attack.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            swingVisual.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            swingVisual.SetActive(true);
            hideSwingAt = Time.time + swingVisualTime;
        }

        void OnDrawGizmosSelected()
        {
            Vector2 direction = Application.isPlaying ? AttackDirection : Vector2.right;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere((Vector2)transform.position + direction * reach, radius);
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
