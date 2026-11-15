using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// A variable asset that holds a whole number, such as a score, a coin count or lives.
    /// Create one with: Create > Collin Game > Variables > Int Variable.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Variables/Int Variable", fileName = "NewIntVariable")]
    public class IntVariable : Variable<int>
    {
        /// <summary>Adds an amount to the value. Use a negative amount to subtract.</summary>
        public void Add(int amount)
        {
            SetValue(Value + amount);
        }
    }
}
