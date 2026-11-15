using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// An event channel that carries a decimal number, such as a distance or a time in seconds.
    /// Create one with: Create > Collin Game > Events > Float Event Channel.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Events/Float Event Channel", fileName = "NewFloatEvent")]
    public class FloatEventChannel : EventChannel<float>
    {
    }
}
