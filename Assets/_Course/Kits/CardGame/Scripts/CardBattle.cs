using System;
using System.Collections;
using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace CollinGame.Kits.CardGame
{
    /// <summary>
    /// Runs card battles against a list of enemies, one after another.
    ///
    /// Each turn the player draws a hand, gets energy, and plays cards that
    /// cost energy. Ending the turn discards the hand, and the enemy makes the
    /// move it showed. Block stops damage until the blocker's next turn. When
    /// the draw pile runs out, the discard pile is shuffled into it.
    ///
    /// Beat every enemy to win; the player's health carries over between
    /// battles. The UI Card Battle shows it all.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Card Game Kit/Card Battle")]
    public class CardBattle : MonoBehaviour
    {
        public enum Phase
        {
            Starting,
            PlayerTurn,
            EnemyTurn,
            BetweenBattles,
            Won,
            Lost
        }

        [Header("Player")]

        [Tooltip("The player's deck. Put a card in twice to have two copies.")]
        [SerializeField] List<CardData> deck = new List<CardData>();

        [SerializeField, Min(1)] int maxHealth = 40;

        [Tooltip("Energy at the start of each turn.")]
        [SerializeField, Min(0)] int energyPerTurn = 3;

        [Tooltip("Cards drawn at the start of each turn.")]
        [SerializeField, Min(1)] int handSize = 5;

        [Tooltip("The most cards the hand can hold. Draws past it are lost.")]
        [SerializeField, Min(1)] int maxHandSize = 10;

        [Tooltip("Optional. A number variable kept equal to the player's health, for other scripts and the HUD.")]
        [SerializeField] IntVariable playerHealth;

        [Header("Enemies")]

        [Tooltip("The battles, in order.")]
        [SerializeField] List<EnemyData> enemies = new List<EnemyData>();

        [Tooltip("Health the player gets back after each battle.")]
        [SerializeField, Min(0)] int healBetweenBattles = 10;

        [Tooltip("Seconds the enemy waits before and after its move, so the player can see it.")]
        [SerializeField, Min(0f)] float enemyTurnPause = 0.6f;

        [Tooltip("Seconds between one battle and the next.")]
        [SerializeField, Min(0f)] float pauseBetweenBattles = 1.5f;

        [Header("Results")]

        [SerializeField] VoidEventChannel raiseOnWin;
        [SerializeField] VoidEventChannel raiseOnLose;

        [Header("Sounds and effects")]

        [SerializeField] UnityEvent onCardPlayed;
        [SerializeField] UnityEvent onEnemyHit;
        [SerializeField] UnityEvent onPlayerHit;
        [SerializeField] UnityEvent onBlock;
        [SerializeField] UnityEvent onEnemyDefeated;

        readonly List<CardData> drawPile = new List<CardData>();
        readonly List<CardData> hand = new List<CardData>();
        readonly List<CardData> discardPile = new List<CardData>();
        int nextMove;

        /// <summary>Raised whenever anything the screen shows has changed.</summary>
        public event Action Changed;

        /// <summary>Raised with a line describing what just happened, such as "Slime attacks for 6."</summary>
        public event Action<string> Logged;

        public Phase CurrentPhase { get; private set; } = Phase.Starting;
        public IReadOnlyList<CardData> Hand => hand;
        public int DrawCount => drawPile.Count;
        public int DiscardCount => discardPile.Count;

        public int Energy { get; private set; }
        public int EnergyPerTurn => energyPerTurn;
        public int PlayerHealth { get; private set; }
        public int PlayerMaxHealth => maxHealth;
        public int PlayerBlock { get; private set; }

        public EnemyData Enemy { get; private set; }
        public int EnemyHealth { get; private set; }
        public int EnemyBlock { get; private set; }

        /// <summary>The move the enemy will make at the end of this turn.</summary>
        public Intent EnemyIntent { get; private set; }

        /// <summary>The battle being fought, counting from 1.</summary>
        public int BattleNumber { get; private set; }
        public int BattleCount => enemies.Count;

        void Start()
        {
            PlayerHealth = maxHealth;
            SetHealthVariable();

            if (enemies.Count == 0 || deck.Count == 0)
            {
                Debug.LogWarning($"Card Battle on \"{name}\": it needs at least one card in Deck and one enemy in Enemies.", this);
                return;
            }

            StartBattle(0);
        }

        // ---------------------------------------------------------------- Battles

        void StartBattle(int index)
        {
            BattleNumber = index + 1;
            Enemy = enemies[index];
            EnemyHealth = Enemy.MaxHealth;
            EnemyBlock = 0;
            nextMove = 0;

            drawPile.Clear();
            hand.Clear();
            discardPile.Clear();

            foreach (CardData card in deck)
            {
                if (card != null)
                {
                    drawPile.Add(card);
                }
            }

            Shuffle(drawPile);
            Log($"Battle {BattleNumber}: {Enemy.DisplayName} appears!");
            StartPlayerTurn();
        }

        void StartPlayerTurn()
        {
            CurrentPhase = Phase.PlayerTurn;
            PlayerBlock = 0;
            Energy = energyPerTurn;
            ChooseEnemyIntent();
            Draw(handSize);
            Changed?.Invoke();
        }

        void ChooseEnemyIntent()
        {
            IReadOnlyList<Intent> moves = Enemy.Moves;

            if (moves.Count == 0)
            {
                EnemyIntent = null;
                return;
            }

            EnemyIntent = Enemy.RandomOrder ? moves[Random.Range(0, moves.Count)] : moves[nextMove % moves.Count];
            nextMove++;
        }

        // ---------------------------------------------------------------- Cards

        /// <summary>True if the card in this hand position can be played now.</summary>
        public bool CanPlay(int handIndex)
        {
            return CurrentPhase == Phase.PlayerTurn && handIndex >= 0 && handIndex < hand.Count && hand[handIndex].Cost <= Energy;
        }

        /// <summary>Plays the card in this hand position, counting from 0.</summary>
        public void PlayCard(int handIndex)
        {
            if (!CanPlay(handIndex) || PauseController.GamePaused)
            {
                return;
            }

            CardData card = hand[handIndex];
            hand.RemoveAt(handIndex);
            Energy -= card.Cost;

            if (!card.Exhaust)
            {
                discardPile.Add(card);
            }

            onCardPlayed?.Invoke();

            foreach (CardEffect effect in card.Effects)
            {
                if (effect == null)
                {
                    continue;
                }

                for (int i = 0; i < effect.times; i++)
                {
                    Apply(effect);

                    if (EnemyHealth <= 0)
                    {
                        break;
                    }
                }

                if (EnemyHealth <= 0)
                {
                    break;
                }
            }

            Changed?.Invoke();

            if (EnemyHealth <= 0)
            {
                StartCoroutine(EnemyDefeated());
            }
        }

        void Apply(CardEffect effect)
        {
            switch (effect.type)
            {
                case CardEffectType.Damage:
                    int blocked = Mathf.Min(EnemyBlock, effect.amount);
                    EnemyBlock -= blocked;
                    int dealt = effect.amount - blocked;
                    EnemyHealth = Mathf.Max(0, EnemyHealth - dealt);
                    Log(dealt > 0 ? $"You deal {dealt}." : $"{Enemy.DisplayName} blocks it.");
                    onEnemyHit?.Invoke();
                    break;

                case CardEffectType.Block:
                    PlayerBlock += effect.amount;
                    onBlock?.Invoke();
                    break;

                case CardEffectType.Heal:
                    PlayerHealth = Mathf.Min(maxHealth, PlayerHealth + effect.amount);
                    SetHealthVariable();
                    break;

                case CardEffectType.Draw:
                    Draw(effect.amount);
                    break;

                case CardEffectType.Energy:
                    Energy += effect.amount;
                    break;
            }
        }

        /// <summary>Ends the player's turn: discards the hand, and the enemy moves.</summary>
        public void EndTurn()
        {
            if (CurrentPhase != Phase.PlayerTurn || PauseController.GamePaused)
            {
                return;
            }

            discardPile.AddRange(hand);
            hand.Clear();
            CurrentPhase = Phase.EnemyTurn;
            Changed?.Invoke();
            StartCoroutine(EnemyTurn());
        }

        IEnumerator EnemyTurn()
        {
            yield return new WaitForSeconds(enemyTurnPause);

            // The enemy's block from last turn wears off as its turn starts.
            EnemyBlock = 0;
            Intent move = EnemyIntent;

            if (move != null)
            {
                for (int i = 0; i < move.times; i++)
                {
                    switch (move.type)
                    {
                        case IntentType.Attack:
                            int blocked = Mathf.Min(PlayerBlock, move.amount);
                            PlayerBlock -= blocked;
                            int damage = move.amount - blocked;
                            PlayerHealth = Mathf.Max(0, PlayerHealth - damage);
                            SetHealthVariable();
                            Log(damage > 0 ? $"{Enemy.DisplayName} hits you for {damage}." : $"You block {Enemy.DisplayName}'s attack.");

                            if (damage > 0)
                            {
                                onPlayerHit?.Invoke();
                            }
                            else
                            {
                                onBlock?.Invoke();
                            }
                            break;

                        case IntentType.Block:
                            EnemyBlock += move.amount;
                            Log($"{Enemy.DisplayName} blocks {move.amount}.");
                            break;

                        case IntentType.Heal:
                            EnemyHealth = Mathf.Min(Enemy.MaxHealth, EnemyHealth + move.amount);
                            Log($"{Enemy.DisplayName} heals {move.amount}.");
                            break;
                    }

                    Changed?.Invoke();

                    if (PlayerHealth <= 0)
                    {
                        break;
                    }

                    if (move.times > 1)
                    {
                        yield return new WaitForSeconds(enemyTurnPause * 0.4f);
                    }
                }
            }

            if (PlayerHealth <= 0)
            {
                CurrentPhase = Phase.Lost;
                Log("You have been defeated.");
                Changed?.Invoke();

                if (raiseOnLose != null)
                {
                    raiseOnLose.Raise();
                }

                yield break;
            }

            yield return new WaitForSeconds(enemyTurnPause);
            StartPlayerTurn();
        }

        IEnumerator EnemyDefeated()
        {
            CurrentPhase = Phase.BetweenBattles;
            Log($"{Enemy.DisplayName} is defeated!");
            onEnemyDefeated?.Invoke();
            discardPile.AddRange(hand);
            hand.Clear();
            Changed?.Invoke();

            if (BattleNumber >= enemies.Count)
            {
                CurrentPhase = Phase.Won;
                Changed?.Invoke();

                if (raiseOnWin != null)
                {
                    raiseOnWin.Raise();
                }

                yield break;
            }

            if (healBetweenBattles > 0)
            {
                PlayerHealth = Mathf.Min(maxHealth, PlayerHealth + healBetweenBattles);
                SetHealthVariable();
                Log($"You rest, and heal {healBetweenBattles}.");
            }

            yield return new WaitForSeconds(pauseBetweenBattles);
            StartBattle(BattleNumber);
        }

        // ---------------------------------------------------------------- Piles

        void Draw(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0)
                    {
                        return;
                    }

                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    Shuffle(drawPile);
                }

                CardData card = drawPile[drawPile.Count - 1];
                drawPile.RemoveAt(drawPile.Count - 1);

                if (hand.Count < maxHandSize)
                {
                    hand.Add(card);
                }
                else
                {
                    discardPile.Add(card);
                }
            }
        }

        static void Shuffle(List<CardData> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }

        void SetHealthVariable()
        {
            if (playerHealth != null)
            {
                playerHealth.SetValue(PlayerHealth);
            }
        }

        void Log(string line)
        {
            Logged?.Invoke(line);
        }
    }
}
