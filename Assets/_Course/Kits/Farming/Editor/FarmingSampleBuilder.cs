using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.EditorTools;
using CollinGame.TwoD;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.Farming.EditorTools
{
    /// <summary>
    /// Builds the Farming Kit's sample, a two-week farm, following
    /// "Farming Kit — Building the Sample". The farm is drawn as the text map below.
    /// Menu: Collin Game → Kit Samples → Build Farming Sample.
    /// </summary>
    public static class FarmingSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/Farming";
        const string TownPack = "kenney_tiny-town";
        const string Dungeon = "kenney_tiny-dungeon";
        const string MenuScene = "Farm_Menu";
        const string GameScene = "Farm_Game";

        // .  grass   d  dirt path   F  farmland (the field)   f  fence
        // B  bed     S  shop sign    P  player
        static readonly string[] Map =
        {
            "ffffffffffffffffffffffff",
            "f......................f",
            "f..ffffff..............f",
            "f..fFFFFf.......S......f",
            "f..fFFFFf.......d......f",
            "f..fFFFFf.......d......f",
            "f..fFFFFf.......d......f",
            "f..ff.fff.......d......f",
            "f.....dddddddddddd.....f",
            "f.....d................f",
            "f.....d.....B..........f",
            "f.....dddddddP.........f",
            "f......................f",
            "ffffffffffffffffffffffff",
        };

        [MenuItem("Collin Game/Kit Samples/Build Farming Sample")]
        static void Build()
        {
            Run("Farming", KitFolder, new[] { "Data", "Items", "Crops", "Tiles", "Prefabs", "Scenes" },
                new[] { Kenney + TownPack, Kenney + Dungeon }, BuildSample);
        }

        static Sprite Town(string tile) => KenneySprite(TownPack, "Tiles/" + tile);

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — Data
            IntVariable money = b.Int("Money", 60);
            IntVariable day = b.Int("Day", 1);
            IntVariable energy = b.Int("Energy", 30);
            StringVariable clock = b.Text("Clock", "6:00 AM");
            VoidEventChannel won = b.Channel("FarmWon");
            VoidEventChannel lost = b.Channel("FarmLost");

            // Part 2 — Soil tiles
            Tile tilled = b.TileFrom(Town("tile_0025"), "Tilled Soil");
            ColorUtility.TryParseHtmlString("#A68070", out Color wetColor);
            Tile watered = b.TileFrom(Town("tile_0025"), "Watered Soil", false, wetColor);

            // Part 3 — Items and crops
            ItemData Item(string itemName, string icon, ItemUse use, bool usedUp, int buy, int sell, string description)
            {
                ItemData item = b.Asset<ItemData>("Items", itemName);
                Set(item, "displayName", itemName);
                Set(item, "icon", Town(icon));
                Set(item, "description", description);
                Set(item, "use", use);
                Set(item, "usedUp", usedUp);
                Set(item, "buyPrice", buy);
                Set(item, "sellPrice", sell);
                return item;
            }

            ItemData hoe = Item("Hoe", "tile_0127", ItemUse.Till, false, 0, 0, "Turns grass in the field into soil.");
            ItemData can = Item("Watering Can", "tile_0131", ItemUse.Water, false, 0, 0, "Waters soil, so crops grow overnight.");
            ItemData shovel = Item("Shovel", "tile_0128", ItemUse.Clear, false, 0, 0, "Clears a crop and its soil.");
            ItemData spores = Item("Mushroom Spores", "tile_0017", ItemUse.Plant, true, 10, 0, "Quick and safe. Ready in 2 days.");
            ItemData berrySeeds = Item("Berry Seeds", "tile_0017", ItemUse.Plant, true, 20, 0, "Ready in 4 days, then every 2 days.");
            ItemData goldSeeds = Item("Goldleaf Seeds", "tile_0017", ItemUse.Plant, true, 60, 0, "Slow and valuable. Dies after 2 dry days.");
            ItemData mushroom = Item("Mushroom", "tile_0029", ItemUse.Nothing, false, 0, 20, "Sells for $20.");
            ItemData berries = Item("Berries", "tile_0005", ItemUse.Nothing, false, 0, 15, "Sells for $15.");
            ItemData goldleaf = Item("Goldleaf", "tile_0015", ItemUse.Nothing, false, 0, 150, "Sells for $150.");

            CropData Crop(string cropName, string[] stages, int daysPerStage, ItemData produce, int regrow, int diesDry)
            {
                CropData crop = b.Asset<CropData>("Crops", cropName);
                Set(crop, "displayName", cropName);
                Sprite[] sprites = new Sprite[stages.Length];

                for (int i = 0; i < stages.Length; i++)
                {
                    sprites[i] = Town(stages[i]);
                }

                Set(crop, "stages", sprites);
                Set(crop, "daysPerStage", daysPerStage);
                Set(crop, "produce", produce);
                Set(crop, "amount", 1);
                Set(crop, "regrowDays", regrow);
                Set(crop, "diesAfterDryDays", diesDry);
                return crop;
            }

            Set(spores, "crop", Crop("Mushrooms", new[] { "tile_0017", "tile_0029" }, 2, mushroom, 0, 0));
            Set(berrySeeds, "crop", Crop("Berry Bush", new[] { "tile_0017", "tile_0005" }, 4, berries, 2, 0));
            Set(goldSeeds, "crop", Crop("Goldleaf", new[] { "tile_0017", "tile_0016", "tile_0015" }, 3, goldleaf, 0, 2));

            // Part 4 — The player
            GameObject player = SpriteObject("Player", KenneySprite(Dungeon, "Tiles/tile_0098"), 20);
            player.tag = "Player";
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            CapsuleCollider2D feet = player.AddComponent<CapsuleCollider2D>();
            feet.size = new Vector2(0.6f, 0.5f);
            feet.offset = new Vector2(0f, -0.2f);
            feet.direction = CapsuleDirection2D.Horizontal;
            TopDownController2D mover = player.AddComponent<TopDownController2D>();
            Set(mover, "input", b.Input);
            Set(mover, "maxSpeed", 4f);
            Set(mover, "facing", TopDownController2D.FacingMode.FlipSprite);
            Set(mover, "spriteToFlip", player.GetComponent<SpriteRenderer>());
            Inventory inventory = player.AddComponent<Inventory>();
            Set(inventory, "size", 8);
            Set(inventory, "startingItems", new List<Inventory.Slot>
            {
                new Inventory.Slot { item = hoe, count = 1 },
                new Inventory.Slot { item = can, count = 1 },
                new Inventory.Slot { item = shovel, count = 1 },
                new Inventory.Slot { item = spores, count = 5 },
            });
            FarmerTools2D tools = player.AddComponent<FarmerTools2D>();
            Set(tools, "input", b.Input);
            Set(tools, "reach", 1.6f);
            Set(tools, "feetOffset", new Vector2(0f, -0.3f));
            Set(tools, "energy", energy);
            Set(tools, "energyPerUse", 1);
            GameObject playerPrefab = b.Prefab(player);

            // Part 5 — Farm Systems
            GameObject root = new GameObject("Farm Systems");
            Systems systems = b.StandardSystems(root, MenuScene);

            DayCycle cycle = Child(root, "Day Cycle").AddComponent<DayCycle>();
            Set(cycle, "day", day);
            Set(cycle, "lastDay", 14);
            Set(cycle, "secondsPerDay", 240f);
            Set(cycle, "startHour", 6);
            Set(cycle, "endHour", 26);
            Set(cycle, "clock", clock);
            Set(cycle, "resetEachMorning", new VariableBase[] { energy });
            Set(cycle, "raiseWhenTimeUp", lost);

            GameObject win = Child(root, "Win");
            Condition(win, money, "AtLeast", 800, won);
            Listen(win, won, Call(cycle.StopGame));

            GameObject screen = Child(root, "Screen");
            b.Document(screen, KitFolder + "/UI/FarmHUD.uxml", 0);
            Set(screen.AddComponent<UIFarmScreen>(), "dayCycle", cycle);
            UIShop shop = screen.AddComponent<UIShop>();
            Set(shop, "shopName", "General Store");
            Set(shop, "forSale", new List<ItemData> { spores, berrySeeds, goldSeeds });
            Set(shop, "money", money);
            Set(shop, "input", b.Input);
            Bind(screen,
                ("day-label", day, "Day {0} of 14"),
                ("clock-label", clock, "{0}"),
                ("money-label", money, "${0} / $800"),
                ("energy-label", energy, "Energy: {0}"));

            VariableBase[] all = { money, day, energy, clock };
            Resetter(Child(root, "Reset"), true, all);

            b.EndScreen(systems, "Won", true, won, new (string, VariableBase, string)[] { ("score-label", day, "You made it on day {0}!") }, all, "A Good Harvest");
            b.EndScreen(systems, "Lost", false, lost, null, all, "Out of Time", "Two weeks are up.");

            GameObject systemsPrefab = b.Prefab(root);

            // Part 7 — Menu (first, for the Scene List)
            Color green = new Color(0.36f, 0.6f, 0.3f);
            b.MenuScene(MenuScene, "Two Weeks", "Till, plant, water and sell: earn $800 in fourteen days.", GameScene, all, green,
                "Earn $800 by the end of day 14.\n\n" +
                "Move with WASD or the arrow keys. Choose a tool with 1–8, the mouse wheel, or by clicking the bar at the bottom. Left click (or Enter) uses it on the ground in front of you, or the square you click nearby.\n\n" +
                "Till the grass in the field with the hoe, plant seeds, and water them. Watered crops grow overnight; the soil dries every morning. Click a ripe crop to harvest it.\n\n" +
                "Each tool use costs energy. Sleep in the bed (E) to end the day and get your energy back. At 2 AM you fall asleep wherever you are.\n\n" +
                "Sell crops and buy seeds at the shop (E). Mushrooms are quick, berry bushes grow back, and Goldleaf is slow but worth a lot.");

            // Part 6 — The farm
            BuildFarm(b, systemsPrefab, playerPrefab, tilled, watered);
        }

        static void BuildFarm(SampleBuild b, GameObject systemsPrefab, GameObject playerPrefab, Tile tilled, Tile watered)
        {
            Scene scene = NewScene();
            int height = Map.Length;
            int width = Map[0].Length;
            Camera camera = Camera2D(new Vector3(width / 2f, height / 2f - 0.5f, -10f), height / 2f + 0.5f, new Color(0.36f, 0.6f, 0.3f));
            GlobalLight2D();

            Tile grass = b.TileFrom(Town("tile_0000"), "Grass");
            Tile path = b.TileFrom(Town("tile_0040"), "Path");
            Tile fence = b.TileFrom(Town("tile_0045"), "Fence", true);
            Tile field = b.TileFrom(Town("tile_0000"), "Field Grass");

            GameObject grid = GridObject();
            Tilemap ground = TilemapObject(grid, "Ground", 0);
            Tilemap farmland = TilemapObject(grid, "Farmland", 1);
            Tilemap soil = TilemapObject(grid, "Soil", 2);
            Tilemap walls = TilemapObject(grid, "Walls", 3, true);

            FarmField farm = grid.AddComponent<FarmField>();
            Set(farm, "farmland", farmland);
            Set(farm, "soil", soil);
            Set(farm, "tilledTile", tilled);
            Set(farm, "wateredTile", watered);
            Set(farm, "cropOrder", 5);
            Set(farm, "emptySoilLasts", 3);
            Set(farm, "rainChance", 0.15f);

            Vector3 playerSpot = Vector3.zero;
            Vector3 bedSpot = Vector3.zero;
            Vector3 shopSpot = Vector3.zero;

            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = Map[row][x];
                    int y = height - 1 - row;
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    Vector3 center = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    ground.SetTile(cell, c == 'd' ? path : grass);

                    switch (c)
                    {
                        case 'F': farmland.SetTile(cell, field); break;
                        case 'f': walls.SetTile(cell, fence); break;
                        case 'P': playerSpot = center; break;
                        case 'B': bedSpot = center; break;
                        case 'S': shopSpot = center; break;
                    }
                }
            }

            GameObject systems = Place(systemsPrefab, Vector3.zero);
            DayCycle cycle = systems.GetComponentInChildren<DayCycle>();
            UIShop shop = systems.GetComponentInChildren<UIShop>();
            UIFarmScreen farmScreen = systems.GetComponentInChildren<UIFarmScreen>();

            GameObject player = Place(playerPrefab, playerSpot + new Vector3(0f, 0.2f, 0f));
            Inventory inventory = player.GetComponent<Inventory>();

            // Bed, and where the player wakes up.
            GameObject bed = SpriteObject("Bed", Town("tile_0104"), 3);
            bed.transform.position = bedSpot;
            Transform wakeUp = Child(bed, "Wake Up Point").transform;
            wakeUp.localPosition = new Vector3(0f, -1f, 0f);
            TriggerBox(bed, new Vector2(1.6f, 1.6f), Vector2.zero);
            InteractZone2D bedZone = bed.AddComponent<InteractZone2D>();
            Set(bedZone, "input", b.Input);
            Set(bedZone, "prompt", "E: Sleep");
            On(bedZone, "onUsed", Call(cycle.Sleep));

            // Shop sign, with the counter area in front of it.
            GameObject sign = SpriteObject("Shop", Town("tile_0083"), 3);
            sign.transform.position = shopSpot;
            TriggerBox(sign, new Vector2(2f, 2f), new Vector2(0f, -0.8f));
            InteractZone2D shopZone = sign.AddComponent<InteractZone2D>();
            Set(shopZone, "input", b.Input);
            Set(shopZone, "prompt", "E: Shop");
            On(shopZone, "onUsed", Call(shop.Open));

            // The cursor that shows which square the tools will work on.
            GameObject cursor = SpriteObject("Cursor", b.WhiteSquare(), 15);
            cursor.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.35f);
            FarmerTools2D tools = player.GetComponent<FarmerTools2D>();
            Set(tools, "cursor", cursor.transform);
            KeepChanges(tools);

            // Night falls: a see-through square in front of everything, tinted through the day.
            GameObject night = SpriteObject("Night", b.WhiteSquare(), 100);
            night.transform.SetParent(camera.transform, false);
            night.transform.localPosition = new Vector3(0f, 0f, 1f);
            night.transform.localScale = new Vector3(40f, 25f, 1f);
            night.GetComponent<SpriteRenderer>().color = Color.clear;

            Set(cycle, "player", player.transform);
            Set(cycle, "wakeUpPoint", wakeUp);
            Set(cycle, "overlay", night.GetComponent<SpriteRenderer>());
            KeepChanges(cycle);
            Set(shop, "inventory", inventory);
            KeepChanges(shop);
            Set(farmScreen, "inventory", inventory);
            KeepChanges(farmScreen);

            b.SaveScene(scene, GameScene);
        }
    }
}
