using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.TowerDefense.EditorTools
{
    /// <summary>
    /// Builds the Tower Defense Kit's sample: projectiles, three towers with
    /// upgrades, four enemies, ten waves, the map and the menu, following
    /// "Tower Defense Kit — Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Tower Defense Sample.
    /// </summary>
    public static class TowerDefenseSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/TowerDefense";
        const string Pack = "kenney_tower-defense-top-down";
        const string MenuName = "TD_Menu";
        const string MapName = "TD_Map";

        static readonly Color Backdrop = new Color(0.18f, 0.35f, 0.2f);

        // The road, as grid cells at each bend: in from the left edge, out at the right.
        static readonly Vector2Int[] Bends =
        {
            new Vector2Int(-12, 2), new Vector2Int(-6, 2), new Vector2Int(-6, -3), new Vector2Int(-1, -3), new Vector2Int(-1, 3),
            new Vector2Int(4, 3), new Vector2Int(4, -2), new Vector2Int(8, -2), new Vector2Int(8, 1), new Vector2Int(12, 1)
        };

        // Build pads, each within reach of two stretches of road.
        static readonly Vector2Int[] Pads =
        {
            new Vector2Int(-8, 0), new Vector2Int(-4, 0), new Vector2Int(-3, -5), new Vector2Int(-3, 5),
            new Vector2Int(1, 0), new Vector2Int(2, 5), new Vector2Int(6, 0), new Vector2Int(10, -1)
        };

        const string HowToPlay =
            "Stop the enemies walking the road from reaching the right edge.\n\n" +
            "Click a grey pad to build a tower: Archers are cheap and quick, Cannons hit groups, Frost towers slow enemies down. Click a tower to upgrade or sell it.\n\n" +
            "Press Start Wave when you're ready. Each enemy you stop pays money, and so does every wave you clear. Each enemy that gets through costs lives.\n\n" +
            "Survive ten waves to win. Esc pauses.";

        [MenuItem("Collin Game/Kit Samples/Build Tower Defense Sample")]
        static void Build()
        {
            Run("Tower Defense", KitFolder, new[] { "Data", "Towers", "Prefabs", "Scenes" },
                new[] { Kenney + Pack }, BuildSample);
        }

        static Sprite Art(int index) => KenneySprite(Pack, $"PNG/Default size/towerDefense_tile{index:000}");

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — Data
            IntVariable money = b.Int("Money", 120);
            IntVariable lives = b.Int("Lives", 20);
            IntVariable wave = b.Int("Wave");
            VoidEventChannel won = b.Channel("MapWon");
            VoidEventChannel lost = b.Channel("MapLost");
            Sprite circle = b.WhiteCircle();

            // Part 2 — Projectiles
            GameObject boom = SpriteObject("Explosion", Art(295), 21);
            boom.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
            Lifetime boomLife = boom.AddComponent<Lifetime>();
            Set(boomLife, "seconds", 0.3f);
            SetEnum(boomLife, "whenTimeIsUp", "Destroy");
            GameObject explosion = b.Prefab(boom);

            TowerProjectile2D arrow = Projectile(b, "Arrow", Art(272), Color.white, null);
            TowerProjectile2D cannonball = Projectile(b, "Cannonball", Art(274), Color.white, explosion);
            TowerProjectile2D frostBolt = Projectile(b, "Frost Bolt", Art(275), new Color(0.7f, 0.9f, 1f), null);

            // Tower prefabs
            Tower2D archerTower = TowerPrefab(b, "Archer Tower", Art(249), Color.white, circle);
            Tower2D cannonTower = TowerPrefab(b, "Cannon Tower", Art(250), Color.white, circle);
            Tower2D frostTower = TowerPrefab(b, "Frost Tower", Art(206), new Color(0.7f, 0.9f, 1f), circle);

            // Tower assets: the upgrades first, so the first level can point to them.
            TowerData archer2 = Tower(b, "Archer II", null, 60, 3.5f, 2, 2.5f, arrow, 14f, 0f, 1f, 0f, null);
            TowerData archer = Tower(b, "Archer", archerTower, 50, 3f, 1, 2f, arrow, 12f, 0f, 1f, 0f, archer2);
            TowerData cannon2 = Tower(b, "Cannon II", null, 110, 3f, 5, 0.7f, cannonball, 7f, 1.5f, 1f, 0f, null);
            TowerData cannon = Tower(b, "Cannon", cannonTower, 90, 2.5f, 3, 0.6f, cannonball, 6f, 1.2f, 1f, 0f, cannon2);
            TowerData frost2 = Tower(b, "Frost II", null, 70, 3f, 0, 1.2f, frostBolt, 12f, 1.2f, 0.35f, 2.5f, null);
            TowerData frost = Tower(b, "Frost", frostTower, 70, 2.5f, 0, 1f, frostBolt, 10f, 0.8f, 0.5f, 2f, frost2);

            // Part 3 — Enemies
            PathEnemy2D scout = Enemy(b, "Scout", Art(245), 1f, 3, 2.5f, 4, 1);
            PathEnemy2D soldier = Enemy(b, "Soldier", Art(247), 1f, 8, 1.5f, 6, 1);
            PathEnemy2D brute = Enemy(b, "Brute", Art(268), 1f, 30, 0.9f, 15, 3);
            PathEnemy2D swarmling = Enemy(b, "Swarmling", Art(248), 0.7f, 2, 2f, 2, 1);

            // Build pad
            GameObject pad = SpriteObject("Build Spot", Art(84), 2);
            pad.AddComponent<CircleCollider2D>().radius = 0.45f;
            pad.AddComponent<BuildSpot2D>();
            GameObject padPrefab = b.Prefab(pad);

            // Part 4 — TD Systems
            GameObject root = new GameObject("TD Systems");

            GameObject waves = Child(root, "Waves");
            TowerWaveSpawner spawner = waves.AddComponent<TowerWaveSpawner>();
            Set(spawner, "money", money);
            Set(spawner, "startingMoney", 120);
            Set(spawner, "lives", lives);
            Set(spawner, "startingLives", 20);
            Set(spawner, "waveNumber", wave);
            Set(spawner, "raiseWhenWon", won);

            TowerWaveSpawner.Wave W(int bonus, params (int count, PathEnemy2D enemy, float seconds)[] groups)
            {
                TowerWaveSpawner.Wave w = new TowerWaveSpawner.Wave { bonus = bonus };
                foreach ((int count, PathEnemy2D enemy, float seconds) in groups)
                {
                    w.groups.Add(new TowerWaveSpawner.Group { enemy = enemy, count = count, secondsBetween = seconds });
                }
                return w;
            }

            Set(spawner, "waves", new List<TowerWaveSpawner.Wave>
            {
                W(20, (6, scout, 1.2f)),
                W(20, (10, scout, 0.9f)),
                W(25, (6, soldier, 1.2f)),
                W(25, (8, scout, 0.6f), (6, soldier, 1f)),
                W(30, (2, brute, 3f)),
                W(30, (20, swarmling, 0.4f)),
                W(35, (10, soldier, 0.8f), (2, brute, 2.5f)),
                W(35, (30, swarmling, 0.3f), (8, scout, 0.5f)),
                W(40, (4, brute, 2f), (12, soldier, 0.6f)),
                W(0, (25, swarmling, 0.3f), (6, brute, 2f), (15, soldier, 0.5f))
            });

            Condition(Child(root, "Lose"), lives, "AtMost", 0, lost);

            GameObject hud = Child(root, "HUD");
            b.Document(hud, KitFolder + "/UI/TowerHUD.uxml", 0);
            UIBuildMenu buildMenu = hud.AddComponent<UIBuildMenu>();
            Set(buildMenu, "towers", new List<TowerData> { archer, cannon, frost });
            Set(buildMenu, "money", money);
            Set(buildMenu, "spawner", spawner);
            Bind(hud, ("money-label", money, "${0}"), ("lives-label", lives, "Lives: {0}"), ("wave-label", wave, "Wave {0} / 10"));

            Systems s = b.StandardSystems(root, MenuName);
            b.EndScreen(s, "Won", true, won, new (string, VariableBase, string)[] { ("score-label", lives, "Lives left: {0}") }, null, "Map Defended!");
            b.EndScreen(s, "Lost", false, lost, null, null, "The Enemy Got Through", "Try a different mix of towers.");
            GameObject systems = b.Prefab(root);

            // Part 6 — Menu first, so it's first in the Scene List
            b.MenuScene(MenuName, "Hold the Road", "Build and upgrade towers to stop ten waves from crossing the map.", MapName,
                null, Backdrop, HowToPlay);

            // Part 5 — The map
            Scene scene = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 6f, Backdrop);
            GlobalLight2D();

            GameObject grid = GridObject();
            Tilemap grass = TilemapObject(grid, "Grass", -10);
            Tilemap road = TilemapObject(grid, "Road", -9);
            Tile grassTile = b.TileFrom(Art(157), "Grass");
            Tile roadTile = b.TileFrom(Art(158), "Road");

            for (int x = -12; x < 12; x++)
            {
                for (int y = -7; y < 7; y++)
                {
                    grass.SetTile(new Vector3Int(x, y, 0), grassTile);
                }
            }

            GameObject pathObject = new GameObject("Path");
            EnemyPath2D path = pathObject.AddComponent<EnemyPath2D>();

            for (int i = 0; i < Bends.Length; i++)
            {
                GameObject point = Child(pathObject, $"Point {i + 1}");
                point.transform.position = new Vector3(Bends[i].x + 0.5f, Bends[i].y + 0.5f, 0f);

                if (i > 0)
                {
                    Vector2Int from = Bends[i - 1];
                    Vector2Int to = Bends[i];
                    Vector2Int step = new Vector2Int(System.Math.Sign(to.x - from.x), System.Math.Sign(to.y - from.y));

                    for (Vector2Int cell = from; ; cell += step)
                    {
                        road.SetTile(new Vector3Int(cell.x, cell.y, 0), roadTile);

                        if (cell == to)
                        {
                            break;
                        }
                    }
                }
            }

            GameObject pads = new GameObject("Build Spots");
            foreach (Vector2Int cell in Pads)
            {
                Place(padPrefab, new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f), pads.transform);
            }

            GameObject placedSystems = Place(systems, Vector3.zero);
            TowerWaveSpawner placedSpawner = placedSystems.GetComponentInChildren<TowerWaveSpawner>();
            Set(placedSpawner, "path", path);
            KeepChanges(placedSpawner);

            b.SaveScene(scene, MapName);
        }

        static TowerProjectile2D Projectile(SampleBuild b, string projectileName, Sprite sprite, Color color, GameObject hitEffect)
        {
            GameObject shot = SpriteObject(projectileName, sprite, 20);
            shot.GetComponent<SpriteRenderer>().color = color;
            TowerProjectile2D projectile = shot.AddComponent<TowerProjectile2D>();
            Set(projectile, "hitEffect", hitEffect);
            return b.Prefab(shot).GetComponent<TowerProjectile2D>();
        }

        static Tower2D TowerPrefab(SampleBuild b, string towerName, Sprite turretSprite, Color tint, Sprite circle)
        {
            GameObject tower = SpriteObject(towerName, Art(181), 5);

            // The turret's art points up; the Turret object points right, as Tower 2D expects.
            GameObject turret = Child(tower, "Turret");
            GameObject gun = SpriteObject("Gun", turretSprite, 6);
            gun.GetComponent<SpriteRenderer>().color = tint;
            gun.transform.SetParent(turret.transform, false);
            gun.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

            GameObject range = SpriteObject("Range", circle, 1);
            range.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.2f);
            range.transform.SetParent(tower.transform, false);

            Tower2D script = tower.AddComponent<Tower2D>();
            SetEnum(script, "target", "First");
            Set(script, "turret", turret.transform);
            Set(script, "rangeIndicator", range);
            return b.Prefab(tower).GetComponent<Tower2D>();
        }

        static TowerData Tower(SampleBuild b, string towerName, Tower2D prefab, int cost, float range, int damage, float shotsPerSecond,
            TowerProjectile2D projectile, float projectileSpeed, float splash, float slowTo, float slowSeconds, TowerData upgradesTo)
        {
            TowerData data = b.Asset<TowerData>("Towers", towerName);
            Set(data, "displayName", towerName);
            Set(data, "prefab", prefab);
            Set(data, "cost", cost);
            Set(data, "range", range);
            Set(data, "damage", damage);
            Set(data, "shotsPerSecond", shotsPerSecond);
            Set(data, "projectile", projectile);
            Set(data, "projectileSpeed", projectileSpeed);
            Set(data, "splashRadius", splash);
            Set(data, "slowTo", slowTo);
            Set(data, "slowSeconds", slowSeconds);
            Set(data, "upgradesTo", upgradesTo);
            return data;
        }

        static PathEnemy2D Enemy(SampleBuild b, string enemyName, Sprite sprite, float scale, int health, float speed, int reward, int livesTaken)
        {
            GameObject enemy = SpriteObject(enemyName, sprite, 10);
            enemy.transform.localScale = new Vector3(scale, scale, 1f);
            Health life = enemy.AddComponent<Health>();
            Set(life, "maxHealth", health);
            Set(life, "invulnerableTime", 0f);
            SetEnum(life, "whenHealthRunsOut", "Destroy");
            PathEnemy2D mover = enemy.AddComponent<PathEnemy2D>();
            Set(mover, "speed", speed);
            Set(mover, "reward", reward);
            Set(mover, "livesTaken", livesTaken);
            return b.Prefab(enemy).GetComponent<PathEnemy2D>();
        }
    }
}
