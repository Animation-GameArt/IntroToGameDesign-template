using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.Stealth
{
    /// <summary>
    /// A place to hide: a locker, under a desk, a dark corner. While the player
    /// is inside it (crouching, if Needs Crouch is ticked), guards can't see
    /// or catch them, though a guard can still come looking.
    ///
    /// Setup: a collider with Is Trigger ticked, covering the hiding place.
    /// </summary>
    [AddComponentMenu("Collin Game/Stealth Kit/Hiding Spot 3D")]
    public class HidingSpot3D : MonoBehaviour
    {
        [Tooltip("Ticked: the player must crouch to be hidden here, such as under a desk.")]
        [SerializeField] bool needsCrouch;

        [SerializeField] UnityEvent onEnter;
        [SerializeField] UnityEvent onExit;

        public bool NeedsCrouch => needsCrouch;

        void OnTriggerEnter(Collider other)
        {
            StealthTarget3D target = TouchUtility.OwnerOf(other).GetComponentInParent<StealthTarget3D>();

            if (target != null)
            {
                target.EnterSpot(this);
                onEnter?.Invoke();
            }
        }

        void OnTriggerExit(Collider other)
        {
            StealthTarget3D target = TouchUtility.OwnerOf(other).GetComponentInParent<StealthTarget3D>();

            if (target != null)
            {
                target.LeaveSpot(this);
                onExit?.Invoke();
            }
        }
    }
}
