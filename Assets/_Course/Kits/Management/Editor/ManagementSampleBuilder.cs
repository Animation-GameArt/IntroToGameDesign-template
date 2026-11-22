using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.Management.EditorTools
{
    /// <summary>
    /// Builds the Management Kit's sample, the ten-year village, following
    /// "Management Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Management Sample.
    /// </summary>
    public static class ManagementSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/Management";
        const string Town = "kenney_tiny-town";
        const string MenuScene = "Village_Menu";
        const string GameScene = "Village_Game";

        [MenuItem("Collin Game/Kit Samples/Build Management Sample")]
        static void Build()
        {
            Run("Management", KitFolder, new[] { "Data", "Rules", "Buildings", "Events", "Prefabs", "Scenes" },
                new[] { Kenney + Town }, BuildSample);
        }

        static Effect E(IntVariable resource, int amount, IntVariable timesEach = null, int divideBy = 1, int randomExtra = 0,
            Effect.Comparison onlyWhen = Effect.Comparison.Always, IntVariable ifResource = null, int ifValue = 0)
        {
            return new Effect
            {
                resource = resource, amount = amount, timesEach = timesEach, divideBy = divideBy, randomExtra = randomExtra,
                onlyWhen = onlyWhen, ifResource = ifResource, ifValue = ifValue
            };
        }

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — Resources and events
            IntVariable people = b.Int("People", 12);
            IntVariable food = b.Int("Food", 40);
            IntVariable gold = b.Int("Gold", 60);
            IntVariable wood = b.Int("Wood", 10);
            IntVariable year = b.Int("Year", 1);
            StringVariable report = b.Text("Report");
            VoidEventChannel won = b.Channel("VillageWon");
            VoidEventChannel lost = b.Channel("VillageLost");

            // Part 2 — Turn Rules, in order
            TurnRule Rule(string ruleName, Effect effect)
            {
                TurnRule rule = b.Asset<TurnRule>("Rules", ruleName);
                Set(rule, "effects", new List<Effect> { effect });
                return rule;
            }

            List<TurnRule> rules = new List<TurnRule>
            {
                Rule("Eating", E(food, -1, people)),
                Rule("Hunger", E(people, -3, randomExtra: 1, onlyWhen: Effect.Comparison.AtMost, ifResource: food, ifValue: 0)),
                Rule("Taxes", E(gold, 1, people, divideBy: 3)),
                Rule("Growth", E(people, 1, onlyWhen: Effect.Comparison.AtLeast, ifResource: food, ifValue: 30)),
            };

            // Part 3 — Buildings
            BuildingData Building(string buildingName, string icon, string description, List<Effect> cost, Effect everyTurn)
            {
                BuildingData building = b.Asset<BuildingData>("Buildings", buildingName);
                Set(building, "displayName", buildingName);
                Set(building, "icon", KenneySprite(Town, "Tiles/" + icon));
                Set(building, "description", description);
                Set(building, "cost", cost);
                Set(building, "everyTurn", new List<Effect> { everyTurn });
                return building;
            }

            List<BuildingData> buildings = new List<BuildingData>
            {
                Building("Farm", "tile_0116", "+6 Food each year", new List<Effect> { E(gold, -20) }, E(food, 6)),
                Building("Lumber Camp", "tile_0127", "+4 Wood each year", new List<Effect> { E(gold, -15) }, E(wood, 4)),
                Building("House", "tile_0067", "+1 Person each year", new List<Effect> { E(wood, -12) }, E(people, 1)),
                Building("Market", "tile_0093", "+6 Gold each year", new List<Effect> { E(gold, -30), E(wood, -10) }, E(gold, 6)),
            };

            // Part 4 — Events
            ManagementEvent Event(string assetName, string title, string text, float weight, int earliest, bool onlyOnce,
                List<Effect> effects, List<ManagementEvent.Choice> choices = null)
            {
                ManagementEvent e = b.Asset<ManagementEvent>("Events", assetName);
                Set(e, "title", title);
                Set(e, "text", text);
                Set(e, "weight", weight);
                Set(e, "earliestTurn", earliest);
                Set(e, "onlyOnce", onlyOnce);
                Set(e, "effects", effects ?? new List<Effect>());
                Set(e, "choices", choices ?? new List<ManagementEvent.Choice>());
                return e;
            }

            ManagementEvent.Choice Choice(string label, string result, params Effect[] effects) =>
                new ManagementEvent.Choice { label = label, result = result, effects = new List<Effect>(effects) };

            List<ManagementEvent> events = new List<ManagementEvent>
            {
                Event("Rats", "Rats in the Granary", "Rats got into the grain store.", 1f, 1, false, new List<Effect> { E(food, -10) }),
                Event("Good Harvest", "Good Harvest", "The weather was kind this year.", 1f, 1, false, new List<Effect> { E(food, 15) }),
                Event("Storm", "Storm", "A storm blew down a woodpile.", 1f, 1, false, new List<Effect> { E(wood, -5) }),
                Event("Plague", "Plague", "Sickness spread through the village.", 0.5f, 4, true, new List<Effect> { E(people, -3) }),
                Event("Merchant", "A Merchant Arrives", "A merchant offers to trade.", 1.5f, 2, false, null, new List<ManagementEvent.Choice>
                {
                    Choice("Buy grain", "", E(gold, -15), E(food, 20)),
                    Choice("Sell wood", "", E(wood, -8), E(gold, 12)),
                    Choice("Send him away", ""),
                }),
                Event("Festival", "Festival", "The people ask for a harvest festival.", 1f, 3, false, null, new List<ManagementEvent.Choice>
                {
                    Choice("Hold the festival", "", E(gold, -10), E(people, 2)),
                    Choice("Not this year", "The people grumble."),
                }),
            };

            // Part 5 — Village Systems
            GameObject root = new GameObject("Village Systems");
            Systems systems = b.StandardSystems(root, MenuScene);

            GameObject cycleObject = Child(root, "Turn Cycle");
            TurnCycle cycle = cycleObject.AddComponent<TurnCycle>();
            Set(cycle, "turnNumber", year);
            Set(cycle, "turnsToWin", 10);
            Set(cycle, "secondsPerTurn", 0f);
            Set(cycle, "rules", rules);
            Set(cycle, "resources", new List<IntVariable> { people, food, gold, wood });
            Set(cycle, "noNegativeResources", true);
            Set(cycle, "events", events);
            Set(cycle, "eventChance", 0.5f);
            Set(cycle, "report", report);
            Set(cycle, "raiseOnWin", won);

            GameObject lose = Child(root, "Lose");
            Condition(lose, people, "AtMost", 0, lost);
            Listen(lose, lost, Call(cycle.StopGame));

            GameObject screen = Child(root, "Screen");
            b.Document(screen, KitFolder + "/UI/ManagementHUD.uxml", 0);
            Set(screen.AddComponent<UIManagementScreen>(), "turnCycle", cycle);
            UIBuildGrid grid = screen.AddComponent<UIBuildGrid>();
            Set(grid, "buildings", buildings);
            Set(grid, "plots", 12);
            Set(grid, "plotsPerRow", 4);
            Set(grid, "startingBuildings", new List<BuildingData>());
            Set(grid, "watchResources", new List<IntVariable> { people, food, gold, wood });
            Bind(screen,
                ("turn-label", year, "Year {0}"),
                ("resource-1-label", people, "People: {0}"),
                ("resource-2-label", food, "Food: {0}"),
                ("resource-3-label", gold, "Gold: {0}"),
                ("resource-4-label", wood, "Wood: {0}"));

            VariableBase[] all = { people, food, gold, wood, year };
            Resetter(Child(root, "Reset"), true, all);

            b.EndScreen(systems, "Won", true, won, new (string, VariableBase, string)[] { ("score-label", people, "Your village has {0} people") }, all, "Ten Good Years");
            b.EndScreen(systems, "Lost", false, lost, new (string, VariableBase, string)[] { ("score-label", year, "The village lasted until year {0}") }, all, "The Village Is Empty");

            GameObject systemsPrefab = b.Prefab(root);

            // Part 6 — Scenes
            b.MenuScene(MenuScene, "Ten Years", "Lead a village through ten years: feed your people, build wisely, and weather what comes.",
                GameScene, all, new Color(0.24f, 0.32f, 0.22f),
                "Keep your village alive for ten years.\n\n" +
                "Every year, each person eats 1 Food. If the Food runs out, people leave. When there's plenty of Food (30 or more), the village grows. Every 3 people pay 1 Gold in taxes.\n\n" +
                "Drag a building card onto an empty plot to build it, if you can afford it. Farms make Food, Lumber Camps make Wood, Houses bring people, and Markets make Gold. Drag a building back onto the cards to remove it.\n\n" +
                "Click End Turn to end the year, and read the report to see what changed. Sometimes something happens, and some events ask you to choose.\n\n" +
                "If no one is left, the game is over.");

            Scene game = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, new Color(0.24f, 0.32f, 0.22f));
            Place(systemsPrefab, Vector3.zero);
            b.SaveScene(game, GameScene);
        }
    }
}
