using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Listens to a Bool Event Channel. When the channel is raised, this runs its
    /// Response and passes along the value.
    /// </summary>
    [AddComponentMenu("Collin Game/Events/Bool Event Listener")]
    public class BoolEventListener : EventListener<bool, BoolEventChannel>
    {
    }
}
