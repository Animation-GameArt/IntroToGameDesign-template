using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CollinGame.Core;
using CollinGame.EditorTools;
using CollinGame.TwoD;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CollinGame.Kits.Platformer.EditorTools
{
    /// <summary>
    /// Builds the Platformer Kit's sample game in one step: data, prefabs, tiles,
    /// two levels, a menu and a win screen, and the Scene List. It follows
    /// "Platformer Kit — Building the Sample" on the course site, so the result
    /// can be checked against that guide.
    ///
    /// Menu: Collin Game → Kit Samples → Build Platformer Sample.
    /// </summary>
    public static class PlatformerSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/Platformer";
        const string SampleFolder = KitFolder + "/Sample";
        const string DataFolder = SampleFolder + "/Data";
        const string PrefabFolder = SampleFolder + "/Prefabs";
        const string SceneFolder = SampleFolder + "/Scenes";
        const string TileFolder = SampleFolder + "/Tiles";

        const string ArtFolder = "Assets/_Course/Art/Kenney/kenney_pixel-platformer/Tiles";
        const string CharacterFolder = ArtFolder + "/Characters";

        const string InputReaderPath = "Assets/_Course/Core/Input/InputReader.asset";
        const string CreditsPath = "Assets/_Course/Credits/Course Credits.txt";
        const string CoreUI = "Assets/_Course/Core/UI/";
        const string KitUI = KitFolder + "/UI/";

        const string MenuScene = "Platformer_Menu";
        const string Level1Scene = "Platformer_Level1";
        const string Level2Scene = "Platformer_Level2";
        const string WinScene = "Platformer_Win";

        static readonly Color SkyColor = new Color(0.68f, 0.85f, 0.95f);

        // The levels, drawn with characters. Each character is one tile; the bottom row is y = 0.
        //   #  ground (surface or dirt, picked automatically)   =  floating ledge   B  block
        //   P  player   c  coin   h  heart   k  key   G  goal gem   K  checkpoint
        //   W  walker   F  flyer  M  moving platform (3 wide, moves 4 right)
        //   S  spring   o  one-way platform   L  lock   ^  spikes
        static readonly string[] Level1Map =
        {
            "B                                                                                                B",
            "B                                                                 B                              B",
            "B                                                                 B                              B",
            "B                                                                 B                              B",
            "B                                                    k            B                              B",
            "B                                                  =====          B                              B",
            "B                                                                 B                              B",
            "B              cc                                       oo        B                              B",
            "B                                                                 B            F                 B",
            "B                                                         oo      L                              B",
            "B  P   c c c       B    W    B    K              S                L   h   ^ ^               G    B",
            "B##############  #################### M       ##################################    #############B",
            "B##############  ####################         ##################################    #############B",
            "B##############  ####################         ##################################    #############B",
        };

        static readonly string[] Level2Map =
        {
            "B                                                              B",
            "B                                                              B",
            "B                                                              B",
            "B                                                              B",
            "B                                                              B",
            "B                                                              B",
            "B                                    F                         B",
            "B               c   c   c   c                                  B",
            "B                                                              B",
            "B              oo  oo  oo  oo                                  B",
            "B P  c c c    ^^^^^^^^^^^^^^^^   K             h  W  B  W   G  B",
            "B################################### M       ##################B",
            "B###################################         ##################B",
            "B###################################         ##################B",
        };

        // Everything the builder creates, gathered as it goes.
        class Assets
        {
            public IntVariable coins, lives, playerHealth;
            public VoidEventChannel levelCompleted, gameOver, keyCollected;
            public InputReader input;
            public PanelSettings panel;
            public readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
            public readonly Dictionary<string, TileBase> tiles = new Dictionary<string, TileBase>();
        }

        [MenuItem("Collin Game/Kit Samples/Build Platformer Sample")]
        static void Build()
        {
            if (!EditorUtility.DisplayDialog("Build Platformer Sample",
                    "This creates the Platformer Kit's sample game in " + SampleFolder + ": data, prefabs, tiles, four scenes, and their place in the Scene List.\n\n" +
                    (AssetDatabase.IsValidFolder(SampleFolder) ? "The existing Sample folder will be replaced.\n\n" : "") +
                    "Continue?", "Build", "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                if (!AssetDatabase.IsValidFolder(ArtFolder))
                {
                    throw new BuildProblem($"The art pack isn't at {ArtFolder}. Pull the template project, which includes it.");
                }

                // Start from an empty scene, so no open scene is inside the folder being replaced.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateFolders();

                Assets a = new Assets();
                LoadShared(a);
                CreateData(a);
                CreateTiles(a);

                // Prefabs are built in a scratch scene, then that scene becomes Level 1.
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreatePrefabs(a);
                BuildLevel(a, scene, Level1Map, Level1Scene);

                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuildLevel(a, scene, Level2Map, Level2Scene);

                BuildMenu(a);
                BuildWin(a);
                AddToSceneList();

                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(ScenePath(MenuScene));

                Debug.Log("Platformer sample built in " + SampleFolder + ". Press Play to start from the menu. " +
                          "Check it against Part 10 of \"Platformer Kit — Building the Sample\".");
                EditorUtility.DisplayDialog("Build Platformer Sample",
                    "Done. The menu scene is open: press Play.\n\nThe four scenes are at the end of the Scene List.", "OK");
            }
            catch (BuildProblem problem)
            {
                Debug.LogError("Platformer sample: " + problem.Message);
                EditorUtility.DisplayDialog("Build Platformer Sample", problem.Message, "OK");
            }
        }

        class BuildProblem : Exception
        {
            public BuildProblem(string message) : base(message) { }
        }

        // ---------------------------------------------------------------- Setup

        static void CreateFolders()
        {
            if (AssetDatabase.IsValidFolder(SampleFolder))
            {
                AssetDatabase.DeleteAsset(SampleFolder);
            }

            AssetDatabase.CreateFolder(KitFolder, "Sample");

            foreach (string folder in new[] { "Data", "Prefabs", "Scenes", "Tiles" })
            {
                AssetDatabase.CreateFolder(SampleFolder, folder);
            }
        }

        static void LoadShared(Assets a)
        {
            a.input = Load<InputReader>(InputReaderPath);
            a.panel = CoursePanelSettingsTool.GetOrCreate();
        }

        static void CreateData(Assets a)
        {
            a.coins = CreateAsset<IntVariable>("Coins");
            a.lives = CreateAsset<IntVariable>("Lives");
            Set(a.lives, "initialValue", 3);
            a.playerHealth = CreateAsset<IntVariable>("PlayerHealth");
            a.levelCompleted = CreateAsset<VoidEventChannel>("LevelCompleted");
            a.gameOver = CreateAsset<VoidEventChannel>("GameOver");
            a.keyCollected = CreateAsset<VoidEventChannel>("KeyCollected");
        }

        static T CreateAsset<T>(string assetName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, $"{DataFolder}/{assetName}.asset");
            return asset;
        }

        // A Tile asset for every sprite directly in the art pack's Tiles folder.
        static void CreateTiles(Assets a)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { ArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (Path.GetDirectoryName(path)?.Replace('\\', '/') != ArtFolder)
                {
                    continue;
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Grid;
                string tileName = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(tile, $"{TileFolder}/{tileName}.asset");
                a.tiles[tileName] = tile;
            }

            if (a.tiles.Count == 0)
            {
                throw new BuildProblem($"No sprites were found in {ArtFolder}.");
            }
        }

        // ---------------------------------------------------------------- Prefabs

        static void CreatePrefabs(Assets a)
        {
            // Player
            GameObject player = SpriteObject("Player", Character("tile_0000"), 10);
            player.tag = "Player";
            CapsuleCollider2D body = player.AddComponent<CapsuleCollider2D>();
            body.size = new Vector2(0.8f, 1.1f);
            PlatformerController2D controller = player.AddComponent<PlatformerController2D>();
            Set(controller, "input", a.input);
            Set(controller, "spriteToFlip", player.GetComponent<SpriteRenderer>());
            Health health = player.AddComponent<Health>();
            Set(health, "maxHealth", 3);
            Set(health, "invulnerableTime", 1f);
            Set(health, "healthVariable", a.playerHealth);
            UnityEventTools.AddIntPersistentListener(GetEvent(health, "onDied"), a.lives.Add, -1);
            player.AddComponent<Respawner>();
            player.AddComponent<HealthPopups>();
            PlatformerSpriteAnimator animator = player.AddComponent<PlatformerSpriteAnimator>();
            Set(animator, "target", player.GetComponent<SpriteRenderer>());
            Set(animator, "idle", Character("tile_0000"));
            Set(animator, "walk", new[] { Character("tile_0000"), Character("tile_0001") });
            Set(animator, "inAir", Character("tile_0001"));
            SavePrefab(a, player);

            // Pickups
            GameObject coin = SpriteObject("Coin", TileSprite("tile_0151"), 3);
            TriggerCircle(coin, 0.3f);
            Collectible coinPickup = coin.AddComponent<Collectible>();
            Set(coinPickup, "addTo", a.coins);
            Set(coinPickup, "amount", 1);
            Flipbook(coin, 6f, TileSprite("tile_0151"), TileSprite("tile_0152"));
            SavePrefab(a, coin);

            GameObject heart = SpriteObject("Heart", TileSprite("tile_0044"), 3);
            TriggerCircle(heart, 0.35f);
            Collectible heartPickup = heart.AddComponent<Collectible>();
            Set(heartPickup, "heal", 1);
            SavePrefab(a, heart);

            GameObject key = SpriteObject("Key", TileSprite("tile_0027"), 3);
            key.AddComponent<BoxCollider2D>().isTrigger = true;
            Collectible keyPickup = key.AddComponent<Collectible>();
            Set(keyPickup, "raiseEvent", a.keyCollected);
            SavePrefab(a, key);

            GameObject gem = SpriteObject("Goal Gem", TileSprite("tile_0067"), 3);
            TriggerCircle(gem, 0.4f);
            TriggerZone zone = gem.AddComponent<TriggerZone>();
            Set(zone, "onlyOnce", true);
            Set(zone, "raiseOnEnter", a.levelCompleted);
            SavePrefab(a, gem);

            // Obstacles
            GameObject lockBlock = SpriteObject("Lock", TileSprite("tile_0028"), 0);
            lockBlock.AddComponent<BoxCollider2D>();
            VoidEventListener unlock = lockBlock.AddComponent<VoidEventListener>();
            Set(unlock, "channel", a.keyCollected);
            UnityEventTools.AddBoolPersistentListener(GetEvent(unlock, "response"), lockBlock.SetActive, false);
            SavePrefab(a, lockBlock);

            GameObject spikes = SpriteObject("Spikes", TileSprite("tile_0068"), 0);
            TriggerBox(spikes, new Vector2(0.8f, 0.4f), new Vector2(0f, -0.3f));
            spikes.AddComponent<DamageOnTouch>();
            SavePrefab(a, spikes);

            GameObject spring = SpriteObject("Spring", TileSprite("tile_0107"), 0);
            TriggerBox(spring, new Vector2(0.8f, 0.4f), new Vector2(0f, -0.1f));
            BouncePad2D bounce = spring.AddComponent<BouncePad2D>();
            Set(bounce, "bounceHeight", 6f);
            Set(bounce, "spriteToChange", spring.GetComponent<SpriteRenderer>());
            Set(bounce, "firedSprite", TileSprite("tile_0108"));
            SavePrefab(a, spring);

            GameObject oneWay = SpriteObject("One-Way Platform", TileSprite("tile_0146"), 0);
            BoxCollider2D plank = oneWay.AddComponent<BoxCollider2D>();
            plank.size = new Vector2(1f, 0.5f);
            plank.offset = new Vector2(0f, 0.25f);
            plank.usedByEffector = true;
            oneWay.AddComponent<PlatformEffector2D>().useOneWay = true;
            SavePrefab(a, oneWay);

            GameObject checkpoint = SpriteObject("Checkpoint", TileSprite("tile_0111"), 0);
            SpriteRenderer flag = checkpoint.GetComponent<SpriteRenderer>();
            flag.color = new Color(0.5f, 0.5f, 0.5f);
            TriggerBox(checkpoint, new Vector2(1f, 2f), new Vector2(0f, 0.5f));
            Checkpoint save = checkpoint.AddComponent<Checkpoint>();
            Set(save, "indicator", flag);
            Set(save, "activeColor", Color.white);
            Flipbook(checkpoint, 4f, TileSprite("tile_0111"), TileSprite("tile_0112"));
            SavePrefab(a, checkpoint);

            // Moving Platform
            GameObject platform = new GameObject("Moving Platform");
            string[] platformTiles = { "tile_0001", "tile_0002", "tile_0003" };

            for (int i = 0; i < 3; i++)
            {
                GameObject part = SpriteObject("Part " + (i + 1), TileSprite(platformTiles[i]), 0);
                part.transform.SetParent(platform.transform, false);
                part.transform.localPosition = new Vector3(i - 1, 0f, 0f);
            }

            platform.AddComponent<BoxCollider2D>().size = new Vector2(3f, 1f);
            WaypointMover2D platformMover = platform.AddComponent<WaypointMover2D>();
            Set(platformMover, "stops", new[] { new Vector2(4f, 0f) });
            Set(platformMover, "speed", 2f);
            Set(platformMover, "waitAtStops", 0.5f);
            SavePrefab(a, platform);

            // Enemies
            GameObject walker = SpriteObject("Walker", Character("tile_0018"), 5);
            BoxCollider2D walkerBody = walker.AddComponent<BoxCollider2D>();
            walkerBody.size = new Vector2(0.75f, 0.6f);
            walkerBody.offset = new Vector2(0f, -0.3f);
            PatrolEnemy2D patrol = walker.AddComponent<PatrolEnemy2D>();
            Set(patrol, "spriteToFlip", walker.GetComponent<SpriteRenderer>());
            walker.AddComponent<StompableEnemy2D>();
            Flipbook(walker, 4f, Character("tile_0018"), Character("tile_0019"));
            SavePrefab(a, walker);

            GameObject flyer = SpriteObject("Flyer", Character("tile_0024"), 5);
            TriggerCircle(flyer, 0.35f);
            WaypointMover2D flyerMover = flyer.AddComponent<WaypointMover2D>();
            Set(flyerMover, "stops", new[] { new Vector2(4f, 0f) });
            Set(flyerMover, "speed", 2f);
            Set(flyerMover, "waitAtStops", 0f);
            flyer.AddComponent<DamageOnTouch>();
            Flipbook(flyer, 8f, Character("tile_0024"), Character("tile_0025"), Character("tile_0026"), Character("tile_0025"));
            SavePrefab(a, flyer);

            CreateLevelSystems(a);
        }

        static void CreateLevelSystems(Assets a)
        {
            GameObject root = new GameObject("Level Systems");

            SceneChanger sceneChanger = Child(root, "Scene Changer").AddComponent<SceneChanger>();
            SceneChanger toMenu = Child(root, "To Menu").AddComponent<SceneChanger>();
            Set(toMenu, "sceneName", MenuScene);

            GameObject rules = Child(root, "Game Rules");
            IntVariableCondition lose = rules.AddComponent<IntVariableCondition>();
            Set(lose, "variable", a.lives);
            SetEnum(lose, "comparison", "AtMost");
            Set(lose, "value", 0);
            Set(lose, "onlyOnce", true);
            Set(lose, "raiseEvent", a.gameOver);
            VariableResetter resetter = rules.AddComponent<VariableResetter>();
            Set(resetter, "variables", new VariableBase[] { a.lives, a.coins });
            Set(resetter, "resetWhenSceneStarts", false);

            GameObject pit = Child(root, "Pit");
            pit.transform.localPosition = new Vector3(0f, -12f, 0f);
            TriggerBox(pit, new Vector2(400f, 2f), Vector2.zero);
            pit.AddComponent<KillZone>();

            // HUD
            GameObject hud = Child(root, "HUD");
            Document(a, hud, KitUI + "PlatformerHUD.uxml", 0);
            Bind(hud, ("health-label", a.playerHealth, "Health: {0}"), ("lives-label", a.lives, "Lives: {0}"), ("coins-label", a.coins, "Coins: {0}"));

            // Pause Menu
            GameObject pauseMenu = Child(root, "Pause Menu");
            Document(a, pauseMenu, CoreUI + "PauseMenu.uxml", 1);
            UIScreen pauseScreen = MakeScreen(pauseMenu, true, "resume-button");
            PauseController pause = pauseMenu.AddComponent<PauseController>();
            Set(pause, "input", a.input);
            Set(pause, "pauseScreen", pauseScreen);
            UIButtonEvents pauseButtons = pauseMenu.AddComponent<UIButtonEvents>();
            AddButton(pauseButtons, "resume-button", e => UnityEventTools.AddVoidPersistentListener(e, pause.Resume));
            AddButton(pauseButtons, "restart-button", e => UnityEventTools.AddVoidPersistentListener(e, sceneChanger.ReloadScene));
            AddButton(pauseButtons, "menu-button", e => UnityEventTools.AddVoidPersistentListener(e, toMenu.Load));

            // Settings, opened from the pause menu
            UIScreen pauseSettings = MakeSettings(a, root.transform, 4);
            AddButton(pauseButtons, "settings-button", e => UnityEventTools.AddVoidPersistentListener(e, pauseSettings.Show));

            // Level Complete
            GameObject complete = Child(root, "Level Complete");
            Document(a, complete, KitUI + "LevelComplete.uxml", 2);
            UIScreen completeScreen = MakeScreen(complete, true, "next-button");
            Bind(complete, ("coins-label", a.coins, "Coins: {0}"));
            Listen(complete, a.levelCompleted,
                e => UnityEventTools.AddVoidPersistentListener(e, completeScreen.Show),
                e => UnityEventTools.AddVoidPersistentListener(e, a.input.DisablePlayerInput));
            UIButtonEvents completeButtons = complete.AddComponent<UIButtonEvents>();
            AddButton(completeButtons, "next-button", e => UnityEventTools.AddVoidPersistentListener(e, sceneChanger.LoadNextScene));
            AddButton(completeButtons, "menu-button", e => UnityEventTools.AddVoidPersistentListener(e, toMenu.Load));

            // Game Over
            GameObject over = Child(root, "Game Over");
            Document(a, over, CoreUI + "GameOver.uxml", 3);
            UIScreen overScreen = MakeScreen(over, true, "try-again-button");
            Bind(over, ("score-label", a.coins, "Coins: {0}"));
            Listen(over, a.gameOver,
                e => UnityEventTools.AddVoidPersistentListener(e, overScreen.Show),
                e => UnityEventTools.AddVoidPersistentListener(e, a.input.DisablePlayerInput));
            UIButtonEvents overButtons = over.AddComponent<UIButtonEvents>();
            AddButton(overButtons, "try-again-button",
                e => UnityEventTools.AddVoidPersistentListener(e, resetter.ResetAll),
                e => UnityEventTools.AddVoidPersistentListener(e, sceneChanger.ReloadScene));
            AddButton(overButtons, "menu-button", e => UnityEventTools.AddVoidPersistentListener(e, toMenu.Load));

            SavePrefab(a, root);
        }

        // ---------------------------------------------------------------- Levels

        static void BuildLevel(Assets a, Scene scene, string[] map, string sceneName)
        {
            int height = map.Length;
            int width = map[0].Length;

            Camera camera = MainCamera(new Vector3(width / 2f, height / 2f, -10f));
            camera.gameObject.AddComponent<CinemachineBrain>();
            GlobalLight();

            // Tilemaps
            GameObject grid = new GameObject("Grid", typeof(Grid));
            Tilemap ground = TilemapObject(grid, "Ground", 0);
            Rigidbody2D groundBody = ground.gameObject.AddComponent<Rigidbody2D>();
            groundBody.bodyType = RigidbodyType2D.Static;
            TilemapCollider2D groundCollider = ground.gameObject.AddComponent<TilemapCollider2D>();
            groundCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            ground.gameObject.AddComponent<CompositeCollider2D>();
            TilemapObject(grid, "Decor", -1);

            GameObject things = new GameObject("Level Objects");
            GameObject player = null;

            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    int y = height - 1 - row;
                    char c = map[row][x];

                    string tileName = GroundTile(map, x, row);

                    if (tileName != null)
                    {
                        ground.SetTile(new Vector3Int(x, y, 0), a.tiles[tileName]);
                        continue;
                    }

                    string prefabName = PrefabFor(c, out float lift);

                    if (prefabName == null)
                    {
                        continue;
                    }

                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(a.prefabs[prefabName]);
                    instance.transform.SetParent(things.transform, true);
                    instance.transform.position = new Vector3(x + 0.5f, y + lift, 0f);

                    if (c == 'P')
                    {
                        instance.transform.SetParent(null, true);
                        player = instance;
                    }
                }
            }

            if (player == null)
            {
                throw new BuildProblem($"The map for {sceneName} has no player (P).");
            }

            // Camera that follows the player and stays inside the level.
            GameObject bounds = new GameObject("Camera Bounds");
            PolygonCollider2D shape = bounds.AddComponent<PolygonCollider2D>();
            shape.isTrigger = true;
            shape.points = new[] { new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(width, height), new Vector2(0f, height) };

            GameObject cameraObject = new GameObject("Player Camera");
            CinemachineCamera follow = cameraObject.AddComponent<CinemachineCamera>();
            follow.Follow = player.transform;
            LensSettings lens = follow.Lens;
            lens.OrthographicSize = 5f;
            follow.Lens = lens;
            cameraObject.AddComponent<CinemachinePositionComposer>();
            CinemachineConfiner2D confiner = cameraObject.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = shape;

            PrefabUtility.InstantiatePrefab(a.prefabs["Level Systems"]);

            Save(scene, sceneName);
        }

        // Picks the tile for a ground character from its neighbors, or null if it isn't ground.
        static string GroundTile(string[] map, int x, int row)
        {
            char c = map[row][x];
            bool Is(int dx, int drow, char kind)
            {
                int r = row + drow;
                int cx = x + dx;
                return r >= 0 && r < map.Length && cx >= 0 && cx < map[r].Length && map[r][cx] == kind;
            }

            switch (c)
            {
                case 'B':
                    return "tile_0000";

                case '=':
                    return Ends(Is(-1, 0, '='), Is(1, 0, '='), "tile_0001", "tile_0002", "tile_0003");

                case '#':
                    bool covered = Is(0, -1, '#');
                    return covered
                        ? Ends(Is(-1, 0, '#'), Is(1, 0, '#'), "tile_0121", "tile_0122", "tile_0123")
                        : Ends(Is(-1, 0, '#'), Is(1, 0, '#'), "tile_0021", "tile_0022", "tile_0023");

                default:
                    return null;
            }
        }

        static string Ends(bool left, bool right, string leftEnd, string middle, string rightEnd)
        {
            if (left && right) return middle;
            if (right) return leftEnd;
            if (left) return rightEnd;
            return middle;
        }

        // Which prefab a map character places, and how high in its cell.
        static string PrefabFor(char c, out float lift)
        {
            lift = 0.5f;

            switch (c)
            {
                case 'P': lift = 0.7f; return "Player";
                case 'c': return "Coin";
                case 'h': return "Heart";
                case 'k': return "Key";
                case 'G': return "Goal Gem";
                case 'K': return "Checkpoint";
                case 'W': lift = 0.65f; return "Walker";
                case 'F': return "Flyer";
                case 'M': return "Moving Platform";
                case 'S': return "Spring";
                case 'o': return "One-Way Platform";
                case 'L': return "Lock";
                case '^': return "Spikes";
                default: return null;
            }
        }

        // ---------------------------------------------------------------- Menu and win scenes

        static void BuildMenu(Assets a)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MainCamera(new Vector3(0f, 0f, -10f));

            GameObject credits = new GameObject("Credits");
            Document(a, credits, CoreUI + "Credits.uxml", 1);
            UIScreen creditsScreen = MakeScreen(credits, true, "back-button");
            UICredits creditsText = credits.AddComponent<UICredits>();
            Set(creditsText, "creditFiles", new List<TextAsset> { Load<TextAsset>(CreditsPath) });
            UIButtonEvents creditsButtons = credits.AddComponent<UIButtonEvents>();
            AddButton(creditsButtons, "back-button", e => UnityEventTools.AddVoidPersistentListener(e, creditsScreen.Hide));

            UIScreen settingsScreen = MakeSettings(a, null, 2);

            GameObject help = new GameObject("How to Play");
            Document(a, help, KitUI + "PlatformerHowToPlay.uxml", 3);
            UIScreen helpScreen = MakeScreen(help, true, "back-button");
            UIButtonEvents helpButtons = help.AddComponent<UIButtonEvents>();
            AddButton(helpButtons, "back-button", e => UnityEventTools.AddVoidPersistentListener(e, helpScreen.Hide));

            GameObject menu = new GameObject("Main Menu");
            Document(a, menu, KitUI + "PlatformerMenu.uxml", 0);
            MakeScreen(menu, false, "play-button");
            SceneChanger play = menu.AddComponent<SceneChanger>();
            Set(play, "sceneName", Level1Scene);
            UIButtonEvents menuButtons = menu.AddComponent<UIButtonEvents>();
            AddButton(menuButtons, "play-button", e => UnityEventTools.AddVoidPersistentListener(e, play.Load));
            AddButton(menuButtons, "how-to-play-button", e => UnityEventTools.AddVoidPersistentListener(e, helpScreen.Show));
            AddButton(menuButtons, "settings-button", e => UnityEventTools.AddVoidPersistentListener(e, settingsScreen.Show));
            AddButton(menuButtons, "credits-button", e => UnityEventTools.AddVoidPersistentListener(e, creditsScreen.Show));
            AddButton(menuButtons, "quit-button", e => UnityEventTools.AddVoidPersistentListener(e, play.QuitGame));

            VariableResetter newGame = new GameObject("Game Rules").AddComponent<VariableResetter>();
            Set(newGame, "variables", new VariableBase[] { a.lives, a.coins });
            Set(newGame, "resetWhenSceneStarts", true);

            Save(scene, MenuScene);
        }

        static void BuildWin(Assets a)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MainCamera(new Vector3(0f, 0f, -10f));

            SceneChanger toLevel1 = new GameObject("To Level 1").AddComponent<SceneChanger>();
            Set(toLevel1, "sceneName", Level1Scene);
            SceneChanger toMenu = new GameObject("To Menu").AddComponent<SceneChanger>();
            Set(toMenu, "sceneName", MenuScene);

            VariableResetter rules = new GameObject("Game Rules").AddComponent<VariableResetter>();
            Set(rules, "variables", new VariableBase[] { a.lives, a.coins });
            Set(rules, "resetWhenSceneStarts", false);

            GameObject win = new GameObject("Win Screen");
            Document(a, win, CoreUI + "EndScreen.uxml", 0);
            MakeScreen(win, false, "play-again-button");
            Bind(win, ("score-label", a.coins, "Coins: {0}"));
            UIButtonEvents buttons = win.AddComponent<UIButtonEvents>();
            AddButton(buttons, "play-again-button",
                e => UnityEventTools.AddVoidPersistentListener(e, rules.ResetAll),
                e => UnityEventTools.AddVoidPersistentListener(e, toLevel1.Load));
            AddButton(buttons, "menu-button", e => UnityEventTools.AddVoidPersistentListener(e, toMenu.Load));

            Save(scene, WinScene);
        }

        static void AddToSceneList()
        {
            string[] paths = { ScenePath(MenuScene), ScenePath(Level1Scene), ScenePath(Level2Scene), ScenePath(WinScene) };

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(s => !paths.Contains(s.path))
                .ToList();

            scenes.AddRange(paths.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------------------------------------------------------------- Helpers: objects

        static GameObject SpriteObject(string objectName, Sprite sprite, int order)
        {
            GameObject go = new GameObject(objectName);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return go;
        }

        static GameObject Child(GameObject parent, string childName)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        static void TriggerCircle(GameObject go, float radius)
        {
            CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
            circle.radius = radius;
            circle.isTrigger = true;
        }

        static void TriggerBox(GameObject go, Vector2 size, Vector2 offset)
        {
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.offset = offset;
            box.isTrigger = true;
        }

        static void Flipbook(GameObject go, float framesPerSecond, params Sprite[] frames)
        {
            SpriteFlipbook flipbook = go.AddComponent<SpriteFlipbook>();
            Set(flipbook, "frames", frames);
            Set(flipbook, "framesPerSecond", framesPerSecond);
            Set(flipbook, "target", go.GetComponent<SpriteRenderer>());
        }

        static Tilemap TilemapObject(GameObject grid, string objectName, int order)
        {
            GameObject go = new GameObject(objectName, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid.transform, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = order;
            return go.GetComponent<Tilemap>();
        }

        static Camera MainCamera(Vector3 position)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = position;
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;
            go.AddComponent<AudioListener>();
            return camera;
        }

        // A Global Light 2D, as in Unity's 2D scene template. The Light 2D type lives in
        // a URP assembly whose name has changed between versions, so it's found by name.
        static void GlobalLight()
        {
            Type lightType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEngine.Rendering.Universal.Light2D", false))
                .FirstOrDefault(type => type != null);

            if (lightType == null)
            {
                Debug.LogWarning("Platformer sample: couldn't find the Light 2D component, so the level has no Global Light 2D. Add one with GameObject → Light → Global Light 2D.");
                return;
            }

            GameObject go = new GameObject("Global Light 2D");
            Component light = go.AddComponent(lightType);
            PropertyInfo kind = lightType.GetProperty("lightType");

            if (kind != null)
            {
                kind.SetValue(light, Enum.Parse(kind.PropertyType, "Global"));
            }
        }

        static void SavePrefab(Assets a, GameObject go)
        {
            string path = $"{PrefabFolder}/{go.name}.prefab";
            a.prefabs[go.name] = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void Save(Scene scene, string sceneName)
        {
            if (!EditorSceneManager.SaveScene(scene, ScenePath(sceneName)))
            {
                throw new BuildProblem($"Couldn't save {sceneName}.");
            }
        }

        static string ScenePath(string sceneName) => $"{SceneFolder}/{sceneName}.unity";

        // ---------------------------------------------------------------- Helpers: UI

        static void Document(Assets a, GameObject go, string uxmlPath, int sortOrder)
        {
            UIDocument document = go.AddComponent<UIDocument>();
            document.panelSettings = a.panel;
            document.visualTreeAsset = Load<VisualTreeAsset>(uxmlPath);
            document.sortingOrder = sortOrder;
        }

        // A Settings screen (volume and full screen), hidden until shown, with Back hiding it.
        static UIScreen MakeSettings(Assets a, Transform parent, int sortOrder)
        {
            GameObject settings = new GameObject("Settings");

            if (parent != null)
            {
                settings.transform.SetParent(parent, false);
            }

            Document(a, settings, CoreUI + "Settings.uxml", sortOrder);
            UIScreen screen = MakeScreen(settings, true, "music-slider");
            settings.AddComponent<UISettings>();
            UIButtonEvents buttons = settings.AddComponent<UIButtonEvents>();
            AddButton(buttons, "back-button", e => UnityEventTools.AddVoidPersistentListener(e, screen.Hide));
            return screen;
        }

        static UIScreen MakeScreen(GameObject go, bool hiddenAtStart, string selectWhenShown)
        {
            UIScreen screen = go.AddComponent<UIScreen>();
            Set(screen, "hiddenAtStart", hiddenAtStart);
            Set(screen, "selectWhenShown", selectWhenShown);
            return screen;
        }

        static void Bind(GameObject go, params (string label, VariableBase variable, string format)[] bindings)
        {
            UIVariableBinder binder = go.AddComponent<UIVariableBinder>();
            List<UIVariableBinder.Binding> list = new List<UIVariableBinder.Binding>();

            foreach ((string label, VariableBase variable, string format) in bindings)
            {
                list.Add(new UIVariableBinder.Binding { labelName = label, variable = variable, format = format });
            }

            Set(binder, "bindings", list);
        }

        static void AddButton(UIButtonEvents buttons, string buttonName, params Action<UnityEvent>[] wiring)
        {
            List<UIButtonEvents.ButtonEvent> list = (List<UIButtonEvents.ButtonEvent>)Get(buttons, "buttons") ?? new List<UIButtonEvents.ButtonEvent>();
            UIButtonEvents.ButtonEvent entry = new UIButtonEvents.ButtonEvent { buttonName = buttonName, onClick = new UnityEvent() };

            foreach (Action<UnityEvent> wire in wiring)
            {
                wire(entry.onClick);
            }

            list.Add(entry);
            Set(buttons, "buttons", list);
        }

        static void Listen(GameObject go, VoidEventChannel channel, params Action<UnityEvent>[] wiring)
        {
            VoidEventListener listener = go.AddComponent<VoidEventListener>();
            Set(listener, "channel", channel);
            UnityEvent response = GetEvent(listener, "response");

            foreach (Action<UnityEvent> wire in wiring)
            {
                wire(response);
            }
        }

        // ---------------------------------------------------------------- Helpers: assets and fields

        static Sprite TileSprite(string spriteName) => Load<Sprite>($"{ArtFolder}/{spriteName}.png");

        static Sprite Character(string spriteName) => Load<Sprite>($"{CharacterFolder}/{spriteName}.png");

        static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                throw new BuildProblem($"Couldn't find {path}.");
            }

            return asset;
        }

        // Sets a field the Inspector shows, even when it's private.
        static void Set(Object target, string fieldName, object value)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }

        static void SetEnum(Object target, string fieldName, string valueName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            field.SetValue(target, Enum.Parse(field.FieldType, valueName));
            EditorUtility.SetDirty(target);
        }

        static object Get(Object target, string fieldName) => FindField(target.GetType(), fieldName).GetValue(target);

        // Gets a UnityEvent field, creating it if it hasn't been yet.
        static UnityEvent GetEvent(Object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);

            if (!(field.GetValue(target) is UnityEvent unityEvent))
            {
                unityEvent = new UnityEvent();
                field.SetValue(target, unityEvent);
            }

            return unityEvent;
        }

        static FieldInfo FindField(Type type, string fieldName)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null)
                {
                    return field;
                }
            }

            throw new BuildProblem($"{type.Name} has no field \"{fieldName}\". The kit's scripts and this builder are out of step; tell Claude.");
        }
    }
}
