using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.Farming
{
    /// <summary>
    /// Short messages for the player ("Too tired", "Your bag is full") and the
    /// prompt for whatever the player is standing at ("E: Sleep"). Scripts call
    /// FarmMessage.Say; the UI Farm Screen shows them. The prompt is a button,
    /// so it can also be clicked or tapped.
    /// </summary>
    public static class FarmMessage
    {
        /// <summary>Raised with a message to show for a moment.</summary>
        public static event Action<string> Said;

        /// <summary>Raised when the prompt changes. An empty prompt hides it.</summary>
        public static event Action<string> PromptChanged;

        public static void Say(string text)
        {
            Said?.Invoke(text);
        }

        /// <summary>What clicking the prompt does, or null.</summary>
        public static Action PromptAction { get; private set; }

        public static void SetPrompt(string text, Action onPressed = null)
        {
            PromptAction = string.IsNullOrEmpty(text) ? null : onPressed;
            PromptChanged?.Invoke(text ?? string.Empty);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Said = null;
            PromptChanged = null;
            PromptAction = null;
        }

        /// <summary>True when a screen position is over a visible UI element, such as the hotbar.</summary>
        public static bool PointerOverUI(Vector2 screen)
        {
            foreach (UIDocument document in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                IPanel panel = document.rootVisualElement?.panel;

                if (panel == null)
                {
                    continue;
                }

                Vector2 panelPoint = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
                VisualElement picked = panel.Pick(panelPoint);

                if (picked != null && picked != document.rootVisualElement)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
