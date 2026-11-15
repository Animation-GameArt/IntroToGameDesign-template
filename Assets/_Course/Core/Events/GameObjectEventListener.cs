using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Listens to a GameObject Event Channel. When the channel is raised, this runs its
    /// Response and passes along the value.
    /// </summary>
    [AddComponentMenu("Collin Game/Events/GameObject Event Listener")]
    public class GameObjectEventListener : EventListener<GameObject, GameObjectEventChannel>
    {
    }
}
