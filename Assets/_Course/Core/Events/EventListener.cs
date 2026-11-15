using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// The base for listener components that hear an event channel carrying a value.
    /// When the channel is raised, the listener runs its Response, which you set up
    /// in the Inspector. The value is passed along to the Response.
    ///
    /// Use one of the ready-made types: Int, Float, Bool, String or GameObject Event Listener.
    /// </summary>
    public abstract class EventListener<T, TChannel> : MonoBehaviour where TChannel : EventChannel<T>
    {
        [Tooltip("The event channel to listen to.")]
        [SerializeField] TChannel channel;

        [Tooltip("What to do when the event is raised.")]
        [SerializeField] UnityEvent<T> response;

        protected virtual void OnEnable()
        {
            if (channel == null)
            {
                Debug.LogWarning($"{GetType().Name} on \"{name}\": no Channel is set, so it will never respond.", this);
                return;
            }

            channel.Raised += Respond;
        }

        protected virtual void OnDisable()
        {
            if (channel != null)
            {
                channel.Raised -= Respond;
            }
        }

        void Respond(T value)
        {
            response?.Invoke(value);
        }
    }
}
