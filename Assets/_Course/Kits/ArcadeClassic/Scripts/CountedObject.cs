using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// Counts this object in a number variable while it is in the scene: the
    /// bricks left in Breakout, the asteroids left in Asteroids. The count goes
    /// down when the object is switched off or destroyed, and up when a new one
    /// appears, such as the pieces of a split asteroid.
    ///
    /// Use an Int Variable Condition on the same variable (Equal To 0) to know
    /// when they're all gone. Don't put a Variable Resetter on the counted
    /// variable: the count is set here.
    /// </summary>
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Counted Object")]
    public class CountedObject : MonoBehaviour
    {
        [Tooltip("The number variable that counts these objects.")]
        [SerializeField] IntVariable count;

        static readonly Dictionary<IntVariable, int> Counts = new Dictionary<IntVariable, int>();

        bool counted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Counts.Clear();
        }

        void OnEnable()
        {
            if (count == null)
            {
                Debug.LogWarning($"Counted Object on \"{name}\": Count is empty, so this object isn't counted.", this);
                return;
            }

            counted = true;
            Change(count, 1);
        }

        void Start()
        {
            // Set it again, in case a Variable Resetter changed it after this object was counted.
            if (counted)
            {
                count.SetValue(Counts[count]);
            }
        }

        void OnDisable()
        {
            if (counted)
            {
                counted = false;
                Change(count, -1);
            }
        }

        static void Change(IntVariable variable, int amount)
        {
            Counts.TryGetValue(variable, out int current);
            current = Mathf.Max(0, current + amount);
            Counts[variable] = current;
            variable.SetValue(current);
        }
    }
}
