using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace CollinGame.Kits.Stealth
{
    /// <summary>
    /// Something dangerous that patrols, looks and listens: a guard, a monster.
    ///
    ///   Patrol       walks between its points, pausing at each.
    ///   Investigate  heard something, or glimpsed the player: goes to look,
    ///                and looks around for a while.
    ///   Chase        its alert meter is full: runs at the player. Getting
    ///                within Catch Distance catches them.
    ///
    /// It sees the player inside its view cone, if nothing is in the way; it
    /// fills its alert meter faster the closer the player is, and from farther
    /// away if their flashlight is on. It hears the player within Hearing
    /// Range times their loudness: sprinting is loud, crouching is nearly
    /// silent. Hidden players can't be seen or caught.
    ///
    /// Setup: a NavMesh Agent on the same object, and a baked NavMesh (a
    /// NavMesh Surface, from the AI Navigation package). Empty objects for the
    /// patrol points, outside the guard.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [AddComponentMenu("Collin Game/Stealth Kit/Guard 3D")]
    public class Guard3D : MonoBehaviour
    {
        public enum State
        {
            Patrol,
            Investigate,
            Chase
        }

        [Header("Patrol")]

        [Tooltip("Points to walk between, in a loop.")]
        [SerializeField] List<Transform> patrolPoints = new List<Transform>();

        [SerializeField, Min(0f)] float patrolSpeed = 2f;

        [Tooltip("Seconds it waits at each patrol point.")]
        [SerializeField, Min(0f)] float waitAtPoint = 1.5f;

        [Header("Seeing")]

        [Tooltip("How far it can see, in units.")]
        [SerializeField, Min(0f)] float viewDistance = 12f;

        [Tooltip("How wide its view is, in degrees.")]
        [SerializeField, Range(1f, 360f)] float viewAngle = 90f;

        [Tooltip("The height of its eyes above its feet.")]
        [SerializeField, Min(0f)] float eyeHeight = 1.6f;

        [Tooltip("Seconds to fill the alert meter when the player is right in front of it. Farther away takes longer.")]
        [SerializeField, Min(0.05f)] float timeToSpot = 1f;

        [Tooltip("With the player's flashlight on, it sees this many times farther.")]
        [SerializeField, Min(1f)] float flashlightSightBonus = 1.5f;

        [Tooltip("What blocks its view, such as walls. Usually Everything.")]
        [SerializeField] LayerMask blocksView = ~0;

        [Header("Hearing")]

        [Tooltip("How far it hears a sprinting player. Walking is heard at half this, crouching at a seventh.")]
        [SerializeField, Min(0f)] float hearingRange = 10f;

        [Header("Investigating and chasing")]

        [SerializeField, Min(0f)] float investigateSpeed = 3f;

        [Tooltip("Seconds it looks around where it heard or saw something.")]
        [SerializeField, Min(0f)] float lookAroundTime = 4f;

        [SerializeField, Min(0f)] float chaseSpeed = 4.5f;

        [Tooltip("Seconds without seeing the player before it gives up the chase and searches instead.")]
        [SerializeField, Min(0f)] float loseTrackAfter = 3f;

        [Tooltip("How much of the alert meter drains per second while it can't see the player.")]
        [SerializeField, Min(0f)] float calmDownRate = 0.15f;

        [Tooltip("How close it has to get to catch the player.")]
        [SerializeField, Min(0.1f)] float catchDistance = 1.2f;

        [Header("Results")]

        [Tooltip("Raised when it catches the player, such as to show the game over screen.")]
        [SerializeField] VoidEventChannel raiseOnCaught;

        [SerializeField] UnityEvent onSuspicious;
        [SerializeField] UnityEvent onChase;
        [SerializeField] UnityEvent onGaveUp;
        [SerializeField] UnityEvent onCaught;

        static readonly List<Guard3D> all = new List<Guard3D>();

        NavMeshAgent agent;
        readonly List<Vector3> route = new List<Vector3>();
        int nextPoint;
        float waitUntil;
        Vector3 investigatePoint;
        float lookAroundUntil;
        bool lookingAround;
        float lastSawTime = -100f;
        Vector3 lastSeenPosition;
        bool caught;

        /// <summary>Every guard in the scene, for the HUD.</summary>
        public static IReadOnlyList<Guard3D> All => all;

        /// <summary>The alert meter, from 0 (calm) to 1 (chasing).</summary>
        public float Alert { get; private set; }

        public State CurrentState { get; private set; } = State.Patrol;

        /// <summary>True if it could see the player this frame.</summary>
        public bool CanSeePlayer { get; private set; }

        public float ViewDistance => viewDistance;
        public float ViewAngle => viewAngle;
        public float EyeHeight => eyeHeight;
        public LayerMask BlocksView => blocksView;

        /// <summary>The highest alert of any guard, from 0 to 1.</summary>
        public static float HighestAlert
        {
            get
            {
                float highest = 0f;

                foreach (Guard3D guard in all)
                {
                    highest = Mathf.Max(highest, guard.Alert);
                }

                return highest;
            }
        }

        /// <summary>True if any guard is chasing.</summary>
        public static bool AnyChasing
        {
            get
            {
                foreach (Guard3D guard in all)
                {
                    if (guard.CurrentState == State.Chase) return true;
                }

                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            all.Clear();
        }

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        void OnEnable()
        {
            all.Add(this);
        }

        void OnDisable()
        {
            all.Remove(this);
        }

        void Start()
        {
            foreach (Transform point in patrolPoints)
            {
                if (point != null)
                {
                    route.Add(point.position);
                }
            }

            EnterPatrol();
        }

        void Update()
        {
            if (caught || !agent.isOnNavMesh)
            {
                return;
            }

            agent.isStopped = PauseController.GamePaused;

            if (PauseController.GamePaused)
            {
                return;
            }

            StealthTarget3D target = StealthTarget3D.Instance;
            Look(target);
            Listen(target);

            switch (CurrentState)
            {
                case State.Patrol: Patrol(); break;
                case State.Investigate: Investigate(); break;
                case State.Chase: Chase(target); break;
            }
        }

        // ---------------------------------------------------------------- Senses

        void Look(StealthTarget3D target)
        {
            CanSeePlayer = target != null && Sees(target);

            if (CanSeePlayer)
            {
                float range = viewDistance * (target.IsLit ? flashlightSightBonus : 1f);
                float distance = Vector3.Distance(Eye, target.LookPoint);
                float closeness = 1f - Mathf.Clamp01(distance / range);

                // Close up it fills in Time To Spot; at the edge of its view, about four times slower.
                float rate = Mathf.Lerp(0.25f, 1f, closeness) / timeToSpot;
                Alert = Mathf.Min(1f, Alert + rate * Time.deltaTime);
                lastSawTime = Time.time;
                lastSeenPosition = target.transform.position;

                if (Alert >= 1f && CurrentState != State.Chase)
                {
                    EnterChase();
                }
                else if (Alert >= 0.35f && CurrentState == State.Patrol)
                {
                    EnterInvestigate(lastSeenPosition);
                }
                else if (CurrentState == State.Investigate)
                {
                    investigatePoint = lastSeenPosition;
                    agent.SetDestination(investigatePoint);
                    lookingAround = false;
                }
            }
            else if (CurrentState != State.Chase)
            {
                Alert = Mathf.Max(0f, Alert - calmDownRate * Time.deltaTime);
            }
        }

        bool Sees(StealthTarget3D target)
        {
            if (target.IsHidden)
            {
                return false;
            }

            Vector3 eye = Eye;
            Vector3 toTarget = target.LookPoint - eye;
            float range = viewDistance * (target.IsLit ? flashlightSightBonus : 1f);

            if (toTarget.sqrMagnitude > range * range)
            {
                return false;
            }

            if (Vector3.Angle(transform.forward, toTarget) > viewAngle * 0.5f)
            {
                return false;
            }

            // Is anything in the way?
            if (Physics.Raycast(eye, toTarget.normalized, out RaycastHit hit, toTarget.magnitude, blocksView, QueryTriggerInteraction.Ignore))
            {
                return hit.transform.IsChildOf(target.transform) || target.transform.IsChildOf(hit.transform);
            }

            return true;
        }

        void Listen(StealthTarget3D target)
        {
            if (target == null || CurrentState == State.Chase || target.IsHidden)
            {
                return;
            }

            float loudness = target.Loudness;

            if (loudness <= 0f)
            {
                return;
            }

            float heardAt = hearingRange * loudness;

            if ((target.transform.position - transform.position).sqrMagnitude <= heardAt * heardAt)
            {
                Alert = Mathf.Max(Alert, 0.35f);

                if (CurrentState == State.Patrol)
                {
                    EnterInvestigate(target.transform.position);
                }
                else
                {
                    // Already searching: follow the new sound.
                    investigatePoint = target.transform.position;
                    agent.SetDestination(investigatePoint);
                    lookingAround = false;
                }
            }
        }

        Vector3 Eye => transform.position + Vector3.up * eyeHeight;

        // ---------------------------------------------------------------- States

        void EnterPatrol()
        {
            CurrentState = State.Patrol;
            agent.speed = patrolSpeed;
            waitUntil = 0f;

            if (route.Count > 0 && agent.isOnNavMesh)
            {
                agent.SetDestination(route[nextPoint % route.Count]);
            }
        }

        void Patrol()
        {
            if (route.Count == 0 || agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.1f)
            {
                return;
            }

            if (waitUntil <= 0f)
            {
                waitUntil = Time.time + waitAtPoint;
            }
            else if (Time.time >= waitUntil)
            {
                waitUntil = 0f;
                nextPoint = (nextPoint + 1) % route.Count;
                agent.SetDestination(route[nextPoint]);
            }
        }

        void EnterInvestigate(Vector3 point)
        {
            CurrentState = State.Investigate;
            agent.speed = investigateSpeed;
            investigatePoint = point;
            lookingAround = false;
            agent.SetDestination(point);
            onSuspicious?.Invoke();
        }

        void Investigate()
        {
            if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.3f)
            {
                return;
            }

            if (!lookingAround)
            {
                lookingAround = true;
                lookAroundUntil = Time.time + lookAroundTime;
            }

            // Turn slowly, looking around.
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);

            if (Time.time >= lookAroundUntil)
            {
                onGaveUp?.Invoke();
                EnterPatrol();
            }
        }

        void EnterChase()
        {
            CurrentState = State.Chase;
            agent.speed = chaseSpeed;
            Alert = 1f;
            onChase?.Invoke();
        }

        void Chase(StealthTarget3D target)
        {
            if (target == null)
            {
                EnterPatrol();
                return;
            }

            if (CanSeePlayer)
            {
                agent.SetDestination(target.transform.position);
            }
            else
            {
                agent.SetDestination(lastSeenPosition);

                if (Time.time - lastSawTime > loseTrackAfter)
                {
                    Alert = 0.6f;
                    EnterInvestigate(lastSeenPosition);
                    return;
                }
            }

            if (!target.IsHidden && (target.transform.position - transform.position).sqrMagnitude <= catchDistance * catchDistance)
            {
                Catch();
            }
        }

        void Catch()
        {
            caught = true;
            agent.isStopped = true;
            onCaught?.Invoke();

            if (raiseOnCaught != null)
            {
                raiseOnCaught.Raise();
            }
        }

        // ---------------------------------------------------------------- Editor drawing

        void OnDrawGizmosSelected()
        {
            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            Quaternion left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f);
            Quaternion right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f);
            Gizmos.DrawLine(eye, eye + left * transform.forward * viewDistance);
            Gizmos.DrawLine(eye, eye + right * transform.forward * viewDistance);
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, hearingRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, catchDistance);
        }
    }
}
