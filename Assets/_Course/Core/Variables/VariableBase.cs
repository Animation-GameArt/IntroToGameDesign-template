using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// The base for all variable assets (Int, Float, Bool and String Variable).
    ///
    /// A variable asset holds one value, such as the score, that lives outside any
    /// scene. Any object can read it or change it, and any object can be told when
    /// it changes. Objects that share a variable never need to know about each other.
    ///
    /// Every variable goes back to its Initial Value each time the game starts.
    /// It keeps its value when a scene changes or restarts. To set it back during
    /// the game, use a Variable Resetter or call ResetToInitial.
    /// </summary>
    public abstract class VariableBase : ScriptableObject
    {
        // Every variable asset that is currently loaded, so they can all be
        // reset when Play mode starts.
        static readonly List<VariableBase> loadedVariables = new List<VariableBase>();

        [Tooltip("A note for yourself: what this variable is for.")]
        [TextArea(1, 3)]
        [SerializeField] string description;

        /// <summary>Raised whenever the value changes.</summary>
        public event Action ValueChanged;

        /// <summary>The note written on this asset.</summary>
        public string Description => description;

        /// <summary>The current value, as text.</summary>
        public abstract string ValueAsText { get; }

        /// <summary>Sets the value back to the Initial Value.</summary>
        public abstract void ResetToInitial();

        /// <summary>The current value as text for a save, written the same way on every computer.</summary>
        public abstract string ToSaveText();

        /// <summary>Sets the value from text written by ToSaveText. Returns false if the text can't be read.</summary>
        public abstract bool LoadFromSaveText(string text);

        protected void NotifyValueChanged()
        {
            ValueChanged?.Invoke();
        }

        protected virtual void OnEnable()
        {
            if (!loadedVariables.Contains(this))
            {
                loadedVariables.Add(this);
            }

            ResetToInitial();
        }

        protected virtual void OnDisable()
        {
            loadedVariables.Remove(this);
        }

        // Resets every variable when Play mode starts. This matters when
        // "Reload Domain" is turned off in the Enter Play Mode settings.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAllWhenPlayStarts()
        {
            for (int i = 0; i < loadedVariables.Count; i++)
            {
                if (loadedVariables[i] != null)
                {
                    loadedVariables[i].ResetToInitial();
                }
            }
        }
    }
}
