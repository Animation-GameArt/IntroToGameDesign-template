using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// When this object's health runs out, it breaks into smaller pieces, as
    /// a big asteroid splits into two medium ones, and a medium one into two small.
    ///
    /// Setup: needs a Health component (When Health Runs Out: Destroy). Make a
    /// prefab for each size, and drag the next size down into Piece Prefab. The
    /// smallest size has no Split On Death 2D.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Split On Death 2D")]
    public class SplitOnDeath2D : MonoBehaviour
    {
        [Tooltip("The prefab of the smaller pieces.")]
        [SerializeField] GameObject piecePrefab;

        [Tooltip("How many pieces to create.")]
        [SerializeField, Min(1)] int pieces = 2;

        [Tooltip("How far from the middle each piece appears, in units.")]
        [SerializeField, Min(0f)] float spread = 0.3f;

        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            health.Died += Split;
        }

        void OnDisable()
        {
            health.Died -= Split;
        }

        void Split()
        {
            if (piecePrefab == null)
            {
                return;
            }

            // Spread the pieces evenly around the middle, from a random starting angle.
            float start = Random.Range(0f, 360f);

            for (int i = 0; i < pieces; i++)
            {
                float angle = (start + 360f / pieces * i) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spread;
                Instantiate(piecePrefab, transform.position + offset, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            }
        }
    }
}
