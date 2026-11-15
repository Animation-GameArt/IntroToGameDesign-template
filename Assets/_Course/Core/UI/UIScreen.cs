using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Core
{
    /// <summary>
    /// Shows and hides a whole UI Document: a pause menu, a win screen, a HUD.
    /// Add it to the same GameObject as the UI Document.
    ///
    /// Call Show or Hide from a button, an Event Listener or another script.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/UI/UI Screen")]
    public class UIScreen : MonoBehaviour
    {
        [Tooltip("Ticked: the screen is hidden when the scene starts, and appears when Show is called.")]
        [SerializeField] bool hiddenAtStart;

        [Tooltip("Optional. The name of the button to select when the screen appears, so a gamepad or keyboard can use the menu straight away.")]
        [SerializeField] string selectWhenShown = "";

        UIDocument document;
        bool visible;

        /// <summary>True while the screen is showing.</summary>
        public bool IsVisible => visible;

        void Awake()
        {
            document = GetComponent<UIDocument>();
            visible = !hiddenAtStart;
        }

        void OnEnable()
        {
            Apply();
        }

        /// <summary>Makes the screen appear.</summary>
        public void Show()
        {
            visible = true;
            Apply();
        }

        /// <summary>Makes the screen disappear.</summary>
        public void Hide()
        {
            visible = false;
            Apply();
        }

        /// <summary>Shows the screen if it is hidden, and hides it if it is showing.</summary>
        public void Toggle()
        {
            visible = !visible;
            Apply();
        }

        void Apply()
        {
            VisualElement root = document != null ? document.rootVisualElement : null;

            if (root == null)
            {
                return;
            }

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (visible && !string.IsNullOrEmpty(selectWhenShown))
            {
                VisualElement first = root.Q<VisualElement>(selectWhenShown);

                if (first != null)
                {
                    first.Focus();
                }
            }
        }
    }
}
