using System;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// An event channel with no value. It only says "this happened":
    /// the key was collected, the level was finished, the player died.
    ///
    /// One object broadcasts by calling Raise. Any number of objects listen,
    /// usually with a Void Event Listener component.
    ///
    /// Create one with: Create > Collin Game > Events > Void Event Channel.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Events/Void Event Channel", fileName = "NewEvent")]
    public class VoidEventChannel : ScriptableObject
    {
        [Tooltip("A note for yourself: what this event means and who raises it.")]
        [TextArea(1, 3)]
        [SerializeField] string description;

        [Tooltip("Write a message in the Console every time this event is raised.")]
        [SerializeField] bool logWhenRaised;

        /// <summary>Listeners subscribe to this to hear the event.</summary>
        public event Action Raised;

        /// <summary>The note written on this asset.</summary>
        public string Description => description;

        /// <summary>Broadcasts the event to everything that is listening.</summary>
        public void Raise()
        {
            if (logWhenRaised)
            {
                Debug.Log($"Event \"{name}\" was raised.", this);
            }

            Raised?.Invoke();
        }
    }
}
