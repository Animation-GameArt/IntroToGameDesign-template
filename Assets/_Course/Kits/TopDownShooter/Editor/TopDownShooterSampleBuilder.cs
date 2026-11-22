using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.EditorTools;
using CollinGame.TwoD;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.TopDownShooter.EditorTools
{
    /// <summary>
    /// Builds the Top-Down Shooter Kit's sample: weapons, the player ship, four enemies,
    /// pickups, the wave arena, the scrolling level, and the menu and win scenes,
    /// following "Top-Down Shooter Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Top-Down Shooter Sample.
    /// </summary>
    public static class TopDownShooterSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/TopDownShooter";
        const string Pack = "kenney_pixel-shmup";
        const string MenuName = "Shooter_Menu";
        const string ArenaName = "Shooter_Arena";
        const string ScrollingName = "Shooter_Scrolling";
        const string WinName = "Shooter_Win";

        static readonly Color Dark = new Color(0.08f, 0.08f, 0.12f);

        const string HowToPlay =
            "Survive four waves in the arena, then fly the scrolling level and beat the boss.\n\n" +
            "WASD or the arrow keys move. Aim with the mouse (or the right stick) and hold the left mouse button (or Space) to fire.\n\n" +
            "Shot planes sometimes drop a red heart, which heals, or a power-up: P is a spread shot, the shield is rapid fire, each for 10 seconds.\n\n" +
            "Enemies and their bullets cost health. Esc pauses.";

        [MenuItem("Collin Game/Kit Samples/Build Top-Down Shooter Sample")]
        static void Build()
        {
            Run("Top-Down Shooter", KitFolder, new[] { "Data", "Prefabs", "Scenes", "Weapons" },
                new[] { Kenney + Pack }, BuildSample);
        }

        static Sprite Tile(int index) => KenneySprite(Pack, $"Tiles/tile_{index:0000}");
        static Sprite Ship(int index) => KenneySprite(Pack, $"Ships/ship_{index:0000}");

        sealed class Kit
        {
            public IntVariable Score, Wave, PlayerHealth;
            public StringVariable WeaponName;
            public VoidEventChannel Cleared, Died, ReachedBoss, BossDefeated;
            public WeaponData Blaster, Spread, Rapid, EnemyShot, BossSpread;
            public GameObject Player, ScrollingPlayer, Drone, Gunship, Weaver, Boss, Heart, SpreadUp, RapidUp, Systems;
            public Sprite Square;
        }

        static void BuildSample(SampleBuild b)
        {
            EnsureTag("Enemy");

            // Part 1 — Data
            Kit k = new Kit
            {
                Score = b.Int("Score"),
                Wave = b.Int("Wave"),
                PlayerHealth = b.Int("PlayerHealth", 5),
                WeaponName = b.Text("WeaponName"),
                Cleared = b.Channel("ArenaCleared"),
                Died = b.Channel("PlayerDied"),
                ReachedBoss = b.Channel("ReachedBoss"),
                BossDefeated = b.Channel("BossDefeated"),
                Square = b.WhiteSquare()
            };

            // Part 2 — Projectiles and weapons
            Projectile2D playerBullet = Bullet(b, "Player Bullet", Tile(0), Color.white, "Enemy", 1.5f);
            Projectile2D enemyBullet = Bullet(b, "Enemy Bullet", Tile(2), new Color(1f, 0.4f, 0.6f), "Player", 3f);

            k.Blaster = Weapon(b, "Blaster", playerBullet, 14f, 6f, 1, 0f, 0f, true);
            k.Spread = Weapon(b, "Spread Shot", playerBullet, 13f, 4f, 5, 50f, 0f, true);
            k.Rapid = Weapon(b, "Rapid Fire", playerBullet, 16f, 14f, 1, 0f, 6f, true);
            k.EnemyShot = Weapon(b, "Enemy Shot", enemyBullet, 6f, 1f, 1, 0f, 3f, false);
            k.BossSpread = Weapon(b, "Boss Spread", enemyBullet, 5f, 1f, 7, 90f, 0f, false);

            // Part 5 — Pickups (before the enemies, which drop them)
            GameObject heart = SpriteObject("Heart", Tile(24), 6);
            TriggerCircle(heart, 0.45f);
            Collectible heal = heart.AddComponent<Collectible>();
            Set(heal, "heal", 1);
            k.Heart = b.Prefab(heart);
            k.SpreadUp = PowerUp(b, "Spread Power-Up", Tile(25), k.Spread);
            k.RapidUp = PowerUp(b, "Rapid Power-Up", Tile(26), k.Rapid);

            // Part 3 — The player
            BuildPlayer(b, k);

            // Part 4 — Enemies
            GameObject drone = Enemy(k, "Drone", Ship(9), 0.75f, 0.45f, "Chase", 2.5f, 1, 10);
            Drops(drone, 0.1f, k.Heart);
            k.Drone = b.Prefab(drone);

            GameObject gunship = Enemy(k, "Gunship", Ship(10), 0.75f, 0.45f, "Circle", 2f, 3, 30);
            Set(gunship.GetComponent<EnemyMover2D>(), "preferredDistance", 4f);
            Gun(b, gunship, k.EnemyShot, 1.5f, 1f);
            Drops(gunship, 0.3f, k.SpreadUp, k.RapidUp);
            k.Gunship = b.Prefab(gunship);

            GameObject weaver = Enemy(k, "Weaver", Ship(11), 0.75f, 0.45f, "Wave", 2f, 2, 20);
            EnemyMover2D weaverMover = weaver.GetComponent<EnemyMover2D>();
            Set(weaverMover, "flyDirection", new Vector2(0f, -1f));
            Set(weaverMover, "waveWidth", 1.5f);
            WeaponShooter2D weaverGun = Gun(b, weaver, k.EnemyShot, 2f, 1f);
            WakeOnScreen(weaver, weaverMover, weaverGun);
            k.Weaver = b.Prefab(weaver);

            GameObject boss = Enemy(k, "Boss", Ship(16), 1.5f, 0.6f, "KeepAway", 1.5f, 40, 500);
            Set(boss.GetComponent<EnemyMover2D>(), "preferredDistance", 5f);
            Gun(b, boss, k.BossSpread, 1.2f, 1f);
            Set(boss.GetComponent<Health>(), "raiseOnDied", k.BossDefeated);
            k.Boss = b.Prefab(boss);

            // Part 6 — Arena Systems
            k.Systems = ArenaSystems(b, k);

            // Part 9 — Menu first, so it's first in the Scene List
            b.MenuScene(MenuName, "Sky Patrol", "Blast through four waves in the arena, then fly north to face the boss.", ArenaName,
                new VariableBase[] { k.Score, k.Wave, k.WeaponName }, Dark, HowToPlay);

            BuildArena(b, k);
            BuildScrolling(b, k);

            b.WinScene(WinName, "Mission Complete!", "", ArenaName, MenuName, new VariableBase[] { k.Score }, Dark,
                ("score-label", k.Score, "Score: {0}"));
        }

        // ---------------------------------------------------------------- Parts

        static Projectile2D Bullet(SampleBuild b, string bulletName, Sprite sprite, Color color, string target, float lifetime)
        {
            GameObject bullet = SpriteObject(bulletName, sprite, 8);
            bullet.GetComponent<SpriteRenderer>().color = color;
            TriggerCircle(bullet, 0.15f);
            Projectile2D projectile = bullet.AddComponent<Projectile2D>();
            Set(projectile, "targetTag", target);
            Set(projectile, "lifetime", lifetime);
            return b.Prefab(bullet).GetComponent<Projectile2D>();
        }

        static WeaponData Weapon(SampleBuild b, string weaponName, Projectile2D projectile, float speed, float shotsPerSecond,
            int perShot, float spread, float inaccuracy, bool holdToFire)
        {
            WeaponData weapon = b.Asset<WeaponData>("Weapons", weaponName);
            Set(weapon, "displayName", weaponName);
            Set(weapon, "projectilePrefab", projectile);
            Set(weapon, "projectileSpeed", speed);
            Set(weapon, "damage", 1);
            Set(weapon, "shotsPerSecond", shotsPerSecond);
            Set(weapon, "projectilesPerShot", perShot);
            Set(weapon, "spreadAngle", spread);
            Set(weapon, "inaccuracy", inaccuracy);
            Set(weapon, "holdToFire", holdToFire);
            return weapon;
        }

        static GameObject PowerUp(SampleBuild b, string pickupName, Sprite sprite, WeaponData weapon)
        {
            GameObject pickup = SpriteObject(pickupName, sprite, 6);
            TriggerCircle(pickup, 0.45f);
            PowerUp2D power = pickup.AddComponent<PowerUp2D>();
            Set(power, "weapon", weapon);
            Set(power, "duration", 10f);
            Set(power, "disappearAfter", 8f);
            return b.Prefab(pickup);
        }

        static void BuildPlayer(SampleBuild b, Kit k)
        {
            GameObject player = new GameObject("Player Ship") { tag = "Player" };
            GameObject turret = Child(player, "Turret");
            GameObject hull = SpriteObject("Hull", Ship(0), 10);
            hull.transform.SetParent(turret.transform, false);
            hull.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            hull.transform.localScale = new Vector3(0.75f, 0.75f, 1f);

            player.AddComponent<CircleCollider2D>().radius = 0.4f;
            TopDownController2D controller = player.AddComponent<TopDownController2D>();
            Set(controller, "input", b.Input);
            Set(controller, "maxSpeed", 6f);
            SetEnum(controller, "facing", "None");

            Aim2D aim = player.AddComponent<Aim2D>();
            SetEnum(aim, "aimWith", "MouseOrStick");
            Set(aim, "turnToAim", turret.transform);

            WeaponShooter2D shooter = player.AddComponent<WeaponShooter2D>();
            Set(shooter, "weapon", k.Blaster);
            SetEnum(shooter, "fireWhen", "AttackButton");
            Set(shooter, "input", b.Input);
            SetEnum(shooter, "direction", "Aim");
            Set(shooter, "weaponName", k.WeaponName);

            Health health = player.AddComponent<Health>();
            Set(health, "maxHealth", 5);
            Set(health, "invulnerableTime", 1f);
            Set(health, "healthVariable", k.PlayerHealth);
            Set(health, "raiseOnDied", k.Died);
            SetEnum(health, "whenHealthRunsOut", "Deactivate");

            Set(player.AddComponent<KeepOnScreen2D>(), "margin", 0.5f);
            k.Player = b.Prefab(player);

            // The scrolling version: a Prefab Variant that always aims up.
            GameObject variant = (GameObject)PrefabUtility.InstantiatePrefab(k.Player);
            Aim2D variantAim = variant.GetComponent<Aim2D>();
            SetEnum(variantAim, "aimWith", "FixedDirection");
            Set(variantAim, "fixedDirection", new Vector2(0f, 1f));
            Set(variantAim, "turnToAim", null);
            variant.transform.Find("Turret").localRotation = Quaternion.Euler(0f, 0f, 90f);
            KeepChanges(variantAim);
            KeepChanges(variant.transform.Find("Turret"));
            k.ScrollingPlayer = PrefabUtility.SaveAsPrefabAsset(variant, $"{b.Folder("Prefabs")}/Player Ship (Scrolling).prefab");
            Object.DestroyImmediate(variant);
        }

        static GameObject Enemy(Kit k, string enemyName, Sprite sprite, float scale, float radius, string pattern, float speed, int health, int points)
        {
            GameObject enemy = SpriteObject(enemyName, sprite, 5);
            enemy.tag = "Enemy";
            enemy.transform.localScale = new Vector3(scale, scale, 1f);
            enemy.AddComponent<CircleCollider2D>().radius = radius / scale;

            EnemyMover2D mover = enemy.AddComponent<EnemyMover2D>();
            SetEnum(mover, "pattern", pattern);
            Set(mover, "speed", speed);

            Health life = enemy.AddComponent<Health>();
            Set(life, "maxHealth", health);
            Set(life, "invulnerableTime", 0f);
            SetEnum(life, "whenHealthRunsOut", "Destroy");
            On(life, "onDied", Call(k.Score.Add, points));

            Set(enemy.AddComponent<DamageOnTouch>(), "targetTag", "Player");
            return enemy;
        }

        static WeaponShooter2D Gun(SampleBuild b, GameObject enemy, WeaponData weapon, float interval, float firstDelay)
        {
            WeaponShooter2D gun = enemy.AddComponent<WeaponShooter2D>();
            Set(gun, "weapon", weapon);
            SetEnum(gun, "fireWhen", "Automatic");
            SetEnum(gun, "direction", "AtTarget");
            Set(gun, "targetTag", "Player");
            Set(gun, "autoFireInterval", interval);
            Set(gun, "firstShotDelay", firstDelay);
            return gun;
        }

        static void Drops(GameObject enemy, float chance, params GameObject[] drops)
        {
            DropOnDeath2D drop = enemy.AddComponent<DropOnDeath2D>();
            Set(drop, "chance", chance);
            Set(drop, "drops", drops);
        }

        static WakeOnScreen2D WakeOnScreen(GameObject enemy, params Behaviour[] wake)
        {
            WakeOnScreen2D waker = enemy.AddComponent<WakeOnScreen2D>();
            Set(waker, "wakeUp", wake);
            return waker;
        }

        // ---------------------------------------------------------------- Part 6 — Arena Systems

        static GameObject ArenaSystems(SampleBuild b, Kit k)
        {
            GameObject root = new GameObject("Arena Systems");

            GameObject hud = Child(root, "HUD");
            b.Document(hud, KitFolder + "/UI/ShooterHUD.uxml", 0);
            Bind(hud, ("health-label", k.PlayerHealth, "Health: {0}"), ("score-label", k.Score, "Score: {0}"),
                ("wave-label", k.Wave, "Wave {0}"), ("weapon-label", k.WeaponName, "{0}"));

            Systems s = b.StandardSystems(root, MenuName);
            b.EndScreen(s, "Game Over", false, k.Died, new (string, VariableBase, string)[] { ("score-label", k.Score, "Score: {0}") },
                new VariableBase[] { k.Score });

            GameObject cleared = Child(root, "Cleared");
            b.Document(cleared, b.UxmlCopy("Assets/_Course/Kits/Platformer/UI/LevelComplete.uxml", "ShooterCleared",
                ("text=\"Level Complete!\"", "text=\"Sector Cleared!\"")), 12);
            UIScreen clearedScreen = MakeScreen(cleared, true, "next-button");
            Bind(cleared, ("coins-label", k.Score, "Score: {0}"));
            Listen(cleared, k.Cleared, Call(clearedScreen.Show), Call(b.Input.DisablePlayerInput));
            UIButtonEvents buttons = cleared.AddComponent<UIButtonEvents>();
            AddButton(buttons, "next-button", Call(s.SceneChanger.LoadNextScene));
            AddButton(buttons, "menu-button", Call(s.ToMenu.Load));

            Resetter(Child(root, "Reset"), true, k.Wave, k.WeaponName);

            // Waves, with eight spawn points just inside the arena walls, two on each side.
            GameObject waves = Child(root, "Waves");
            WaveSpawner2D spawner = waves.AddComponent<WaveSpawner2D>();
            Vector2[] points =
            {
                new Vector2(-6f, 5.6f), new Vector2(6f, 5.6f), new Vector2(-6f, -5.6f), new Vector2(6f, -5.6f),
                new Vector2(-11f, 3f), new Vector2(-11f, -3f), new Vector2(11f, 3f), new Vector2(11f, -3f)
            };
            Transform[] spawnPoints = new Transform[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                GameObject point = Child(waves, $"Spawn Point {i + 1}");
                point.transform.localPosition = points[i];
                spawnPoints[i] = point.transform;
            }

            WaveSpawner2D.Wave W(float seconds, params (GameObject enemy, int count)[] groups)
            {
                WaveSpawner2D.Wave wave = new WaveSpawner2D.Wave { secondsBetweenEnemies = seconds };
                foreach ((GameObject enemy, int count) in groups)
                {
                    wave.groups.Add(new WaveSpawner2D.Group { enemy = enemy, count = count });
                }
                return wave;
            }

            Set(spawner, "waves", new List<WaveSpawner2D.Wave>
            {
                W(0.6f, (k.Drone, 6)),
                W(0.5f, (k.Drone, 8), (k.Gunship, 2)),
                W(0.4f, (k.Drone, 10), (k.Gunship, 4)),
                W(0.8f, (k.Boss, 1), (k.Drone, 6))
            });
            Set(spawner, "spawnPoints", spawnPoints);
            Set(spawner, "waveNumber", k.Wave);
            Set(spawner, "raiseWhenCleared", k.Cleared);

            return b.Prefab(root);
        }

        // ---------------------------------------------------------------- Part 7 — The arena

        static void Wall(Kit k, string wallName, Vector2 position, Vector2 size)
        {
            GameObject wall = SpriteObject(wallName, k.Square, 1);
            wall.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.3f, 0.38f);
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(size.x, size.y, 1f);
            wall.AddComponent<BoxCollider2D>();
        }

        static void BuildArena(SampleBuild b, Kit k)
        {
            Scene scene = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 7f, Dark);
            GlobalLight2D();

            TiledSprite("Floor", Tile(56), -10, new Vector2(26f, 15f));

            Wall(k, "Top Wall", new Vector2(0f, 6.6f), new Vector2(25f, 0.6f));
            Wall(k, "Bottom Wall", new Vector2(0f, -6.6f), new Vector2(25f, 0.6f));
            Wall(k, "Left Wall", new Vector2(-12f, 0f), new Vector2(0.6f, 14f));
            Wall(k, "Right Wall", new Vector2(12f, 0f), new Vector2(0.6f, 14f));

            Place(k.Player, Vector3.zero);
            Place(k.Systems, Vector3.zero);
            b.SaveScene(scene, ArenaName);
        }

        // ---------------------------------------------------------------- Part 8 — The scrolling level

        static void BuildScrolling(SampleBuild b, Kit k)
        {
            Scene scene = NewScene();
            Camera camera = Camera2D(new Vector3(0f, 0f, -10f), 7f, Dark);
            ScrollCamera2D scroll = camera.gameObject.AddComponent<ScrollCamera2D>();
            Set(scroll, "direction", new Vector2(0f, 1f));
            Set(scroll, "speed", 2f);
            Set(scroll, "stopAfter", 80f);
            Set(scroll, "raiseAtEnd", k.ReachedBoss);
            GlobalLight2D();

            GameObject floor = TiledSprite("Floor", Tile(50), -10, new Vector2(26f, 100f));
            floor.transform.position = new Vector3(0f, 43f, 0f);

            Place(k.ScrollingPlayer, new Vector3(0f, -4.5f, 0f));

            // This level places its enemies by hand, and is won by beating the boss.
            GameObject systems = Place(k.Systems, Vector3.zero);
            GameObject waves = systems.transform.Find("Waves").gameObject;
            waves.SetActive(false);
            KeepChanges(waves);
            VoidEventListener clearedListener = systems.transform.Find("Cleared").GetComponent<VoidEventListener>();
            Set(clearedListener, "channel", k.BossDefeated);
            KeepChanges(clearedListener);

            GameObject enemies = new GameObject("Enemies");

            // Weavers in small groups along the way.
            float[] weaverRows = { 12f, 20f, 28f, 38f, 46f, 56f, 64f };
            for (int row = 0; row < weaverRows.Length; row++)
            {
                float side = row % 2 == 0 ? -1f : 1f;
                for (int i = 0; i < 3; i++)
                {
                    Place(k.Weaver, new Vector3(side * (3f + i * 2f), weaverRows[row] + i * 0.8f, 0f), enemies.transform);
                }
            }

            // A few gunships, which wake when they scroll into view.
            foreach (Vector2 position in new[] { new Vector2(0f, 24f), new Vector2(-5f, 42f), new Vector2(5f, 60f), new Vector2(0f, 70f) })
            {
                GameObject gunship = Place(k.Gunship, position, enemies.transform);
                WakeOnScreen(gunship, gunship.GetComponent<EnemyMover2D>(), gunship.GetComponent<WeaponShooter2D>());
            }

            // The boss, at the top, where the camera stops.
            GameObject boss = Place(k.Boss, new Vector3(0f, 84f, 0f), enemies.transform);
            WakeOnScreen(boss, boss.GetComponent<EnemyMover2D>(), boss.GetComponent<WeaponShooter2D>());

            b.SaveScene(scene, ScrollingName);
        }
    }
}
