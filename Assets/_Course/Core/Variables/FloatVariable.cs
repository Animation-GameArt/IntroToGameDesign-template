using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// A variable asset that holds a decimal number, such as health, speed or time left.
    /// Create one with: Create > Collin Game > Variables > Float Variable.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Variables/Float Variable", fileName = "NewFloatVariable")]
    public class FloatVariable : Variable<float>
    {
        /// <summary>Adds an amount to the value. Use a negative amount to subtract.</summary>
        public void Add(float amount)
        {
            SetValue(Value + amount);
        }
    }
}
