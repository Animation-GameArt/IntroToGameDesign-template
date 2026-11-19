using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.PhysicsPuzzle
{
    /// <summary>
    /// Something that breaks when it's hit hard enough: a glass pane, a wooden
    /// plank, a target. The harder the material, the higher its Break Speed.
    ///
    /// Setup: this GameObject needs a solid 2D collider and a Rigidbody 2D.
    /// If it also has a Health component, each hard hit takes 1 health instead,
    /// and it breaks when the health runs out.
    /// </summary>
    [AddComponentMenu("Collin Game/Physics Puzzle Kit/Breakable 2D")]
    public class Breakable2D : MonoBehaviour
    {
        [Tooltip("How fast something must hit this, in units per second, to break it. Glass about 3, wood about 6, stone about 10.")]
        [SerializeField, Min(0f)] float breakSpeed = 6f;

        [Tooltip("Hits are ignored for this many seconds after the scene starts, while stacked objects settle.")]
        [SerializeField, Min(0f)] float settleTime = 1f;

        [Tooltip("Break if this object falls below this height. Use it for things knocked off the level.")]
        [SerializeField] float breakBelowY = -10f;

        [Header("When broken")]

        [Tooltip("Optional. A number to add to, such as the score.")]
        [SerializeField] IntVariable addTo;

        [Tooltip("How much to add.")]
        [SerializeField] int amount = 100;

        [Tooltip("Optional. A prefab to create where this breaks, such as pieces or a puff of dust.")]
        [SerializeField] GameObject spawnWhenBroken;

        [Tooltip("Optional. An event to raise.")]
        [SerializeField] VoidEventChannel raiseOnBroken;

        [Tooltip("Optional. Things to do, such as playing a sound.")]
        [SerializeField] UnityEvent onBroken;

        Health health;
        float activeFrom;
        bool broken;

        /// <summary>True once this has broken.</summary>
        public bool IsBroken => broken;

        void Awake()
        {
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            broken = false;
            activeFrom = Time.time + settleTime;

            if (health != null)
            {
                health.Died += Break;
            }
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Died -= Break;
            }
        }

        void Update()
        {
            if (!broken && transform.position.y < breakBelowY)
            {
                Break();
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (broken || Time.time < activeFrom)
            {
                return;
            }

            if (collision.relativeVelocity.magnitude < breakSpeed)
            {
                return;
            }

            if (health != null)
            {
                health.TakeDamage(1);
            }
            else
            {
                Break();
            }
        }

        /// <summary>Breaks this right now.</summary>
        public void Break()
        {
            if (broken)
            {
                return;
            }

            broken = true;

            if (addTo != null)
            {
                addTo.Add(amount);
            }

            if (spawnWhenBroken != null)
            {
                Instantiate(spawnWhenBroken, transform.position, transform.rotation);
            }

            if (raiseOnBroken != null)
            {
                raiseOnBroken.Raise();
            }

            onBroken?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
