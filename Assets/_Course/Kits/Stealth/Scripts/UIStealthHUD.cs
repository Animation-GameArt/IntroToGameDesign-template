using CollinGame.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.Stealth
{
    /// <summary>
    /// The stealth HUD: an alert meter showing the most alert guard, a word
    /// for how things stand (Unseen, Hidden, Suspicious, Spotted!), and the
    /// flashlight's battery.
    ///
    /// Add it to the same GameObject as a UI Document using StealthHUD. Show
    /// objectives with a UI Variable Binder on objective-label.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Stealth Kit/UI Stealth HUD")]
    public class UIStealthHUD : MonoBehaviour
    {
        [Tooltip("Optional. The flashlight battery variable (0 to 100), for the battery bar.")]
        [SerializeField] FloatVariable batteryPercent;

        VisualElement alertFill;
        Label stateLabel;
        VisualElement batteryFill;
        VisualElement batteryBar;

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            alertFill = root?.Q<VisualElement>("alert-fill");
            stateLabel = root?.Q<Label>("state-label");
            batteryFill = root?.Q<VisualElement>("battery-fill");
            batteryBar = root?.Q<VisualElement>("battery-bar");

            if (batteryBar != null)
            {
                batteryBar.style.display = batteryPercent != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void Update()
        {
            float alert = Guard3D.HighestAlert;
            bool chasing = Guard3D.AnyChasing;
            StealthTarget3D target = StealthTarget3D.Instance;
            bool hidden = target != null && target.IsHidden;

            if (alertFill != null)
            {
                alertFill.style.width = Length.Percent(alert * 100f);
                alertFill.EnableInClassList("alert-fill--chase", chasing);
            }

            if (stateLabel != null)
            {
                string text = chasing ? "Spotted!" : hidden ? "Hidden" : alert > 0.05f ? "Suspicious" : "Unseen";
                stateLabel.text = text;
                stateLabel.EnableInClassList("state--danger", chasing);
                stateLabel.EnableInClassList("state--hidden", hidden && !chasing);
            }

            if (batteryFill != null && batteryPercent != null)
            {
                batteryFill.style.width = Length.Percent(Mathf.Clamp(batteryPercent.Value, 0f, 100f));
            }
        }
    }
}
