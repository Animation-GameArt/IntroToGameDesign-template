using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Kits.Management
{
    /// <summary>
    /// Something that happens every turn, such as "People eat" or "Fields grow".
    /// Each rule is a list of Effects, applied in order at the end of every turn.
    ///
    /// Create one with Create → Collin Game → Management Kit → Turn Rule.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Management Kit/Turn Rule", fileName = "New Turn Rule", order = 0)]
    public class TurnRule : ScriptableObject
    {
        [Tooltip("A short note on what this rule does, for the designer.")]
        [SerializeField, TextArea(1, 3)] string description = "";

        [Tooltip("The changes, applied in order.")]
        [SerializeField] List<Effect> effects = new List<Effect>();

        /// <summary>The rule's description.</summary>
        public string Description => description;

        /// <summary>Applies every effect.</summary>
        public void Apply()
        {
            foreach (Effect effect in effects)
            {
                effect.Apply();
            }
        }
    }
}
