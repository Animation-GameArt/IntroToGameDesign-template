using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Watches a number variable and reacts when it reaches a value.
    ///
    /// Examples: when Lives is at most 0, the game is over. When Coins is at
    /// least 10, a door opens. When Enemies Left equals 0, the wave is cleared.
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Int Variable Condition")]
    public class IntVariableCondition : MonoBehaviour
    {
        /// <summary>How the variable is compared with the value.</summary>
        public enum Comparison
        {
            /// <summary>The variable is exactly the value.</summary>
            EqualTo,

            /// <summary>The variable is the value or more.</summary>
            AtLeast,

            /// <summary>The variable is the value or less.</summary>
            AtMost
        }

        [Tooltip("The number variable to watch.")]
        [SerializeField] IntVariable variable;

        [Tooltip("How the variable is compared with the value.")]
        [SerializeField] Comparison comparison = Comparison.AtLeast;

        [Tooltip("The value to compare with.")]
        [SerializeField] int value = 1;

        [Tooltip("Ticked: reacts only the first time. Unticked: reacts every time the condition becomes true again.")]
        [SerializeField] bool onlyOnce = true;

        [Header("When the condition becomes true")]

        [Tooltip("Optional. An event to raise.")]
        [SerializeField] VoidEventChannel raiseEvent;

        [Tooltip("Optional. Things to do.")]
        [SerializeField] UnityEvent onBecameTrue;

        bool wasTrue;
        bool hasReacted;

        void OnEnable()
        {
            if (variable == null)
            {
                Debug.LogWarning($"Int Variable Condition on \"{name}\": no Variable is set, so it will never react.", this);
                return;
            }

            variable.Changed += Check;
        }

        void OnDisable()
        {
            if (variable != null)
            {
                variable.Changed -= Check;
            }
        }

        void Start()
        {
            // Look once at the start, in case the condition is already true.
            if (variable != null)
            {
                Check(variable.Value);
            }
        }

        void Check(int current)
        {
            bool isTrue = comparison == Comparison.EqualTo ? current == value
                : comparison == Comparison.AtLeast ? current >= value
                : current <= value;

            bool becameTrue = isTrue && !wasTrue;
            wasTrue = isTrue;

            if (!becameTrue || (onlyOnce && hasReacted))
            {
                return;
            }

            hasReacted = true;

            if (raiseEvent != null)
            {
                raiseEvent.Raise();
            }

            onBecameTrue?.Invoke();
        }
    }
}
