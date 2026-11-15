using System;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Gives an object health that can be lost and regained: the player, an enemy,
    /// a crate that breaks.
    ///
    /// Other components change it by calling TakeDamage, Heal or Kill. Health then
    /// announces what happened, so nothing has to check it every frame.
    ///
    /// On the player, add a Respawner as well: when health runs out, the player
    /// goes back to the last checkpoint with full health.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Core/Health")]
    public class Health : MonoBehaviour
    {
        /// <summary>What happens to this object when its health runs out.</summary>
        public enum DeathAction
        {
            /// <summary>Nothing extra. Use this on the player, with a Respawner.</summary>
            Nothing,

            /// <summary>The object is hidden and switched off. A Spawner can reuse it.</summary>
            Deactivate,

            /// <summary>The object is removed from the scene.</summary>
            Destroy
        }

        [Tooltip("The most health this object can have, and the amount it starts with.")]
        [SerializeField, Min(1)] int maxHealth = 3;

        [Tooltip("After taking damage, no more damage can be taken for this many seconds.")]
        [SerializeField, Min(0f)] float invulnerableTime = 1f;

        [Tooltip("What happens to this object when its health runs out.")]
        [SerializeField] DeathAction whenHealthRunsOut = DeathAction.Nothing;

        [Header("Share with other objects")]

        [Tooltip("Optional. This variable is kept equal to the current health, so a HUD or a text can show it.")]
        [SerializeField] IntVariable healthVariable;

        [Tooltip("Optional. An event to raise when health runs out.")]
        [SerializeField] VoidEventChannel raiseOnDied;

        [Header("Events")]

        [Tooltip("Runs when damage is taken. Passes along the amount.")]
        [SerializeField] UnityEvent<int> onDamaged;

        [Tooltip("Runs when health is regained. Passes along the amount.")]
        [SerializeField] UnityEvent<int> onHealed;

        [Tooltip("Runs when health runs out.")]
        [SerializeField] UnityEvent onDied;

        float invulnerableUntil;

        /// <summary>Raised when damage is taken. Passes along the amount.</summary>
        public event Action<int> Damaged;

        /// <summary>Raised when health is regained. Passes along the amount.</summary>
        public event Action<int> Healed;

        /// <summary>Raised when health runs out.</summary>
        public event Action Died;

        /// <summary>Raised whenever the current health changes. Passes along the new value.</summary>
        public event Action<int> Changed;

        /// <summary>The health this object has right now.</summary>
        public int Current { get; private set; }

        /// <summary>The most health this object can have.</summary>
        public int Max => maxHealth;

        /// <summary>True once health has run out.</summary>
        public bool IsDead => Current <= 0;

        /// <summary>True for a short time after taking damage.</summary>
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        void OnEnable()
        {
            // Start full every time this object appears, including when a Spawner reuses it.
            ResetHealth();
        }

        /// <summary>Takes away health. Ignored while invulnerable, or once dead.</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead || IsInvulnerable)
            {
                return;
            }

            SetCurrent(Current - amount);
            invulnerableUntil = Time.time + invulnerableTime;

            Damaged?.Invoke(amount);
            onDamaged?.Invoke(amount);

            if (IsDead)
            {
                Die();
            }
        }

        /// <summary>Gives back health, up to the maximum.</summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            int before = Current;
            SetCurrent(Current + amount);
            int gained = Current - before;

            if (gained > 0)
            {
                Healed?.Invoke(gained);
                onHealed?.Invoke(gained);
            }
        }

        /// <summary>Takes away all health at once, even while invulnerable.</summary>
        public void Kill()
        {
            if (IsDead)
            {
                return;
            }

            int amount = Current;
            SetCurrent(0);

            Damaged?.Invoke(amount);
            onDamaged?.Invoke(amount);

            Die();
        }

        /// <summary>Sets health back to the maximum.</summary>
        public void ResetHealth()
        {
            invulnerableUntil = 0f;
            SetCurrent(maxHealth);
        }

        void Die()
        {
            onDied?.Invoke();

            if (raiseOnDied != null)
            {
                raiseOnDied.Raise();
            }

            // A Respawner on this object listens to this and may refill health.
            Died?.Invoke();

            if (!IsDead)
            {
                return; // Something brought this object back, so leave it in the scene.
            }

            if (whenHealthRunsOut == DeathAction.Deactivate)
            {
                gameObject.SetActive(false);
            }
            else if (whenHealthRunsOut == DeathAction.Destroy)
            {
                Destroy(gameObject);
            }
        }

        void SetCurrent(int value)
        {
            Current = Mathf.Clamp(value, 0, maxHealth);

            if (healthVariable != null)
            {
                healthVariable.SetValue(Current);
            }

            Changed?.Invoke(Current);
        }
    }
}
