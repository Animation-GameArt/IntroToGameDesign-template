#nullable enable annotations

using System.Threading;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Yarn.Unity;

namespace CollinGame.Kits.VisualNovel
{
    /// <summary>
    /// Shows Yarn Spinner dialogue in a UI Toolkit screen: the speaker's name,
    /// the line typed out letter by letter, and a button for each choice.
    ///
    /// Click, tap, Space, Enter, E or the gamepad's bottom face button shows the
    /// whole line, then goes to the next one. Choices are clicked, tapped, or
    /// chosen with the arrow keys and Enter.
    ///
    /// Add it to the same GameObject as a UI Document using VisualNovel (or any
    /// screen with the same element names), and drag it into the Dialogue
    /// Runner's Dialogue Presenters list.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Visual Novel Kit/UI Dialogue Presenter")]
    public class UIDialoguePresenter : DialoguePresenterBase
    {
        [Tooltip("Letters per second as a line is typed out. 0 shows the whole line at once.")]
        [SerializeField, Min(0f)] float lettersPerSecond = 45f;

        [Tooltip("Ticked: choices whose condition is false are shown greyed out. Unticked: they're hidden.")]
        [SerializeField] bool showUnavailableChoices = true;

        [Tooltip("Ticked: the player can't move while dialogue runs. For using the kit in other games, such as talking to people in a top-down game.")]
        [SerializeField] bool stopPlayerWhileTalking = true;

        [Tooltip("Optional. The stage, for name colors and highlighting the speaker. Found on this GameObject if empty.")]
        [SerializeField] VisualNovelStage stage;

        [Header("Events")]

        [Tooltip("Runs when dialogue starts.")]
        [SerializeField] UnityEvent onDialogueStarted;

        [Tooltip("Runs when a line starts, such as for a blip sound.")]
        [SerializeField] UnityEvent onLineStarted;

        [Tooltip("Runs when dialogue ends.")]
        [SerializeField] UnityEvent onDialogueComplete;

        InputReader input;
        VisualElement panel;
        Label speakerLabel;
        Label lineLabel;
        VisualElement continueMark;
        VisualElement choices;
        bool waitingForAdvance;
        bool advanceRequested;
        float lineStartTime;

        /// <summary>True while dialogue is on screen.</summary>
        public bool IsShowing { get; private set; }

        void Awake()
        {
            input = InputReader.FindInProject();

            if (stage == null)
            {
                stage = GetComponent<VisualNovelStage>();
            }
        }

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            panel = root?.Q<VisualElement>("dialogue-panel");
            speakerLabel = root?.Q<Label>("speaker-label");
            lineLabel = root?.Q<Label>("line-label");
            continueMark = root?.Q<VisualElement>("continue-mark");
            choices = root?.Q<VisualElement>("choices");

            if (panel == null || lineLabel == null || choices == null)
            {
                Debug.LogWarning($"UI Dialogue Presenter on \"{name}\": the UI Document needs elements named \"dialogue-panel\", \"line-label\" and \"choices\". The kit's VisualNovel screen has them.", this);
                return;
            }

            // Clicking or tapping the line box also advances.
            panel.RegisterCallback<PointerDownEvent>(OnPanelPointerDown);

            Show(panel, false);
            Show(choices, false);
            Show(continueMark, false);
        }

        void OnDisable()
        {
            panel?.UnregisterCallback<PointerDownEvent>(OnPanelPointerDown);
        }

        void OnPanelPointerDown(PointerDownEvent e)
        {
            if (waitingForAdvance && !PauseController.GamePaused)
            {
                advanceRequested = true;
            }
        }

        void Update()
        {
            if (!waitingForAdvance || PauseController.GamePaused)
            {
                return;
            }

            // A short pause after each line starts, so one press doesn't skip two lines.
            if (Time.unscaledTime - lineStartTime < 0.1f)
            {
                return;
            }

            if (AdvancePressed())
            {
                advanceRequested = true;
            }
        }

