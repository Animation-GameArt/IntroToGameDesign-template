using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.TurnBasedBattle.EditorTools
{
    /// <summary>
    /// Builds the Turn-Based Battle Kit's sample, three battles, following
    /// "Turn-Based Battle Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Turn-Based Battle Sample.
    /// </summary>
    public static class TurnBasedBattleSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/TurnBasedBattle";
        const string Dungeon = "kenney_tiny-dungeon";
        const string MenuScene = "Battle_Menu";
        const string WinScene = "Battle_Win";
        static readonly Color Backdrop = new Color(0.16f, 0.15f, 0.22f);

        [MenuItem("Collin Game/Kit Samples/Build Turn-Based Battle Sample")]
        static void Build()
        {
            Run("Turn-Based Battle", KitFolder, new[] { "Data", "Actions", "Prefabs", "Scenes" },
                new[] { Kenney + Dungeon }, BuildSample);
        }

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — Data and actions
            StringVariable status = b.Text("BattleStatus");
            StringVariable log = b.Text("BattleLog");
            IntVariable round = b.Int("BattleRound");
            VoidEventChannel won = b.Channel("BattleWon");
            VoidEventChannel lost = b.Channel("BattleLost");

            BattleAction Action(string actionName, BattleAction.Effect effect, BattleAction.Target target, int power, int accuracy, int uses, string description)
            {
                BattleAction action = b.Asset<BattleAction>("Actions", actionName);
                Set(action, "displayName", actionName);
                Set(action, "description", description);
                Set(action, "effect", effect);
                Set(action, "target", target);
                Set(action, "power", power);
                Set(action, "accuracy", accuracy);
                Set(action, "usesPerBattle", uses);
                return action;
            }

            const BattleAction.Effect Damage = BattleAction.Effect.Damage;
            const BattleAction.Effect Heal = BattleAction.Effect.Heal;
            const BattleAction.Effect Defend = BattleAction.Effect.Defend;
            const BattleAction.Target Opponent = BattleAction.Target.OneOpponent;
            const BattleAction.Target Self = BattleAction.Target.Self;

            BattleAction slash = Action("Slash", Damage, Opponent, 3, 95, 0, "A quick, reliable cut.");
            BattleAction powerStrike = Action("Power Strike", Damage, Opponent, 6, 70, 0, "Hits hard, but often misses.");
            BattleAction defend = Action("Defend", Defend, Self, 0, 100, 0, "Take half damage until your next turn.");
            BattleAction potion = Action("Potion", Heal, Self, 5, 100, 2, "Recover 5 health. Two per battle.");
            BattleAction staff = Action("Staff", Damage, Opponent, 2, 95, 0, "A weak blow.");
            BattleAction heal = Action("Heal", Heal, BattleAction.Target.OneAlly, 4, 100, 3, "Recover 4 health for one ally.");
            BattleAction bite = Action("Bite", Damage, Opponent, 2, 90, 0, "");
            BattleAction club = Action("Club", Damage, Opponent, 4, 85, 0, "");
            BattleAction roar = Action("Roar", Defend, Self, 0, 100, 0, "");
            BattleAction firestorm = Action("Firestorm", Damage, BattleAction.Target.AllOpponents, 2, 100, 1, "Burns every enemy. Once per battle.");

            // Part 2 — Fighters
            Sprite square = b.WhiteSquare();

            GameObject Fighter(string fighterName, string tile, bool party, int maxHealth, int attack, int defense, params BattleAction[] actions)
            {
                GameObject root = new GameObject(fighterName);
                GameObject art = SpriteObject("Sprite", KenneySprite(Dungeon, "Tiles/" + tile), 10);
                art.transform.SetParent(root.transform, false);
                art.transform.localScale = Vector3.one * 3f;
                art.GetComponent<SpriteRenderer>().flipX = !party;

                TurnTaker taker = root.AddComponent<TurnTaker>();
                Set(taker, "displayName", fighterName);
                Set(taker, "playerSide", party);

                Health health = root.AddComponent<Health>();
                Set(health, "maxHealth", maxHealth);
                Set(health, "invulnerableTime", 0f);
                Set(health, "whenHealthRunsOut", Health.DeathAction.Deactivate);

                BattleActor actor = root.AddComponent<BattleActor>();
                Set(actor, "controlledBy", party ? BattleActor.Controller.Player : BattleActor.Controller.Computer);
                Set(actor, "actions", new List<BattleAction>(actions));
                Set(actor, "attack", attack);
                Set(actor, "defense", defense);
                Set(actor, "lungePart", art.transform);

                GameObject bar = Child(root, "Health Bar");
                bar.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                GameObject background = SpriteObject("Background", square, 20);
                background.transform.SetParent(bar.transform, false);
                background.transform.localScale = new Vector3(1.2f, 0.15f, 1f);
                background.GetComponent<SpriteRenderer>().color = new Color(0.2f, 0.2f, 0.2f);
                GameObject fill = SpriteObject("Fill", square, 21);
                fill.transform.SetParent(bar.transform, false);
                fill.transform.localScale = new Vector3(1.2f, 0.15f, 1f);
                fill.GetComponent<SpriteRenderer>().color = new Color(0.35f, 0.85f, 0.4f);

                HealthBar2D healthBar = root.AddComponent<HealthBar2D>();
                Set(healthBar, "health", health);
                Set(healthBar, "fill", fill.GetComponent<SpriteRenderer>());
                return b.Prefab(root);
            }

            GameObject hero = Fighter("Hero", "tile_0096", true, 20, 1, 1, slash, powerStrike, defend, potion, firestorm);
            GameObject healer = Fighter("Healer", "tile_0099", true, 14, 0, 0, staff, heal, defend);
            GameObject slime = Fighter("Slime", "tile_0108", false, 8, 0, 0, bite);
            GameObject ogre = Fighter("Ogre", "tile_0109", false, 30, 2, 2, club, roar);

            // Battle Systems
            GameObject systemsRoot = new GameObject("Battle Systems");
            Systems systems = b.StandardSystems(systemsRoot, MenuScene);

            TurnManager manager = Child(systemsRoot, "Turn Manager").AddComponent<TurnManager>();
            Set(manager, "startAutomatically", true);
            Set(manager, "pauseBetweenTurns", 0.6f);
            Set(manager, "statusText", status);
            Set(manager, "roundVariable", round);
            Set(manager, "raiseOnPlayerWon", won);
            Set(manager, "raiseOnPlayerLost", lost);

            BattleLog battleLog = Child(systemsRoot, "Battle Log").AddComponent<BattleLog>();
            Set(battleLog, "logText", log);
            Set(battleLog, "linesToKeep", 4);

            GameObject screen = Child(systemsRoot, "Battle Screen");
            b.Document(screen, KitFolder + "/UI/BattleUI.uxml", 0);
            screen.AddComponent<UIBattleMenu>();
            Bind(screen, ("status-label", status, "{0}"), ("log-label", log, "{0}"));

            // Victory: next battle, or the menu.
            GameObject victory = Child(systemsRoot, "Victory");
            b.Document(victory, b.UxmlCopy("Assets/_Course/Kits/Platformer/UI/LevelComplete.uxml", "Victory",
                ("text=\"Level Complete!\"", "text=\"Victory!\"")), 12);
            UIScreen victoryScreen = MakeScreen(victory, true, "next-button");
            Bind(victory, ("coins-label", round, "Rounds: {0}"));
            Listen(victory, won, Call(victoryScreen.Show));
            UIButtonEvents victoryButtons = victory.AddComponent<UIButtonEvents>();
            AddButton(victoryButtons, "next-button", Call(systems.SceneChanger.LoadNextScene));
            AddButton(victoryButtons, "menu-button", Call(systems.ToMenu.Load));

            b.EndScreen(systems, "Defeat", false, lost, null, null, "Defeated", "The party falls.");

            GameObject systemsPrefab = b.Prefab(systemsRoot);

            // Part 4 — Menu first, so it's first in the Scene List
            b.MenuScene(MenuScene, "Three Battles", "Turn-Based Battle Kit sample", "Battle_1", null, Backdrop);

            // Part 3 — Battles
            void Battle(string sceneName, (GameObject prefab, string name)[] party, (GameObject prefab, string name)[] foes)
            {
                Scene scene = NewScene();
                Camera2D(new Vector3(0f, 0f, -10f), 5f, Backdrop);

                GameObject floor = SpriteObject("Floor", square, 0);
                floor.transform.position = new Vector3(0f, -0.4f, 0f);
                floor.transform.localScale = new Vector3(20f, 1.2f, 1f);
                floor.GetComponent<SpriteRenderer>().color = new Color(0.27f, 0.24f, 0.3f);

                List<TurnTaker> order = new List<TurnTaker>();

                void Line((GameObject prefab, string name)[] fighters, float x, float step)
                {
                    for (int i = 0; i < fighters.Length; i++)
                    {
                        GameObject fighter = Place(fighters[i].prefab, new Vector3(x + step * i, 1.2f - 0.9f * i, 0f));
                        fighter.name = fighters[i].name;
                        TurnTaker taker = fighter.GetComponent<TurnTaker>();
                        Set(taker, "displayName", fighters[i].name);
                        KeepChanges(taker);
                        order.Add(taker);
                    }
                }

                Line(party, -4f, -1.2f);
                Line(foes, 4f, 1.2f);

                GameObject instance = Place(systemsPrefab, Vector3.zero);
                TurnManager placed = instance.GetComponentInChildren<TurnManager>();
                Set(placed, "turnOrder", order);
                KeepChanges(placed);
                b.SaveScene(scene, sceneName);
            }

            Battle("Battle_1", new[] { (hero, "Hero") }, new[] { (slime, "Slime") });
            Battle("Battle_2", new[] { (hero, "Hero"), (healer, "Healer") }, new[] { (slime, "Slime A"), (slime, "Slime B") });
            Battle("Battle_3", new[] { (hero, "Hero"), (healer, "Healer") }, new[] { (ogre, "Ogre") });

            b.WinScene(WinScene, "Victory!", "All three battles won.", "Battle_1", MenuScene, null, Backdrop);
        }
    }
}
