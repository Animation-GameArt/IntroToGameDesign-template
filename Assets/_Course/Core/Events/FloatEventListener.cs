using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Listens to a Float Event Channel. When the channel is raised, this runs its
    /// Response and passes along the value.
    /// </summary>
    [AddComponentMenu("Collin Game/Events/Float Event Listener")]
    public class FloatEventListener : EventListener<float, FloatEventChannel>
    {
    }
}
