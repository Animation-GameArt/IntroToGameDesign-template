using CollinGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.TopDownAdventure
{
    /// <summary>
    /// Shows a text variable in a message box of a UI Document, such as a sign's
    /// text or "Locked. You need 1 key." The box is hidden while the text is empty.
    ///
    /// Add it to the same GameObject as the UI Document. AdventureHUD has a box
    /// named "message-box" holding a label named "message-label".
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Top-Down Adventure Kit/UI Message Box")]
    public class UIMessageBox : MonoBehaviour
    {
        [Tooltip("The text variable to show. Interactable 2D and Key Door 2D write their messages to it.")]
        [SerializeField] StringVariable message;

        [Tooltip("Seconds before a message clears by itself. 0 means it stays until the text changes.")]
        [SerializeField, Min(0f)] float clearAfter = 4f;

        [Tooltip("The name of the box element in the UXML.")]
        [SerializeField] string boxName = "message-box";

        [Tooltip("The name of the label inside the box.")]
        [SerializeField] string labelName = "message-label";

        VisualElement box;
        Label label;
        float clearAt = -1f;

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            box = root?.Q<VisualElement>(boxName);
            label = root?.Q<Label>(labelName);

            if (box == null || label == null)
            {
                Debug.LogWarning($"UI Message Box on \"{name}\": the UI Document needs an element named \"{boxName}\" with a label named \"{labelName}\" inside. AdventureHUD has both.", this);
                return;
            }

            if (message == null)
            {
                Debug.LogWarning($"UI Message Box on \"{name}\": Message is empty. Drag in the text variable to show.", this);
                box.style.display = DisplayStyle.None;
                return;
            }

            message.Changed += Show;
            message.SetValue(string.Empty);
            Show(string.Empty);
        }

        void OnDisable()
        {
            if (message != null)
            {
                message.Changed -= Show;
            }
        }

        void Update()
        {
            // Unscaled time, so a message still clears while the game is paused.
            if (clearAt > 0f && Time.unscaledTime >= clearAt)
            {
                clearAt = -1f;
                message.SetValue(string.Empty);
            }
        }

        void Show(string text)
        {
            if (box == null)
            {
                return;
            }

            bool hasText = !string.IsNullOrEmpty(text);
            label.text = text;
            box.style.display = hasText ? DisplayStyle.Flex : DisplayStyle.None;
            clearAt = hasText && clearAfter > 0f ? Time.unscaledTime + clearAfter : -1f;
        }
    }
}
