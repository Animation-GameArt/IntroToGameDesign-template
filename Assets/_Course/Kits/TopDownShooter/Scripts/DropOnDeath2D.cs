using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.TopDownShooter
{
    /// <summary>
    /// Sometimes leaves a pickup behind when this object's health runs out:
    /// a coin, a heart, a power-up.
    ///
    /// Setup: on an enemy with Health. List the possible drops; one is picked
    /// at random each time something drops.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [AddComponentMenu("Collin Game/Top-Down Shooter Kit/Drop On Death 2D")]
    public class DropOnDeath2D : MonoBehaviour
    {
        [Tooltip("The chance of dropping anything, from 0 (never) to 1 (always).")]
        [SerializeField, Range(0f, 1f)] float chance = 0.25f;

        [Tooltip("The pickups it might drop. One is chosen at random.")]
        [SerializeField] GameObject[] drops = new GameObject[0];

        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            health.Died += Drop;
        }

        void OnDisable()
        {
            health.Died -= Drop;
        }

        void Drop()
        {
            if (drops.Length == 0 || Random.value > chance)
            {
                return;
            }

            GameObject drop = drops[Random.Range(0, drops.Length)];

            if (drop != null)
            {
                Instantiate(drop, transform.position, Quaternion.identity);
            }
        }
    }
}
