using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Kits.Management
{
    /// <summary>
    /// A random event, such as "Rats in the granary" or "A merchant arrives".
    /// It can just happen (its Effects apply at once), or offer the player a
    /// choice of two or three answers, each with its own effects, as in Reigns.
    ///
    /// Create one with Create → Collin Game → Management Kit → Event.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Management Kit/Event", fileName = "New Event", order = 1)]
    public class ManagementEvent : ScriptableObject
    {
        /// <summary>One answer to an event.</summary>
        [Serializable]
        public class Choice
        {
            [Tooltip("The button text, such as Pay the merchant.")]
            public string label = "OK";

            [Tooltip("What happens if this answer is chosen.")]
            public List<Effect> effects = new List<Effect>();

            [Tooltip("Optional. Shown in the report after choosing.")]
            [TextArea(1, 3)] public string result = "";
        }

        [Tooltip("The event's title.")]
        [SerializeField] string title = "";

        [Tooltip("What happened.")]
        [SerializeField, TextArea(2, 5)] string text = "";

        [Tooltip("How often it's picked compared with other events. 2 is twice as often as 1.")]
        [SerializeField, Min(0f)] float weight = 1f;

        [Tooltip("It can't happen before this turn.")]
        [SerializeField, Min(1)] int earliestTurn = 1;

        [Tooltip("Ticked: it only happens once per game.")]
        [SerializeField] bool onlyOnce = false;

        [Tooltip("Optional. It only happens when this condition is met, such as Gold at least 100. Only the Only When fields are used.")]
        [SerializeField] Effect condition = new Effect();

        [Tooltip("Effects that happen at once, with no choice.")]
        [SerializeField] List<Effect> effects = new List<Effect>();

        [Tooltip("Optional. Two or three answers for the player to choose from.")]
        [SerializeField] List<Choice> choices = new List<Choice>();

        /// <summary>The event's title.</summary>
        public string Title => string.IsNullOrEmpty(title) ? name : title;

        /// <summary>What happened.</summary>
        public string Text => text;

        /// <summary>How often it's picked.</summary>
        public float Weight => weight;

        /// <summary>True if it only happens once per game.</summary>
        public bool OnlyOnce => onlyOnce;

        /// <summary>The answers, if any.</summary>
        public IReadOnlyList<Choice> Choices => choices;

        /// <summary>True if it can happen on this turn.</summary>
        public bool CanHappen(int turn) => turn >= earliestTurn && weight > 0f && (condition == null || condition.ConditionMet());

        /// <summary>Applies the effects that happen at once.</summary>
        public void ApplyEffects()
        {
            foreach (Effect effect in effects)
            {
                effect.Apply();
            }
        }
    }
}
