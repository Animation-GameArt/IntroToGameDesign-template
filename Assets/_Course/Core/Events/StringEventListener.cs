using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Listens to a String Event Channel. When the channel is raised, this runs its
    /// Response and passes along the value.
    /// </summary>
    [AddComponentMenu("Collin Game/Events/String Event Listener")]
    public class StringEventListener : EventListener<string, StringEventChannel>
    {
    }
}