        // Read straight from the devices, so it works while the player's controls are switched off.
        static bool AdvancePressed()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame))
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;

            if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
            {
                return true;
            }

            // Clicks and taps anywhere count, except on UI such as the pause button (the line box handles its own).
            Mouse mouse = Mouse.current;

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                return !PointerOverUI(mouse.position.ReadValue());
            }

            Touchscreen touch = Touchscreen.current;

            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                return !PointerOverUI(touch.primaryTouch.position.ReadValue());
            }

            return false;
        }

        static bool PointerOverUI(Vector2 screen)
        {
            foreach (UIDocument document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                IPanel uiPanel = document.rootVisualElement?.panel;

                if (uiPanel == null)
                {
                    continue;
                }

                VisualElement picked = uiPanel.Pick(RuntimePanelUtils.ScreenToPanel(uiPanel, new Vector2(screen.x, Screen.height - screen.y)));

                if (picked != null && picked != document.rootVisualElement)
                {
                    return true;
                }
            }

            return false;
        }

        // ---------------------------------------------------------------- Yarn Spinner calls these

        public override YarnTask OnDialogueStartedAsync(CancellationToken token)
        {
            IsShowing = true;
            Show(panel, true);

            if (stopPlayerWhileTalking && input != null)
            {
                input.DisablePlayerInput();
            }

            onDialogueStarted?.Invoke();
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueCompleteAsync(CancellationToken token)
        {
            IsShowing = false;
            waitingForAdvance = false;
            Show(panel, false);
            Show(choices, false);

            if (stage != null)
            {
                stage.HighlightSpeaker(null);
            }

            if (stopPlayerWhileTalking && input != null)
            {
                input.EnablePlayerInput();
            }

            onDialogueComplete?.Invoke();
            return YarnTask.CompletedTask;
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (lineLabel == null)
            {
                return;
            }

            string speaker = line.CharacterName;
            string text = line.TextWithoutCharacterName.Text;

            Show(panel, true);
            Show(choices, false);
            Show(continueMark, false);
            ShowSpeaker(speaker);

            if (stage != null)
            {
                stage.HighlightSpeaker(speaker);
            }

            onLineStarted?.Invoke();

            advanceRequested = false;
            waitingForAdvance = true;
            lineStartTime = Time.unscaledTime;

            // Type the line out.
            if (lettersPerSecond > 0f)
            {
                float shown = 0f;

                while (shown < text.Length && !token.IsNextContentRequested)
                {
                    if (advanceRequested || token.IsHurryUpRequested)
                    {
                        advanceRequested = false;
                        break;
                    }

                    shown += lettersPerSecond * Time.deltaTime;
                    lineLabel.text = text.Substring(0, Mathf.Min(text.Length, Mathf.FloorToInt(shown)));
                    await YarnTask.Yield();
                }
            }

            lineLabel.text = text;
            Show(continueMark, true);
            lineStartTime = Time.unscaledTime;

            // Wait for the player.
            while (!advanceRequested && !token.IsNextContentRequested)
            {
                await YarnTask.Yield();
            }

            advanceRequested = false;
            waitingForAdvance = false;
            Show(continueMark, false);
        }

        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
        {
            if (choices == null)
            {
                return null;
            }

            DialogueOption? chosen = null;
            Button first = null;
            choices.Clear();
            Show(continueMark, false);

            foreach (DialogueOption option in dialogueOptions)
            {
                if (!option.IsAvailable && !showUnavailableChoices)
                {
                    continue;
                }

                DialogueOption captured = option;
                Button button = new Button(() => chosen = captured) { text = option.Line.TextWithoutCharacterName.Text };
                button.AddToClassList("vn-choice");
                button.SetEnabled(option.IsAvailable);
                choices.Add(button);

                if (first == null && option.IsAvailable)
                {
                    first = button;
                }
            }

            Show(choices, true);
            first?.Focus();

            while (chosen == null && !cancellationToken.IsNextContentRequested)
            {
                await YarnTask.Yield();
            }

            Show(choices, false);
            choices.Clear();
            return chosen;
        }

        // ---------------------------------------------------------------- Helpers

        void ShowSpeaker(string speaker)
        {
            if (speakerLabel == null)
            {
                return;
            }

            bool hasSpeaker = !string.IsNullOrEmpty(speaker);
            Show(speakerLabel, hasSpeaker);

            if (!hasSpeaker)
            {
                return;
            }

            CharacterData character = stage != null ? stage.FindCharacter(speaker) : null;
            speakerLabel.text = character != null ? character.CharacterName : speaker;
            speakerLabel.style.color = character != null ? new StyleColor(character.NameColor) : new StyleColor(StyleKeyword.Null);
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
