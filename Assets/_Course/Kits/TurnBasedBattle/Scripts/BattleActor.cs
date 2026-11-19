using System;
using System.Collections;
using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TurnBasedBattle
{
    /// <summary>
    /// A fighter in a turn-based battle: the hero, a party member, a monster.
    /// It has a list of Battle Actions and simple stats, and does one action
    /// each turn.
    ///
    ///   Player     the UI Battle Menu shows its actions when its turn starts.
    ///   Computer   picks an action and a target by itself.
    ///
    /// Setup: on the same GameObject, a Turn Taker (Player Side ticked for the
    /// player's fighters) and a Health with Invulnerable Time set to 0. Add the
    /// fighter to the Turn Manager's Turn Order.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TurnTaker))]
    [RequireComponent(typeof(Health))]
    [AddComponentMenu("Collin Game/Turn-Based Battle Kit/Battle Actor")]
    public class BattleActor : MonoBehaviour
    {
        /// <summary>Who chooses this fighter's actions.</summary>
        public enum Controller
        {
            Player,
            Computer
        }

        [Tooltip("Who chooses this fighter's actions.")]
        [SerializeField] Controller controlledBy = Controller.Player;

        [Tooltip("The actions this fighter can choose from, in menu order.")]
        [SerializeField] List<BattleAction> actions = new List<BattleAction>();

        [Header("Stats")]

        [Tooltip("Added to the power of every damage action this fighter uses.")]
        [SerializeField] int attack = 0;

        [Tooltip("Taken away from the damage of every hit this fighter receives. A hit always does at least 1.")]
        [SerializeField] int defense = 0;

        [Header("Computer")]

        [Tooltip("Seconds the computer waits before acting, so the player can follow what happens.")]
        [SerializeField, Min(0f)] float thinkTime = 0.8f;

        [Tooltip("The computer heals when its health is at or below this share of its maximum, if it has a heal action.")]
        [SerializeField, Range(0f, 1f)] float healBelow = 0.35f;

        [Header("Looks")]

        [Tooltip("Optional. The part that moves forward when acting, usually the sprite. Leave empty to use the first sprite found on this object.")]
        [SerializeField] Transform lungePart;

        [Tooltip("How far it moves toward the target when acting, in units. 0 means it doesn't move.")]
        [SerializeField, Min(0f)] float lungeDistance = 0.4f;

        [Header("Events")]

        [Tooltip("Runs when this fighter starts an action. Use it for a sound or an animation.")]
        [SerializeField] UnityEvent onAction;

        [Tooltip("Runs when this fighter defends.")]
        [SerializeField] UnityEvent onDefend;

        [Tooltip("Runs when an attack misses this fighter.")]
        [SerializeField] UnityEvent onDodged;

        const float LungeTime = 0.12f;
        const float AfterActionPause = 0.25f;

        static readonly List<BattleActor> all = new List<BattleActor>();

        TurnTaker taker;
        Health health;
        readonly Dictionary<BattleAction, int> usesLeft = new Dictionary<BattleAction, int>();
        bool busy;
        bool invulnerabilityReported;

        /// <summary>Every fighter in the scene.</summary>
        public static IReadOnlyList<BattleActor> All => all;

        /// <summary>Raised when a player-controlled fighter's turn starts. The UI Battle Menu listens to this.</summary>
        public static event Action<BattleActor> PlayerTurnStarted;

        /// <summary>Raised when any fighter starts an action, after it has been chosen.</summary>
        public static event Action<BattleActor, BattleAction> ActionStarted;

        /// <summary>Raised with a line describing what happened, for a Battle Log.</summary>
        public static event Action<string> Said;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
            PlayerTurnStarted = null;
            ActionStarted = null;
            Said = null;
        }

        /// <summary>The name shown in menus and messages.</summary>
        public string DisplayName => taker != null ? taker.DisplayName : name;

        /// <summary>True if this fighter is on the player's side.</summary>
        public bool PlayerSide => taker != null && taker.PlayerSide;

        /// <summary>True if the player chooses this fighter's actions.</summary>
        public bool PlayerControlled => controlledBy == Controller.Player;

        /// <summary>True if this fighter is still in the battle.</summary>
        public bool CanAct => taker != null && taker.CanAct;

        /// <summary>True while it is this fighter's turn.</summary>
        public bool IsMyTurn => taker != null && taker.IsMyTurn;

        /// <summary>True from defending until this fighter's next turn starts.</summary>
        public bool IsDefending { get; private set; }

        /// <summary>This fighter's Health.</summary>
        public Health Health => health;

        /// <summary>The actions this fighter can choose from.</summary>
        public IReadOnlyList<BattleAction> Actions => actions;

        void Awake()
        {
            taker = GetComponent<TurnTaker>();
            health = GetComponent<Health>();

            if (lungePart == null)
            {
                SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
                lungePart = sprite != null ? sprite.transform : null;
            }

            ResetUses();
        }

        void OnEnable()
        {
            all.Add(this);
            taker.TurnStarted += OnTurnStarted;
        }

        void OnDisable()
        {
            all.Remove(this);
            taker.TurnStarted -= OnTurnStarted;
            busy = false;
        }

        /// <summary>Gives back every limited use. Called automatically when the scene starts.</summary>
        public void ResetUses()
        {
            usesLeft.Clear();

            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i] != null && actions[i].UsesPerBattle > 0)
                {
                    usesLeft[actions[i]] = actions[i].UsesPerBattle;
                }
            }
        }

        /// <summary>How many uses of an action are left. -1 means no limit.</summary>
        public int UsesLeft(BattleAction action)
        {
            if (action == null)
            {
                return 0;
            }

            return usesLeft.TryGetValue(action, out int left) ? left : -1;
        }

        /// <summary>True if the action has uses left and something to use it on.</summary>
        public bool CanUse(BattleAction action)
        {
            return action != null && UsesLeft(action) != 0 && PossibleTargets(action).Count > 0;
        }

        /// <summary>The fighters an action can be used on right now.</summary>
        public List<BattleActor> PossibleTargets(BattleAction action)
        {
            List<BattleActor> targets = new List<BattleActor>();

            if (action == null)
            {
                return targets;
            }

            if (action.Targets == BattleAction.Target.Self || action.ActionEffect == BattleAction.Effect.Defend)
            {
                targets.Add(this);
                return targets;
            }

            bool wantOpponents = action.Targets == BattleAction.Target.OneOpponent || action.Targets == BattleAction.Target.AllOpponents;

            for (int i = 0; i < all.Count; i++)
            {
                BattleActor other = all[i];

                if (other.CanAct && (other.PlayerSide != PlayerSide) == wantOpponents)
                {
                    targets.Add(other);
                }
            }

            return targets;
        }

        void OnTurnStarted()
        {
            // Defending lasts until this fighter's next turn.
            IsDefending = false;

            if (controlledBy == Controller.Computer)
            {
                StartCoroutine(ComputerTurn());
            }
            else if (PlayerTurnStarted != null)
            {
                PlayerTurnStarted.Invoke(this);
            }
            else
            {
                Debug.LogWarning($"Battle Actor \"{name}\": it's a player fighter's turn, but there is no UI Battle Menu in the scene to choose an action.", this);
            }
        }

        IEnumerator ComputerTurn()
        {
            if (thinkTime > 0f)
            {
                yield return new WaitForSeconds(thinkTime);
            }

            if (!IsMyTurn)
            {
                yield break;
            }

            BattleAction choice = ChooseAction();

            if (choice == null)
            {
                Say($"{DisplayName} can't do anything.");
                taker.EndTurn();
                yield break;
            }

            List<BattleActor> targets = PossibleTargets(choice);
            Perform(choice, targets[UnityEngine.Random.Range(0, targets.Count)]);
        }

        // Heal when low, otherwise pick a random usable attack, otherwise anything usable.
        BattleAction ChooseAction()
        {
            List<BattleAction> attacks = new List<BattleAction>();
            List<BattleAction> usable = new List<BattleAction>();
            BattleAction heal = null;

            for (int i = 0; i < actions.Count; i++)
            {
                BattleAction action = actions[i];

                if (!CanUse(action))
                {
                    continue;
                }

                usable.Add(action);

                if (action.ActionEffect == BattleAction.Effect.Damage)
                {
                    attacks.Add(action);
                }
                else if (action.ActionEffect == BattleAction.Effect.Heal && heal == null)
                {
                    heal = action;
                }
            }

            bool low = health.Current <= Mathf.FloorToInt(health.Max * healBelow);

            if (heal != null && low)
            {
                return heal;
            }

            if (attacks.Count > 0)
            {
                return attacks[UnityEngine.Random.Range(0, attacks.Count)];
            }

            return usable.Count > 0 ? usable[UnityEngine.Random.Range(0, usable.Count)] : null;
        }

        /// <summary>
        /// Uses an action on a target, then ends this fighter's turn. For actions
        /// that hit everyone, or only the user, the target is ignored.
        /// Does nothing when it isn't this fighter's turn.
        /// </summary>
        public void Perform(BattleAction action, BattleActor target)
        {
            if (!IsMyTurn || busy || action == null)
            {
                return;
            }

            if (!CanUse(action))
            {
                Say($"{DisplayName} can't use {action.DisplayName} now.");
                return;
            }

            StartCoroutine(Act(action, target));
        }

        IEnumerator Act(BattleAction action, BattleActor chosen)
        {
            busy = true;

            if (usesLeft.ContainsKey(action))
            {
                usesLeft[action]--;
            }

            List<BattleActor> targets = PossibleTargets(action);

            if (action.NeedsChosenTarget)
            {
                BattleActor single = chosen != null && targets.Contains(chosen) ? chosen : targets[0];
                targets.Clear();
                targets.Add(single);
            }

            ActionStarted?.Invoke(this, action);
            onAction?.Invoke();

            if (action.Sound != null)
            {
                action.Sound.Play();
            }

            // Lunge toward the first target, apply the action, and step back.
            Vector3 home = lungePart != null ? lungePart.localPosition : Vector3.zero;
            BattleActor lungeTarget = targets.Count > 0 && targets[0] != this ? targets[0] : null;

            if (lungeTarget != null && lungePart != null && lungeDistance > 0f)
            {
                Vector3 toward = (lungeTarget.transform.position - transform.position).normalized * lungeDistance;
                yield return Slide(lungePart, home, home + transform.InverseTransformVector(toward));
            }

            for (int i = 0; i < targets.Count; i++)
            {
                Apply(action, targets[i]);
            }

            if (lungeTarget != null && lungePart != null && lungeDistance > 0f)
            {
                yield return Slide(lungePart, lungePart.localPosition, home);
            }

            yield return new WaitForSeconds(AfterActionPause);

            busy = false;
            taker.EndTurn();
        }

        static IEnumerator Slide(Transform part, Vector3 from, Vector3 to)
        {
            for (float t = 0f; t < LungeTime; t += Time.deltaTime)
            {
                part.localPosition = Vector3.Lerp(from, to, t / LungeTime);
                yield return null;
            }

            part.localPosition = to;
        }

        void Apply(BattleAction action, BattleActor target)
        {
            switch (action.ActionEffect)
            {
                case BattleAction.Effect.Defend:
                    IsDefending = true;
                    onDefend?.Invoke();
                    Say($"{DisplayName} defends.");
                    break;

                case BattleAction.Effect.Heal:
                    int before = target.health.Current;
                    target.health.Heal(action.Power);
                    int healed = target.health.Current - before;
                    Say(target == this
                        ? $"{DisplayName} uses {action.DisplayName} and recovers {healed}."
                        : $"{DisplayName} uses {action.DisplayName}. {target.DisplayName} recovers {healed}.");
                    break;

                case BattleAction.Effect.Damage:
                    if (UnityEngine.Random.Range(0, 100) >= action.Accuracy)
                    {
                        target.onDodged?.Invoke();
                        Say($"{DisplayName} uses {action.DisplayName}, but misses {target.DisplayName}.");
                        break;
                    }

                    int dealt = target.ReceiveDamage(action.Power + attack);
                    Say($"{DisplayName} uses {action.DisplayName}. {target.DisplayName} takes {dealt}{(target.IsDefending ? " (defending)" : "")}.");

                    if (target.health.IsDead)
                    {
                        Say($"{target.DisplayName} is defeated!");
                    }
                    break;
            }
        }

        /// <summary>
        /// Takes damage after Defense, halved while defending, and at least 1.
        /// Returns the health actually lost.
        /// </summary>
        public int ReceiveDamage(int amount)
        {
            int damage = Mathf.Max(1, amount - defense);

            if (IsDefending)
            {
                damage = Mathf.Max(1, Mathf.CeilToInt(damage / 2f));
            }

            int before = health.Current;
            health.TakeDamage(damage);
            int lost = before - health.Current;

            if (lost == 0 && health.IsInvulnerable && !invulnerabilityReported)
            {
                invulnerabilityReported = true;
                Debug.LogWarning($"Battle Actor \"{name}\": a hit was ignored because Health is still invulnerable from the last hit. Set Invulnerable Time to 0 on its Health.", this);
            }

            return lost;
        }

        static void Say(string message)
        {
            Said?.Invoke(message);
        }
    }
}
