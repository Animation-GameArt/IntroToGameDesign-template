using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Sets variable assets back to their Initial Value.
    ///
    /// Variables keep their value when a scene changes or restarts. Put a Variable
    /// Resetter in a scene to start that scene fresh, for example to set the score
    /// back to 0 when a level restarts. Leave a variable out of the list if it
    /// should carry over from scene to scene.
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Variable Resetter")]
    public class VariableResetter : MonoBehaviour
    {
        [Tooltip("The variable assets to reset.")]
        [SerializeField] VariableBase[] variables;

        [Tooltip("Reset the variables as soon as this scene starts.")]
        [SerializeField] bool resetWhenSceneStarts = true;

        void Awake()
        {
            if (resetWhenSceneStarts)
            {
                ResetAll();
            }
        }

        /// <summary>Sets every variable in the list back to its Initial Value.</summary>
        public void ResetAll()
        {
            if (variables == null)
            {
                return;
            }

            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i] != null)
                {
                    variables[i].ResetToInitial();
                }
            }
        }
    }
}
