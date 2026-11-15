using System;
using TMPro;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Shows the value of a variable asset in a TextMeshPro text, and keeps it up to
    /// date whenever the value changes. Add it to the same GameObject as the text.
    ///
    /// Format is the text to show. {0} is replaced by the value,
    /// so "Score: {0}" shows "Score: 3".
    /// </summary>
    [AddComponentMenu("Collin Game/Core/Variable Label")]
    public class VariableLabel : MonoBehaviour
    {
        [Tooltip("The variable asset to show.")]
        [SerializeField] VariableBase variable;

        [Tooltip("The text to write into. Leave empty to use the text on this GameObject.")]
        [SerializeField] TMP_Text label;

        [Tooltip("The text to show. {0} is replaced by the value, for example: Score: {0}")]
        [SerializeField] string format = "{0}";

        void Awake()
        {
            if (label == null)
            {
                label = GetComponent<TMP_Text>();
            }
        }

        void OnEnable()
        {
            if (variable != null)
            {
                variable.ValueChanged += Refresh;
            }

            Refresh();
        }

        void OnDisable()
        {
            if (variable != null)
            {
                variable.ValueChanged -= Refresh;
            }
        }

        void Start()
        {
            if (variable == null)
            {
                Debug.LogWarning($"Variable Label on \"{name}\": no Variable is set, so there is nothing to show.", this);
            }

            if (label == null)
            {
                Debug.LogWarning($"Variable Label on \"{name}\": there is no TextMeshPro text on this GameObject, and no Label is set.", this);
            }

            Refresh();
        }

        void Refresh()
        {
            if (variable == null || label == null)
            {
                return;
            }

            string value = variable.ValueAsText;

            if (string.IsNullOrEmpty(format))
            {
                label.text = value;
                return;
            }

            try
            {
                label.text = string.Format(format, value);
            }
            catch (FormatException)
            {
                // The Format text has a stray { or }. Show the plain value instead of failing.
                label.text = value;
            }
        }
    }
}
