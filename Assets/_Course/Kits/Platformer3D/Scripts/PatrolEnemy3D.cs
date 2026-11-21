using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.ThreeD;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.Platformer3D
{
    /// <summary>
    /// An enemy that walks between points. Landing on top of it defeats it and
    /// bounces the player; touching it any other way hurts the player.
    ///
    /// Setup: a collider with Is Trigger ticked around the enemy, and empty
    /// child objects for the points it walks between (their positions are
    /// remembered at the start). The player needs a Player Controller 3D and a
    /// Health.
    /// </summary>
    [AddComponentMenu("Collin Game/3D Platformer Kit/Patrol Enemy 3D")]
    public class PatrolEnemy3D : MonoBehaviour
    {
        [Tooltip("The points it walks between, back and forth. Empty: it stands still.")]
        [SerializeField] List<Transform> points = new List<Transform>();

        [SerializeField, Min(0f)] float speed = 2f;

        [Tooltip("Seconds it waits at each point.")]
        [SerializeField, Min(0f)] float waitTime = 0.5f;

        [Header("Touching the player")]

        [Tooltip("Damage to the player when touched from the side.")]
        [SerializeField, Min(0)] int damage = 1;

        [Tooltip("Ticked: landing on top defeats it. Unticked: it can't be stomped, like spikes.")]
        [SerializeField] bool canBeStomped = true;

        [Tooltip("How high the player bounces after a stomp.")]
        [SerializeField, Min(0f)] float stompBounce = 3f;

        [Tooltip("Optional. Raised when it's stomped, such as to count enemies.")]
        [SerializeField] VoidEventChannel raiseOnStomped;

        [SerializeField] UnityEvent onStomped;
        [SerializeField] UnityEvent onHitPlayer;

        readonly List<Vector3> positions = new List<Vector3>();
        int target;
        int direction = 1;
        float waitUntil;
        bool defeated;

        void Start()
        {
            foreach (Transform point in points)
            {
                if (point != null)
                {
                    positions.Add(point.position);
                }
            }
        }

        void Update()
        {
            if (defeated || positions.Count < 2 || Time.time < waitUntil || PauseController.GamePaused)
            {
                return;
            }

            Vector3 goal = positions[target];
            goal.y = transform.position.y;
            Vector3 toGoal = goal - transform.position;

            if (toGoal.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toGoal.normalized, Vector3.up), 540f * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);

            if ((transform.position - goal).sqrMagnitude < 0.0004f)
            {
                waitUntil = Time.time + waitTime;

                if (target + direction >= positions.Count || target + direction < 0)
                {
                    direction = -direction;
                }

                target += direction;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (defeated)
            {
                return;
            }

            GameObject owner = TouchUtility.OwnerOf(other);
            PlayerController3D player = owner.GetComponentInParent<PlayerController3D>();

            if (player == null)
            {
                return;
            }

            // A stomp: the player is falling, and their feet are above the enemy's middle.
            Collider own = GetComponent<Collider>();
            float middle = own != null ? own.bounds.center.y : transform.position.y;

            if (canBeStomped && player.Velocity.y < 0f && player.transform.position.y > middle)
            {
                defeated = true;
                player.Bounce(stompBounce);
                onStomped?.Invoke();

                if (raiseOnStomped != null)
                {
                    raiseOnStomped.Raise();
                }

                Destroy(gameObject);
                return;
            }

            Health health = player.GetComponentInParent<Health>();

            if (health != null && damage > 0)
            {
                health.TakeDamage(damage);
                onHitPlayer?.Invoke();
            }
        }
    }
}
