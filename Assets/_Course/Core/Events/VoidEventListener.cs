using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Listens to a Void Event Channel. When the channel is raised, this runs its
    /// Response, which you set up in the Inspector: for example, hide a door,
    /// play a sound or load a scene.
    ///
    /// It only listens while its GameObject is active.
    /// </summary>
    [AddComponentMenu("Collin Game/Events/Void Event Listener")]
    public class VoidEventListener : MonoBehaviour
    {
        [Tooltip("The event channel to listen to.")]
        [SerializeField] VoidEventChannel channel;

        [Tooltip("What to do when the event is raised.")]
        [SerializeField] UnityEvent response;

        void OnEnable()
        {
            if (channel == null)
            {
                Debug.LogWarning($"Void Event Listener on \"{name}\": no Channel is set, so it will never respond.", this);
                return;
            }

            channel.Raised += Respond;
        }

        void OnDisable()
        {
            if (channel != null)
            {
                channel.Raised -= Respond;
            }
        }

        void Respond()
        {
            response?.Invoke();
        }
    }
}
