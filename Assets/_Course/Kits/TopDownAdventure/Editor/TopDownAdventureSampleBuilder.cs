using CollinGame.Core;
using CollinGame.EditorTools;
using CollinGame.TwoD;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.TopDownAdventure.EditorTools
{
    /// <summary>
    /// Builds the Top-Down Adventure Kit's sample: a three-room dungeon with a sword,
    /// slimes, a bat, a key, a locked door and a treasure chest, plus the menu and win
    /// scenes, following "Top-Down Adventure Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Top-Down Adventure Sample.
    /// </summary>
    public static class TopDownAdventureSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/TopDownAdventure";
        const string Dungeon = "kenney_tiny-dungeon";
        const string Town = "kenney_tiny-town";
        const string MenuName = "Adventure_Menu";
        const string DungeonName = "Adventure_Dungeon";
        const string WinName = "Adventure_Win";
        const int RoomWidth = 16;
        const int RoomHeight = 9;
        const int DoorRow = 4;

        static readonly Color Backdrop = new Color(0.12f, 0.1f, 0.14f);

        const string HowToPlay =
            "Explore three rooms and find the treasure.\n\n" +
            "WASD or the arrow keys move. Enter (or the left mouse button) swings your sword. E reads signs and opens chests.\n\n" +
            "Slimes and bats cost a heart when they touch you. The red potion heals. Lose every heart and you go back to the last checkpoint.\n\n" +
            "The last room is locked: find the key first. Esc pauses.";

        [MenuItem("Collin Game/Kit Samples/Build Top-Down Adventure Sample")]
        static void Build()
        {
            Run("Top-Down Adventure", KitFolder, new[] { "Data", "Prefabs", "Scenes", "Tiles" },
                new[] { Kenney + Dungeon, Kenney + Town }, BuildSample);
        }

        static Sprite D(int tile) => KenneySprite(Dungeon, $"Tiles/tile_{tile:0000}");
        static Sprite T(int tile) => KenneySprite(Town, $"Tiles/tile_{tile:0000}");

        sealed class Kit
        {
            public IntVariable Health, Keys, Coins;
            public StringVariable Message;
            public VoidEventChannel Cleared;
            public GameObject Player, Slime, Bat, Coin, Heart, Key, Door, Sign, Chest, Checkpoint, Systems;
        }

        static void BuildSample(SampleBuild b)
        {
            EnsureTag("Enemy");

            // Part 1 — Data
            Kit k = new Kit
            {
                Health = b.Int("PlayerHealth", 5),
                Keys = b.Int("Keys"),
                Coins = b.Int("Coins"),
                Message = b.Text("Message"),
                Cleared = b.Channel("DungeonCleared")
            };

            // Part 2 — Prefabs
            BuildPlayer(b, k);
            BuildEnemies(b, k);

            k.Coin = Pickup(b, "Coin", T(93), k.Coins, 0);
            k.Heart = Pickup(b, "Heart", D(115), null, 1);
            k.Key = Pickup(b, "Key", T(117), k.Keys, 0);

            GameObject door = SpriteObject("Locked Door", D(45), 2);
            door.AddComponent<BoxCollider2D>();
            KeyDoor2D lockScript = door.AddComponent<KeyDoor2D>();
            Set(lockScript, "keys", k.Keys);
            Set(lockScript, "keysNeeded", 1);
            Set(lockScript, "useUpKeys", true);
            Set(lockScript, "messageText", k.Message);
            k.Door = b.Prefab(door);

            k.Sign = Readable(b, k, "Sign", T(83), false, "Swing your sword with Enter.", "Find the key to open the door.");
            GameObject chest = Readable(b, k, "Treasure Chest", D(89), true, "You found the treasure!");
            k.Chest = chest;

            GameObject checkpoint = SpriteObject("Checkpoint", T(43), 1);
            TriggerBox(checkpoint, new Vector2(1f, 1f), Vector2.zero);
            Set(checkpoint.AddComponent<Checkpoint>(), "indicator", checkpoint.GetComponent<SpriteRenderer>());
            k.Checkpoint = b.Prefab(checkpoint);

            k.Systems = LevelSystems(b, k);

            // Part 4 — Menu first, so it's first in the Scene List
            b.MenuScene(MenuName, "Three Rooms", "Fight your way through a small dungeon to find the treasure.", DungeonName,
                new VariableBase[] { k.Keys, k.Coins, k.Message }, Backdrop, HowToPlay);

            BuildDungeon(b, k);

            b.WinScene(WinName, "Treasure Found!", "", DungeonName, MenuName, new VariableBase[] { k.Keys, k.Coins, k.Message }, Backdrop,
                ("score-label", k.Coins, "Coins: {0}"));
        }

        static void BuildPlayer(SampleBuild b, Kit k)
        {
            GameObject player = SpriteObject("Player", D(98), 10);
            player.tag = "Player";
            CircleCollider2D feet = player.AddComponent<CircleCollider2D>();
            feet.radius = 0.35f;
            feet.offset = new Vector2(0f, -0.1f);

            TopDownController2D controller = player.AddComponent<TopDownController2D>();
            Set(controller, "input", b.Input);
            Set(controller, "maxSpeed", 5f);
            SetEnum(controller, "facing", "FlipSprite");
            Set(controller, "spriteToFlip", player.GetComponent<SpriteRenderer>());

            Health health = player.AddComponent<Health>();
            Set(health, "maxHealth", 5);
            Set(health, "invulnerableTime", 1f);
            Set(health, "healthVariable", k.Health);
            SetEnum(health, "whenHealthRunsOut", "Nothing");
            player.AddComponent<Respawner>();
            player.AddComponent<Knockback2D>();

            GameObject swing = SpriteObject("Swing", D(104), 11);
            swing.transform.SetParent(player.transform, false);
            swing.transform.localPosition = new Vector3(0.7f, 0f, 0f);
            swing.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

            MeleeAttack2D sword = player.AddComponent<MeleeAttack2D>();
            Set(sword, "input", b.Input);
            Set(sword, "reach", 0.8f);
            Set(sword, "radius", 0.55f);
            Set(sword, "targetTag", "Enemy");
            Set(sword, "damage", 1);
            Set(sword, "knockbackForce", 8f);
            Set(sword, "swingVisual", swing);
            k.Player = b.Prefab(player);
        }

        static void BuildEnemies(SampleBuild b, Kit k)
        {
            GameObject slime = SpriteObject("Slime", D(108), 5);
            slime.tag = "Enemy";
            slime.AddComponent<CircleCollider2D>().radius = 0.4f;
            TopDownChaser2D chaser = slime.AddComponent<TopDownChaser2D>();
            Set(chaser, "wanderSpeed", 0.8f);
            Set(chaser, "wanderRadius", 1.5f);
            Set(chaser, "noticeRange", 4f);
            Set(chaser, "giveUpRange", 7f);
            Set(chaser, "chaseSpeed", 2f);
            Set(chaser, "needsLineOfSight", true);
            Health health = slime.AddComponent<Health>();
            Set(health, "maxHealth", 2);
            Set(health, "invulnerableTime", 0.3f);
            SetEnum(health, "whenHealthRunsOut", "Destroy");
            slime.AddComponent<Knockback2D>();
            DamageOnTouch damage = slime.AddComponent<DamageOnTouch>();
            Set(damage, "targetTag", "Player");
            Set(damage, "damage", 1);
            k.Slime = b.Prefab(slime);

            // The bat: a Prefab Variant of the slime.
            GameObject bat = (GameObject)PrefabUtility.InstantiatePrefab(k.Slime);
            SpriteRenderer batLook = bat.GetComponent<SpriteRenderer>();
            batLook.sprite = D(120);
            TopDownChaser2D batChaser = bat.GetComponent<TopDownChaser2D>();
            Set(batChaser, "chaseSpeed", 3.5f);
            Set(batChaser, "wanderSpeed", 2f);
            Set(batChaser, "needsLineOfSight", false);
            Health batHealth = bat.GetComponent<Health>();
            Set(batHealth, "maxHealth", 1);
            KeepChanges(batLook);
            KeepChanges(batChaser);
            KeepChanges(batHealth);
            k.Bat = PrefabUtility.SaveAsPrefabAsset(bat, $"{b.Folder("Prefabs")}/Bat.prefab");
            Object.DestroyImmediate(bat);
        }

        static GameObject Pickup(SampleBuild b, string pickupName, Sprite sprite, IntVariable addTo, int heal)
        {
            GameObject pickup = SpriteObject(pickupName, sprite, 3);
            TriggerCircle(pickup, 0.4f);
            Collectible collectible = pickup.AddComponent<Collectible>();
            Set(collectible, "addTo", addTo);
            Set(collectible, "amount", addTo != null ? 1 : 0);
            Set(collectible, "heal", heal);
            return b.Prefab(pickup);
        }

        /// <summary>A sign or chest: solid in the middle, with a bigger trigger around it to stand in and press E.</summary>
        static GameObject Readable(SampleBuild b, Kit k, string objectName, Sprite sprite, bool onlyOnce, params string[] lines)
        {
            GameObject go = SpriteObject(objectName, sprite, 2);
            TriggerBox(go, new Vector2(2f, 2f), Vector2.zero);
            Child(go, "Solid").AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);

            Interactable2D interactable = go.AddComponent<Interactable2D>();
            Set(interactable, "input", b.Input);
            Set(interactable, "messageText", k.Message);
            Set(interactable, "lines", lines);
            Set(interactable, "onlyOnce", onlyOnce);

            if (onlyOnce)
            {
                On(interactable, "onUsed", Call(k.Cleared.Raise));
            }

            return b.Prefab(go);
        }

        static GameObject LevelSystems(SampleBuild b, Kit k)
        {
            GameObject root = new GameObject("Level Systems");

            GameObject hud = Child(root, "HUD");
            b.Document(hud, KitFolder + "/UI/AdventureHUD.uxml", 0);
            UIHeartRow hearts = hud.AddComponent<UIHeartRow>();
            Set(hearts, "health", k.Health);
            Set(hearts, "maxHearts", 5);
            Bind(hud, ("keys-label", k.Keys, "Keys: {0}"), ("coins-label", k.Coins, "Coins: {0}"));
            UIMessageBox messages = hud.AddComponent<UIMessageBox>();
            Set(messages, "message", k.Message);
            Set(messages, "clearAfter", 4f);

            b.StandardSystems(root, MenuName);

            GameObject win = Child(root, "Win");
            SceneChanger toWin = win.AddComponent<SceneChanger>();
            Set(toWin, "sceneName", WinName);
            Listen(win, k.Cleared, Call(toWin.Load));

            Resetter(Child(root, "Reset Data"), true, k.Keys, k.Coins, k.Message);
            return b.Prefab(root);
        }

        // ---------------------------------------------------------------- Part 3 — The dungeon

        static void BuildDungeon(SampleBuild b, Kit k)
        {
            Scene scene = NewScene();
            Camera camera = Camera2D(new Vector3(RoomWidth / 2f, RoomHeight / 2f, -10f), 4.5f, Backdrop);
            camera.gameObject.AddComponent<CinemachineBrain>();
            GlobalLight2D();

            // Floors and walls: three rooms side by side, with a one-tile doorway between each.
            GameObject grid = GridObject();
            Tilemap floor = TilemapObject(grid, "Floor", 0);
            Tilemap walls = TilemapObject(grid, "Walls", 1, true);
            Tile floorTile = b.TileFrom(D(48), "Floor");
            Tile wallTile = b.TileFrom(D(40), "Wall", true);

            for (int room = 0; room < 3; room++)
            {
                int left = room * RoomWidth;

                for (int x = left; x < left + RoomWidth; x++)
                {
                    for (int y = 0; y < RoomHeight; y++)
                    {
                        bool edge = x == left || x == left + RoomWidth - 1 || y == 0 || y == RoomHeight - 1;
                        bool doorway = y == DoorRow && ((x == left && room > 0) || (x == left + RoomWidth - 1 && room < 2));
                        Vector3Int cell = new Vector3Int(x, y, 0);
                        floor.SetTile(cell, floorTile);

                        if (edge && !doorway)
                        {
                            walls.SetTile(cell, wallTile);
                        }
                    }
                }
            }

            // Rooms 1 to 3
            GameObject[] rooms = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                rooms[i] = new GameObject($"Room {i + 1}");
                rooms[i].transform.position = new Vector3(i * RoomWidth + RoomWidth / 2f, RoomHeight / 2f, 0f);
                TriggerBox(rooms[i], new Vector2(RoomWidth, RoomHeight), Vector2.zero);
                rooms[i].AddComponent<CameraRoom2D>();
            }

            Vector3 At(int room, float x, float y) => new Vector3(room * RoomWidth + x + 0.5f, y + 0.5f, 0f);

            // 1 — Entrance
            GameObject player = Place(k.Player, At(0, 3, DoorRow));
            Place(k.Sign, At(0, 6, 6));
            Place(k.Coin, At(0, 9, 2));
            Place(k.Coin, At(0, 11, 6));
            Place(k.Checkpoint, At(0, 2, DoorRow));

            // 2 — Slime room
            GameObject contents2 = new GameObject("Room Contents");
            contents2.transform.SetParent(rooms[1].transform, true);
            Place(k.Slime, At(1, 6, 6), contents2.transform);
            Place(k.Slime, At(1, 10, 2), contents2.transform);
            Place(k.Key, At(1, 12, 6));
            Place(k.Heart, At(1, 4, 2));
            Place(k.Checkpoint, At(1, 1, DoorRow));
            Set(rooms[1].GetComponent<CameraRoom2D>(), "roomContents", contents2);

            // 3 — Treasure room
            Place(k.Door, At(2, 0, DoorRow));
            GameObject contents3 = new GameObject("Room Contents");
            contents3.transform.SetParent(rooms[2].transform, true);
            Place(k.Bat, At(2, 8, 6), contents3.transform);
            Place(k.Chest, At(2, 13, DoorRow));
            Set(rooms[2].GetComponent<CameraRoom2D>(), "roomContents", contents3);

            // Cameras: Cinemachine follows the player, kept inside the room they're in.
            GameObject cameraObject = new GameObject("Player Camera");
            CinemachineCamera follow = cameraObject.AddComponent<CinemachineCamera>();
            follow.Follow = player.transform;
            LensSettings lens = follow.Lens;
            lens.OrthographicSize = 4.5f;
            follow.Lens = lens;
            cameraObject.AddComponent<CinemachinePositionComposer>();
            cameraObject.AddComponent<CinemachineConfiner2D>();

            Place(k.Systems, Vector3.zero);
            b.SaveScene(scene, DungeonName);
        }
    }
}
