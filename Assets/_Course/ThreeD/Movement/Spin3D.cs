using UnityEngine;

namespace CollinGame.ThreeD
{
    /// <summary>
    /// Spins and bobs an object, such as a coin or a key, so it catches the eye.
    /// </summary>
    [AddComponentMenu("Collin Game/3D/Spin 3D")]
    public class Spin3D : MonoBehaviour
    {
        [Tooltip("Degrees per second around each axis.")]
        [SerializeField] Vector3 spin = new Vector3(0f, 120f, 0f);

        [Tooltip("How far it bobs up and down, in units. 0 for none.")]
        [SerializeField, Min(0f)] float bobHeight = 0.15f;

        [Tooltip("Bobs per second.")]
        [SerializeField, Min(0f)] float bobSpeed = 1f;

        Vector3 start;
        float offset;

        void Start()
        {
            start = transform.localPosition;
            offset = Random.value * 10f;
        }

        void Update()
        {
            transform.Rotate(spin * Time.deltaTime, Space.Self);

            if (bobHeight > 0f)
            {
                transform.localPosition = start + Vector3.up * (Mathf.Sin((Time.time + offset) * bobSpeed * Mathf.PI * 2f) * bobHeight);
            }
        }
    }
}
