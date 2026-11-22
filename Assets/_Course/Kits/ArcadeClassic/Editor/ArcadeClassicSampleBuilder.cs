using CollinGame.Core;
using CollinGame.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.ArcadeClassic.EditorTools
{
    /// <summary>
    /// Builds the Arcade Classic Kit's three sample games (Breakout, Pong and
    /// Asteroids) and their menu, following "Arcade Classic Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Arcade Classic Sample.
    /// </summary>
    public static class ArcadeClassicSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/ArcadeClassic";
        const string KitUI = KitFolder + "/UI/";
        const string Puzzle = "kenney_puzzle-pack-2";
        const string Space = "kenney_space-shooter-remastered";
        const string MenuName = "Arcade_Menu";
        const string Breakout = "Arcade_Breakout";
        const string Pong = "Arcade_Pong";
        const string Asteroids = "Arcade_Asteroids";

        const string HowToPlay =
            "Three classic arcade games.\n\n" +
            "BREAKOUT: move the paddle with the mouse, A/D or the arrow keys. Space serves the ball. Break every brick; grey bricks take three hits. Missing the ball costs a life.\n\n" +
            "PONG: move your paddle (left) with W/S or the arrow keys. Get the ball past the computer. First to 5 wins.\n\n" +
            "ASTEROIDS: A/D or the arrow keys turn, W or Up thrusts, Space shoots. Large rocks split into smaller ones. Clear every piece; touching one costs a life.\n\n" +
            "Esc pauses.";

        [MenuItem("Collin Game/Kit Samples/Build Arcade Classic Sample")]
        static void Build()
        {
            Run("Arcade Classic", KitFolder, new[] { "Data", "Prefabs", "Scenes" },
                new[] { Kenney + Puzzle, Kenney + Space }, BuildSample);
        }

        static Sprite PuzzleSprite(string path) => KenneySprite(Puzzle, "PNG/" + path);
        static Sprite SpaceSprite(string path) => KenneySprite(Space, "PNG/" + path);

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out Color c) ? c : Color.white;

        sealed class Data
        {
            public IntVariable Score, BestBreakout, BestAsteroids, Lives, BricksLeft, AsteroidsLeft, LeftScore, RightScore;
            public VoidEventChannel Cleared, GameOver;
        }

        static void BuildSample(SampleBuild b)
        {
            EnsureTag("Enemy");

            // Part 1 — Data
            Data d = new Data
            {
                Score = b.Int("ArcadeScore"),
                BestBreakout = b.Int("BestBreakout"),
                BestAsteroids = b.Int("BestAsteroids"),
                Lives = b.Int("Lives", 3),
                BricksLeft = b.Int("BricksLeft"),
                AsteroidsLeft = b.Int("AsteroidsLeft"),
                LeftScore = b.Int("LeftScore"),
                RightScore = b.Int("RightScore"),
                Cleared = b.Channel("LevelCleared"),
                GameOver = b.Channel("GameOver")
            };

            Sprite square = b.WhiteSquare();

            // Part 6 first, so the menu is the first scene in the list.
            b.MenuScene(MenuName, "Arcade Classics", "Three classics in one: Breakout, Pong and Asteroids.", Breakout,
                new VariableBase[] { d.Score, d.Lives, d.LeftScore, d.RightScore }, Color.black, HowToPlay,
                new[] { ("pong-button", "Pong"), ("asteroids-button", "Asteroids") },
                (menu, buttons) =>
                {
                    SceneChanger toPong = Child(menu, "To Pong").AddComponent<SceneChanger>();
                    Set(toPong, "sceneName", Pong);
                    SceneChanger toAsteroids = Child(menu, "To Asteroids").AddComponent<SceneChanger>();
                    Set(toAsteroids, "sceneName", Asteroids);
                    AddButton(buttons, "pong-button", Call(toPong.Load));
                    AddButton(buttons, "asteroids-button", Call(toAsteroids.Load));
                },
                playText: "Breakout");

            BuildBreakout(b, d, square);
            BuildPong(b, d, square);
            BuildAsteroids(b, d);
        }

        // ---------------------------------------------------------------- Part 2 — Arcade Systems

        /// <summary>The systems every arcade scene has. The HUD, best score and end conditions differ per game.</summary>
        static Systems ArcadeSystems(SampleBuild b, Data d, string systemsName, string hud, out GameObject hudObject)
        {
            GameObject root = new GameObject(systemsName);
            hudObject = Child(root, "HUD");
            b.Document(hudObject, KitUI + hud, 0);

            Systems s = b.StandardSystems(root, MenuName);
            Resetter(Child(root, "Reset"), true, d.Score, d.Lives, d.LeftScore, d.RightScore);
            return s;
        }

        static void Wall(Sprite square, string wallName, Vector2 position, Vector2 size)
        {
            GameObject wall = SpriteObject(wallName, square, 0);
            wall.GetComponent<SpriteRenderer>().color = new Color(0.25f, 0.25f, 0.3f);
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);
            wall.AddComponent<BoxCollider2D>();
        }

        // ---------------------------------------------------------------- Part 3 — Breakout

        static void BuildBreakout(SampleBuild b, Data d, Sprite square)
        {
            // Bricks
            GameObject brick = SpriteObject("Brick", PuzzleSprite("Tiles red/tileRed_02"), 1);
            brick.transform.localScale = new Vector3(1.9f, 1.45f, 1f);
            brick.AddComponent<BoxCollider2D>();
            Brick2D brickScript = brick.AddComponent<Brick2D>();
            Set(brickScript, "hitsToBreak", 1);
            Set(brickScript, "addTo", d.Score);
            Set(brickScript, "amount", 10);
            Set(brick.AddComponent<CountedObject>(), "count", d.BricksLeft);
            GameObject brickPrefab = b.Prefab(brick);

            GameObject tough = SpriteObject("Tough Brick", PuzzleSprite("Tiles grey/tileGrey_02"), 1);
            tough.transform.localScale = new Vector3(1.9f, 1.45f, 1f);
            tough.AddComponent<BoxCollider2D>();
            Brick2D toughScript = tough.AddComponent<Brick2D>();
            Set(toughScript, "hitsToBreak", 3);
            Set(toughScript, "colorByHitsLeft", new[] { Hex("FF9C9C"), Hex("FFA040"), Hex("A01818") });
            Set(toughScript, "addTo", d.Score);
            Set(toughScript, "amount", 30);
            Set(tough.AddComponent<CountedObject>(), "count", d.BricksLeft);
            GameObject toughPrefab = b.Prefab(tough);

            // Systems
            Systems s = ArcadeSystems(b, d, "Breakout Systems", "ArcadeHUD.uxml", out GameObject hud);
            Bind(hud, ("score-label", d.Score, "Score: {0}"), ("best-label", d.BestBreakout, "Best: {0}"), ("lives-label", d.Lives, "Lives: {0}"));

            GameObject rules = Child(s.Root, "Rules");
            HighScore best = rules.AddComponent<HighScore>();
            Set(best, "score", d.Score);
            Set(best, "best", d.BestBreakout);
            Set(best, "saveName", "Breakout");
            Condition(rules, d.Lives, "AtMost", 0, d.GameOver);
            Condition(rules, d.BricksLeft, "EqualTo", 0, d.Cleared);

            b.EndScreen(s, "Breakout Cleared", true, d.Cleared, new (string, VariableBase, string)[] { ("score-label", d.Score, "Score: {0}") }, null);
            b.EndScreen(s, "Breakout Game Over", false, d.GameOver, new (string, VariableBase, string)[] { ("score-label", d.Score, "Score: {0}") }, null);
            GameObject systemsPrefab = b.Prefab(s.Root);

            // The scene
            Scene scene = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, Color.black);
            Place(systemsPrefab, Vector3.zero);

            Wall(square, "Left Wall", new Vector2(-9.4f, 0f), new Vector2(1f, 12f));
            Wall(square, "Right Wall", new Vector2(9.4f, 0f), new Vector2(1f, 12f));
            Wall(square, "Top Wall", new Vector2(0f, 5.4f), new Vector2(20f, 1f));

            GameObject paddle = SpriteObject("Paddle", PuzzleSprite("Paddles/paddle_01"), 2);
            paddle.transform.position = new Vector3(0f, -4.2f, 0f);
            paddle.transform.localScale = new Vector3(1f, 0.65f, 1f);
            paddle.AddComponent<BoxCollider2D>();
            Paddle2D paddleScript = paddle.AddComponent<Paddle2D>();
            SetEnum(paddleScript, "movesAlong", "Horizontal");
            SetEnum(paddleScript, "controlledBy", "Player");
            Set(paddleScript, "input", b.Input);
            Set(paddleScript, "followMouse", true);
            Set(paddleScript, "speed", 14f);
            Set(paddleScript, "minPosition", -7.8f);
            Set(paddleScript, "maxPosition", 7.8f);

            GameObject ball = SpriteObject("Ball", PuzzleSprite("Balls/Blue/ballBlue_09"), 3);
            ball.transform.position = new Vector3(0f, -3.75f, 0f);
            ball.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            ball.AddComponent<CircleCollider2D>();
            Ball2D ballScript = ball.AddComponent<Ball2D>();
            SetEnum(ballScript, "playDirection", "UpAndDown");
            Set(ballScript, "startSpeed", 7f);
            Set(ballScript, "speedUpPerPaddleHit", 0.3f);
            Set(ballScript, "maxSpeed", 14f);
            Set(ballScript, "maxBounceAngle", 60f);
            Set(ballScript, "minAngle", 20f);
            Set(ballScript, "serveFrom", paddle.transform);
            Set(ballScript, "serveOffset", new Vector2(0f, 0.45f));
            Set(ballScript, "serveDirection", new Vector2(0.3f, 1f));
            Set(ballScript, "input", b.Input);
            Set(ballScript, "autoServe", 0f);

            GameObject goal = new GameObject("Ball Goal");
            goal.transform.position = new Vector3(0f, -5.8f, 0f);
            TriggerBox(goal, new Vector2(20f, 1f), Vector2.zero);
            BallGoal2D goalScript = goal.AddComponent<BallGoal2D>();
            Set(goalScript, "changeVariable", d.Lives);
            Set(goalScript, "amount", -1);
            Set(goalScript, "resetBall", true);

            // Six rows of ten: tough bricks on top, then a color per row.
            GameObject bricks = new GameObject("Bricks");
            string[] rows = { null, "Tiles red/tileRed_02", "Tiles orange/tileOrange_02", "Tiles yellow/tileYellow_02", "Tiles green/tileGreen_02", "Tiles blue/tileBlue_02" };

            for (int row = 0; row < rows.Length; row++)
            {
                Sprite rowSprite = rows[row] == null ? null : PuzzleSprite(rows[row]);

                for (int column = 0; column < 10; column++)
                {
                    Vector3 position = new Vector3(-6.75f + column * 1.5f, 3.8f - row * 0.6f, 0f);
                    GameObject placed = Place(row == 0 ? toughPrefab : brickPrefab, position, bricks.transform);

                    if (rowSprite != null && row > 1)
                    {
                        SpriteRenderer renderer = placed.GetComponent<SpriteRenderer>();
                        renderer.sprite = rowSprite;
                        KeepChanges(renderer);
                    }
                }
            }

            b.SaveScene(scene, Breakout);
        }

        // ---------------------------------------------------------------- Part 4 — Pong

        static void BuildPong(SampleBuild b, Data d, Sprite square)
        {
            Systems s = ArcadeSystems(b, d, "Pong Systems", "PongHUD.uxml", out GameObject hud);
            Bind(hud, ("left-score", d.LeftScore, "{0}"), ("right-score", d.RightScore, "{0}"));

            GameObject rules = Child(s.Root, "Rules");
            Condition(rules, d.LeftScore, "AtLeast", 5, d.Cleared);
            Condition(rules, d.RightScore, "AtLeast", 5, d.GameOver);

            b.EndScreen(s, "Pong Won", true, d.Cleared, null, null, "You Win!", "First to 5 points.");
            b.EndScreen(s, "Pong Lost", false, d.GameOver, null, null, "The Computer Wins", "Try again?");
            GameObject systemsPrefab = b.Prefab(s.Root);

            Scene scene = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, Color.black);
            Place(systemsPrefab, Vector3.zero);

            Wall(square, "Top Wall", new Vector2(0f, 5.4f), new Vector2(20f, 1f));
            Wall(square, "Bottom Wall", new Vector2(0f, -5.4f), new Vector2(20f, 1f));

            GameObject line = new GameObject("Middle Line");

            for (int i = 0; i < 10; i++)
            {
                GameObject dash = SpriteObject("Dash", square, 0);
                dash.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.25f);
                dash.transform.SetParent(line.transform, false);
                dash.transform.localPosition = new Vector3(0f, -4.5f + i, 0f);
                dash.transform.localScale = new Vector3(0.1f, 0.5f, 1f);
            }

            PongPaddle(b, "Player Paddle", -8f, true);
            PongPaddle(b, "Computer Paddle", 8f, false);

            GameObject ball = SpriteObject("Pong Ball", PuzzleSprite("Balls/Grey/ballGrey_09"), 3);
            ball.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            ball.AddComponent<CircleCollider2D>();
            Ball2D ballScript = ball.AddComponent<Ball2D>();
            SetEnum(ballScript, "playDirection", "SideToSide");
            Set(ballScript, "startSpeed", 7f);
            Set(ballScript, "maxSpeed", 14f);
            Set(ballScript, "minAngle", 20f);
            Set(ballScript, "serveDirection", new Vector2(1f, 0.4f));
            Set(ballScript, "randomServeSide", true);
            Set(ballScript, "autoServe", 1f);

            PongGoal("Left Goal", -9.8f, d.RightScore, new Vector2(-1f, 0.4f));
            PongGoal("Right Goal", 9.8f, d.LeftScore, new Vector2(1f, 0.4f));

            b.SaveScene(scene, Pong);
        }

        static void PongPaddle(SampleBuild b, string paddleName, float x, bool player)
        {
            GameObject paddle = SpriteObject(paddleName, PuzzleSprite(player ? "Paddles/paddle_01" : "Paddles/paddle_05"), 2);
            paddle.transform.position = new Vector3(x, 0f, 0f);
            paddle.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            paddle.transform.localScale = new Vector3(1f, 0.65f, 1f);
            paddle.AddComponent<BoxCollider2D>();
            Paddle2D script = paddle.AddComponent<Paddle2D>();
            SetEnum(script, "movesAlong", "Vertical");
            SetEnum(script, "controlledBy", player ? "Player" : "Computer");
            Set(script, "input", player ? b.Input : null);
            Set(script, "followMouse", false);
            Set(script, "speed", 10f);
            Set(script, "minPosition", -4f);
            Set(script, "maxPosition", 4f);
            Set(script, "computerSpeed", 0.6f);
            Set(script, "computerDeadZone", 0.3f);
        }

        static void PongGoal(string goalName, float x, IntVariable scoreFor, Vector2 nextServe)
        {
            GameObject goal = new GameObject(goalName);
            goal.transform.position = new Vector3(x, 0f, 0f);
            TriggerBox(goal, new Vector2(1f, 12f), Vector2.zero);
            BallGoal2D script = goal.AddComponent<BallGoal2D>();
            Set(script, "changeVariable", scoreFor);
            Set(script, "amount", 1);
            Set(script, "resetBall", true);
            Set(script, "nextServeDirection", nextServe);
        }

        // ---------------------------------------------------------------- Part 5 — Asteroids

        static void BuildAsteroids(SampleBuild b, Data d)
        {
            // Bullet
            GameObject bullet = SpriteObject("Bullet", SpaceSprite("Lasers/laserBlue01"), 4);
            bullet.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            bullet.AddComponent<BoxCollider2D>().isTrigger = true;
            Body2D(bullet);
            DamageOnTouch bulletDamage = bullet.AddComponent<DamageOnTouch>();
            Set(bulletDamage, "targetTag", "Enemy");
            Set(bulletDamage, "damage", 1);
            Set(bulletDamage, "removeSelfAfterHit", true);
            Lifetime life = bullet.AddComponent<Lifetime>();
            Set(life, "seconds", 1f);
            SetEnum(life, "whenTimeIsUp", "Destroy");
            bullet.AddComponent<ScreenWrap2D>();
            GameObject bulletPrefab = b.Prefab(bullet);

            // Asteroids: smallest first, so each size can split into the one below.
            GameObject small = Asteroid(b, d, "Asteroid Small", 0.35f, 1.8f, 3f, 100, null);
            GameObject medium = Asteroid(b, d, "Asteroid Medium", 0.6f, 1.2f, 2.2f, 50, small);
            GameObject large = Asteroid(b, d, "Asteroid Large", 1f, 0.8f, 1.5f, 20, medium);

            // Ship
            GameObject ship = SpriteObject("Ship", SpaceSprite("playerShip1_blue"), 5);
            ship.tag = "Player";
            ship.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            ship.AddComponent<PolygonCollider2D>();

            GameObject muzzle = Child(ship, "Muzzle");
            muzzle.transform.localPosition = new Vector3(0f, 0.45f, 0f);

            GameObject flame = SpriteObject("Flame", SpaceSprite("Effects/fire00"), 4);
            flame.transform.SetParent(ship.transform, false);
            flame.transform.localPosition = new Vector3(0f, -0.6f, 0f);

            ShipController2D controller = ship.AddComponent<ShipController2D>();
            Set(controller, "input", b.Input);
            Set(controller, "engineFlame", flame);

            Shooter2D shooter = ship.AddComponent<Shooter2D>();
            Set(shooter, "input", b.Input);
            Set(shooter, "projectilePrefab", bulletPrefab);
            Set(shooter, "muzzle", muzzle.transform);
            SetEnum(shooter, "aim", "MuzzleUp");
            Set(shooter, "projectileSpeed", 14f);
            Set(shooter, "shotsPerSecond", 5f);
            Set(shooter, "maxOnScreen", 4);

            ship.AddComponent<ScreenWrap2D>();

            Health health = ship.AddComponent<Health>();
            Set(health, "maxHealth", 1);
            SetEnum(health, "whenHealthRunsOut", "Nothing");
            On(health, "onDied", Call(d.Lives.Add, -1));

            Respawner respawner = ship.AddComponent<Respawner>();
            On(respawner, "onRespawn", Call(controller.StopAndFaceUp));
            GameObject shipPrefab = b.Prefab(ship);

            // Systems
            Systems s = ArcadeSystems(b, d, "Asteroids Systems", "ArcadeHUD.uxml", out GameObject hud);
            Bind(hud, ("score-label", d.Score, "Score: {0}"), ("best-label", d.BestAsteroids, "Best: {0}"), ("lives-label", d.Lives, "Lives: {0}"));

            GameObject rules = Child(s.Root, "Rules");
            HighScore best = rules.AddComponent<HighScore>();
            Set(best, "score", d.Score);
            Set(best, "best", d.BestAsteroids);
            Set(best, "saveName", "Asteroids");
            Condition(rules, d.Lives, "AtMost", 0, d.GameOver);
            Condition(rules, d.AsteroidsLeft, "EqualTo", 0, d.Cleared);

            b.EndScreen(s, "Asteroids Cleared", true, d.Cleared, new (string, VariableBase, string)[] { ("score-label", d.Score, "Score: {0}") }, null);
            b.EndScreen(s, "Asteroids Game Over", false, d.GameOver, new (string, VariableBase, string)[] { ("score-label", d.Score, "Score: {0}") }, null);
            GameObject systemsPrefab = b.Prefab(s.Root);

            // The scene
            Scene scene = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, Color.black);
            Place(systemsPrefab, Vector3.zero);
            Place(shipPrefab, Vector3.zero);

            GameObject rocks = new GameObject("Asteroids");
            Place(large, new Vector3(-6.5f, 3.5f, 0f), rocks.transform);
            Place(large, new Vector3(6.5f, 3.5f, 0f), rocks.transform);
            Place(large, new Vector3(-6.5f, -3.5f, 0f), rocks.transform);
            Place(large, new Vector3(6.5f, -3.5f, 0f), rocks.transform);

            b.SaveScene(scene, Asteroids);
        }

        static GameObject Asteroid(SampleBuild b, Data d, string asteroidName, float scale, float minSpeed, float maxSpeed, int points, GameObject splitInto)
        {
            GameObject rock = SpriteObject(asteroidName, SpaceSprite("Meteors/meteorBrown_big1"), 3);
            rock.tag = "Enemy";
            rock.transform.localScale = new Vector3(scale, scale, 1f);
            rock.AddComponent<CircleCollider2D>().isTrigger = true;
            Body2D(rock);

            Drift2D drift = rock.AddComponent<Drift2D>();
            Set(drift, "minSpeed", minSpeed);
            Set(drift, "maxSpeed", maxSpeed);
            rock.AddComponent<ScreenWrap2D>();

            DamageOnTouch damage = rock.AddComponent<DamageOnTouch>();
            Set(damage, "targetTag", "Player");
            Set(damage, "damage", 1);
            Set(rock.AddComponent<CountedObject>(), "count", d.AsteroidsLeft);

            Health health = rock.AddComponent<Health>();
            Set(health, "maxHealth", 1);
            Set(health, "invulnerableTime", 0f);
            SetEnum(health, "whenHealthRunsOut", "Destroy");
            On(health, "onDied", Call(d.Score.Add, points));

            if (splitInto != null)
            {
                SplitOnDeath2D split = rock.AddComponent<SplitOnDeath2D>();
                Set(split, "piecePrefab", splitInto);
                Set(split, "pieces", 2);
            }

            return b.Prefab(rock);
        }
    }
}
