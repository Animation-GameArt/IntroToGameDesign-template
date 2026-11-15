using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace CollinGame.Core
{
    /// <summary>
    /// Connects the buttons in a UI Document to things in the scene, with no code.
    /// Add it to the same GameObject as the UI Document.
    ///
    /// For each button, type its name (as set in the UXML file, for example
    /// "play-button") and choose what happens when it is clicked.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/UI/UI Button Events")]
    public class UIButtonEvents : MonoBehaviour
    {
        /// <summary>One button and what it does.</summary>
        [Serializable]
        public class ButtonEvent
        {
            [Tooltip("The button's name in the UXML file, for example: play-button")]
            public string buttonName;

            [Tooltip("What happens when the button is clicked.")]
            public UnityEvent onClick;
        }

        [Tooltip("The buttons to connect.")]
        [SerializeField] List<ButtonEvent> buttons = new List<ButtonEvent>();

        readonly List<Button> hookedButtons = new List<Button>();
        readonly List<Action> hookedHandlers = new List<Action>();

        void OnEnable()
        {
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document != null ? document.rootVisualElement : null;

            if (root == null)
            {
                Debug.LogWarning($"UI Button Events on \"{name}\": the UI Document has no Source Asset, so there are no buttons to connect.", this);
                return;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                ButtonEvent entry = buttons[i];

                if (entry == null || string.IsNullOrEmpty(entry.buttonName))
                {
                    continue;
                }

                Button button = root.Q<Button>(entry.buttonName);

                if (button == null)
                {
                    Debug.LogWarning($"UI Button Events on \"{name}\": there is no button named \"{entry.buttonName}\" in the UI Document. Check the spelling against the UXML file.", this);
                    continue;
                }

                UnityEvent response = entry.onClick;
                Action handler = () => response?.Invoke();

                button.clicked += handler;
                hookedButtons.Add(button);
                hookedHandlers.Add(handler);
            }
        }

        void OnDisable()
        {
            for (int i = 0; i < hookedButtons.Count; i++)
            {
                hookedButtons[i].clicked -= hookedHandlers[i];
            }

            hookedButtons.Clear();
            hookedHandlers.Clear();
        }
    }
}
