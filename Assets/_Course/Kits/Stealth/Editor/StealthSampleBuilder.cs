using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CollinGame.Core;
using CollinGame.EditorTools;
using CollinGame.ThreeD;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using static CollinGame.EditorTools.SampleBuild;
using Object = UnityEngine.Object;

namespace CollinGame.Kits.Stealth.EditorTools
{
    /// <summary>
    /// Builds the Stealth Kit's sample, "Night Shift": a dark office building with two
    /// guards, three keys, hiding spots and an exit, made from Unity's basic shapes,
    /// following "Stealth Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Stealth Sample.
    /// </summary>
    public static class StealthSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/Stealth";
        const string MenuName = "NightShift_Menu";
        const string LevelName = "NightShift_Level";
        const float WallHeight = 3f;
        const float WallThickness = 0.3f;

        static readonly Color Night = new Color(0.03f, 0.04f, 0.07f);

        const string HowToPlay =
            "Find three keys hidden in the building, then get out through the exit in Security, without being caught.\n\n" +
            "WASD or the left stick move; the mouse or right stick turn the camera. Shift sprints, but guards hear it. Ctrl or C crouches: slower, and quiet even right behind a guard.\n\n" +
            "Stay out of the guards' vision cones. Yellow: calm. Orange: suspicious, it will come to look. Red: it has seen you, so break line of sight and hide.\n\n" +
            "Hide in a locker, or crouch under a desk. Click to switch the flashlight on or off: it helps you see, but guards spot it from farther away, and the battery runs down.\n\n" +
            "Esc pauses.";

        [MenuItem("Collin Game/Kit Samples/Build Stealth Sample")]
        static void Build()
        {
            Run("Stealth", KitFolder, new[] { "Data", "Materials", "Prefabs", "Scenes" }, new string[0], BuildSample);
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out Color c) ? c : Color.white;

        sealed class Kit
        {
            public IntVariable Keys;
            public FloatVariable Battery;
            public StringVariable Message;
            public VoidEventChannel ExitOpened, Escaped, Caught;
            public Material Floor, Wall, Furniture, Locker, Key, Guard, Vision;
            public GameObject Player, GuardPrefab, KeyPrefab, LockerPrefab, Systems;
            public Transform Level;
        }

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — Data and materials
            Kit k = new Kit
            {
                Keys = b.Int("Keys"),
                Battery = b.Float("Battery", 100f),
                Message = b.Text("Message", "Find the three keys, then get to Security."),
                ExitOpened = b.Channel("ExitOpened"),
                Escaped = b.Channel("Escaped"),
                Caught = b.Channel("Caught"),
                Floor = b.LitMaterial("Materials", "Floor", Hex("4A4F5C")),
                Wall = b.LitMaterial("Materials", "Wall", Hex("8A8F9C")),
                Furniture = b.LitMaterial("Materials", "Furniture", Hex("6B5640")),
                Locker = b.LitMaterial("Materials", "Locker", Hex("5C7A8C")),
                Key = b.LitMaterial("Materials", "Key", Hex("FFD23F"), true),
                Guard = b.LitMaterial("Materials", "Guard", Hex("C04848")),
                Vision = b.SeeThroughMaterial("Materials", "Vision", new Color(1f, 1f, 1f, 0.3f))
            };

            BuildPrefabs(b, k);
            k.Systems = StealthSystems(b, k);

            // Part 7 — Menu first, so it's first in the Scene List
            b.MenuScene(MenuName, "Night Shift", "Sneak through a guarded office at night, find three keys and slip out unseen.", LevelName,
                new VariableBase[] { k.Keys, k.Battery, k.Message }, Night, HowToPlay);

