using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CollinGame.Kits.CardGame
{
    /// <summary>What a card effect does.</summary>
    public enum CardEffectType
    {
        /// <summary>Damages the enemy. Their block is used up first.</summary>
        Damage,
        /// <summary>Gives the player block, which stops damage until the player's next turn.</summary>
        Block,
        /// <summary>Gives the player health back, up to their maximum.</summary>
        Heal,
        /// <summary>Draws more cards.</summary>
        Draw,
        /// <summary>Gives more energy this turn.</summary>
        Energy
    }

    /// <summary>One thing a card does, such as Damage 6, or Damage 3 twice.</summary>
    [Serializable]
    public class CardEffect
    {
        public CardEffectType type = CardEffectType.Damage;

        [Min(0)] public int amount = 5;

        [Tooltip("How many times it happens, such as 3 for a card that hits three times.")]
        [Min(1)] public int times = 1;

        public string Describe()
        {
            string text = type switch
            {
                CardEffectType.Damage => $"Deal {amount}",
                CardEffectType.Block => $"Block {amount}",
                CardEffectType.Heal => $"Heal {amount}",
                CardEffectType.Draw => amount == 1 ? "Draw 1 card" : $"Draw {amount} cards",
                CardEffectType.Energy => $"Gain {amount} energy",
                _ => string.Empty
            };

            return times > 1 ? $"{text} x{times}" : text;
        }
    }

    /// <summary>
    /// A card: its cost, what it does, and how it looks.
    /// Create one with Create > Collin Game > Card Game Kit > Card.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Card Game Kit/Card", fileName = "New Card")]
    public class CardData : ScriptableObject
    {
        [Tooltip("The card's name.")]
        [SerializeField] string displayName = "Card";

        [Tooltip("Optional. The whole card's picture, such as a playing card. Empty: the screen's blank card is used, with the name written on it.")]
        [SerializeField] Sprite face;

        [Tooltip("Ticked: the name is written on the card. Untick it for pictures that already show it.")]
        [SerializeField] bool showName = true;

        [Tooltip("Energy it costs to play.")]
        [SerializeField, Min(0)] int cost = 1;

        [Tooltip("What it does, in order.")]
        [SerializeField] List<CardEffect> effects = new List<CardEffect>();

        [Tooltip("Ticked: after it's played, it's gone for the rest of the battle instead of going to the discard pile.")]
        [SerializeField] bool exhaust;

        [Tooltip("Optional. The text under the card. Empty: it's written from the effects, such as \"Deal 6. Block 3.\"")]
        [SerializeField, TextArea(1, 3)] string description;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Face => face;
        public bool ShowName => showName;
        public int Cost => cost;
        public IReadOnlyList<CardEffect> Effects => effects;
        public bool Exhaust => exhaust;

        /// <summary>The text under the card.</summary>
        public string Description
        {
            get
            {
                if (!string.IsNullOrEmpty(description))
                {
                    return description;
                }

                StringBuilder text = new StringBuilder();

                foreach (CardEffect effect in effects)
                {
                    if (effect != null)
                    {
                        text.Append(effect.Describe()).Append(". ");
                    }
                }

                if (exhaust)
                {
                    text.Append("Exhaust.");
                }

                return text.ToString().Trim();
            }
        }
    }
}
