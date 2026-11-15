using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// An event channel that carries a GameObject, such as the enemy that was defeated.
    /// Create one with: Create > Collin Game > Events > GameObject Event Channel.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Events/GameObject Event Channel", fileName = "NewGameObjectEvent")]
    public class GameObjectEventChannel : EventChannel<GameObject>
    {
    }
}
