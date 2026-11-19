using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.PhysicsPuzzle
{
    /// <summary>
    /// Something the player can cut by swiping across it, such as a rope link.
    /// Cutting switches off the joints on this object, so whatever hung from it
    /// falls.
    ///
    /// Setup: this GameObject needs a 2D collider. The scene needs a Cutter 2D.
    /// Ropes made by Rope 2D get this on every link automatically.
    /// </summary>
    [AddComponentMenu("Collin Game/Physics Puzzle Kit/Cuttable 2D")]
    public class Cuttable2D : MonoBehaviour
    {
        [Tooltip("Ticked: the object disappears when cut. Unticked: only its joints are switched off.")]
        [SerializeField] bool hideWhenCut = true;

        [Tooltip("Runs when this is cut.")]
        [SerializeField] UnityEvent onCut;

        bool cut;

        /// <summary>True once this has been cut.</summary>
        public bool IsCut => cut;

        /// <summary>Cuts this right now.</summary>
        public void Cut()
        {
            if (cut)
            {
                return;
            }

            cut = true;

            Joint2D[] joints = GetComponents<Joint2D>();

            for (int i = 0; i < joints.Length; i++)
            {
                joints[i].enabled = false;
            }

            onCut?.Invoke();

            if (hideWhenCut)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
