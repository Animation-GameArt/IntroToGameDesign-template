using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CollinGame.Kits.CardGame
{
    /// <summary>
    /// The card battle screen: the enemy with its health, block and next move;
    /// the player's health, block and energy; the hand of cards; the draw and
    /// discard piles; End Turn; and a line saying what just happened.
    ///
    /// Click or tap a card to play it. The number keys 1 to 9 play cards too,
    /// and E or Enter ends the turn.
    ///
    /// Add it to the same GameObject as a UI Document using CardBattle.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Card Game Kit/UI Card Battle")]
    public class UICardBattle : MonoBehaviour
    {
        [Tooltip("The battle to show. Found in the scene if empty.")]
        [SerializeField] CardBattle battle;

        [Tooltip("The blank card for cards without a Face, such as Kenney's card_empty.")]
        [SerializeField] Sprite blankCard;

        VisualElement hand;
        VisualElement enemyPortrait;
        Label enemyName;
        VisualElement enemyHealthFill;
        Label enemyHealthLabel;
        Label enemyBlockLabel;
        Label enemyIntentLabel;
        VisualElement playerHealthFill;
        Label playerHealthLabel;
        Label playerBlockLabel;
        Label energyLabel;
        Label drawLabel;
        Label discardLabel;
        Label battleLabel;
        Label logLabel;
        Button endTurnButton;
        readonly List<VisualElement> cardElements = new List<VisualElement>();

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;

            if (root == null)
            {
                return;
            }

            hand = root.Q<VisualElement>("hand");
            enemyPortrait = root.Q<VisualElement>("enemy-portrait");
            enemyName = root.Q<Label>("enemy-name");
            enemyHealthFill = root.Q<VisualElement>("enemy-health-fill");
            enemyHealthLabel = root.Q<Label>("enemy-health-label");
            enemyBlockLabel = root.Q<Label>("enemy-block-label");
            enemyIntentLabel = root.Q<Label>("enemy-intent-label");
            playerHealthFill = root.Q<VisualElement>("player-health-fill");
            playerHealthLabel = root.Q<Label>("player-health-label");
            playerBlockLabel = root.Q<Label>("player-block-label");
            energyLabel = root.Q<Label>("energy-label");
            drawLabel = root.Q<Label>("draw-label");
            discardLabel = root.Q<Label>("discard-label");
            battleLabel = root.Q<Label>("battle-label");
            logLabel = root.Q<Label>("log-label");
            endTurnButton = root.Q<Button>("end-turn-button");

            if (hand == null)
            {
                Debug.LogWarning($"UI Card Battle on \"{name}\": the UI Document needs an element named \"hand\". The kit's CardBattle screen has it.", this);
                return;
            }

            if (battle == null)
            {
                battle = FindFirstObjectByType<CardBattle>();
            }

            if (battle != null)
            {
                battle.Changed += Refresh;
                battle.Logged += ShowLog;
            }

            if (endTurnButton != null && battle != null)
            {
                endTurnButton.clicked += battle.EndTurn;
            }
        }

        void OnDisable()
        {
            if (battle != null)
            {
                battle.Changed -= Refresh;
                battle.Logged -= ShowLog;

                if (endTurnButton != null)
                {
                    endTurnButton.clicked -= battle.EndTurn;
                }
            }
        }

        void Update()
        {
            if (battle == null || PauseController.GamePaused || battle.CurrentPhase != CardBattle.Phase.PlayerTurn)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            for (int i = 0; i < 9; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                {
                    battle.PlayCard(i);
                    return;
                }
            }

            if (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            {
                battle.EndTurn();
            }
        }

        // ---------------------------------------------------------------- Showing the battle

        void Refresh()
        {
            if (battle == null || hand == null)
            {
                return;
            }

            bool playerTurn = battle.CurrentPhase == CardBattle.Phase.PlayerTurn;

            // The enemy
            EnemyData enemy = battle.Enemy;

            if (enemy != null)
            {
                SetText(enemyName, enemy.DisplayName);

                if (enemyPortrait != null)
                {
                    enemyPortrait.style.backgroundImage = enemy.Portrait != null ? new StyleBackground(enemy.Portrait) : new StyleBackground(StyleKeyword.None);
                    enemyPortrait.EnableInClassList("enemy-portrait--defeated", battle.EnemyHealth <= 0);
                }

                SetBar(enemyHealthFill, battle.EnemyHealth, enemy.MaxHealth);
                SetText(enemyHealthLabel, $"{battle.EnemyHealth} / {enemy.MaxHealth}");
                SetText(enemyBlockLabel, battle.EnemyBlock > 0 ? $"Block {battle.EnemyBlock}" : string.Empty);
                SetText(enemyIntentLabel, battle.EnemyIntent != null && battle.EnemyHealth > 0 ? $"Next: {battle.EnemyIntent.Describe()}" : string.Empty);
            }

            // The player
            SetBar(playerHealthFill, battle.PlayerHealth, battle.PlayerMaxHealth);
            SetText(playerHealthLabel, $"{battle.PlayerHealth} / {battle.PlayerMaxHealth}");
            SetText(playerBlockLabel, battle.PlayerBlock > 0 ? $"Block {battle.PlayerBlock}" : string.Empty);
            SetText(energyLabel, $"Energy {battle.Energy} / {battle.EnergyPerTurn}");
            SetText(drawLabel, $"Draw: {battle.DrawCount}");
            SetText(discardLabel, $"Discard: {battle.DiscardCount}");
            SetText(battleLabel, $"Battle {battle.BattleNumber} of {battle.BattleCount}");
            endTurnButton?.SetEnabled(playerTurn);

            BuildHand(playerTurn);
        }

        void BuildHand(bool playerTurn)
        {
            hand.Clear();
            cardElements.Clear();

            for (int i = 0; i < battle.Hand.Count; i++)
            {
                CardData card = battle.Hand[i];
                VisualElement slot = new VisualElement();
                slot.AddToClassList("card-slot");

                VisualElement face = new VisualElement { pickingMode = PickingMode.Ignore };
                face.AddToClassList("card");
                Sprite picture = card.Face != null ? card.Face : blankCard;

                if (picture != null)
                {
                    face.style.backgroundImage = new StyleBackground(picture);
                }

                if (card.ShowName)
                {
                    Label cardName = new Label(card.DisplayName) { pickingMode = PickingMode.Ignore };
                    cardName.AddToClassList("card-name");
                    face.Add(cardName);
                }

                Label cost = new Label(card.Cost.ToString()) { pickingMode = PickingMode.Ignore };
                cost.AddToClassList("card-cost");
                face.Add(cost);

                Label text = new Label(card.Description) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("card-text");

                Label key = new Label(i < 9 ? (i + 1).ToString() : string.Empty) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("card-key");

                slot.Add(face);
                slot.Add(text);
                slot.Add(key);
                slot.EnableInClassList("card-slot--unplayable", !battle.CanPlay(i));
                slot.tooltip = $"{card.DisplayName}: {card.Description}";

                int index = i;
                slot.RegisterCallback<ClickEvent>(e => battle.PlayCard(index));
                hand.Add(slot);
                cardElements.Add(slot);
            }

            hand.EnableInClassList("hand--waiting", !playerTurn);
        }

        void ShowLog(string line)
        {
            SetText(logLabel, line);
        }

        static void SetText(Label label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        static void SetBar(VisualElement fill, int value, int max)
        {
            if (fill != null)
            {
                fill.style.width = Length.Percent(max > 0 ? 100f * Mathf.Clamp(value, 0, max) / max : 0f);
            }
        }
    }
}
