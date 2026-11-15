using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Core
{
    /// <summary>
    /// Shows variable assets in the labels of a UI Document, and keeps them up to
    /// date. This is how a HUD shows the score and the health.
    /// Add it to the same GameObject as the UI Document.
    ///
    /// For each one, type the label's name (as set in the UXML file, for example
    /// "score-label"), choose the variable, and write the text to show.
    /// {0} is replaced by the value, so "Score: {0}" shows "Score: 3".
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/UI/UI Variable Binder")]
    public class UIVariableBinder : MonoBehaviour
    {
        /// <summary>One label and the variable it shows.</summary>
        [Serializable]
        public class Binding
        {
            [Tooltip("The label's name in the UXML file, for example: score-label")]
            public string labelName;

            [Tooltip("The variable asset to show.")]
            public VariableBase variable;

            [Tooltip("The text to show. {0} is replaced by the value, for example: Score: {0}")]
            public string format = "{0}";

            [NonSerialized] public Label label;
        }

        [Tooltip("The labels to keep up to date.")]
        [SerializeField] List<Binding> bindings = new List<Binding>();

        void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document != null ? document.rootVisualElement : null;

            if (root == null)
            {
                Debug.LogWarning($"UI Variable Binder on \"{name}\": the UI Document has no Source Asset, so there are no labels to fill in.", this);
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                Binding binding = bindings[i];

                if (binding == null || string.IsNullOrEmpty(binding.labelName))
                {
                    continue;
                }

                binding.label = root.Q<Label>(binding.labelName);

                if (binding.label == null)
                {
                    Debug.LogWarning($"UI Variable Binder on \"{name}\": there is no label named \"{binding.labelName}\" in the UI Document. Check the spelling against the UXML file.", this);
                }

                if (binding.variable == null)
                {
                    Debug.LogWarning($"UI Variable Binder on \"{name}\": no Variable is set for the label \"{binding.labelName}\".", this);
                }
                else
                {
                    binding.variable.ValueChanged += RefreshAll;
                }
            }

            RefreshAll();
        }

        void OnDisable()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                Binding binding = bindings[i];

                if (binding == null)
                {
                    continue;
                }

                if (binding.variable != null)
                {
                    binding.variable.ValueChanged -= RefreshAll;
                }

                binding.label = null;
            }
        }

        void RefreshAll()
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                Binding binding = bindings[i];

                if (binding == null || binding.label == null || binding.variable == null)
                {
                    continue;
                }

                string value = binding.variable.ValueAsText;

                if (string.IsNullOrEmpty(binding.format))
                {
                    binding.label.text = value;
                    continue;
                }

                try
                {
                    binding.label.text = string.Format(binding.format, value);
                }
                catch (FormatException)
                {
                    // The Format text has a stray { or }. Show the plain value instead of failing.
                    binding.label.text = value;
                }
            }
        }
    }
}
