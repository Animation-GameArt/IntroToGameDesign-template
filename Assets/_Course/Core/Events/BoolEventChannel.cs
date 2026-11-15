using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// An event channel that carries true or false, such as whether the game is paused.
    /// Create one with: Create > Collin Game > Events > Bool Event Channel.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Events/Bool Event Channel", fileName = "NewBoolEvent")]
    public class BoolEventChannel : EventChannel<bool>
    {
    }
}