            BuildLevel(b, k);
        }

        // ---------------------------------------------------------------- Parts 3–5 — Prefabs

        static void BuildPrefabs(SampleBuild b, Kit k)
        {
            // The player
            GameObject player = new GameObject("Player") { tag = "Player" };
            Shape(PrimitiveType.Capsule, "Body", new Vector3(0f, 1f, 0f), Vector3.one, k.Locker, player.transform, false);
            Child(player, "Camera Target").transform.localPosition = new Vector3(0f, 1.4f, 0f);
            Shape(PrimitiveType.Cube, "Nose", new Vector3(0f, 1.5f, 0.45f), new Vector3(0.3f, 0.2f, 0.4f), k.Wall, player.transform, false);

            PlayerController3D controller = player.AddComponent<PlayerController3D>();
            CharacterController body = player.GetComponent<CharacterController>();
            body.height = 2f;
            body.radius = 0.4f;
            body.center = new Vector3(0f, 1f, 0f);
            Set(controller, "input", b.Input);
            SetEnum(controller, "facing", "FaceMovement");
            Set(controller, "walkSpeed", 4f);
            Set(controller, "sprintSpeed", 6.5f);
            Set(controller, "canCrouch", true);
            Set(controller, "crouchSpeed", 1.8f);
            Set(controller, "crouchHeight", 1f);
            Set(controller, "canJump", false);

            GameObject torch = Child(player, "Flashlight");
            torch.transform.localPosition = new Vector3(0f, 1.4f, 0.3f);
            Light spot = torch.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 15f;
            spot.spotAngle = 45f;
            spot.intensity = 8f;
            spot.shadows = LightShadows.Soft;
            Flashlight3D flashlight = torch.AddComponent<Flashlight3D>();
            Set(flashlight, "input", b.Input);
            Set(flashlight, "batterySeconds", 120f);
            Set(flashlight, "batteryPercent", k.Battery);
            Set(flashlight, "flickerBelow", 20f);
            Set(flashlight, "onAtStart", false);

            Set(player.AddComponent<StealthTarget3D>(), "flashlight", flashlight);
            k.Player = b.Prefab(player);

            // Part 4 — Guard
            GameObject guard = new GameObject("Guard");
            Shape(PrimitiveType.Capsule, "Body", new Vector3(0f, 1f, 0f), Vector3.one, k.Guard, guard.transform, false);
            Shape(PrimitiveType.Cube, "Visor", new Vector3(0f, 1.6f, 0.35f), new Vector3(0.6f, 0.15f, 0.2f), k.Floor, guard.transform, false);

            NavMeshAgent agent = guard.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.3f;

            Guard3D guardScript = guard.AddComponent<Guard3D>();
            Set(guardScript, "patrolSpeed", 2f);
            Set(guardScript, "waitAtPoint", 1.5f);
            Set(guardScript, "viewDistance", 11f);
            Set(guardScript, "viewAngle", 90f);
            Set(guardScript, "eyeHeight", 1.6f);
            Set(guardScript, "timeToSpot", 1f);
            Set(guardScript, "flashlightSightBonus", 1.5f);
            Set(guardScript, "hearingRange", 10f);
            Set(guardScript, "investigateSpeed", 3f);
            Set(guardScript, "lookAroundTime", 4f);
            Set(guardScript, "chaseSpeed", 4.5f);
            Set(guardScript, "loseTrackAfter", 3f);
            Set(guardScript, "calmDownRate", 0.15f);
            Set(guardScript, "catchDistance", 1.2f);
            Set(guardScript, "raiseOnCaught", k.Caught);

            GameObject cone = Child(guard, "Vision Cone");
            cone.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            VisionCone3D vision = cone.AddComponent<VisionCone3D>();
            cone.GetComponent<MeshRenderer>().sharedMaterial = k.Vision;
            cone.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Set(vision, "guard", guardScript);
            Set(vision, "rays", 24);
            k.GuardPrefab = b.Prefab(guard);

            // Part 5 — Key
            GameObject key = Shape(PrimitiveType.Cube, "Key", Vector3.zero, new Vector3(0.3f, 0.3f, 0.3f), k.Key);
            // A pickup zone bigger than the key (1.5 units across), so a key on a table can be reached.
            BoxCollider keyZone = (BoxCollider)key.GetComponent<Collider>();
            keyZone.isTrigger = true;
            keyZone.size = new Vector3(5f, 5f, 5f);
            Set(key.AddComponent<Collectible>(), "addTo", k.Keys);
            Set(key.AddComponent<Spin3D>(), "spin", new Vector3(0f, 120f, 0f));
            k.KeyPrefab = b.Prefab(key);

            // Locker: an open-fronted box (the front faces +Z), with a hiding spot inside.
            GameObject locker = new GameObject("Locker");
            Shape(PrimitiveType.Cube, "Back", new Vector3(0f, 1.25f, -0.55f), new Vector3(1.4f, 2.5f, 0.1f), k.Locker, locker.transform);
            Shape(PrimitiveType.Cube, "Left", new Vector3(-0.65f, 1.25f, 0f), new Vector3(0.1f, 2.5f, 1.1f), k.Locker, locker.transform);
            Shape(PrimitiveType.Cube, "Right", new Vector3(0.65f, 1.25f, 0f), new Vector3(0.1f, 2.5f, 1.1f), k.Locker, locker.transform);
            Shape(PrimitiveType.Cube, "Top", new Vector3(0f, 2.55f, 0f), new Vector3(1.4f, 0.1f, 1.2f), k.Locker, locker.transform);
            GameObject inside = Child(locker, "Hiding Spot");
            BoxCollider insideZone = inside.AddComponent<BoxCollider>();
            insideZone.isTrigger = true;
            insideZone.center = new Vector3(0f, 1.2f, 0f);
            insideZone.size = new Vector3(1.1f, 2.4f, 0.9f);
            Set(inside.AddComponent<HidingSpot3D>(), "needsCrouch", false);
            k.LockerPrefab = b.Prefab(locker);
        }

        // ---------------------------------------------------------------- Part 6 — Stealth Systems

        static GameObject StealthSystems(SampleBuild b, Kit k)
        {
            GameObject root = new GameObject("Stealth Systems");

            GameObject hud = Child(root, "HUD");
            b.Document(hud, KitFolder + "/UI/StealthHUD.uxml", 0);
            Set(hud.AddComponent<UIStealthHUD>(), "batteryPercent", k.Battery);
            Bind(hud, ("objective-label", k.Keys, "Keys: {0} / 3"), ("message-label", k.Message, "{0}"));

            GameObject alert = Child(root, "Alert Audio");
            AudioSource heartbeat = alert.AddComponent<AudioSource>();
            heartbeat.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(KitFolder + "/Audio/Heartbeat.wav");
            heartbeat.loop = true;
            heartbeat.playOnAwake = true;
            heartbeat.spatialBlend = 0f;
            heartbeat.volume = 0f;
            AlertAudio alertAudio = alert.AddComponent<AlertAudio>();
            Set(alertAudio, "calmVolume", 0f);
            Set(alertAudio, "alertVolume", 0.8f);
            Set(alertAudio, "chasePitch", 1.25f);

            Resetter(Child(root, "Reset"), true, k.Keys, k.Battery, k.Message);

            CursorLock3D cursorLock = Child(root, "Cursor Lock").AddComponent<CursorLock3D>();
            Set(cursorLock, "input", b.Input);

            Systems s = b.StandardSystems(root, MenuName);
            b.EndScreen(s, "Escaped", true, k.Escaped, null, null, "You Escaped", "Out into the night, keys in hand.", Call(cursorLock.Unlock));
            b.EndScreen(s, "Caught", false, k.Caught, null, null, "Caught!", "A guard got you.", Call(cursorLock.Unlock));
            return b.Prefab(root);
        }

        // ---------------------------------------------------------------- Part 2 — The building

        /// <summary>A wall along X (at z) or along Z (at x), from one end to the other, leaving 2-unit door gaps centred on the given points.</summary>
        static void Wall(Kit k, string wallName, bool alongX, float at, float from, float to, params float[] doors)
        {
            List<float> cuts = new List<float> { from };
            foreach (float door in doors.OrderBy(d => d))
            {
                cuts.Add(door - 1f);
                cuts.Add(door + 1f);
            }
            cuts.Add(to);

            for (int i = 0; i + 1 < cuts.Count; i += 2)
            {
                float start = cuts[i];
                float end = cuts[i + 1];
                float length = end - start;

                if (length <= 0.01f)
                {
                    continue;
                }

                float middle = (start + end) / 2f;
                Vector3 position = alongX ? new Vector3(middle, WallHeight / 2f, at) : new Vector3(at, WallHeight / 2f, middle);
                Vector3 size = alongX ? new Vector3(length, WallHeight, WallThickness) : new Vector3(WallThickness, WallHeight, length);
                Shape(PrimitiveType.Cube, wallName, position, size, k.Wall, k.Level);
            }
        }

        /// <summary>A desk with room to crouch under it, and a hiding spot there.</summary>
        static void Desk(Kit k, Vector3 position)
        {
            GameObject desk = new GameObject("Desk");
            desk.transform.SetParent(k.Level, false);
            desk.transform.localPosition = position;
            Shape(PrimitiveType.Cube, "Top", new Vector3(0f, 1.2f, 0f), new Vector3(2f, 0.1f, 1f), k.Furniture, desk.transform);
            Shape(PrimitiveType.Cube, "Left", new Vector3(-0.95f, 0.575f, 0f), new Vector3(0.1f, 1.15f, 1f), k.Furniture, desk.transform);
            Shape(PrimitiveType.Cube, "Right", new Vector3(0.95f, 0.575f, 0f), new Vector3(0.1f, 1.15f, 1f), k.Furniture, desk.transform);

            GameObject under = Child(desk, "Hiding Spot");
            BoxCollider zone = under.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 0.55f, 0f);
            zone.size = new Vector3(1.8f, 1.1f, 1f);
            Set(under.AddComponent<HidingSpot3D>(), "needsCrouch", true);
        }

        static void Lamp(string lampName, Vector3 position)
        {
            Light lamp = new GameObject(lampName).AddComponent<Light>();
            lamp.transform.position = position;
            lamp.type = LightType.Point;
            lamp.intensity = 2f;
            lamp.range = 6f;
            lamp.color = new Color(1f, 0.8f, 0.55f);
        }

        static Transform Point(Transform routes, string pointName, float x, float z)
        {
            GameObject point = new GameObject(pointName);
            point.transform.SetParent(routes, false);
            point.transform.position = new Vector3(x, 0f, z);
            return point.transform;
        }

        static void BuildLevel(SampleBuild b, Kit k)
        {
            Scene scene = NewScene();
            Camera camera = Camera3D(new Vector3(0f, 6f, -12f), Quaternion.Euler(25f, 0f, 0f), Night);
            camera.gameObject.AddComponent<CinemachineBrain>();
            Sun(Hex("4060A0"), 0.15f, new Vector3(50f, -30f, 0f));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.08f, 0.12f);

            // The building: 30 × 20, a corridor along the middle (z −2 to 2). North of it the
            // Lobby, Office and Storage; south of it the Break Room and Security.
            k.Level = new GameObject("Level").transform;
            Shape(PrimitiveType.Cube, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(30f, 0.2f, 20f), k.Floor, k.Level);
            Shape(PrimitiveType.Cube, "Exit Step", new Vector3(12f, -0.1f, -11.5f), new Vector3(3f, 0.2f, 3f), k.Floor, k.Level);

            Wall(k, "Outer Wall", true, 10f, -15f, 15f);
            Wall(k, "Outer Wall", true, -10f, -15f, 15f, 12f);              // the exit gap, in Security
            Wall(k, "Outer Wall", false, -15f, -10f, 10f);
            Wall(k, "Outer Wall", false, 15f, -10f, 10f);
            Wall(k, "Corridor Wall", true, 2f, -15f, 15f, -10f, 0f, 10f);   // doors to the Lobby, Office, Storage
            Wall(k, "Corridor Wall", true, -2f, -15f, 15f, -7.5f, 7.5f);    // doors to the Break Room, Security
            Wall(k, "Room Wall", false, -5f, 2f, 10f);
            Wall(k, "Room Wall", false, 5f, 2f, 10f);
            Wall(k, "Room Wall", false, 0f, -10f, -2f);

            // Office desks, Storage shelves, the Break Room table.
            Desk(k, new Vector3(-2.5f, 0f, 6f));
            Desk(k, new Vector3(2.5f, 0f, 6f));
            Shape(PrimitiveType.Cube, "Shelf", new Vector3(9f, 1.25f, 5.5f), new Vector3(3f, 2.5f, 0.6f), k.Furniture, k.Level);
            Shape(PrimitiveType.Cube, "Shelf", new Vector3(11.5f, 1.25f, 7.5f), new Vector3(3f, 2.5f, 0.6f), k.Furniture, k.Level);
            Shape(PrimitiveType.Cube, "Table", new Vector3(-7.5f, 0.4f, -6f), new Vector3(2f, 0.8f, 1.2f), k.Furniture, k.Level);

            // Lockers: one in Storage, two in the corridor.
            GameObject storageLocker = Place(k.LockerPrefab, new Vector3(14.2f, 0f, 4f), k.Level);
            storageLocker.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            GameObject corridorLocker1 = Place(k.LockerPrefab, new Vector3(-4f, 0f, -1.3f), k.Level);
            GameObject corridorLocker2 = Place(k.LockerPrefab, new Vector3(4f, 0f, 1.3f), k.Level);
            corridorLocker2.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            // The exit: a door in the gap, and the way out behind it.
            GameObject exitDoor = Shape(PrimitiveType.Cube, "Exit Door", new Vector3(12f, WallHeight / 2f, -10f), new Vector3(2f, WallHeight, WallThickness), k.Guard);

            // Bake the floor plan for the guards (Level and everything in it).
            NavMeshSurface surface = k.Level.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();
            if (surface.navMeshData != null)
            {
                surface.navMeshData.name = "NightShift NavMesh";
                AssetDatabase.CreateAsset(surface.navMeshData, $"{b.Folder("Scenes")}/NightShift NavMesh.asset");
            }
            else
            {
                Debug.LogWarning("Stealth sample: the NavMesh didn't bake. Select Level and click Bake on its NavMesh Surface.");
            }

            Lamp("Lobby Lamp", new Vector3(-10f, 2.6f, 6f));
            Lamp("Break Room Lamp", new Vector3(-7.5f, 2.6f, -6f));
            Lamp("Security Lamp", new Vector3(7.5f, 2.6f, -6f));

            GameObject exitZone = new GameObject("Exit Zone");
            exitZone.transform.position = new Vector3(12f, 1f, -11.5f);
            BoxCollider exitBox = exitZone.AddComponent<BoxCollider>();
            exitBox.isTrigger = true;
            exitBox.size = new Vector3(2.5f, 2f, 2f);
            TriggerZone escape = exitZone.AddComponent<TriggerZone>();
            Set(escape, "onlyOnce", true);
            Set(escape, "raiseOnEnter", k.Escaped);

            GameObject exitLogic = new GameObject("Exit Logic");
            Condition(exitLogic, k.Keys, "AtLeast", 3, k.ExitOpened);
            Listen(exitLogic, k.ExitOpened, Call(exitDoor.SetActive, false), Call(k.Message.SetValue, "The exit is open. Get to Security!"));

            // Keys and the battery.
            Transform pickups = new GameObject("Pickups").transform;
            Place(k.KeyPrefab, new Vector3(0f, 0.6f, 6f), pickups).name = "Key 1";
            Place(k.KeyPrefab, new Vector3(13.5f, 0.6f, 9f), pickups).name = "Key 2";
            Place(k.KeyPrefab, new Vector3(-7.5f, 1.1f, -5.65f), pickups).name = "Key 3";   // on the table, near the side facing the door

            // The player, at the Lobby's far end.
            GameObject player = Place(k.Player, new Vector3(-12.5f, 0.05f, 8f));
            Flashlight3D flashlight = player.GetComponentInChildren<Flashlight3D>();

            GameObject battery = Shape(PrimitiveType.Cube, "Battery", new Vector3(6.5f, 0.3f, 9f), new Vector3(0.25f, 0.4f, 0.25f), k.Locker, pickups);
            battery.GetComponent<Collider>().isTrigger = true;
            Collectible batteryPickup = battery.AddComponent<Collectible>();
            On(batteryPickup, "onCollected", Call(flashlight.AddBattery, 50f));

            // Part 4 — Guards and their routes (not children of the guards).
            Transform routes = new GameObject("Routes").transform;
            GameObject guardA = Place(k.GuardPrefab, new Vector3(-10f, 0f, 0f));
            guardA.name = "Guard A";
            Guard3D a = guardA.GetComponent<Guard3D>();
            Set(a, "patrolPoints", new List<Transform>
            {
                Point(routes, "A1", -10f, 0f), Point(routes, "A2", 0f, 0.5f), Point(routes, "A3", 0f, 8.5f), Point(routes, "A4", 8f, -0.5f)
            });
            KeepChanges(a);

            GameObject guardB = Place(k.GuardPrefab, new Vector3(10f, 0f, 4f));
            guardB.name = "Guard B";
            Guard3D bGuard = guardB.GetComponent<Guard3D>();
            Set(bGuard, "patrolPoints", new List<Transform>
            {
                Point(routes, "B1", 10f, 4f), Point(routes, "B2", 13.5f, 8.5f), Point(routes, "B3", -7.5f, -2.6f), Point(routes, "B4", -7.5f, -8f)
            });
            Set(bGuard, "waitAtPoint", 3f);
            KeepChanges(bGuard);

            // The orbit camera, closer in than the 3D Platformer's.
            OrbitCamera(player.transform.Find("Camera Target"), 5f);

            Place(k.Systems, Vector3.zero);
            b.SaveScene(scene, LevelName);
        }

        // ---------------------------------------------------------------- The orbit camera (as in the 3D Platformer sample)

        static void OrbitCamera(Transform target, float radius)
        {
            GameObject go = new GameObject("Orbit Camera");
            CinemachineCamera cam = go.AddComponent<CinemachineCamera>();
            cam.Follow = target;
            cam.LookAt = target;

            // The target is the player's Camera Target, at head height, so the camera's
            // obstacle checks start above the ground the player stands on.
            CinemachineOrbitalFollow orbit = go.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            orbit.Radius = radius;
            orbit.TargetOffset = Vector3.zero;
            orbit.TrackerSettings.PositionDamping = new Vector3(0.3f, 0.3f, 0.3f);
            orbit.VerticalAxis.Range = new Vector2(-10f, 60f);
            orbit.VerticalAxis.Center = 25f;
            orbit.VerticalAxis.Value = 25f;

            CinemachineRotationComposer composer = go.AddComponent<CinemachineRotationComposer>();
            composer.TargetOffset = Vector3.zero;
            composer.Damping = new Vector2(0.2f, 0.2f);

            // Moves the camera in front of scenery that hides the player, ignoring the player
            // themselves, and not for things that only pass between them for a moment.
            CinemachineDeoccluder deoccluder = go.AddComponent<CinemachineDeoccluder>();
            deoccluder.IgnoreTag = "Player";
            deoccluder.MinimumDistanceFromTarget = 1f;
            CinemachineDeoccluder.ObstacleAvoidance avoid = deoccluder.AvoidObstacles;
            avoid.Enabled = true;
            avoid.CameraRadius = 0.2f;
            avoid.MinimumOcclusionTime = 0.2f;
            avoid.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            avoid.MaximumEffort = 4;
            avoid.Damping = 0.6f;
            avoid.DampingWhenOccluded = 0.2f;
            deoccluder.AvoidObstacles = avoid;
            AddLookInput(go);
        }

        static void AddLookInput(GameObject go)
        {
            const string manual = "Select Orbit Camera and check the 3D Platformer guide, Part 4, step 4 (Look Orbit X and Y → Player/Look).";

            try
            {
                Type controllerType = typeof(CinemachineCamera).Assembly.GetType("Unity.Cinemachine.CinemachineInputAxisController");

                if (controllerType == null)
                {
                    Debug.LogWarning("Stealth sample: couldn't add Cinemachine Input Axis Controller. " + manual);
                    return;
                }

                Component controller = go.AddComponent(controllerType);
                Object look = AssetDatabase.LoadAllAssetsAtPath("Assets/_Course/Core/Input/InputSystem_Actions.inputactions")
                    .FirstOrDefault(a => a != null && a.GetType().Name == "InputActionReference" && a.name.EndsWith("/Look"));

                MethodInfo sync = controllerType.GetMethod("SynchronizeControllers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                sync?.Invoke(controller, null);

                // Controllers is a property (one entry per camera axis: Look Orbit X, Look Orbit Y, Orbit Scale).
                IList list = controllerType.GetProperty("Controllers", BindingFlags.Instance | BindingFlags.Public)?.GetValue(controller) as IList;

                if (look == null || list == null || list.Count == 0)
                {
                    Debug.LogWarning("Couldn't connect the orbit camera to the mouse. " + manual);
                    return;
                }

                int connected = 0;

                foreach (object item in list)
                {
                    string axisName = item.GetType().GetField("Name")?.GetValue(item) as string ?? "";
                    bool x = axisName.EndsWith("X");
                    bool y = axisName.EndsWith("Y");

                    if (!x && !y)
                    {
                        continue;
                    }

                    object reader = item.GetType().GetField("Input")?.GetValue(item);
                    FieldInfo actionField = reader?.GetType().GetField("InputAction");
                    FieldInfo gainField = reader?.GetType().GetField("Gain");

                    if (actionField == null)
                    {
                        continue;
                    }

                    actionField.SetValue(reader, look);
                    // Moving the mouse up tilts the view up.
                    gainField?.SetValue(reader, x ? 1f : -1f);
                    connected++;
                }

                if (connected < 2)
                {
                    Debug.LogWarning("Connected only part of the orbit camera to the mouse. " + manual);
                }

                EditorUtility.SetDirty(controller);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Stealth sample: couldn't set up the orbit camera's input ({e.Message}). " + manual);
            }
        }
    }
}
