using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// An event channel that carries a piece of text, such as a message to show the player.
    /// Create one with: Create > Collin Game > Events > String Event Channel.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Events/String Event Channel", fileName = "NewStringEvent")]
    public class StringEventChannel : EventChannel<string>
    {
    }
}
