using UnityEngine;

namespace CollinGame.Kits.EndlessRunner
{
    /// <summary>
    /// One piece of track for the Track Spawner 2D: some ground, gaps,
    /// obstacles and coins, starting at this object's position and running
    /// Length units to the right.
    ///
    /// Build each chunk as a prefab, with its ground starting at x = 0 and its
    /// height lined up with the other chunks, so they join without a step.
    /// Select it to see where it starts and ends.
    /// </summary>
    [AddComponentMenu("Collin Game/Endless Runner Kit/Track Chunk")]
    public class TrackChunk : MonoBehaviour
    {
        [Tooltip("How long the chunk is, in units, from its position to where the next chunk starts.")]
        [SerializeField, Min(1f)] float length = 20f;

        [Tooltip("The chunk only appears once the difficulty (0 at the start, 1 at top speed) reaches this.")]
        [SerializeField, Range(0f, 1f)] float minDifficulty = 0f;

        [Tooltip("The chunk stops appearing once the difficulty passes this.")]
        [SerializeField, Range(0f, 1f)] float maxDifficulty = 1f;

        [Tooltip("How often this chunk is picked compared with the others. 2 is twice as often as 1.")]
        [SerializeField, Min(0f)] float weight = 1f;

        /// <summary>How long the chunk is, in units.</summary>
        public float Length => length;

        /// <summary>True if the chunk can appear at this difficulty.</summary>
        public bool AllowedAt(float difficulty) => difficulty >= minDifficulty && difficulty <= maxDifficulty;

        /// <summary>How often this chunk is picked compared with the others.</summary>
        public float Weight => weight;

        void OnDrawGizmos()
        {
            Vector3 start = transform.position;
            Vector3 end = start + Vector3.right * length;

            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
            Gizmos.DrawLine(start + Vector3.down * 3f, start + Vector3.up * 6f);
            Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.9f);
            Gizmos.DrawLine(end + Vector3.down * 3f, end + Vector3.up * 6f);
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            Gizmos.DrawLine(start, end);
        }
    }
}
