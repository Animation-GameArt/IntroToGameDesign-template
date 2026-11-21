using UnityEngine;

namespace CollinGame.Kits.TowerDefense
{
    /// <summary>
    /// The road enemies follow, from the first point to the last. The points
    /// are this object's children, in order: add an empty child for each corner.
    ///
    /// Select it to see the path in the Scene view.
    /// </summary>
    [AddComponentMenu("Collin Game/Tower Defense Kit/Enemy Path 2D")]
    public class EnemyPath2D : MonoBehaviour
    {
        /// <summary>How many points the path has.</summary>
        public int Count => transform.childCount;

        /// <summary>The world position of a point.</summary>
        public Vector2 Point(int index) => transform.GetChild(Mathf.Clamp(index, 0, transform.childCount - 1)).position;

        /// <summary>The total length of the path, in units.</summary>
        public float Length
        {
            get
            {
                float length = 0f;

                for (int i = 1; i < Count; i++)
                {
                    length += Vector2.Distance(Point(i - 1), Point(i));
                }

                return length;
            }
        }

        void OnDrawGizmos()
        {
            if (transform.childCount < 2)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);

            for (int i = 1; i < transform.childCount; i++)
            {
                Gizmos.DrawLine(Point(i - 1), Point(i));
            }

            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
            Gizmos.DrawWireSphere(Point(0), 0.3f);
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            Gizmos.DrawWireSphere(Point(transform.childCount - 1), 0.3f);
        }
    }
}
