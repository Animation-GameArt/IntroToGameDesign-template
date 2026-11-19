using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.TurnBasedBattle
{
    /// <summary>
    /// The battle menu. When a player fighter's turn starts, it shows a button
    /// for each of the fighter's actions. After an action is picked, it shows
    /// a button for each possible target, if there is a choice to make.
    ///
    /// Add it to the same GameObject as a UI Document using BattleUI (from this
    /// kit's UI folder), or any UXML with elements of the same names.
    /// Mouse, keyboard (arrow keys and Enter) and gamepad all work.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Turn-Based Battle Kit/UI Battle Menu")]
    public class UIBattleMenu : MonoBehaviour
    {
        [Tooltip("The name of the element that is shown only while the player is choosing.")]
        [SerializeField] string menuName = "battle-menu";

        [Tooltip("The name of the label that shows whose turn it is.")]
        [SerializeField] string actorLabelName = "actor-label";

        [Tooltip("The name of the label that shows the selected action's description.")]
        [SerializeField] string descriptionLabelName = "description-label";

        [Tooltip("The name of the empty element that holds the action buttons.")]
        [SerializeField] string actionsName = "actions";

        [Tooltip("The name of the empty element that holds the target buttons.")]
        [SerializeField] string targetsName = "targets";

        [Tooltip("The USS class given to every generated button.")]
        [SerializeField] string buttonClass = "battle-button";

        VisualElement menu;
        Label actorLabel;
        Label descriptionLabel;
        VisualElement actionList;
        VisualElement targetList;

        BattleActor actor;
        BattleAction chosenAction;

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            if (root == null)
            {
                Debug.LogWarning($"UI Battle Menu on \"{name}\": the UI Document has no Source Asset. Use BattleUI from the kit's UI folder.", this);
                return;
            }

            menu = root.Q<VisualElement>(menuName);
            actorLabel = root.Q<Label>(actorLabelName);
            descriptionLabel = root.Q<Label>(descriptionLabelName);
            actionList = root.Q<VisualElement>(actionsName);
            targetList = root.Q<VisualElement>(targetsName);

            if (menu == null || actionList == null || targetList == null)
            {
                Debug.LogWarning($"UI Battle Menu on \"{name}\": the UI Document needs elements named \"{menuName}\", \"{actionsName}\" and \"{targetsName}\". BattleUI has all of them.", this);
                return;
            }

            BattleActor.PlayerTurnStarted += ShowActions;
            BattleActor.ActionStarted += OnActionStarted;
            Hide();
        }

        void OnDisable()
        {
            BattleActor.PlayerTurnStarted -= ShowActions;
            BattleActor.ActionStarted -= OnActionStarted;
        }

        void Update()
        {
            // Close the menu if the turn ended some other way, such as the battle ending.
            if (actor != null && !actor.IsMyTurn)
            {
                Hide();
            }
        }

        void ShowActions(BattleActor turnActor)
        {
            actor = turnActor;
            chosenAction = null;

            menu.style.display = DisplayStyle.Flex;
            targetList.style.display = DisplayStyle.None;
            actionList.style.display = DisplayStyle.Flex;
            actionList.Clear();

            SetText(actorLabel, $"{actor.DisplayName}'s turn");
            SetText(descriptionLabel, string.Empty);

            Button first = null;

            for (int i = 0; i < actor.Actions.Count; i++)
            {
                BattleAction action = actor.Actions[i];

                if (action == null)
                {
                    continue;
                }

                int uses = actor.UsesLeft(action);
                string text = uses >= 0 ? $"{action.DisplayName} ({uses})" : action.DisplayName;

                Button button = MakeButton(text, () => ChooseAction(action), action.Description);
                button.SetEnabled(actor.CanUse(action));
                actionList.Add(button);

                if (first == null && actor.CanUse(action))
                {
                    first = button;
                }
            }

            first?.Focus();
        }

        void ChooseAction(BattleAction action)
        {
            if (actor == null || !actor.IsMyTurn)
            {
                return;
            }

            List<BattleActor> targets = actor.PossibleTargets(action);

            if (!action.NeedsChosenTarget || targets.Count <= 1)
            {
                actor.Perform(action, targets.Count > 0 ? targets[0] : null);
                return;
            }

            chosenAction = action;
            BattleActor user = actor;
            actionList.style.display = DisplayStyle.None;
            targetList.style.display = DisplayStyle.Flex;
            targetList.Clear();

            SetText(descriptionLabel, $"{action.DisplayName}: choose a target.");

            Button first = null;

            for (int i = 0; i < targets.Count; i++)
            {
                BattleActor target = targets[i];
                string text = $"{target.DisplayName}  {target.Health.Current}/{target.Health.Max}";
                Button button = MakeButton(text, () => user.Perform(action, target), null);
                targetList.Add(button);
                first ??= button;
            }

            Button back = MakeButton("Back", () => ShowActions(user), null);
            back.AddToClassList(buttonClass + "--back");
            targetList.Add(back);

            first?.Focus();
        }

        Button MakeButton(string text, System.Action onClick, string description)
        {
            Button button = new Button(onClick) { text = text };
            button.AddToClassList(buttonClass);

            if (description != null)
            {
                // Show the description when the button is pointed at or selected with keys.
                button.RegisterCallback<FocusInEvent>(_ => SetText(descriptionLabel, description));
                button.RegisterCallback<PointerEnterEvent>(_ => SetText(descriptionLabel, description));
            }

            return button;
        }

        void OnActionStarted(BattleActor who, BattleAction action)
        {
            if (who == actor)
            {
                Hide();
            }
        }

        void Hide()
        {
            actor = null;
            chosenAction = null;

            if (menu != null)
            {
                menu.style.display = DisplayStyle.None;
            }
        }

        static void SetText(Label label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
