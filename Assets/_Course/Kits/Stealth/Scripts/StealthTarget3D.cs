using CollinGame.ThreeD;
using UnityEngine;

namespace CollinGame.Kits.Stealth
{
    /// <summary>
    /// Marks the player as someone guards look and listen for. It tells guards
    /// how loud the player is (from the Player Controller 3D), whether they're
    /// hidden (in a Hiding Spot 3D), and whether their flashlight is on.
    ///
    /// Add it to the player, with a Player Controller 3D.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Stealth Kit/Stealth Target 3D")]
    public class StealthTarget3D : MonoBehaviour
    {
        [Tooltip("The point guards look at, such as the player's chest. Empty: 1.2 units above the player's feet.")]
        [SerializeField] Transform lookPoint;

        [Tooltip("Optional. The player's flashlight. While it's on, guards see the player from farther away.")]
        [SerializeField] Flashlight3D flashlight;

        PlayerController3D controller;
        HidingSpot3D spot;

        /// <summary>The player in the open scene.</summary>
        public static StealthTarget3D Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        void Awake()
        {
            Instance = this;
            controller = GetComponent<PlayerController3D>();

            if (flashlight == null)
            {
                flashlight = GetComponentInChildren<Flashlight3D>();
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Where guards look.</summary>
        public Vector3 LookPoint => lookPoint != null ? lookPoint.position : transform.position + Vector3.up * 1.2f;

        /// <summary>From 0 (silent) to 1 (sprinting).</summary>
        public float Loudness => controller != null ? controller.Loudness : 0.5f;

        /// <summary>True while in a hiding spot (and crouching, if the spot needs it). Hidden players can't be seen or caught.</summary>
        public bool IsHidden => spot != null && (!spot.NeedsCrouch || (controller != null && controller.IsCrouching));

        /// <summary>True while the flashlight is on.</summary>
        public bool IsLit => flashlight != null && flashlight.IsOn;

        public void EnterSpot(HidingSpot3D hidingSpot)
        {
            spot = hidingSpot;
        }

        public void LeaveSpot(HidingSpot3D hidingSpot)
        {
            if (spot == hidingSpot)
            {
                spot = null;
            }
        }
    }
}
