using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CollinGame.Core;
using CollinGame.EditorTools;
using CollinGame.Kits.Platformer;
using CollinGame.ThreeD;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CollinGame.EditorTools.SampleBuild;
using Object = UnityEngine.Object;

namespace CollinGame.Kits.Platformer3D.EditorTools
{
    /// <summary>
    /// Builds the 3D Platformer Kit's sample, a floating-islands level made from
    /// Unity's basic shapes, following "3D Platformer Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build 3D Platformer Sample.
    /// </summary>
    public static class Platformer3DSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/Platformer3D";
        const string MenuName = "Islands_Menu";
        const string LevelName = "Islands_Level";

        static readonly Color Sky = new Color32(0x8F, 0xD3, 0xFF, 0xFF);

        const string HowToPlay =
            "Cross the floating islands and touch the flag at the top of the tower.\n\n" +
            "WASD or the left stick move, relative to the camera. The mouse or the right stick turn the camera. Space jumps (hold it to jump higher); Shift sprints.\n\n" +
            "Moving platforms carry you; crumbling ones fall soon after you land. The red pad bounces you up high. Jump on the red cubes to defeat them; walking into them costs a heart.\n\n" +
            "Fall off, or lose every heart, and you start again from the last pole you touched. Collect all twenty coins if you can. Esc pauses.";

        [MenuItem("Collin Game/Kit Samples/Build 3D Platformer Sample")]
        static void Build()
        {
            Run("3D Platformer", KitFolder, new[] { "Data", "Materials", "Prefabs", "Scenes" }, new string[0], BuildSample);
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out Color c) ? c : Color.white;

        sealed class Kit
        {
            public IntVariable Coins, Health;
            public VoidEventChannel Won;
            public Material Grass, Stone, Gold, Danger, Shadow, DarkGrass;
            public GameObject Player, Coin, Checkpoint, MovingPlatform, Crumbling, BouncePad, Enemy, Systems;
            public Transform Level;
        }

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — Data and materials
            Kit k = new Kit
            {
                Coins = b.Int("Coins"),
                Health = b.Int("PlayerHealth", 3),
                Won = b.Channel("LevelWon"),
                Grass = b.LitMaterial("Materials", "Grass", Hex("6DBE45")),
                Stone = b.LitMaterial("Materials", "Stone", Hex("9A9AA6")),
                Gold = b.LitMaterial("Materials", "Coin", Hex("FFD23F"), true),
                Danger = b.LitMaterial("Materials", "Danger", Hex("E04848")),
                DarkGrass = b.LitMaterial("Materials", "Dark Grass", Hex("4C8A30")),
                Shadow = b.ShadowMaterial("Materials", "Shadow", 0.6f)
            };

            BuildPrefabs(b, k);
            k.Systems = IslandsSystems(b, k);

            // Part 8 — Menu first, so it's first in the Scene List
            b.MenuScene(MenuName, "Sky Islands", "Run, jump and bounce across floating islands to the flag at the top of the tower.", LevelName,
                new VariableBase[] { k.Coins, k.Health }, Sky, HowToPlay);

