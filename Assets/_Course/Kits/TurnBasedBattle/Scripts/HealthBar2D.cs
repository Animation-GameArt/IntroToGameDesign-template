using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.TurnBasedBattle
{
    /// <summary>
    /// A health bar drawn in the game world, above a fighter or an enemy. The
    /// fill sprite shrinks from the right as health is lost.
    ///
    /// Setup: make two child sprites of the fighter, a dark Background and a
    /// colored Fill on top of it, the same size. Add this to the fighter (or to
    /// the bar's parent) and drag the Fill into Fill.
    /// </summary>
    [AddComponentMenu("Collin Game/Turn-Based Battle Kit/Health Bar 2D")]
    public class HealthBar2D : MonoBehaviour
    {
        [Tooltip("The Health to show. Leave empty to use the Health on this object or a parent.")]
        [SerializeField] Health health;

        [Tooltip("The sprite that shrinks as health is lost.")]
        [SerializeField] SpriteRenderer fill;

        [Tooltip("Optional. Hidden while health is full, and shown once damage is taken. Usually the whole bar; then put this component on the fighter, not on the bar.")]
        [SerializeField] GameObject hideWhenFull;

        Vector3 fullScale;
        Vector3 fullPosition;
        float fullWidth;

        void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (fill == null)
            {
                Debug.LogWarning($"Health Bar 2D on \"{name}\": Fill is empty. Drag in the sprite that should shrink.", this);
                return;
            }

            fullScale = fill.transform.localScale;
            fullPosition = fill.transform.localPosition;
            fullWidth = fill.sprite != null ? fill.sprite.bounds.size.x * fullScale.x : fullScale.x;
        }

        void OnEnable()
        {
            if (health == null)
            {
                Debug.LogWarning($"Health Bar 2D on \"{name}\": there is no Health to show. Drag one into Health.", this);
                return;
            }

            health.Changed += Show;
            Show(health.Current);
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Changed -= Show;
            }
        }

        void Show(int current)
        {
            if (fill == null || health == null)
            {
                return;
            }

            float share = health.Max > 0 ? Mathf.Clamp01((float)current / health.Max) : 0f;

            // Shrink from the right, keeping the left edge in place.
            Vector3 scale = fullScale;
            scale.x = fullScale.x * share;
            fill.transform.localScale = scale;

            Vector3 position = fullPosition;
            position.x = fullPosition.x - (1f - share) * fullWidth * 0.5f;
            fill.transform.localPosition = position;

            if (hideWhenFull != null)
            {
                hideWhenFull.SetActive(current < health.Max);
            }
        }
    }
}
