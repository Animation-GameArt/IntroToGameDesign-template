using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// A variable asset that holds true or false, such as whether the player has the key.
    /// Create one with: Create > Collin Game > Variables > Bool Variable.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Variables/Bool Variable", fileName = "NewBoolVariable")]
    public class BoolVariable : Variable<bool>
    {
        /// <summary>Changes true to false, or false to true.</summary>
        public void Toggle()
        {
            SetValue(!Value);
        }
    }
}
