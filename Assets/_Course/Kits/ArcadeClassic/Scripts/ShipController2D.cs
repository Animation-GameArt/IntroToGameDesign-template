using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// A ship that turns and thrusts, as in Asteroids. Left and right turn the
    /// ship; up fires the engine, pushing the ship the way its nose points. It
    /// keeps drifting after the engine stops.
    ///
    /// Setup: a sprite whose nose points up, with a 2D collider. A Rigidbody 2D
    /// is added and set up for you. Add Shooter 2D to fire, and Screen Wrap 2D
    /// to wrap around the screen.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Ship Controller 2D")]
    public class ShipController2D : MonoBehaviour
    {
        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("How fast the ship turns, in degrees per second.")]
        [SerializeField, Min(0f)] float turnSpeed = 220f;

        [Tooltip("How hard the engine pushes.")]
        [SerializeField, Min(0f)] float thrust = 9f;

        [Tooltip("The ship's top speed, in units per second.")]
        [SerializeField, Min(0f)] float maxSpeed = 8f;

        [Tooltip("How quickly the ship slows down by itself. 0 drifts forever, as in space.")]
        [SerializeField, Min(0f)] float drag = 0.4f;

        [Tooltip("Optional. Shown while the engine is firing, such as a flame sprite behind the ship.")]
        [SerializeField] GameObject engineFlame;

        Rigidbody2D body;
        Vector2 moveInput;

        /// <summary>True while the engine is firing.</summary>
        public bool Thrusting { get; private set; }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = drag;
            body.angularDamping = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (input == null)
            {
                Debug.LogError($"Ship Controller 2D on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
            }
        }

        void OnDisable()
        {
            moveInput = Vector2.zero;
            SetThrusting(false);
        }

        void Update()
        {
            moveInput = input != null && !PauseController.GamePaused ? input.Move : Vector2.zero;
            SetThrusting(moveInput.y > 0.1f);
        }

        void FixedUpdate()
        {
            body.angularVelocity = -moveInput.x * turnSpeed;

            if (Thrusting)
            {
                body.AddForce((Vector2)transform.up * thrust * moveInput.y);
            }

            if (body.linearVelocity.magnitude > maxSpeed)
            {
                body.linearVelocity = body.linearVelocity.normalized * maxSpeed;
            }
        }

        /// <summary>Stops the ship and points it straight up. Call it from a Respawner's On Respawn.</summary>
        public void StopAndFaceUp()
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.rotation = 0f;
            transform.rotation = Quaternion.identity;
        }

        void SetThrusting(bool value)
        {
            Thrusting = value;

            if (engineFlame != null && engineFlame.activeSelf != value)
            {
                engineFlame.SetActive(value);
            }
        }

#if UNITY_EDITOR
        // Runs when the component is first added: fills in the Input field for you.
        void Reset()
        {
            input = InputReader.FindInProject();
        }
#endif
    }
}
