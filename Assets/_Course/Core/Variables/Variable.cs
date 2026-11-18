using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// The shared code for variable assets of every type.
    /// Use one of the ready-made types: Int, Float, Bool or String Variable.
    /// </summary>
    public abstract class Variable<T> : VariableBase
    {
        [Tooltip("The value this variable has each time the game starts.")]
        [SerializeField] T initialValue;

        [Tooltip("Write a message in the Console every time the value changes.")]
        [SerializeField] bool logChanges;

        // The value while the game runs. It is not saved in the asset,
        // so playing the game never changes the Initial Value.
        [NonSerialized] T currentValue;

        /// <summary>Raised whenever the value changes. It passes along the new value.</summary>
        public event Action<T> Changed;

        /// <summary>The value this variable has each time the game starts.</summary>
        public T InitialValue => initialValue;

        /// <summary>The current value. Setting it tells every listener that it changed.</summary>
        public T Value
        {
            get => currentValue;
            set => SetValue(value);
        }

        public override string ValueAsText => currentValue != null ? currentValue.ToString() : string.Empty;

        /// <summary>Changes the value. If it is different from before, every listener is told.</summary>
        public void SetValue(T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                return;
            }

            currentValue = newValue;

            if (logChanges)
            {
                Debug.Log($"Variable \"{name}\" changed to {ValueAsText}.", this);
            }

            Changed?.Invoke(currentValue);
            NotifyValueChanged();
        }

        public override string ToSaveText()
        {
            object value = currentValue;

            if (value == null)
            {
                return string.Empty;
            }

            if (value is float number)
            {
                return number.ToString("R", CultureInfo.InvariantCulture);
            }

            if (value is bool flag)
            {
                return flag ? "true" : "false";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        public override bool LoadFromSaveText(string text)
        {
            try
            {
                object parsed;

                if (typeof(T) == typeof(string))
                {
                    parsed = text ?? string.Empty;
                }
                else if (typeof(T) == typeof(bool))
                {
                    parsed = bool.Parse(text);
                }
                else
                {
                    parsed = Convert.ChangeType(text, typeof(T), CultureInfo.InvariantCulture);
                }

                SetValue((T)parsed);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override void ResetToInitial()
        {
            SetValue(initialValue);
        }

        // Keeps the current value in step when the Initial Value is edited
        // in the Inspector while the game is not running.
        protected virtual void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ResetToInitial();
            }
        }
    }
}
