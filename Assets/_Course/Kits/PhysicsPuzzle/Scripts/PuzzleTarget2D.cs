using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.PhysicsPuzzle
{
    /// <summary>
    /// Marks an object as a target the player must get rid of. It counts itself
    /// in Targets Left when the scene starts, and counts itself out when it's
    /// switched off, for example when a Breakable 2D on it breaks.
    ///
    /// The Puzzle Rules component uses Targets Left to decide when the level is won.
    /// </summary>
    [AddComponentMenu("Collin Game/Physics Puzzle Kit/Puzzle Target 2D")]
    public class PuzzleTarget2D : MonoBehaviour
    {
        [Tooltip("The number variable that counts the targets left. Use the same one as Puzzle Rules.")]
        [SerializeField] IntVariable targetsLeft;

        bool counted;

        void Start()
        {
            // Counted in Start, after Puzzle Rules has set the count to 0 in Awake.
            if (targetsLeft == null)
            {
                Debug.LogWarning($"Puzzle Target 2D on \"{name}\": Targets Left is empty, so this target isn't counted.", this);
                return;
            }

            targetsLeft.Add(1);
            counted = true;
        }

        void OnDisable()
        {
            if (counted && targetsLeft != null)
            {
                counted = false;
                targetsLeft.Add(-1);
            }
        }
    }
}
