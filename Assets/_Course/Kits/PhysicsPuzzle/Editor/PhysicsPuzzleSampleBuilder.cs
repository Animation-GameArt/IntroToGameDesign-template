using CollinGame.Core;
using CollinGame.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.PhysicsPuzzle.EditorTools
{
    /// <summary>
    /// Builds the Physics Puzzle Kit's sample, three levels that each teach one idea,
    /// following "Physics Puzzle Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Physics Puzzle Sample.
    /// </summary>
    public static class PhysicsPuzzleSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/PhysicsPuzzle";
        const string Pack = "kenney_physics-assets";
        const string MenuName = "Puzzle_Menu";
        const string WinName = "Puzzle_Win";
        const float GroundTop = -4f;

        static readonly Color Sky = new Color(0.55f, 0.75f, 0.9f);

        const string HowToPlay =
            "Knock out every green alien in each level.\n\n" +
            "Press near the slingshot, drag back and let go. The dots show where the ball will fly. (Keyboard: hold Enter, aim with WASD or the arrow keys, let go of Enter.)\n\n" +
            "Glass breaks easily, wood needs a good hit, stone hardly ever breaks. Unused shots are worth bonus points.\n\n" +
            "In the last level there's no slingshot: swipe across a rope to cut it.\n\n" +
            "Esc pauses.";

        [MenuItem("Collin Game/Kit Samples/Build Physics Puzzle Sample")]
        static void Build()
        {
            Run("Physics Puzzle", KitFolder, new[] { "Data", "Prefabs", "Scenes", "Materials" },
                new[] { Kenney + Pack }, BuildSample);
        }

        static Sprite Art(string path) => KenneySprite(Pack, "PNG/" + path);

        sealed class Kit
        {
            public IntVariable ShotsLeft, TargetsLeft, Score, Best;
            public VoidEventChannel ShotFinished, Won, Lost;
            public PhysicsMaterial2D Rough, Slippery, Bouncy;
            public Sprite Square, Circle;
            public GameObject Ball, Wood, Glass, Stone, Target, Launcher, Systems;
        }

        static void BuildSample(SampleBuild b)
        {
            Kit k = new Kit
            {
                // Part 1 — Data
                ShotsLeft = b.Int("ShotsLeft", 3),
                TargetsLeft = b.Int("TargetsLeft"),
                Score = b.Int("PuzzleScore"),
                Best = b.Int("BestPuzzleScore"),
                ShotFinished = b.Channel("ShotFinished"),
                Won = b.Channel("PuzzleWon"),
                Lost = b.Channel("PuzzleLost"),
                Rough = b.PhysicsMaterial("Materials", "Rough", 0.8f, 0f),
                Slippery = b.PhysicsMaterial("Materials", "Slippery", 0.05f, 0f),
                Bouncy = b.PhysicsMaterial("Materials", "Bouncy", 0.3f, 0.85f),
                Square = b.WhiteSquare(),
                Circle = b.WhiteCircle()
            };

            // Part 2 — Prefabs
            GameObject ball = SpriteObject("Ball", k.Circle, 5);
            ball.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.3f, 0.35f);
            ball.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            ball.AddComponent<CircleCollider2D>().sharedMaterial = k.Rough;
            Rigidbody2D ballBody = Body2D(ball, RigidbodyType2D.Dynamic, 1f);
            ballBody.mass = 2f;
            ballBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            k.Ball = b.Prefab(ball);

            k.Wood = Block(b, k, "Wood Block", "Wood elements/elementWood002", 1f, 6f, 100);
            k.Glass = Block(b, k, "Glass Block", "Glass elements/elementGlass001", 0.6f, 3f, 50);
            k.Stone = Block(b, k, "Stone Block", "Stone elements/elementStone000", 3f, 11f, 200);

            GameObject target = SpriteObject("Target", Art("Aliens/alienGreen_round"), 3);
            target.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            target.AddComponent<CircleCollider2D>();
            Body2D(target, RigidbodyType2D.Dynamic, 1f).mass = 0.8f;
            Breakable2D targetBreak = target.AddComponent<Breakable2D>();
            Set(targetBreak, "breakSpeed", 3f);
            Set(targetBreak, "addTo", k.Score);
            Set(targetBreak, "amount", 500);
            Set(target.AddComponent<PuzzleTarget2D>(), "targetsLeft", k.TargetsLeft);
            k.Target = b.Prefab(target);

            GameObject launcher = new GameObject("Launcher");
            GameObject post = SpriteObject("Post", Art("Wood elements/elementWood019"), 1);
            post.transform.SetParent(launcher.transform, false);
            post.transform.localScale = new Vector3(0.5f, 0.6f, 1f);
            post.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            Launcher2D launcherScript = launcher.AddComponent<Launcher2D>();
            Set(launcherScript, "input", b.Input);
            Set(launcherScript, "projectilePrefab", k.Ball.GetComponent<Rigidbody2D>());
            Set(launcherScript, "maxPull", 2.5f);
            Set(launcherScript, "power", 7f);
            Set(launcherScript, "shotsLeft", k.ShotsLeft);
            Set(launcherScript, "dotSprite", k.Circle);
            Set(launcherScript, "raiseOnShotFinished", k.ShotFinished);
            Set(launcherScript, "removeProjectileAfterShot", true);
            k.Launcher = b.Prefab(launcher);

            k.Systems = LevelSystems(b, k);

            // Part 4 — Menu first, so it's first in the Scene List
            b.MenuScene(MenuName, "Knock Down", "Launch, smash and cut your way through three physics puzzles.", "Puzzle_Level1",
                new VariableBase[] { k.Score }, Sky, HowToPlay);

            // Part 3 — Levels
            Level1(b, k);
            Level2(b, k);
            Level3(b, k);

            b.WinScene(WinName, "All Puzzles Solved!", "", "Puzzle_Level1", MenuName, new VariableBase[] { k.Score }, Sky,
                ("score-label", k.Score, "Score: {0}"));
        }

        static GameObject Block(SampleBuild b, Kit k, string blockName, string sprite, float mass, float breakSpeed, int amount)
        {
            GameObject block = SpriteObject(blockName, Art(sprite), 2);
            block.AddComponent<BoxCollider2D>();
            Body2D(block, RigidbodyType2D.Dynamic, 1f).mass = mass;
            Breakable2D breakable = block.AddComponent<Breakable2D>();
            Set(breakable, "breakSpeed", breakSpeed);
            Set(breakable, "addTo", k.Score);
            Set(breakable, "amount", amount);
            return b.Prefab(block);
        }

        // ---------------------------------------------------------------- Level Systems

        static GameObject LevelSystems(SampleBuild b, Kit k)
        {
            GameObject root = new GameObject("Level Systems");
            Systems s = b.StandardSystems(root, MenuName);

            GameObject rules = Child(root, "Rules");
            PuzzleRules puzzleRules = rules.AddComponent<PuzzleRules>();
            Set(puzzleRules, "targetsLeft", k.TargetsLeft);
            Set(puzzleRules, "shotsLeft", k.ShotsLeft);
            Set(puzzleRules, "startingShots", 3);
            Set(puzzleRules, "shotFinished", k.ShotFinished);
            Set(puzzleRules, "raiseOnWon", k.Won);
            Set(puzzleRules, "raiseOnLost", k.Lost);
            Set(puzzleRules, "score", k.Score);
            HighScore best = rules.AddComponent<HighScore>();
            Set(best, "score", k.Score);
            Set(best, "best", k.Best);
            Set(best, "saveName", "Puzzle");

            Child(root, "Cutter").AddComponent<Cutter2D>();

            GameObject hud = Child(root, "HUD");
            b.Document(hud, KitFolder + "/UI/PuzzleHUD.uxml", 0);
            Bind(hud, ("shots-label", k.ShotsLeft, "Shots: {0}"), ("targets-label", k.TargetsLeft, "Targets: {0}"), ("score-label", k.Score, "Score: {0}"));

            GameObject complete = Child(root, "Level Complete");
            b.Document(complete, "Assets/_Course/Kits/Platformer/UI/LevelComplete.uxml", 12);
            UIScreen completeScreen = MakeScreen(complete, true, "next-button");
            Bind(complete, ("coins-label", k.Score, "Score: {0}"));
            Listen(complete, k.Won, Call(completeScreen.Show), Call(b.Input.DisablePlayerInput));
            UIButtonEvents buttons = complete.AddComponent<UIButtonEvents>();
            AddButton(buttons, "next-button", Call(s.SceneChanger.LoadNextScene));
            AddButton(buttons, "menu-button", Call(s.ToMenu.Load));

            b.EndScreen(s, "Game Over", false, k.Lost, null, null, "Out of Shots", "Targets are still standing.");
            return b.Prefab(root);
        }

        // ---------------------------------------------------------------- Level parts

        /// <summary>A new level scene: camera, sky, ground and Level Systems with its starting shots.</summary>
        static Scene NewLevel(Kit k, int startingShots)
        {
            Scene scene = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, Sky);

            GameObject background = SpriteObject("Background", Art("Backgrounds/blue_grass"), -10);
            background.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            background.transform.position = new Vector3(0f, 1.5f, 0f);

            GameObject ground = SpriteObject("Ground", k.Square, 0);
            ground.GetComponent<SpriteRenderer>().color = new Color(0.35f, 0.55f, 0.25f);
            ground.transform.position = new Vector3(0f, GroundTop - 1f, 0f);
            ground.transform.localScale = new Vector3(40f, 2f, 1f);
            ground.AddComponent<BoxCollider2D>().sharedMaterial = k.Rough;

            GameObject systems = Place(k.Systems, Vector3.zero);
            PuzzleRules rules = systems.GetComponentInChildren<PuzzleRules>();
            Set(rules, "startingShots", startingShots);
            KeepChanges(rules);
            return scene;
        }

        static GameObject Put(GameObject prefab, float x, float y, float rotation = 0f, float scaleX = 1f, float scaleY = 1f)
        {
            GameObject placed = Place(prefab, new Vector3(x, y, 0f));
            placed.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            placed.transform.localScale = Vector3.Scale(placed.transform.localScale, new Vector3(scaleX, scaleY, 1f));
            return placed;
        }

        /// <summary>
        /// Two upright blocks (each 1 wide, 2 tall) with one block across the top, centred
        /// on x, standing on the ground, and a target on top.
        /// </summary>
        static void Tower(Kit k, GameObject block, float x)
        {
            Put(block, x - 0.5f, GroundTop + 1f, 90f);
            Put(block, x + 0.5f, GroundTop + 1f, 90f);
            Put(block, x, GroundTop + 2.5f);
            Put(k.Target, x, GroundTop + 3.42f);
        }

        static void Rope(Kit k, Vector2 anchorPosition, Rigidbody2D hanging)
        {
            GameObject anchor = SpriteObject("Rope Anchor", k.Square, 4);
            anchor.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.3f, 0.3f);
            anchor.transform.position = anchorPosition;
            anchor.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            Rope2D rope = anchor.AddComponent<Rope2D>();
            Set(rope, "hangingBody", hanging);
            Set(rope, "linkCount", 10);
            Set(rope, "linkSprite", k.Square);
        }

        // ---------------------------------------------------------------- Part 3 — Levels

        // Launching: one wood tower with a target on top.
        static void Level1(SampleBuild b, Kit k)
        {
            Scene scene = NewLevel(k, 3);
            Place(k.Launcher, new Vector3(-6f, GroundTop + 1.8f, 0f));
            Tower(k, k.Wood, 5f);
            b.SaveScene(scene, "Puzzle_Level1");
        }

        // Materials and break speeds: glass in front, stone behind, a bouncy wall at the back.
        static void Level2(SampleBuild b, Kit k)
        {
            Scene scene = NewLevel(k, 3);
            Place(k.Launcher, new Vector3(-6f, GroundTop + 1.8f, 0f));
            Tower(k, k.Glass, 2f);
            Tower(k, k.Stone, 5.5f);

            GameObject wall = SpriteObject("Bouncy Wall", k.Square, 1);
            wall.GetComponent<SpriteRenderer>().color = new Color(0.95f, 0.45f, 0.75f);
            wall.transform.position = new Vector3(8.4f, GroundTop + 3f, 0f);
            wall.transform.localScale = new Vector3(0.5f, 6f, 1f);
            wall.AddComponent<BoxCollider2D>().sharedMaterial = k.Bouncy;

            b.SaveScene(scene, "Puzzle_Level2");
        }

        // Cutting ropes and joints: a stone over a wood shelter, and a ball over a seesaw.
        static void Level3(SampleBuild b, Kit k)
        {
            Scene scene = NewLevel(k, 1);

            // The shelter: two posts and a long roof, with a target inside.
            float shelterX = 3.5f;
            Put(k.Wood, shelterX - 1.1f, GroundTop + 1f, 90f);
            Put(k.Wood, shelterX + 1.1f, GroundTop + 1f, 90f);
            Put(k.Wood, shelterX, GroundTop + 2.5f, 0f, 1.6f);
            Put(k.Target, shelterX, GroundTop + 0.42f);

            GameObject stone = Put(k.Stone, shelterX, 2.5f);
            Rope(k, new Vector2(shelterX, 4.6f), stone.GetComponent<Rigidbody2D>());

            // The seesaw: a plank on a hinge, resting tipped to the left, with a target on its low end.
            float seesawX = -4f;
            GameObject fulcrum = SpriteObject("Fulcrum", Art("Stone elements/elementStone001"), 1);
            fulcrum.transform.position = new Vector3(seesawX, GroundTop + 0.5f, 0f);
            fulcrum.AddComponent<BoxCollider2D>();

            GameObject plank = SpriteObject("Seesaw", Art("Wood elements/elementWood012"), 2);
            plank.transform.position = new Vector3(seesawX, GroundTop + 1.45f, 0f);
            plank.transform.rotation = Quaternion.Euler(0f, 0f, 15f);
            plank.transform.localScale = new Vector3(1f, 0.4f, 1f);
            plank.AddComponent<BoxCollider2D>();
            Body2D(plank, RigidbodyType2D.Dynamic, 1f).mass = 1f;
            HingeJoint2D hinge = plank.AddComponent<HingeJoint2D>();
            hinge.anchor = Vector2.zero;
            hinge.useLimits = true;
            // Limits count from the starting tilt: it can swing 30 degrees the other way, and no further down.
            hinge.limits = new JointAngleLimits2D { min = -30f, max = 0f };

            Put(k.Target, seesawX - 1.3f, GroundTop + 1.75f);

            GameObject weight = Put(k.Ball, seesawX + 1.3f, 2f);
            Rope(k, new Vector2(seesawX + 1.3f, 4.6f), weight.GetComponent<Rigidbody2D>());

            b.SaveScene(scene, "Puzzle_Level3");
        }
    }
}
