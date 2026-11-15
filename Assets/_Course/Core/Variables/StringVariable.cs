using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// A variable asset that holds a piece of text, such as the player's name.
    /// Create one with: Create > Collin Game > Variables > String Variable.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Variables/String Variable", fileName = "NewStringVariable")]
    public class StringVariable : Variable<string>
    {
    }
}
