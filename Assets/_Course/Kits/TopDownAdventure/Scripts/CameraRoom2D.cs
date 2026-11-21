using CollinGame.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.TopDownAdventure
{
    /// <summary>
    /// One room of a level. When the player walks into the room, the camera is
    /// kept inside the room's edges, as in the early Zelda games.
    ///
    /// Setup: add a Box Collider 2D (or a Polygon Collider 2D) that covers the
    /// room, and tick Is Trigger. The scene needs a Cinemachine Camera with a
    /// Cinemachine Confiner 2D. Rooms may touch, but shouldn't overlap.
    ///
    /// Optionally, put the room's enemies under one object and drag it into Room
    /// Contents. They are only switched on while the player is in the room, and
    /// come back each time the player returns. Keep keys and other one-time
    /// pickups outside Room Contents, or they come back too.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Collin Game/Top-Down Adventure Kit/Camera Room 2D")]
    public class CameraRoom2D : MonoBehaviour
    {
        [Tooltip("The tag of the object that moves between rooms.")]
        [SerializeField] string playerTag = "Player";

        [Tooltip("Optional. The Confiner 2D on the Cinemachine Camera. Leave empty to find it in the scene.")]
        [SerializeField] CinemachineConfiner2D confiner;

        [Tooltip("Optional. An object holding the room's enemies. It is only switched on while the player is in this room. Keep keys outside it, or they come back too.")]
        [SerializeField] GameObject roomContents;

        [Tooltip("Ticked: when the player comes back, the room's contents are reset, so defeated enemies return.")]
        [SerializeField] bool resetContentsOnReturn = true;

        [Tooltip("Runs when the player enters this room. Use it to change the music, or show the room's name.")]
        [SerializeField] UnityEvent onEntered;

        [Tooltip("Runs when the player leaves this room for another.")]
        [SerializeField] UnityEvent onLeft;

        Collider2D area;
        GameObject contentsCopy;
        bool hasCopy;

        /// <summary>The room the player is in right now.</summary>
        public static CameraRoom2D Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = null;
        }

        void Awake()
        {
            area = GetComponent<Collider2D>();

            if (!area.isTrigger)
            {
                Debug.LogWarning($"Camera Room 2D on \"{name}\": the collider should have Is Trigger ticked, or it will block the player. Ticking it for you.", this);
                area.isTrigger = true;
            }

            if (confiner == null)
            {
                confiner = FindFirstObjectByType<CinemachineConfiner2D>();
            }

            if (roomContents != null && resetContentsOnReturn)
            {
                // Keep a hidden copy of the contents, to put back when the player returns.
                roomContents.SetActive(false);
                contentsCopy = Instantiate(roomContents, roomContents.transform.parent);
                contentsCopy.name = roomContents.name + " (saved)";
                hasCopy = true;
            }
            else if (roomContents != null)
            {
                roomContents.SetActive(false);
            }
        }

        void Start()
        {
            // The room the player starts in becomes the current room.
            GameObject player = FindPlayer();

            if (player != null && area.OverlapPoint(player.transform.position))
            {
                Enter();
            }
        }

        void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (TouchUtility.HasTag(TouchUtility.OwnerOf(other), playerTag))
            {
                Enter();
            }
        }

        void OnTriggerStay2D(Collider2D other)
        {
            // Catches the player respawning into a room without passing through its edge.
            if (Current != this && TouchUtility.HasTag(TouchUtility.OwnerOf(other), playerTag)
                && area.OverlapPoint(other.transform.position))
            {
                Enter();
            }
        }

        /// <summary>Makes this the current room and moves the camera into it.</summary>
        public void Enter()
        {
            if (Current == this)
            {
                return;
            }

            if (Current != null)
            {
                Current.Leave();
            }

            Current = this;

            if (confiner == null)
            {
                Debug.LogWarning($"Camera Room 2D on \"{name}\": there is no Cinemachine Confiner 2D in the scene. Add one to the Cinemachine Camera.", this);
            }
            else
            {
                confiner.BoundingShape2D = area;
                confiner.InvalidateBoundingShapeCache();
            }

            if (roomContents != null)
            {
                roomContents.SetActive(true);
            }

            onEntered?.Invoke();
        }

        void Leave()
        {
            if (roomContents != null)
            {
                if (hasCopy)
                {
                    // Swap in a fresh copy for next time.
                    GameObject used = roomContents;
                    roomContents = contentsCopy;
                    roomContents.name = used.name;
                    contentsCopy = Instantiate(roomContents, roomContents.transform.parent);
                    contentsCopy.name = used.name + " (saved)";
                    Destroy(used);
                }
                else
                {
                    roomContents.SetActive(false);
                }
            }

            onLeft?.Invoke();
        }

        GameObject FindPlayer()
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return null;
            }

            try
            {
                return GameObject.FindWithTag(playerTag);
            }
            catch (UnityException)
            {
                Debug.LogWarning($"Camera Room 2D on \"{name}\": there is no tag called \"{playerTag}\". Add it in Project Settings → Tags and Layers.", this);
                return null;
            }
        }

        void OnDrawGizmos()
        {
            Collider2D shape = area != null ? area : GetComponent<Collider2D>();

            if (shape == null)
            {
                return;
            }

            Bounds bounds = shape.bounds;
            Gizmos.color = Current == this ? new Color(0.3f, 1f, 0.4f, 0.9f) : new Color(0.3f, 0.7f, 1f, 0.5f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
