using System;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Marks an object as someone who takes turns: the hero, an enemy, an ally.
    /// The Turn Manager tells each Turn Taker when its turn starts.
    ///
    /// If the object also has Health, it is skipped once its health runs out.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Turn Based/Turn Taker")]
    public class TurnTaker : MonoBehaviour
    {
        [Tooltip("The name shown in messages. Leave empty to use the GameObject's name.")]
        [SerializeField] string displayName = "";

        [Tooltip("Ticked: this is on the player's side. Unticked: this is an opponent.")]
        [SerializeField] bool playerSide = true;

        [Tooltip("Runs when this object's turn starts.")]
        [SerializeField] UnityEvent onTurnStarted;

        [Tooltip("Runs when this object's turn ends.")]
        [SerializeField] UnityEvent onTurnEnded;

        Health ownHealth;

        /// <summary>Raised when this object's turn starts.</summary>
        public event Action TurnStarted;

        /// <summary>Raised when this object's turn ends.</summary>
        public event Action TurnEnded;

        /// <summary>The name shown in messages.</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>True if this is on the player's side.</summary>
        public bool PlayerSide => playerSide;

        /// <summary>True while it is this object's turn.</summary>
        public bool IsMyTurn { get; private set; }

        /// <summary>The Turn Manager running the battle this object is in.</summary>
        public TurnManager Manager { get; internal set; }

        /// <summary>True if this object can still take turns: it is in the scene and has health left.</summary>
        public bool CanAct => isActiveAndEnabled && (ownHealth == null || !ownHealth.IsDead);

        void Awake()
        {
            ownHealth = GetComponent<Health>();
        }

        internal void BeginTurn()
        {
            IsMyTurn = true;
            TurnStarted?.Invoke();
            onTurnStarted?.Invoke();
        }

        internal void FinishTurn()
        {
            IsMyTurn = false;
            TurnEnded?.Invoke();
            onTurnEnded?.Invoke();
        }

        /// <summary>Ends this object's turn and passes play to the next in the order.</summary>
        public void EndTurn()
        {
            if (IsMyTurn && Manager != null)
            {
                Manager.EndTurn(this);
            }
        }
    }
}
