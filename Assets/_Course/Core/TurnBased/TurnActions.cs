using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// What a Turn Taker can do on its turn: attack a target, or heal itself.
    /// Each action uses up the turn.
    ///
    /// Controlled By decides who chooses:
    ///   Player     waits for input. Attack is the Attack button; Heal is the Interact button.
    ///   Computer   waits a moment, then heals if its health is low, and attacks otherwise.
    ///
    /// Heals are limited, so the player has a real decision to make each turn.
    /// </summary>
    [RequireComponent(typeof(TurnTaker))]
    [AddComponentMenu("Collin Game/Turn Based/Turn Actions")]
    public class TurnActions : MonoBehaviour
    {
        /// <summary>Who chooses this object's actions.</summary>
        public enum Controller
        {
            Player,
            Computer
        }

        [Tooltip("Who chooses this object's actions.")]
        [SerializeField] Controller controlledBy = Controller.Player;

        [Tooltip("Needed when Controlled By is Player. The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Header("Attack")]

        [Tooltip("The Health that an attack hurts. Drag the opponent here.")]
        [SerializeField] Health attackTarget;

        [Tooltip("How much health an attack takes away.")]
        [SerializeField, Min(1)] int attackDamage = 2;

        [Header("Heal")]

        [Tooltip("How much health a heal gives back.")]
        [SerializeField, Min(1)] int healAmount = 3;

        [Tooltip("How many times this object can heal in one battle.")]
        [SerializeField, Min(0)] int healsAvailable = 2;

        [Header("Computer")]

        [Tooltip("Seconds the computer waits before acting, so the player can follow what happens.")]
        [SerializeField, Min(0f)] float thinkTime = 1f;

        [Tooltip("The computer heals when its health is at or below this, if it has heals left. 0 means it never heals.")]
        [SerializeField, Min(0)] int healWhenHealthAtOrBelow = 2;

        [Header("Share with other objects")]

        [Tooltip("Optional. A text variable that describes the last action, for a label to show.")]
        [SerializeField] StringVariable messageText;

        [Header("Events")]

        [Tooltip("Runs when this object attacks.")]
        [SerializeField] UnityEvent onAttack;

        [Tooltip("Runs when this object heals.")]
        [SerializeField] UnityEvent onHeal;

        TurnTaker taker;
        Health ownHealth;
        int healsLeft;

        /// <summary>How many heals this object has left.</summary>
        public int HealsLeft => healsLeft;

        void Awake()
        {
            taker = GetComponent<TurnTaker>();
            ownHealth = GetComponent<Health>();
            healsLeft = healsAvailable;

            if (controlledBy == Controller.Player && input == null)
            {
                Debug.LogError($"Turn Actions on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
            }
        }

        void OnEnable()
        {
            taker.TurnStarted += OnTurnStarted;

            if (controlledBy == Controller.Player && input != null)
            {
                input.AttackPressed += Attack;
                input.InteractPressed += Heal;
            }
        }

        void OnDisable()
        {
            taker.TurnStarted -= OnTurnStarted;

            if (input != null)
            {
                input.AttackPressed -= Attack;
                input.InteractPressed -= Heal;
            }
        }

        void OnTurnStarted()
        {
            if (controlledBy == Controller.Computer)
            {
                StartCoroutine(ComputerTurn());
            }
        }

        IEnumerator ComputerTurn()
        {
            if (thinkTime > 0f)
            {
                yield return new WaitForSeconds(thinkTime);
            }

            if (!taker.IsMyTurn)
            {
                yield break;
            }

            bool shouldHeal = ownHealth != null && healsLeft > 0 && healWhenHealthAtOrBelow > 0
                && ownHealth.Current <= healWhenHealthAtOrBelow;

            if (shouldHeal)
            {
                Heal();
            }
            else
            {
                Attack();
            }
        }

        /// <summary>Attacks the target and ends the turn. Does nothing when it isn't this object's turn.</summary>
        public void Attack()
        {
            if (!taker.IsMyTurn)
            {
                return;
            }

            if (attackTarget == null)
            {
                Debug.LogWarning($"Turn Actions on \"{name}\": Attack Target is empty, so the attack does nothing. Drag the opponent into the Attack Target field.", this);
            }
            else
            {
                attackTarget.TakeDamage(attackDamage);
                Say($"{taker.DisplayName} attacks for {attackDamage}.");
            }

            onAttack?.Invoke();
            taker.EndTurn();
        }

        /// <summary>Heals this object and ends the turn. Does nothing when it isn't this object's turn, or when no heals are left.</summary>
        public void Heal()
        {
            if (!taker.IsMyTurn)
            {
                return;
            }

            if (healsLeft <= 0)
            {
                Say($"{taker.DisplayName} has no heals left.");
                return;
            }

            if (ownHealth == null)
            {
                Debug.LogWarning($"Turn Actions on \"{name}\": this object has no Health component, so it can't heal.", this);
                return;
            }

            healsLeft--;
            ownHealth.Heal(healAmount);
            Say($"{taker.DisplayName} heals {healAmount}. Heals left: {healsLeft}.");

            onHeal?.Invoke();
            taker.EndTurn();
        }

        void Say(string message)
        {
            if (messageText != null)
            {
                messageText.SetValue(message);
            }
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
