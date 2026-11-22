using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.CardGame.EditorTools
{
    /// <summary>
    /// Builds the Card Game Kit's sample, Suit Duel, following
    /// "Card Game Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Card Game Sample.
    /// </summary>
    public static class CardGameSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/CardGame";
        const string Cards = "kenney_playing-cards-pack";
        const string CardArt = "PNG/Cards (large)/";
        const string Dungeon = "kenney_tiny-dungeon";
        const string MenuScene = "Cards_Menu";
        const string GameScene = "Cards_Game";

        [MenuItem("Collin Game/Kit Samples/Build Card Game Sample")]
        static void Build()
        {
            Run("Card Game", KitFolder, new[] { "Data", "Cards", "Enemies", "Prefabs", "Scenes" },
                new[] { Kenney + Cards, Kenney + Dungeon }, BuildSample);
        }

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — Data
            IntVariable playerHealth = b.Int("PlayerHealth", 40);
            VoidEventChannel runWon = b.Channel("RunWon");
            VoidEventChannel runLost = b.Channel("RunLost");

            // Part 2 — Cards
            CardData Card(string cardName, string face, int cost, CardEffectType type, int amount)
            {
                CardData card = b.Asset<CardData>("Cards", cardName);
                Set(card, "displayName", cardName);
                Set(card, "face", KenneySprite(Cards, CardArt + face));
                Set(card, "showName", false);
                Set(card, "cost", cost);
                Set(card, "effects", new List<CardEffect> { new CardEffect { type = type, amount = amount, times = 1 } });
                return card;
            }

            List<CardData> deck = new List<CardData>();

            for (int n = 2; n <= 7; n++)
            {
                deck.Add(Card($"Spades {n}", $"card_spades_{n:00}", 1, CardEffectType.Damage, n));
            }

            deck.Add(Card("King of Spades", "card_spades_K", 2, CardEffectType.Damage, 10));

            for (int n = 3; n <= 6; n++)
            {
                deck.Add(Card($"Diamonds {n}", $"card_diamonds_{n:00}", 1, CardEffectType.Block, n));
            }

            deck.Add(Card("Hearts 3", "card_hearts_03", 1, CardEffectType.Heal, 3));
            deck.Add(Card("Hearts 4", "card_hearts_04", 1, CardEffectType.Heal, 4));
            deck.Add(Card("Clubs 2", "card_clubs_02", 1, CardEffectType.Draw, 2));
            deck.Add(Card("Jack of Clubs", "card_clubs_J", 0, CardEffectType.Energy, 2));

            // Part 3 — Enemies
            Intent Move(IntentType type, int amount, int times = 1) => new Intent { type = type, amount = amount, times = times };

            EnemyData Enemy(string enemyName, string portrait, int health, params Intent[] moves)
            {
                EnemyData enemy = b.Asset<EnemyData>("Enemies", enemyName);
                Set(enemy, "displayName", enemyName);
                Set(enemy, "portrait", KenneySprite(Dungeon, "Tiles/" + portrait));
                Set(enemy, "maxHealth", health);
                Set(enemy, "moves", new List<Intent>(moves));
                Set(enemy, "randomOrder", false);
                return enemy;
            }

            List<EnemyData> enemies = new List<EnemyData>
            {
                Enemy("Slime", "tile_0108", 28, Move(IntentType.Attack, 6), Move(IntentType.Attack, 6), Move(IntentType.Block, 4)),
                Enemy("Knight", "tile_0096", 42, Move(IntentType.Block, 10), Move(IntentType.Attack, 11), Move(IntentType.Attack, 8)),
                Enemy("Demon", "tile_0110", 62, Move(IntentType.Attack, 4, 3), Move(IntentType.Block, 10), Move(IntentType.Attack, 15)),
            };

            // Part 4 — Card Systems
            GameObject root = new GameObject("Card Systems");
            Systems systems = b.StandardSystems(root, MenuScene);

            CardBattle battle = Child(root, "Battle").AddComponent<CardBattle>();
            Set(battle, "deck", deck);
            Set(battle, "maxHealth", 40);
            Set(battle, "energyPerTurn", 3);
            Set(battle, "handSize", 5);
            Set(battle, "maxHandSize", 10);
            Set(battle, "playerHealth", playerHealth);
            Set(battle, "enemies", enemies);
            Set(battle, "healBetweenBattles", 10);
            Set(battle, "enemyTurnPause", 0.6f);
            Set(battle, "pauseBetweenBattles", 1.5f);
            Set(battle, "raiseOnWin", runWon);
            Set(battle, "raiseOnLose", runLost);

            GameObject screen = Child(root, "Screen");
            b.Document(screen, KitFolder + "/UI/CardBattle.uxml", 0);
            UICardBattle ui = screen.AddComponent<UICardBattle>();
            Set(ui, "battle", battle);
            Set(ui, "blankCard", KenneySprite(Cards, CardArt + "card_empty"));

            Resetter(Child(root, "Reset"), true, playerHealth);

            b.EndScreen(systems, "Won", true, runWon, new (string, VariableBase, string)[] { ("score-label", playerHealth, "Health left: {0}") }, new VariableBase[] { playerHealth }, "Victory!");
            b.EndScreen(systems, "Lost", false, runLost, null, new VariableBase[] { playerHealth });

            GameObject systemsPrefab = b.Prefab(root);

            // Part 5 — Scenes
            b.MenuScene(MenuScene, "Suit Duel", "Play cards to deal damage, block, heal or gain energy, against ever stronger enemies.",
                GameScene, new VariableBase[] { playerHealth }, new Color(0.13f, 0.36f, 0.23f),
                "Win three battles in a row: a Slime, a Knight and a Demon.\n\n" +
                "Each turn you draw five cards and get 3 energy. Click a card, or press its number, to play it. The number in its corner is its energy cost.\n\n" +
                "Spades deal damage. Diamonds block: block stops damage until your next turn. Hearts heal. Clubs draw more cards, or give energy. The bigger the number on the card, the stronger it is.\n\n" +
                "Above the enemy is what it will do next. Block when an attack is coming; attack when it isn't. Then press End Turn (or E, or Enter).\n\n" +
                "Your health carries over between battles, and you heal 10 after each one.");

            Scene game = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, new Color(0.13f, 0.36f, 0.23f));
            Place(systemsPrefab, Vector3.zero);
            b.SaveScene(game, GameScene);
        }
    }
}
