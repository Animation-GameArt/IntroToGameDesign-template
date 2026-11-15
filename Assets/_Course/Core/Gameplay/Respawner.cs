using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Sends an object back to its respawn point. Put it on the player.
    ///
    /// The respawn point starts as the place the object begins the scene. Touching
    /// a Checkpoint moves it. A Kill Zone sends the object back, and so does
    /// running out of Health, if this object has a Health component.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Core/Respawner")]
    public class Respawner : MonoBehaviour
    {
        [Tooltip("If this object has Health, respawn by itself when the health runs out.")]
        [SerializeField] bool respawnWhenHealthRunsOut = true;

        [Tooltip("Optional. An event to raise each time this object respawns.")]
        [SerializeField] VoidEventChannel raiseOnRespawn;

        [Tooltip("Runs each time this object respawns.")]
        [SerializeField] UnityEvent onRespawn;

        Health health;

        /// <summary>Where this object reappears.</summary>
        public Vector3 RespawnPoint { get; private set; }

        void Awake()
        {
            RespawnPoint = transform.position;
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        void OnDied()
        {
            if (respawnWhenHealthRunsOut)
            {
                Respawn();
            }
        }

        /// <summary>Changes where this object reappears.</summary>
        public void SetRespawnPoint(Vector3 point)
        {
            RespawnPoint = point;
        }

        /// <summary>Changes where this object reappears, to the position of another object.</summary>
        public void SetRespawnPoint(Transform point)
        {
            if (point != null)
            {
                RespawnPoint = point.position;
            }
        }

        /// <summary>Moves this object to its respawn point, stops it, and refills its health.</summary>
        public void Respawn()
        {
            transform.position = RespawnPoint;

            Rigidbody2D body2D = GetComponent<Rigidbody2D>();

            if (body2D != null)
            {
                body2D.position = RespawnPoint;
                body2D.linearVelocity = Vector2.zero;
                body2D.angularVelocity = 0f;
            }

            Rigidbody body3D = GetComponent<Rigidbody>();

            if (body3D != null)
            {
                body3D.position = RespawnPoint;
                body3D.linearVelocity = Vector3.zero;
                body3D.angularVelocity = Vector3.zero;
            }

            if (health != null)
            {
                health.ResetHealth();
            }

            if (raiseOnRespawn != null)
            {
                raiseOnRespawn.Raise();
            }

            onRespawn?.Invoke();
        }
    }
}
