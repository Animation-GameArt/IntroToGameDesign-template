using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// An event channel that carries a whole number, such as points scored or damage dealt.
    /// Create one with: Create > Collin Game > Events > Int Event Channel.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Events/Int Event Channel", fileName = "NewIntEvent")]
    public class IntEventChannel : EventChannel<int>
    {
    }
}
