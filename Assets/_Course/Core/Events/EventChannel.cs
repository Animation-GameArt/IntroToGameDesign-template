using System;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// The base for event channels that carry a value, such as a number.
    ///
    /// An event channel is an asset that works like a radio station. One object
    /// broadcasts on it by calling Raise. Any number of other objects listen.
    /// The broadcaster and the listeners never need to know about each other;
    /// they only need to share the same channel asset.
    ///
    /// Use one of the ready-made types: Int, Float, Bool, String or GameObject Event Channel.
    /// For an event with no value, use a Void Event Channel.
    /// </summary>
    public abstract class EventChannel<T> : ScriptableObject
    {
        [Tooltip("A note for yourself: what this event means and who raises it.")]
        [TextArea(1, 3)]
        [SerializeField] string description;

        [Tooltip("Write a message in the Console every time this event is raised.")]
        [SerializeField] bool logWhenRaised;

        /// <summary>Listeners subscribe to this to hear the event.</summary>
        public event Action<T> Raised;

        /// <summary>The note written on this asset.</summary>
        public string Description => description;

        /// <summary>Broadcasts the event, with a value, to everything that is listening.</summary>
        public void Raise(T value)
        {
            if (logWhenRaised)
            {
                Debug.Log($"Event \"{name}\" was raised with the value {value}.", this);
            }

            Raised?.Invoke(value);
        }
    }
}
