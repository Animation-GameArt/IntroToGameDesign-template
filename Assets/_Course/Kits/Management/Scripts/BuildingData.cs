using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.Management
{
    /// <summary>
    /// A kind of building, such as a farm, a mill or a house: what it costs to
    /// build, and what it does every turn. The UI Build Grid lets the player
    /// drag buildings onto the map.
    ///
    /// Create one with Create → Collin Game → Management Kit → Building.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Management Kit/Building", fileName = "New Building", order = 2)]
    public class BuildingData : ScriptableObject
    {
        [Tooltip("The name on the card. Leave empty to use the asset's name.")]
        [SerializeField] string displayName = "";

        [Tooltip("Optional. The picture on the card and on the map.")]
        [SerializeField] Sprite icon;

        [Tooltip("A short description, such as +3 Food each turn.")]
        [SerializeField, TextArea(1, 3)] string description = "";

        [Tooltip("What it costs to build. Use negative amounts, such as Gold −50.")]
        [SerializeField] List<Effect> cost = new List<Effect>();

        [Tooltip("What each one does every turn, such as Food +3.")]
        [SerializeField] List<Effect> everyTurn = new List<Effect>();

        [Tooltip("Optional. A number variable that counts how many are built. Turn Rules can use it, such as Food +1 per Farm.")]
        [SerializeField] IntVariable count;

        /// <summary>The name on the card.</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>The picture, or null.</summary>
        public Sprite Icon => icon;

        /// <summary>The description.</summary>
        public string Description => description;

        /// <summary>The number variable counting how many are built, or null.</summary>
        public IntVariable Count => count;

        /// <summary>True if every cost can be paid without a resource going below 0.</summary>
        public bool CanAfford()
        {
            foreach (Effect effect in cost)
            {
                if (effect.resource != null && effect.resource.Value + effect.Preview() < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Pays the cost.</summary>
        public void PayCost()
        {
            foreach (Effect effect in cost)
            {
                effect.Apply();
            }
        }

        /// <summary>Applies one building's every-turn effects.</summary>
        public void ApplyTurn()
        {
            foreach (Effect effect in everyTurn)
            {
                effect.Apply();
            }
        }

        /// <summary>The cost as text, such as "Gold 50, Wood 10".</summary>
        public string CostText()
        {
            List<string> parts = new List<string>();

            foreach (Effect effect in cost)
            {
                if (effect.resource != null)
                {
                    parts.Add($"{effect.resource.name} {Mathf.Abs(effect.Preview())}");
                }
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "Free";
        }
    }
}
