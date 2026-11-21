using System;
using CollinGame.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CollinGame.Kits.Management
{
    /// <summary>
    /// One change to one resource, such as "Food −1 for each Person" or
    /// "Gold +10 to +30". Turn Rules, Events and Buildings are lists of these.
    ///
    /// The change is: Amount × (the value of Times Each, if set) + a random
    /// number from −Random Extra to +Random Extra. It can be limited to times
    /// when another resource is at least, or at most, some value.
    /// </summary>
    [Serializable]
    public class Effect
    {
        /// <summary>How a condition compares.</summary>
        public enum Comparison
        {
            Always,
            AtLeast,
            AtMost
        }

        [Tooltip("The resource that changes.")]
        public IntVariable resource;

        [Tooltip("How much it changes. Negative takes away.")]
        public int amount;

        [Tooltip("Optional. Multiply the amount by this resource, such as Food −1 for each Person.")]
        public IntVariable timesEach;

        [Tooltip("Optional. Divide the result by this, rounding down: −1 for each 2 People is Amount −1, Times Each People, Divide By 2.")]
        [Min(1)] public int divideBy = 1;

        [Tooltip("Adds a random number from minus this to plus this.")]
        [Min(0)] public int randomExtra;

        [Header("Only when")]

        [Tooltip("Always, or only when the If resource is at least or at most the If value.")]
        public Comparison onlyWhen = Comparison.Always;

        [Tooltip("The resource to check.")]
        public IntVariable ifResource;

        [Tooltip("The value to compare with.")]
        public int ifValue;

        /// <summary>Works out the change, applies it, and returns it (0 if the condition wasn't met).</summary>
        public int Apply()
        {
            if (resource == null || !ConditionMet())
            {
                return 0;
            }

            int change = Preview();

            if (randomExtra > 0)
            {
                change += Random.Range(-randomExtra, randomExtra + 1);
            }

            resource.Add(change);
            return change;
        }

        /// <summary>The change without the random part, for showing costs and previews.</summary>
        public int Preview()
        {
            int times = timesEach != null ? timesEach.Value : 1;
            return Mathf.FloorToInt(amount * times / (float)Mathf.Max(1, divideBy));
        }

        /// <summary>True if the condition is met, or there is none.</summary>
        public bool ConditionMet()
        {
            if (onlyWhen == Comparison.Always || ifResource == null)
            {
                return true;
            }

            return onlyWhen == Comparison.AtLeast ? ifResource.Value >= ifValue : ifResource.Value <= ifValue;
        }

        /// <summary>A short description, such as "Food −1 per People".</summary>
        public override string ToString()
        {
            if (resource == null)
            {
                return "(no resource)";
            }

            string sign = amount >= 0 ? "+" : "-";
            string text = $"{resource.name} {sign}{Mathf.Abs(amount)}";

            if (timesEach != null)
            {
                text += divideBy > 1 ? $" per {divideBy} {timesEach.name}" : $" per {timesEach.name}";
            }

            return text;
        }
    }
}
