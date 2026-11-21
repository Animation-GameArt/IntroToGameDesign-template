using UnityEngine;

namespace CollinGame.Kits.Platformer3D
{
    /// <summary>
    /// A round shadow on the ground under the player, so the player can tell
    /// where they'll land. 3D platformers almost always have one.
    ///
    /// Setup: add it to the player, and drag in a flat shadow object that is
    /// not a child of the player: a Quad rotated to face up, with a
    /// see-through dark circle material. The kit guide shows how.
    /// </summary>
    [AddComponentMenu("Collin Game/3D Platformer Kit/Blob Shadow 3D")]
    public class BlobShadow3D : MonoBehaviour
    {
        [Tooltip("The flat shadow object.")]
        [SerializeField] Transform shadow;

        [Tooltip("The farthest the ground can be below the player for the shadow to show.")]
        [SerializeField, Min(0.1f)] float maxDistance = 20f;

        [Tooltip("The shadow's size when the player is on the ground.")]
        [SerializeField, Min(0.01f)] float size = 0.9f;

        [Tooltip("The shadow's size at Max Distance, as a fraction of Size.")]
        [SerializeField, Range(0f, 1f)] float farSize = 0.4f;

        [Tooltip("What counts as ground. Untick the player's own layer if the shadow sits on the player.")]
        [SerializeField] LayerMask groundLayers = ~0;

        void LateUpdate()
        {
            if (shadow == null)
            {
                return;
            }

            Vector3 from = transform.position + Vector3.up * 0.2f;
            RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, maxDistance, groundLayers, QueryTriggerInteraction.Ignore);
            bool found = false;
            RaycastHit nearest = default;

            foreach (RaycastHit hit in hits)
            {
                // Skip the player's own colliders.
                if (hit.transform.IsChildOf(transform) || (found && hit.distance >= nearest.distance))
                {
                    continue;
                }

                nearest = hit;
                found = true;
            }

            shadow.gameObject.SetActive(found);

            if (!found)
            {
                return;
            }

            shadow.position = nearest.point + nearest.normal * 0.02f;
            shadow.rotation = Quaternion.FromToRotation(Vector3.forward, -nearest.normal);
            float t = Mathf.Clamp01(nearest.distance / maxDistance);
            shadow.localScale = Vector3.one * size * Mathf.Lerp(1f, farSize, t);
        }
    }
}
