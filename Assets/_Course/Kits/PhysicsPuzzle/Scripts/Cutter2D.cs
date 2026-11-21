using UnityEngine;

namespace CollinGame.Kits.PhysicsPuzzle
{
    /// <summary>
    /// Lets the player cut things by swiping across them with the mouse or a
    /// finger. Anything with a Cuttable 2D that the swipe crosses is cut.
    /// Put one in any scene that has ropes or other cuttable things.
    /// </summary>
    [AddComponentMenu("Collin Game/Physics Puzzle Kit/Cutter 2D")]
    public class Cutter2D : MonoBehaviour
    {
        [Tooltip("Optional. A trail that follows the swipe, so the player can see it.")]
        [SerializeField] TrailRenderer trail;

        readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        Vector2 lastPosition;
        bool swiping;

        void Update()
        {
            if (Time.timeScale == 0f || !PointerUtility.TryGetWorldPosition(out Vector2 position))
            {
                EndSwipe();
                return;
            }

            if (!PointerUtility.IsPressed)
            {
                EndSwipe();
                return;
            }

            if (!swiping)
            {
                swiping = true;
                lastPosition = position;

                if (trail != null)
                {
                    trail.transform.position = position;
                    trail.Clear();
                    trail.emitting = true;
                }

                return;
            }

            if (trail != null)
            {
                trail.transform.position = position;
            }

            // Everything crossed since the last frame, including triggers.
            ContactFilter2D filter = ContactFilter2D.noFilter;

            int count = Physics2D.Linecast(lastPosition, position, filter, hits);

            for (int i = 0; i < count; i++)
            {
                Cuttable2D cuttable = hits[i].collider != null ? hits[i].collider.GetComponent<Cuttable2D>() : null;

                if (cuttable != null)
                {
                    cuttable.Cut();
                }
            }

            lastPosition = position;
        }

        void EndSwipe()
        {
            swiping = false;

            if (trail != null)
            {
                trail.emitting = false;
            }
        }
    }
}
