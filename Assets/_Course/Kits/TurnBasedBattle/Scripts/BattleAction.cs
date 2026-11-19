using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.TurnBasedBattle
{
    /// <summary>
    /// One thing a fighter can do on its turn: an attack, a heal, a defend.
    /// Each action is an asset, so the same "Fireball" can be given to several
    /// fighters, and changing it changes it for all of them.
    ///
    /// Create one with Create → Collin Game → Turn-Based Battle Kit → Battle Action.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Turn-Based Battle Kit/Battle Action", fileName = "New Battle Action", order = 0)]
    public class BattleAction : ScriptableObject
    {
        /// <summary>What the action does.</summary>
        public enum Effect
        {
            /// <summary>Takes health away from the target.</summary>
            Damage,

            /// <summary>Gives health back to the target.</summary>
            Heal,

            /// <summary>The user takes half damage until its next turn.</summary>
            Defend
        }

        /// <summary>Who the action can be used on.</summary>
        public enum Target
        {
            /// <summary>One opponent, chosen from a list.</summary>
            OneOpponent,

            /// <summary>Every opponent at once.</summary>
            AllOpponents,

            /// <summary>The user itself.</summary>
            Self,

            /// <summary>One fighter on the user's side, which may be the user.</summary>
            OneAlly
        }

        [Tooltip("The name on the action's button, such as Slash or Potion. Leave empty to use the asset's name.")]
        [SerializeField] string displayName = "";

        [Tooltip("A short description, shown in the menu when the button is selected.")]
        [SerializeField, TextArea(1, 3)] string description = "";

        [Tooltip("What the action does.")]
        [SerializeField] Effect effect = Effect.Damage;

        [Tooltip("Who the action can be used on.")]
        [SerializeField] Target target = Target.OneOpponent;

        [Tooltip("How strong it is. For damage, the user's Attack is added and the target's Defense is taken away. For a heal, the health given back.")]
        [SerializeField, Min(0)] int power = 3;

        [Tooltip("The chance to hit, from 0 to 100. Heals and defends always work.")]
        [SerializeField, Range(0, 100)] int accuracy = 95;

        [Tooltip("How many times it can be used in one battle. 0 means no limit.")]
        [SerializeField, Min(0)] int usesPerBattle = 0;

        [Tooltip("Optional. A sound to play when the action is used.")]
        [SerializeField] SoundEffect sound;

        /// <summary>The name on the action's button.</summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>A short description of the action.</summary>
        public string Description => description;

        /// <summary>What the action does.</summary>
        public Effect ActionEffect => effect;

        /// <summary>Who the action can be used on.</summary>
        public Target Targets => target;

        /// <summary>How strong it is.</summary>
        public int Power => power;

        /// <summary>The chance to hit, from 0 to 100.</summary>
        public int Accuracy => accuracy;

        /// <summary>How many times it can be used in one battle. 0 means no limit.</summary>
        public int UsesPerBattle => usesPerBattle;

        /// <summary>The sound to play when it is used, or nothing.</summary>
        public SoundEffect Sound => sound;

        /// <summary>True if the player has to choose one target from a list.</summary>
        public bool NeedsChosenTarget => target == Target.OneOpponent || target == Target.OneAlly;
    }
}
