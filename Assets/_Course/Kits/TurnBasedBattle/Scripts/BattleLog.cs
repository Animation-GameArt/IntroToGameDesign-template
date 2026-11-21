using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.TurnBasedBattle
{
    /// <summary>
    /// Keeps the last few lines of what happened in the battle ("Slime attacks.
    /// Hero takes 3.") in a text variable, newest at the bottom.
    ///
    /// Show the variable with a UI Variable Binder on the battle screen, using
    /// the label named "log-label" in BattleUI.
    /// </summary>
    [AddComponentMenu("Collin Game/Turn-Based Battle Kit/Battle Log")]
    public class BattleLog : MonoBehaviour
    {
        [Tooltip("The text variable to write the log to.")]
        [SerializeField] StringVariable logText;

        [Tooltip("How many lines to keep.")]
        [SerializeField, Min(1)] int linesToKeep = 4;

        readonly Queue<string> lines = new Queue<string>();

        void OnEnable()
        {
            if (logText == null)
            {
                Debug.LogWarning($"Battle Log on \"{name}\": Log Text is empty. Drag in a text variable.", this);
                return;
            }

            lines.Clear();
            logText.SetValue(string.Empty);
            BattleActor.Said += Add;
        }

        void OnDisable()
        {
            BattleActor.Said -= Add;
        }

        /// <summary>Adds a line to the log. Call it from an event to add your own messages.</summary>
        public void Add(string line)
        {
            if (logText == null || string.IsNullOrEmpty(line))
            {
                return;
            }

            lines.Enqueue(line);

            while (lines.Count > linesToKeep)
            {
                lines.Dequeue();
            }

            logText.SetValue(string.Join("\n", lines));
        }
    }
}
