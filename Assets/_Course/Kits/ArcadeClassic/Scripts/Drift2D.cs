using UnityEngine;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// Sets an object drifting in a random direction, slowly spinning, as soon
    /// as it appears: asteroids, space junk, bubbles.
    ///
    /// Setup: needs a Rigidbody 2D. Gravity is switched off for you.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Drift 2D")]
    public class Drift2D : MonoBehaviour
    {
        [Tooltip("The slowest it drifts, in units per second.")]
        [SerializeField, Min(0f)] float minSpeed = 1f;

        [Tooltip("The fastest it drifts, in units per second.")]
        [SerializeField, Min(0f)] float maxSpeed = 2.5f;

        [Tooltip("The fastest it spins, in degrees per second. It spins either way, at a random speed up to this.")]
        [SerializeField, Min(0f)] float maxSpin = 60f;

        void OnEnable()
        {
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;

            float speed = Random.Range(minSpeed, Mathf.Max(minSpeed, maxSpeed));
            body.linearVelocity = Random.insideUnitCircle.normalized * speed;
            body.angularVelocity = Random.Range(-maxSpin, maxSpin);
        }
    }
}
