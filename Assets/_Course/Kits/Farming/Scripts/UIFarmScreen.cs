using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.Farming
{
    /// <summary>
    /// The farming game's screen: the hotbar (click or tap a slot to choose
    /// it), the name of the chosen item, the prompt for where the player is
    /// standing (also a button), short messages, and the report shown each
    /// morning.
    ///
    /// Add it to the same GameObject as a UI Document using FarmHUD. Show the
    /// day, time, money and energy with a UI Variable Binder.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Farming Kit/UI Farm Screen")]
    public class UIFarmScreen : MonoBehaviour
    {
        [Tooltip("The player's Inventory.")]
        [SerializeField] Inventory inventory;

        [Tooltip("Optional. The day cycle, for the morning report. Found in the scene if empty.")]
        [SerializeField] DayCycle dayCycle;

        [Tooltip("Seconds a message stays on screen.")]
        [SerializeField, Min(0.5f)] float messageSeconds = 2.5f;

        VisualElement hotbar;
        Label selectedLabel;
        Button promptButton;
        Label messageLabel;
        VisualElement reportPanel;
        Label reportLabel;
        Button continueButton;
        readonly List<VisualElement> slotElements = new List<VisualElement>();
        IVisualElementScheduledItem hideMessage;

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            if (root == null)
            {
                return;
            }

            hotbar = root.Q<VisualElement>("hotbar");
            selectedLabel = root.Q<Label>("selected-label");
            promptButton = root.Q<Button>("prompt-button");
            messageLabel = root.Q<Label>("message-label");
            reportPanel = root.Q<VisualElement>("report-panel");
            reportLabel = root.Q<Label>("report-label");
            continueButton = root.Q<Button>("report-continue-button");

            if (hotbar == null)
            {
                Debug.LogWarning($"UI Farm Screen on \"{name}\": the UI Document needs an element named \"hotbar\". FarmHUD has it.", this);
            }

            if (dayCycle == null)
            {
                dayCycle = FindFirstObjectByType<DayCycle>();
            }

            if (inventory == null)
            {
                inventory = FindFirstObjectByType<Inventory>();
            }

            if (inventory != null) inventory.Changed += Refresh;
            if (dayCycle != null) dayCycle.ReportReady += ShowReport;
            if (promptButton != null) promptButton.clicked += PressPrompt;
            if (continueButton != null) continueButton.clicked += CloseReport;
            FarmMessage.Said += ShowMessage;
            FarmMessage.PromptChanged += ShowPrompt;

            ShowPrompt(string.Empty);
            Show(messageLabel, false);
            Show(reportPanel, false);
        }

        void OnDisable()
        {
            if (inventory != null) inventory.Changed -= Refresh;
            if (dayCycle != null) dayCycle.ReportReady -= ShowReport;
            if (promptButton != null) promptButton.clicked -= PressPrompt;
            if (continueButton != null) continueButton.clicked -= CloseReport;
            FarmMessage.Said -= ShowMessage;
            FarmMessage.PromptChanged -= ShowPrompt;
        }

        void Start()
        {
            BuildHotbar();
            Refresh();
        }

        // ---------------------------------------------------------------- Hotbar

        void BuildHotbar()
        {
            if (hotbar == null || inventory == null)
            {
                return;
            }

            hotbar.Clear();
            slotElements.Clear();

            for (int i = 0; i < inventory.Size; i++)
            {
                VisualElement slot = new VisualElement();
                slot.AddToClassList("hotbar-slot");

                VisualElement icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
                icon.AddToClassList("hotbar-icon");
                slot.Add(icon);

                Label key = new Label(i < 9 ? (i + 1).ToString() : string.Empty) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("hotbar-key");
                slot.Add(key);

                Label count = new Label { name = "count", pickingMode = PickingMode.Ignore };
                count.AddToClassList("hotbar-count");
                slot.Add(count);

                int index = i;
                slot.RegisterCallback<PointerDownEvent>(e =>
                {
                    inventory.Select(index);
                    e.StopPropagation();
                });

                hotbar.Add(slot);
                slotElements.Add(slot);
            }
        }

        void Refresh()
        {
            if (inventory == null)
            {
                return;
            }

            if (slotElements.Count != inventory.Size)
            {
                BuildHotbar();
            }

            for (int i = 0; i < slotElements.Count && i < inventory.Slots.Count; i++)
            {
                Inventory.Slot slot = inventory.Slots[i];
                VisualElement element = slotElements[i];
                VisualElement icon = element.Q<VisualElement>("icon");
                Label count = element.Q<Label>("count");

                bool filled = !slot.IsEmpty;
                icon.style.backgroundImage = filled && slot.item.Icon != null ? new StyleBackground(slot.item.Icon) : StyleKeyword.Null;
                count.text = filled && slot.count > 1 ? slot.count.ToString() : string.Empty;
                element.tooltip = filled ? slot.item.DisplayName : string.Empty;
                element.EnableInClassList("hotbar-slot--selected", i == inventory.SelectedIndex);
            }

            if (selectedLabel != null)
            {
                ItemData selected = inventory.Selected;
                selectedLabel.text = selected != null ? selected.DisplayName : string.Empty;
            }
        }

        // ---------------------------------------------------------------- Prompt and messages

        void ShowPrompt(string text)
        {
            if (promptButton == null)
            {
                return;
            }

            promptButton.text = text;
            Show(promptButton, !string.IsNullOrEmpty(text));
        }

        void PressPrompt()
        {
            FarmMessage.PromptAction?.Invoke();
        }

        void ShowMessage(string text)
        {
            if (messageLabel == null)
            {
                return;
            }

            messageLabel.text = text;
            Show(messageLabel, true);
            hideMessage?.Pause();
            hideMessage = messageLabel.schedule.Execute(() => Show(messageLabel, false)).StartingIn((long)(messageSeconds * 1000f));
        }

        // ---------------------------------------------------------------- Morning report

        void ShowReport(string text)
        {
            if (reportPanel == null)
            {
                // No panel to show it in, so don't keep the player waiting.
                dayCycle.Continue();
                return;
            }

            if (reportLabel != null)
            {
                reportLabel.text = text;
            }

            Show(reportPanel, true);
            continueButton?.Focus();
        }

        void CloseReport()
        {
            Show(reportPanel, false);

            if (dayCycle != null)
            {
                dayCycle.Continue();
            }
        }

        static void Show(VisualElement element, bool visible)
        {
            if (element != null)
            {
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
