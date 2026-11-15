using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Shows a floating number above an object each time its health changes:
    /// "-1" when it is hurt, "+1" when it is healed.
    /// Add it to the same GameObject as the Health component.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [AddComponentMenu("Collin Game/UI/Health Popups")]
    public class HealthPopups : MonoBehaviour
    {
        [Tooltip("The color of damage numbers.")]
        [SerializeField] Color damageColor = new Color(1f, 0.25f, 0.2f);

        [Tooltip("The color of healing numbers.")]
        [SerializeField] Color healColor = new Color(0.3f, 0.9f, 0.4f);

        [Tooltip("The size of the numbers.")]
        [SerializeField, Min(0.1f)] float fontSize = 5f;

        [Tooltip("Where the numbers appear, measured from this object's position.")]
        [SerializeField] Vector3 offset = new Vector3(0f, 1.2f, 0f);

        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Healed += OnHealed;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Healed -= OnHealed;
        }

        void OnDamaged(int amount)
        {
            FloatingText.Spawn("-" + amount, transform.position + offset, damageColor, fontSize);
        }

        void OnHealed(int amount)
        {
            FloatingText.Spawn("+" + amount, transform.position + offset, healColor, fontSize);
        }
    }
}
