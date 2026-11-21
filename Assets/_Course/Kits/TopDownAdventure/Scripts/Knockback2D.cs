using CollinGame.Core;
using CollinGame.TwoD;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TopDownAdventure
{
    /// <summary>
    /// Pushes a character back when it is hit, and stops it moving by itself for
    /// a moment. Put it on the player and on enemies.
    ///
    /// A Melee Attack 2D pushes it away from the attacker. When this object
    /// takes damage from anything else, such as an enemy with Damage On Touch,
    /// it is pushed away from whatever it touched last.
    ///
    /// While pushed back, the movement scripts on this object (Top Down
    /// Controller 2D, Top Down Chaser 2D) are switched off.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/Top-Down Adventure Kit/Knockback 2D")]
    public class Knockback2D : MonoBehaviour
    {
        [Tooltip("How hard this object is pushed when damage comes from touching something.")]
        [SerializeField, Min(0f)] float forceWhenTouched = 6f;

        [Tooltip("How long this object can't move by itself after being pushed, in seconds.")]
        [SerializeField, Min(0f)] float stunTime = 0.2f;

        [Tooltip("Optional. Scripts to switch off while pushed back. Leave empty to use the Top Down Controller 2D or Top Down Chaser 2D on this object.")]
        [SerializeField] Behaviour[] stopWhilePushed;

        [Tooltip("Runs each time this object is pushed back.")]
        [SerializeField] UnityEvent onPushed;

        // Damage that arrives this soon after touching something counts as coming from it.
        const float TouchMemory = 0.15f;

        Rigidbody2D body;
        Health health;
        float stunnedUntil;
        int pushedOnFrame = -1;
        Vector2 lastTouchPoint;
        float lastTouchTime = -1f;
        bool stunned;

        /// <summary>True while this object is being pushed back.</summary>
        public bool IsStunned => stunned;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();

            if (stopWhilePushed == null || stopWhilePushed.Length == 0)
            {
                stopWhilePushed = new Behaviour[]
                {
                    GetComponent<TopDownController2D>(),
                    GetComponent<TopDownChaser2D>()
                };
            }
        }

        void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
            }
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
            }

            EndStun();
        }

        void OnCollisionEnter2D(Collision2D collision) { RememberTouch(collision.transform.position); }
        void OnCollisionStay2D(Collision2D collision) { RememberTouch(collision.transform.position); }
        void OnTriggerEnter2D(Collider2D other) { RememberTouch(other.transform.position); }
        void OnTriggerStay2D(Collider2D other) { RememberTouch(other.transform.position); }

        void RememberTouch(Vector2 point)
        {
            lastTouchPoint = point;
            lastTouchTime = Time.time;
        }

        void OnDamaged(int amount)
        {
            // A Melee Attack 2D already pushed this object on this frame.
            if (pushedOnFrame == Time.frameCount)
            {
                return;
            }

            if (Time.time - lastTouchTime <= TouchMemory)
            {
                Push(lastTouchPoint, forceWhenTouched);
            }
        }

        /// <summary>Pushes this object directly away from a point, such as the attacker's position.</summary>
        public void Push(Vector2 from, float force)
        {
            if (!isActiveAndEnabled || force <= 0f)
            {
                return;
            }

            Vector2 away = (Vector2)transform.position - from;

            if (away.sqrMagnitude < 0.0001f)
            {
                away = Random.insideUnitCircle;
            }

            pushedOnFrame = Time.frameCount;
            body.linearVelocity = away.normalized * force;
            stunnedUntil = Time.time + stunTime;

            if (!stunned)
            {
                stunned = true;
                SetMovementEnabled(false);
            }

            onPushed?.Invoke();
        }

        void FixedUpdate()
        {
            if (!stunned)
            {
                return;
            }

            // Slow down while stunned, so the push is a quick shove.
            float rate = stunTime > 0f ? body.linearVelocity.magnitude / Mathf.Max(stunnedUntil - Time.time, Time.fixedDeltaTime) : 0f;
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, rate * Time.fixedDeltaTime);

            if (Time.time >= stunnedUntil)
            {
                EndStun();
            }
        }

        void EndStun()
        {
            if (!stunned)
            {
                return;
            }

            stunned = false;
            SetMovementEnabled(true);
        }

        void SetMovementEnabled(bool value)
        {
            for (int i = 0; i < stopWhilePushed.Length; i++)
            {
                if (stopWhilePushed[i] != null)
                {
                    stopWhilePushed[i].enabled = value;
                }
            }
        }
    }
}
