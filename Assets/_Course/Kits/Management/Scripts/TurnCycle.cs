using System;
using System.Collections.Generic;
using System.Text;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace CollinGame.Kits.Management
{
    /// <summary>
    /// Runs the turns of a management game. When a turn ends (the End Turn
    /// button, or a timer), it:
    ///   1. applies every Turn Rule, in order;
    ///   2. applies every built building's every-turn effects;
    ///   3. may pick a random Event, and wait for the player's choice;
    ///   4. writes a report of what changed;
    ///   5. checks whether the player has survived enough turns to win.
    /// Losing is up to Int Variable Conditions, such as People at most 0.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Management Kit/Turn Cycle")]
    public class TurnCycle : MonoBehaviour
    {
        [Header("Turns")]

        [Tooltip("Optional. A number variable for the turn number, for the HUD.")]
        [SerializeField] IntVariable turnNumber;

        [Tooltip("The player wins after this many turns. 0 means the game never ends by itself.")]
        [SerializeField, Min(0)] int turnsToWin = 10;

        [Tooltip("Seconds per turn, for a game that runs by itself. 0 means turns end only with the End Turn button.")]
        [SerializeField, Min(0f)] float secondsPerTurn = 0f;

        [Header("Rules")]

        [Tooltip("What happens every turn, in order.")]
        [SerializeField] List<TurnRule> rules = new List<TurnRule>();

        [Tooltip("The resources to show in the report, and to keep from going below 0.")]
        [SerializeField] List<IntVariable> resources = new List<IntVariable>();

        [Tooltip("Ticked: after every turn, resources below 0 are set to 0. Rules can still check for shortages first.")]
        [SerializeField] bool noNegativeResources = true;

        [Header("Events")]

        [Tooltip("The random events that can happen.")]
        [SerializeField] List<ManagementEvent> events = new List<ManagementEvent>();

        [Tooltip("The chance of an event each turn, from 0 to 1.")]
        [SerializeField, Range(0f, 1f)] float eventChance = 0.5f;

        [Header("Report and results")]

        [Tooltip("Optional. A text variable for the end-of-turn report.")]
        [SerializeField] StringVariable report;

        [Tooltip("Optional. An event to raise when the player wins.")]
        [SerializeField] VoidEventChannel raiseOnWin;

        [Tooltip("Runs at the end of every turn, after the report.")]
        [SerializeField] UnityEvent onTurnEnded;

        [Tooltip("Runs when the player wins.")]
        [SerializeField] UnityEvent onWon;

        readonly List<BuildingData> buildings = new List<BuildingData>();
        readonly HashSet<ManagementEvent> usedEvents = new HashSet<ManagementEvent>();
        readonly Dictionary<IntVariable, int> before = new Dictionary<IntVariable, int>();
        readonly StringBuilder notes = new StringBuilder();
        float nextTick;
        bool over;

        /// <summary>The turn cycle in the open scene.</summary>
        public static TurnCycle Instance { get; private set; }

        /// <summary>Raised when an event with choices needs an answer. The UI Management Screen shows it.</summary>
        public event Action<ManagementEvent> ChoiceNeeded;

        /// <summary>Raised with the report text at the end of every turn.</summary>
        public event Action<string> ReportReady;

        /// <summary>The turn being played, counting from 1.</summary>
        public int Turn { get; private set; } = 1;

        /// <summary>The event waiting for an answer, or null.</summary>
        public ManagementEvent PendingEvent { get; private set; }

        /// <summary>True once the game is won or lost.</summary>
        public bool IsOver => over;

        /// <summary>Every building built so far.</summary>
        public IReadOnlyList<BuildingData> Buildings => buildings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            SetTurnNumber();
            nextTick = Time.time + secondsPerTurn;

            if (report != null)
            {
                report.SetValue(string.Empty);
            }
        }

        void Update()
        {
            if (secondsPerTurn > 0f && !over && PendingEvent == null && !PauseController.GamePaused && Time.time >= nextTick)
            {
                EndTurn();
            }
        }

        /// <summary>Adds a building to the turn cycle. The UI Build Grid calls this.</summary>
        public void AddBuilding(BuildingData building)
        {
            if (building == null)
            {
                return;
            }

            buildings.Add(building);

            if (building.Count != null)
            {
                building.Count.Add(1);
            }
        }

        /// <summary>Removes one building of a kind.</summary>
        public void RemoveBuilding(BuildingData building)
        {
            if (buildings.Remove(building) && building.Count != null)
            {
                building.Count.Add(-1);
            }
        }

        /// <summary>Ends the turn. Wire the End Turn button to this.</summary>
        public void EndTurn()
        {
            if (over || PendingEvent != null)
            {
                return;
            }

            nextTick = Time.time + secondsPerTurn;
            notes.Clear();
            Remember();

            foreach (TurnRule rule in rules)
            {
                if (rule != null)
                {
                    rule.Apply();
                }
            }

            foreach (BuildingData building in buildings)
            {
                building.ApplyTurn();
            }

            ManagementEvent picked = PickEvent();

            if (picked != null)
            {
                notes.AppendLine(picked.Title);

                if (picked.Choices.Count > 0)
                {
                    // Wait for the player; the report is written once they choose.
                    PendingEvent = picked;
                    ChoiceNeeded?.Invoke(picked);
                    return;
                }

                picked.ApplyEffects();

                if (!string.IsNullOrEmpty(picked.Text))
                {
                    notes.AppendLine(picked.Text);
                }
            }

            FinishTurn();
        }

        /// <summary>Answers the pending event with one of its choices, counting from 0.</summary>
        public void Choose(int index)
        {
            ManagementEvent pending = PendingEvent;

            if (pending == null || index < 0 || index >= pending.Choices.Count)
            {
                return;
            }

            PendingEvent = null;
            pending.ApplyEffects();
            ManagementEvent.Choice choice = pending.Choices[index];

            foreach (Effect effect in choice.effects)
            {
                effect.Apply();
            }

            notes.AppendLine($"You chose: {choice.label}");

            if (!string.IsNullOrEmpty(choice.result))
            {
                notes.AppendLine(choice.result);
            }

            FinishTurn();
        }

        void FinishTurn()
        {
            if (noNegativeResources)
            {
                foreach (IntVariable resource in resources)
                {
                    if (resource != null && resource.Value < 0)
                    {
                        resource.SetValue(0);
                    }
                }
            }

            string text = BuildReport();

            if (report != null)
            {
                report.SetValue(text);
            }

            ReportReady?.Invoke(text);

            Turn++;
            SetTurnNumber();
            onTurnEnded?.Invoke();

            if (turnsToWin > 0 && Turn > turnsToWin && !over)
            {
                over = true;

                if (raiseOnWin != null)
                {
                    raiseOnWin.Raise();
                }

                onWon?.Invoke();
            }
        }

        /// <summary>Stops the turns, such as when the game is lost. Call it from a lose condition.</summary>
        public void StopGame()
        {
            over = true;
        }

        ManagementEvent PickEvent()
        {
            if (events.Count == 0 || Random.value > eventChance)
            {
                return null;
            }

            float total = 0f;

            foreach (ManagementEvent candidate in events)
            {
                if (Allowed(candidate))
                {
                    total += candidate.Weight;
                }
            }

            float roll = Random.Range(0f, total);

            foreach (ManagementEvent candidate in events)
            {
                if (!Allowed(candidate))
                {
                    continue;
                }

                roll -= candidate.Weight;

                if (roll <= 0f)
                {
                    if (candidate.OnlyOnce)
                    {
                        usedEvents.Add(candidate);
                    }

                    return candidate;
                }
            }

            return null;
        }

        bool Allowed(ManagementEvent candidate)
        {
            return candidate != null && candidate.CanHappen(Turn) && !(candidate.OnlyOnce && usedEvents.Contains(candidate));
        }

        void Remember()
        {
            before.Clear();

            foreach (IntVariable resource in resources)
            {
                if (resource != null)
                {
                    before[resource] = resource.Value;
                }
            }
        }

        string BuildReport()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine($"Turn {Turn}");

            foreach (IntVariable resource in resources)
            {
                if (resource == null || !before.TryGetValue(resource, out int was))
                {
                    continue;
                }

                int change = resource.Value - was;
                string changeText = change > 0 ? $"+{change}" : (change < 0 ? change.ToString() : "no change");
                text.AppendLine($"{resource.name}: {resource.Value}  ({changeText})");
            }

            if (notes.Length > 0)
            {
                text.AppendLine();
                text.Append(notes);
            }

            return text.ToString().TrimEnd();
        }

        void SetTurnNumber()
        {
            if (turnNumber != null)
            {
                turnNumber.SetValue(Turn);
            }
        }
    }
}
