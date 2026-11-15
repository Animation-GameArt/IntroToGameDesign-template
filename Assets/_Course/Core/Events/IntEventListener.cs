using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Listens to a Int Event Channel. When the channel is raised, this runs its
    /// Response and passes along the value.
    /// </summary>
    [AddComponentMenu("Collin Game/Events/Int Event Listener")]
    public class IntEventListener : EventListener<int, IntEventChannel>
    {
    }
}
