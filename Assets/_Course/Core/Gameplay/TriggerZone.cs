using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// An area that reacts when something walks into it or out of it: the end of
    /// a level, a door that opens as you approach, a trap.
    ///
    /// What it does is up to you. It can raise an event channel, and it can run
    /// a response set up in the Inspector.
    ///
    /// Setup: this GameObject needs a collider with Is Trigger ticked, and the
    /// object that enters needs a Rigidbody. Works in 2D and 3D.
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Trigger Zone")]
    public class TriggerZone : MonoBehaviour
    {
        [Tooltip("Only objects with this tag set off the zone. Leave empty to let anything set it off.")]
        [SerializeField] string triggerTag = "Player";

        [Tooltip("Ticked: the zone works only the first time. Unticked: it works every time.")]
        [SerializeField] bool onlyOnce;

        [Header("When something enters")]

        [Tooltip("Optional. An event to raise.")]
        [SerializeField] VoidEventChannel raiseOnEnter;

        [Tooltip("Optional. Things to do.")]
        [SerializeField] UnityEvent onEnter;

        [Header("When it leaves")]

        [Tooltip("Optional. An event to raise.")]
        [SerializeField] VoidEventChannel raiseOnExit;

        [Tooltip("Optional. Things to do.")]
        [SerializeField] UnityEvent onExit;

        bool enterUsed;
        bool exitUsed;

        void OnTriggerEnter2D(Collider2D other)
        {
            Enter(TouchUtility.OwnerOf(other));
        }

        void OnTriggerExit2D(Collider2D other)
        {
            Exit(TouchUtility.OwnerOf(other));
        }

        void OnTriggerEnter(Collider other)
        {
            Enter(TouchUtility.OwnerOf(other));
        }

        void OnTriggerExit(Collider other)
        {
            Exit(TouchUtility.OwnerOf(other));
        }

        void Enter(GameObject visitor)
        {
            if ((onlyOnce && enterUsed) || !TouchUtility.HasTag(visitor, triggerTag))
            {
                return;
            }

            enterUsed = true;

            if (raiseOnEnter != null)
            {
                raiseOnEnter.Raise();
            }

            onEnter?.Invoke();
        }

        void Exit(GameObject visitor)
        {
            if ((onlyOnce && exitUsed) || !TouchUtility.HasTag(visitor, triggerTag))
            {
                return;
            }

            exitUsed = true;

            if (raiseOnExit != null)
            {
                raiseOnExit.Raise();
            }

            onExit?.Invoke();
        }
    }
}
