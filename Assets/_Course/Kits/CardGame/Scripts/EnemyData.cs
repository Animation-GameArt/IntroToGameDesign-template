using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Kits.CardGame
{
    /// <summary>What an enemy does on its turn.</summary>
    public enum IntentType
    {
        Attack,
        Block,
        Heal
    }

    /// <summary>One enemy move, such as Attack 4 three times.</summary>
    [Serializable]
    public class Intent
    {
        public IntentType type = IntentType.Attack;
        [Min(0)] public int amount = 6;
        [Min(1)] public int times = 1;

        public string Describe()
        {
            string text = type switch
            {
                IntentType.Attack => $"Attack {amount}",
                IntentType.Block => $"Block {amount}",
                IntentType.Heal => $"Heal {amount}",
                _ => string.Empty
            };

            return times > 1 ? $"{text} x{times}" : text;
        }
    }

    /// <summary>
    /// An enemy: its health, its picture, and the moves it makes, in order.
    /// The player always sees the next move, so they can plan.
    /// Create one with Create > Collin Game > Card Game Kit > Enemy.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Card Game Kit/Enemy", fileName = "New Enemy")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] string displayName = "Enemy";
        [SerializeField] Sprite portrait;
        [SerializeField, Min(1)] int maxHealth = 30;

        [Tooltip("Its moves. It makes them in order, then starts again from the top.")]
        [SerializeField] List<Intent> moves = new List<Intent>();

        [Tooltip("Ticked: it picks its moves at random instead of in order.")]
        [SerializeField] bool randomOrder;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Portrait => portrait;
        public int MaxHealth => maxHealth;
        public IReadOnlyList<Intent> Moves => moves;
        public bool RandomOrder => randomOrder;
    }
}
