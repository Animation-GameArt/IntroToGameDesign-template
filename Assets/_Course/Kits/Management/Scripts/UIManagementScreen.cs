using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.Management
{
    /// <summary>
    /// Connects a management game's screen to its Turn Cycle: the End Turn
    /// button, the end-of-turn report, and the event panel with one button per
    /// choice.
    ///
    /// Add it to the same GameObject as a UI Document using ManagementHUD (from
    /// this kit's UI folder). Show resources and the turn with a UI Variable Binder.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Management Kit/UI Management Screen")]
    public class UIManagementScreen : MonoBehaviour
    {
        [Tooltip("Optional. The Turn Cycle. Leave empty to use the one in the scene.")]
        [SerializeField] TurnCycle turnCycle;

        Button endTurnButton;
        Label reportLabel;
        VisualElement eventPanel;
        Label eventTitle;
        Label eventText;
        VisualElement eventChoices;

        void OnEnable()
        {
            if (turnCycle == null)
            {
                turnCycle = TurnCycle.Instance != null ? TurnCycle.Instance : FindFirstObjectByType<TurnCycle>();
            }

            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            endTurnButton = root?.Q<Button>("end-turn-button");
            reportLabel = root?.Q<Label>("report-label");
            eventPanel = root?.Q<VisualElement>("event-panel");
            eventTitle = root?.Q<Label>("event-title");
            eventText = root?.Q<Label>("event-text");
            eventChoices = root?.Q<VisualElement>("event-choices");

            if (turnCycle == null)
            {
                Debug.LogWarning($"UI Management Screen on \"{name}\": there is no Turn Cycle in the scene.", this);
                return;
            }

            if (endTurnButton != null)
            {
                endTurnButton.clicked += turnCycle.EndTurn;
            }

            turnCycle.ChoiceNeeded += ShowEvent;
            turnCycle.ReportReady += ShowReport;
            HideEvent();
        }

        void OnDisable()
        {
            if (turnCycle == null)
            {
                return;
            }

            if (endTurnButton != null)
            {
                endTurnButton.clicked -= turnCycle.EndTurn;
            }

            turnCycle.ChoiceNeeded -= ShowEvent;
            turnCycle.ReportReady -= ShowReport;
        }

        void Update()
        {
            if (turnCycle != null)
            {
                endTurnButton?.SetEnabled(!turnCycle.IsOver && turnCycle.PendingEvent == null);
            }
        }

        void ShowReport(string text)
        {
            if (reportLabel != null)
            {
                reportLabel.text = text;
            }
        }

        void ShowEvent(ManagementEvent shown)
        {
            if (eventPanel == null || eventChoices == null)
            {
                Debug.LogWarning($"UI Management Screen on \"{name}\": an event needs a choice, but there's no \"event-panel\" with \"event-choices\". Answering with the first choice.", this);
                turnCycle.Choose(0);
                return;
            }

            eventPanel.style.display = DisplayStyle.Flex;

            if (eventTitle != null) eventTitle.text = shown.Title;
            if (eventText != null) eventText.text = shown.Text;

            eventChoices.Clear();
            Button first = null;

            for (int i = 0; i < shown.Choices.Count; i++)
            {
                int index = i;
                Button button = new Button(() => Answer(index)) { text = shown.Choices[i].label };
                button.AddToClassList("menu-button");
                eventChoices.Add(button);
                first ??= button;
            }

            first?.Focus();
        }

        void Answer(int index)
        {
            HideEvent();
            turnCycle.Choose(index);
        }

        void HideEvent()
        {
            if (eventPanel != null)
            {
                eventPanel.style.display = DisplayStyle.None;
            }
        }
    }
}