            BuildLevel(b, k);
        }

        // ---------------------------------------------------------------- Parts 3 and 5 — Player and prefabs

        static void BuildPrefabs(SampleBuild b, Kit k)
        {
            GameObject player = new GameObject("Player") { tag = "Player" };
            Shape(PrimitiveType.Capsule, "Body", new Vector3(0f, 1f, 0f), Vector3.one, k.Stone, player.transform, false);
            Shape(PrimitiveType.Cube, "Nose", new Vector3(0f, 1.5f, 0.45f), new Vector3(0.3f, 0.2f, 0.4f), k.Danger, player.transform, false);

            PlayerController3D controller = player.AddComponent<PlayerController3D>();
            CharacterController body = player.GetComponent<CharacterController>();
            body.height = 2f;
            body.radius = 0.4f;
            body.center = new Vector3(0f, 1f, 0f);
            Set(controller, "input", b.Input);
            SetEnum(controller, "facing", "FaceMovement");
            Set(controller, "walkSpeed", 6f);
            Set(controller, "sprintSpeed", 9f);
            Set(controller, "jumpHeight", 2.2f);
            Set(controller, "extraAirJumps", 0);
            Set(controller, "canCrouch", false);

            Health health = player.AddComponent<Health>();
            Set(health, "maxHealth", 3);
            Set(health, "invulnerableTime", 1f);
            Set(health, "healthVariable", k.Health);
            SetEnum(health, "whenHealthRunsOut", "Nothing");
            Set(player.AddComponent<Respawner>(), "respawnWhenHealthRunsOut", true);
            Set(player.AddComponent<BlobShadow3D>(), "size", 0.9f);
            k.Player = b.Prefab(player);

            GameObject coin = Shape(PrimitiveType.Cylinder, "Coin", Vector3.zero, new Vector3(0.6f, 0.05f, 0.6f), k.Gold, null, false);
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            SphereCollider pickup = coin.AddComponent<SphereCollider>();
            pickup.isTrigger = true;
            pickup.radius = 1f;
            Set(coin.AddComponent<Collectible>(), "addTo", k.Coins);
            Spin3D spin = coin.AddComponent<Spin3D>();
            Set(spin, "spin", new Vector3(0f, 0f, 180f));
            Set(spin, "bobHeight", 0.15f);
            k.Coin = b.Prefab(coin);

            GameObject checkpoint = new GameObject("Checkpoint");
            BoxCollider zone = checkpoint.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(2f, 3f, 2f);
            zone.center = new Vector3(0f, 1.5f, 0f);
            Shape(PrimitiveType.Cylinder, "Pole", new Vector3(0f, 1.25f, 0f), new Vector3(0.15f, 1.25f, 0.15f), k.Stone, checkpoint.transform, false);
            Shape(PrimitiveType.Cube, "Pennant", new Vector3(0.35f, 2.2f, 0f), new Vector3(0.6f, 0.4f, 0.05f), k.Gold, checkpoint.transform, false);
            GameObject respawnPoint = Child(checkpoint, "Respawn Point");
            respawnPoint.transform.localPosition = new Vector3(0f, 0.1f, 1f);
            Set(checkpoint.AddComponent<Checkpoint>(), "respawnPoint", respawnPoint.transform);
            k.Checkpoint = b.Prefab(checkpoint);

            GameObject moving = new GameObject("Moving Platform");
            Shape(PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(3f, 0.5f, 3f), k.Stone, moving.transform);
            GameObject a = Child(moving, "A");
            GameObject bPoint = Child(moving, "B");
            bPoint.transform.localPosition = new Vector3(0f, 0f, 4f);
            MovingPlatform3D movingScript = moving.AddComponent<MovingPlatform3D>();
            Set(movingScript, "points", new List<Transform> { a.transform, bPoint.transform });
            Set(movingScript, "speed", 2.5f);
            Set(movingScript, "waitTime", 0.8f);
            SetEnum(movingScript, "mode", "BackAndForth");
            k.MovingPlatform = b.Prefab(moving);

            GameObject crumbling = Shape(PrimitiveType.Cube, "Crumbling Platform", Vector3.zero, new Vector3(2f, 0.4f, 2f), k.DarkGrass);
            CrumblingPlatform3D crumble = crumbling.AddComponent<CrumblingPlatform3D>();
            Set(crumble, "holdTime", 0.6f);
            Set(crumble, "returnTime", 3f);
            k.Crumbling = b.Prefab(crumbling);

            GameObject pad = Shape(PrimitiveType.Cylinder, "Bounce Pad", Vector3.zero, new Vector3(1.5f, 0.05f, 1.5f), k.Danger, null, false);
            BoxCollider padTrigger = pad.AddComponent<BoxCollider>();
            padTrigger.isTrigger = true;
            padTrigger.center = new Vector3(0f, 10f, 0f);   // in the pad's own (flattened) units: half a unit above it
            padTrigger.size = new Vector3(1.2f, 20f, 1.2f);
            Set(pad.AddComponent<BouncePad3D>(), "height", 8.5f);
            k.BouncePad = b.Prefab(pad);

            GameObject enemy = Shape(PrimitiveType.Cube, "Patrol Enemy", Vector3.zero, Vector3.one, k.Danger);
            enemy.GetComponent<Collider>().isTrigger = true;
            Shape(PrimitiveType.Cube, "Eyes", new Vector3(0f, 0.2f, 0.45f), new Vector3(0.7f, 0.2f, 0.15f), k.Stone, enemy.transform, false);
            GameObject p1 = Child(enemy, "Point 1");
            GameObject p2 = Child(enemy, "Point 2");
            p2.transform.localPosition = new Vector3(0f, 0f, 4f);
            PatrolEnemy3D patrol = enemy.AddComponent<PatrolEnemy3D>();
            Set(patrol, "points", new List<Transform> { p1.transform, p2.transform });
            Set(patrol, "speed", 2f);
            Set(patrol, "damage", 1);
            Set(patrol, "canBeStomped", true);
            Set(patrol, "stompBounce", 3f);
            k.Enemy = b.Prefab(enemy);
        }

        // ---------------------------------------------------------------- Part 7 — Islands Systems

        static GameObject IslandsSystems(SampleBuild b, Kit k)
        {
            GameObject root = new GameObject("Islands Systems");

            GameObject hud = Child(root, "HUD");
            b.Document(hud, CoreUI + "HUD.uxml", 0);
            Bind(hud, ("health-label", k.Health, "Hearts: {0}"), ("score-label", k.Coins, "Coins: {0} / 20"));

            Resetter(Child(root, "Reset"), true, k.Coins, k.Health);

            CursorLock3D cursorLock = Child(root, "Cursor Lock").AddComponent<CursorLock3D>();
            Set(cursorLock, "input", b.Input);

            Systems s = b.StandardSystems(root, MenuName);
            b.EndScreen(s, "Won", true, k.Won, new (string, VariableBase, string)[] { ("score-label", k.Coins, "Coins: {0} / 20") }, null,
                "You Made It!", null, Call(cursorLock.Unlock));
            return b.Prefab(root);
        }

        // ---------------------------------------------------------------- Part 6 — The level

        /// <summary>An island: stone underneath and a thin layer of grass, its top at topCenter.</summary>
        static void Island(Kit k, string islandName, Vector3 topCenter, float width, float length, float depth)
        {
            GameObject island = new GameObject(islandName);
            island.transform.SetParent(k.Level, false);
            island.transform.localPosition = topCenter;
            Shape(PrimitiveType.Cube, "Rock", new Vector3(0f, -depth / 2f - 0.2f, 0f), new Vector3(width, depth, length), k.Stone, island.transform);
            Shape(PrimitiveType.Cube, "Grass", new Vector3(0f, -0.1f, 0f), new Vector3(width, 0.2f, length), k.Grass, island.transform);
        }

        static void Coin(Kit k, Transform parent, float x, float y, float z) => Place(k.Coin, new Vector3(x, y, z), parent);

        static void BuildLevel(SampleBuild b, Kit k)
        {
            Scene scene = NewScene();

            // Part 2 — Camera and light
            Camera camera = Camera3D(new Vector3(0f, 4f, -8f), Quaternion.Euler(20f, 0f, 0f), Sky);
            camera.gameObject.AddComponent<CinemachineBrain>();
            Sun(new Color(1f, 0.96f, 0.88f), 1.2f, new Vector3(50f, -30f, 0f));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.7f);

            k.Level = new GameObject("Level").transform;
            Transform coins = new GameObject("Coins").transform;

            // Start
            Island(k, "Start", Vector3.zero, 10f, 10f, 2f);
            foreach (float z in new[] { -2f, -0.5f, 1f, 2.5f, 4f }) Coin(k, coins, 0f, 1f, z);

            // Area 1: Stepping Stones, curving left and climbing.
            Vector3[] stones =
            {
                new Vector3(0f, 0.5f, 9f), new Vector3(-2f, 1f, 14f), new Vector3(-5f, 1.5f, 18.5f),
                new Vector3(-9.5f, 2f, 21f), new Vector3(-14.5f, 2.5f, 22f), new Vector3(-19.5f, 3f, 22f)
            };
            for (int i = 0; i < stones.Length; i++)
            {
                Island(k, $"Stepping Stone {i + 1}", stones[i], 3f, 3f, 1f);
                if (i > 0)
                {
                    Vector3 middle = (stones[i - 1] + stones[i]) / 2f;
                    Coin(k, coins, middle.x, Mathf.Max(stones[i - 1].y, stones[i].y) + 1.3f, middle.z);
                }
            }
            Place(k.Checkpoint, stones[0] + new Vector3(1f, 0f, -0.8f));

            // Area 2: Over the Drop
            Island(k, "Ledge", new Vector3(-26f, 3f, 22f), 6f, 6f, 1.5f);
            Place(k.Checkpoint, new Vector3(-25f, 3f, 20.5f));

            GameObject sideways = Place(k.MovingPlatform, new Vector3(-31.5f, 2.75f, 22f));
            PointAt(sideways, "B", new Vector3(-38.5f, 2.75f, 22f));
            GameObject lift = Place(k.MovingPlatform, new Vector3(-43f, 2.75f, 22f));
            PointAt(lift, "B", new Vector3(-43f, 5.75f, 22f));
            Coin(k, coins, -35f, 4.2f, 22f);
            Coin(k, coins, -43f, 7f, 22f);

            Transform bridge = new GameObject("Crumbling Bridge").transform;
            for (int i = 0; i < 5; i++)
            {
                Place(k.Crumbling, new Vector3(-46.5f - i * 3f, 5.8f, 22f), bridge);
            }
            foreach (float x in new[] { -48f, -51f, -54f }) Coin(k, coins, x, 7.3f, 22f);

            // Area 3: The Tower
            Island(k, "Tower Base", new Vector3(-67.5f, 6f, 22f), 14f, 14f, 3f);
            Place(k.Checkpoint, new Vector3(-61.8f, 6f, 16f));
            Island(k, "Tower", new Vector3(-70f, 13f, 22f), 6f, 6f, 6.8f);
            Island(k, "Step 1", new Vector3(-71.5f, 14.5f, 20.5f), 1.5f, 1.5f, 1.3f);
            Island(k, "Step 2", new Vector3(-71.5f, 16f, 23f), 1.5f, 1.5f, 2.8f);
            Island(k, "Summit", new Vector3(-69f, 17.5f, 23.5f), 3f, 3f, 4.3f);
            Place(k.BouncePad, new Vector3(-65.2f, 6.05f, 22f));

            GameObject guard1 = Place(k.Enemy, new Vector3(-64f, 6.5f, 18f));
            PointAt(guard1, "Point 2", new Vector3(-64f, 6.5f, 27f));
            GameObject guard2 = Place(k.Enemy, new Vector3(-74f, 6.5f, 27.5f));
            PointAt(guard2, "Point 2", new Vector3(-66f, 6.5f, 27.5f));
            Coin(k, coins, -64f, 7.2f, 24f);
            Coin(k, coins, -70f, 7.2f, 27.5f);
            Coin(k, coins, -68f, 14.2f, 20f);
            Coin(k, coins, -71.5f, 15.7f, 20.5f);
            Coin(k, coins, -71.5f, 17.2f, 23f);

            // Goal
            GameObject goal = new GameObject("Goal");
            goal.transform.position = new Vector3(-69f, 17.5f, 23.5f);
            Shape(PrimitiveType.Cylinder, "Pole", new Vector3(0f, 1.5f, 0f), new Vector3(0.12f, 1.5f, 0.12f), k.Stone, goal.transform, false);
            Shape(PrimitiveType.Cube, "Cloth", new Vector3(0.5f, 2.6f, 0f), new Vector3(0.9f, 0.6f, 0.05f), k.Danger, goal.transform, false);
            BoxCollider goalZone = goal.AddComponent<BoxCollider>();
            goalZone.isTrigger = true;
            goalZone.center = new Vector3(0f, 1.5f, 0f);
            goalZone.size = new Vector3(2f, 3f, 2f);
            TriggerZone trigger = goal.AddComponent<TriggerZone>();
            Set(trigger, "onlyOnce", true);
            Set(trigger, "raiseOnEnter", k.Won);

            // Kill Zone
            GameObject killZone = new GameObject("Kill Zone");
            killZone.transform.position = new Vector3(-35f, -15f, 20f);
            BoxCollider killBox = killZone.AddComponent<BoxCollider>();
            killBox.isTrigger = true;
            killBox.size = new Vector3(400f, 2f, 400f);
            killZone.AddComponent<KillZone>();

            // Part 3 — The player and its shadow
            GameObject player = Place(k.Player, new Vector3(0f, 0.1f, -3f));
            GameObject shadow = Shape(PrimitiveType.Quad, "Player Shadow", Vector3.zero, Vector3.one, k.Shadow, null, false);
            shadow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            BlobShadow3D blob = player.GetComponent<BlobShadow3D>();
            Set(blob, "shadow", shadow.transform);
            KeepChanges(blob);

            // Part 4 — The orbit camera
            OrbitCamera(player.transform, 7f);

            Place(k.Systems, Vector3.zero);
            b.SaveScene(scene, LevelName);
        }

        /// <summary>Moves a placed prefab's child point (kept as an override).</summary>
        static void PointAt(GameObject placed, string pointName, Vector3 position)
        {
            Transform point = placed.transform.Find(pointName);
            point.position = position;
            KeepChanges(point);
        }

        /// <summary>
        /// A Cinemachine camera that orbits the player with the mouse or right stick, and
        /// moves in when scenery gets between them.
        /// </summary>
        public static CinemachineCamera OrbitCamera(Transform target, float radius)
        {
            GameObject go = new GameObject("Orbit Camera");
            CinemachineCamera cam = go.AddComponent<CinemachineCamera>();
            cam.Follow = target;
            cam.LookAt = target;

            CinemachineOrbitalFollow orbit = go.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = radius;
            orbit.TargetOffset = new Vector3(0f, 1.2f, 0f);
            orbit.VerticalAxis.Range = new Vector2(-10f, 60f);
            orbit.VerticalAxis.Center = 20f;
            orbit.VerticalAxis.Value = 20f;

            go.AddComponent<CinemachineRotationComposer>().TargetOffset = new Vector3(0f, 1.2f, 0f);
            go.AddComponent<CinemachineDeoccluder>();
            AddLookInput(go);
            return cam;
        }

        /// <summary>
        /// Adds Cinemachine's Input Axis Controller and points its look axes at the course's
        /// Player/Look action. Done by name, as its details differ between Cinemachine versions.
        /// </summary>
        static void AddLookInput(GameObject go)
        {
            const string manual = "Select Orbit Camera and check Part 4, step 4 of the guide (Look Orbit X and Y → Player/Look).";

            try
            {
                Type controllerType = typeof(CinemachineCamera).Assembly.GetType("Unity.Cinemachine.CinemachineInputAxisController");

                if (controllerType == null)
                {
                    Debug.LogWarning("3D Platformer sample: couldn't add Cinemachine Input Axis Controller. " + manual);
                    return;
                }

                Component controller = go.AddComponent(controllerType);
                Object look = AssetDatabase.LoadAllAssetsAtPath("Assets/_Course/Core/Input/InputSystem_Actions.inputactions")
                    .FirstOrDefault(a => a != null && a.GetType().Name == "InputActionReference" && a.name.EndsWith("/Look"));

                MethodInfo sync = controllerType.GetMethod("SynchronizeControllers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                sync?.Invoke(controller, null);

                FieldInfo listField = controllerType.GetField("Controllers", BindingFlags.Instance | BindingFlags.Public);
                IList list = listField?.GetValue(controller) as IList;

                if (look == null || list == null || list.Count == 0)
                {
                    Debug.Log("3D Platformer sample: the orbit camera uses Cinemachine's default look input. If the mouse doesn't turn it: " + manual);
                    return;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    object item = list[i];
                    FieldInfo inputField = item.GetType().GetField("Input");
                    object reader = inputField?.GetValue(item);
                    FieldInfo actionField = reader?.GetType().GetField("InputAction");

                    if (actionField != null && actionField.GetValue(reader) == null)
                    {
                        actionField.SetValue(reader, look);
                        inputField.SetValue(item, reader);
                        list[i] = item;
                    }
                }

                EditorUtility.SetDirty(controller);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"3D Platformer sample: couldn't set up the orbit camera's input ({e.Message}). " + manual);
            }
        }
    }
}
