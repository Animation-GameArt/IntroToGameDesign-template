using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.EditorTools;
using CollinGame.TwoD;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.EndlessRunner.EditorTools
{
    /// <summary>
    /// Builds the Endless Runner Kit's sample: the runner, pickups, ten track chunks,
    /// the systems, the game scene and the menu, following "Endless Runner Kit —
    /// Building the Sample".
    /// Menu: Collin Game → Kit Samples → Build Endless Runner Sample.
    /// </summary>
    public static class EndlessRunnerSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/EndlessRunner";
        const string Pack = "kenney_pixel-platformer";
        const string MenuName = "Runner_Menu";
        const string GameName = "Runner_Game";

        static readonly Color Sky = new Color32(223, 246, 245, 255);

        const string HowToPlay =
            "Run as far as you can. The runner speeds up as you go.\n\n" +
            "Space, W or Up jumps: tap for a short hop, hold for a full jump. S or Down slides under low bars.\n\n" +
            "Gaps, crates, spikes and bars end the run. Coins add up; the blue gem pulls coins to you for a few seconds.\n\n" +
            "Esc pauses.";

        [MenuItem("Collin Game/Kit Samples/Build Endless Runner Sample")]
        static void Build()
        {
            Run("Endless Runner", KitFolder, new[] { "Data", "Prefabs", "Chunks", "Scenes" },
                new[] { Kenney + Pack }, BuildSample);
        }

        static Sprite Tile(string tile) => KenneySprite(Pack, "Tiles/" + tile);
        static Sprite Character(string tile) => KenneySprite(Pack, "Tiles/Characters/" + tile);
        static Sprite Background(string tile) => KenneySprite(Pack, "Tiles/Backgrounds/" + tile);

        sealed class Kit
        {
            public IntVariable Distance, Best, Coins;
            public VoidEventChannel Died;
            public GameObject Runner, Coin, Magnet, Crate, Spikes, LowBar, OneWay, Block;
        }

        static void Flipbook(GameObject go, float fps, params Sprite[] frames)
        {
            SpriteFlipbook flipbook = go.AddComponent<SpriteFlipbook>();
            Set(flipbook, "frames", frames);
            Set(flipbook, "framesPerSecond", fps);
        }

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — Data
            Kit k = new Kit
            {
                Distance = b.Int("Distance"),
                Best = b.Int("BestDistance"),
                Coins = b.Int("Coins"),
                Died = b.Channel("PlayerDied")
            };

            // Part 2 — The runner and pickups
            GameObject runner = SpriteObject("Runner", Character("tile_0000"), 10);
            runner.tag = "Player";
            Box(runner, new Vector2(0.7f, 1f), new Vector2(0f, -0.1f));
            RunnerController2D controller = runner.AddComponent<RunnerController2D>();
            Set(controller, "input", b.Input);
            SetEnum(controller, "style", "Jump");
            Set(controller, "startSpeed", 6f);
            Set(controller, "maxSpeed", 14f);
            Set(controller, "speedGainPerSecond", 0.1f);
            Set(controller, "startDelay", 1f);
            Set(controller, "distance", k.Distance);
            Set(controller, "jumpHeight", 3.2f);
            Set(controller, "canSlide", true);
            Set(controller, "slideHeight", 0.5f);
            Health health = runner.AddComponent<Health>();
            Set(health, "maxHealth", 1);
            Set(health, "invulnerableTime", 0f);
            SetEnum(health, "whenHealthRunsOut", "Nothing");
            Set(health, "raiseOnDied", k.Died);
            On(health, "onDied", Call(controller.Stop));
            CoinMagnet2D magnet = runner.AddComponent<CoinMagnet2D>();
            Set(magnet, "radius", 4f);
            Set(magnet, "alwaysOn", false);
            Flipbook(runner, 10f, Character("tile_0000"), Character("tile_0001"));
            k.Runner = b.Prefab(runner);

            GameObject coin = SpriteObject("Coin", Tile("tile_0151"), 3);
            TriggerCircle(coin, 0.3f);
            Set(coin.AddComponent<Collectible>(), "addTo", k.Coins);
            Flipbook(coin, 6f, Tile("tile_0151"), Tile("tile_0152"));
            k.Coin = b.Prefab(coin);

            GameObject gem = SpriteObject("Magnet", Tile("tile_0067"), 3);
            gem.GetComponent<SpriteRenderer>().color = new Color(0.45f, 0.65f, 1f);
            TriggerCircle(gem, 0.4f);
            Set(gem.AddComponent<MagnetPowerUp2D>(), "duration", 8f);
            k.Magnet = b.Prefab(gem);

            // Obstacles and pieces the chunks use (the Platformer sample's spikes and one-way platform, made here too).
            GameObject crate = SpriteObject("Crate", Tile("tile_0026"), 2);
            crate.AddComponent<BoxCollider2D>();
            crate.AddComponent<DamageOnTouch>();
            k.Crate = b.Prefab(crate);

            GameObject spikes = SpriteObject("Spikes", Tile("tile_0068"), 2);
            TriggerBox(spikes, new Vector2(0.8f, 0.4f), new Vector2(0f, -0.3f));
            spikes.AddComponent<DamageOnTouch>();
            k.Spikes = b.Prefab(spikes);

            GameObject bar = TiledSprite("Low Bar", Tile("tile_0028"), 2, new Vector2(3f, 1f));
            TriggerBox(bar, new Vector2(3f, 1f), Vector2.zero);
            bar.AddComponent<DamageOnTouch>();
            k.LowBar = b.Prefab(bar);

            GameObject oneWay = SpriteObject("One-Way Platform", Tile("tile_0146"), 1);
            BoxCollider2D plank = Box(oneWay, new Vector2(1f, 0.5f), new Vector2(0f, 0.25f));
            plank.usedByEffector = true;
            oneWay.AddComponent<PlatformEffector2D>().useOneWay = true;
            k.OneWay = b.Prefab(oneWay);

            GameObject block = SpriteObject("Block", Tile("tile_0000"), 1);
            block.AddComponent<BoxCollider2D>();
            // Running into a block's front ends the run, like a crate; landing on top is safe.
            GameObject front = Child(block, "Front");
            TriggerBox(front, new Vector2(0.2f, 0.9f), new Vector2(-0.45f, 0f));
            front.AddComponent<DamageOnTouch>();
            k.Block = b.Prefab(block);

            // Part 3 — Chunks
            TrackChunk first = BuildChunks(b, k, out List<TrackChunk> chunks);

            // Part 4 — Runner Systems
            GameObject systems = RunnerSystems(b, k, first, chunks);

            // Part 6 — Menu first, so it's first in the Scene List
            b.MenuScene(MenuName, "Dash", "Run, jump and slide as far as you can. It only gets faster.", GameName,
                new VariableBase[] { k.Distance, k.Coins }, Sky, HowToPlay);

            // Part 5 — The game scene
            Scene scene = NewScene();
            Camera camera = Camera2D(new Vector3(0f, 1f, -10f), 6f, Sky);
            camera.gameObject.AddComponent<CinemachineBrain>();
            GlobalLight2D();

            GameObject player = Place(k.Runner, new Vector3(0f, 1f, 0f));

            GameObject cameraObject = new GameObject("Runner Camera");
            CinemachineCamera follow = cameraObject.AddComponent<CinemachineCamera>();
            follow.Follow = player.transform;
            LensSettings lens = follow.Lens;
            lens.OrthographicSize = 6f;
            follow.Lens = lens;
            CinemachinePositionComposer composer = cameraObject.AddComponent<CinemachinePositionComposer>();
            ScreenComposerSettings composition = composer.Composition;
            composition.ScreenPosition = new Vector2(-0.25f, 0f);
            composer.Composition = composition;
            composer.Damping = new Vector3(0f, 1f, 0f);

            Place(systems, Vector3.zero);
            b.SaveScene(scene, GameName);
        }

        // ---------------------------------------------------------------- Part 3 — Chunks

        /// <summary>A chunk's root, with Track Chunk.</summary>
        static GameObject Chunk(string chunkName, float length, float minDifficulty, float maxDifficulty)
        {
            GameObject root = new GameObject(chunkName);
            TrackChunk chunk = root.AddComponent<TrackChunk>();
            Set(chunk, "length", length);
            Set(chunk, "minDifficulty", minDifficulty);
            Set(chunk, "maxDifficulty", maxDifficulty);
            return root;
        }

        /// <summary>Ground with its top at y = 0: a grass strip over earth, and one collider for both.</summary>
        static void Ground(GameObject chunk, float left, float width)
        {
            GameObject ground = TiledSprite("Ground", Tile("tile_0022"), 0, new Vector2(width, 1f));
            ground.transform.SetParent(chunk.transform, false);
            ground.transform.localPosition = new Vector3(left + width / 2f, -0.5f, 0f);
            Box(ground, new Vector2(width, 7f), new Vector2(0f, -3f));

            GameObject earth = TiledSprite("Earth", Tile("tile_0122"), 0, new Vector2(width, 6f));
            earth.transform.SetParent(ground.transform, false);
            earth.transform.localPosition = new Vector3(0f, -3.5f, 0f);
        }

        static GameObject Add(GameObject chunk, GameObject prefab, float x, float y)
        {
            GameObject placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            placed.transform.SetParent(chunk.transform, false);
            placed.transform.localPosition = new Vector3(x, y, 0f);
            return placed;
        }

        static TrackChunk Save(SampleBuild b, GameObject root)
        {
            string path = $"{b.Folder("Chunks")}/{root.name}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved.GetComponent<TrackChunk>();
        }

        static TrackChunk BuildChunks(SampleBuild b, Kit k, out List<TrackChunk> chunks)
        {
            chunks = new List<TrackChunk>();

            GameObject start = Chunk("Chunk Start", 30f, 0f, 1f);
            Ground(start, 0f, 30f);
            TrackChunk first = Save(b, start);

            GameObject flat = Chunk("Chunk Flat", 20f, 0f, 1f);
            Ground(flat, 0f, 20f);
            for (int i = 0; i < 5; i++) Add(flat, k.Coin, 8f + i, 0.5f);
            chunks.Add(Save(b, flat));

            GameObject gap = Chunk("Chunk Gap", 20f, 0f, 0.7f);
            Ground(gap, 0f, 9f);
            Ground(gap, 12f, 8f);
            Add(gap, k.Coin, 9.5f, 2.2f);
            Add(gap, k.Coin, 10.5f, 2.8f);
            Add(gap, k.Coin, 11.5f, 2.2f);
            chunks.Add(Save(b, gap));

            GameObject crates = Chunk("Chunk Crates", 20f, 0f, 1f);
            Ground(crates, 0f, 20f);
            Add(crates, k.Crate, 8f, 0.5f);
            Add(crates, k.Crate, 15f, 0.5f);
            chunks.Add(Save(b, crates));

            GameObject steps = Chunk("Chunk Steps", 20f, 0.3f, 1f);
            Ground(steps, 0f, 20f);
            Add(steps, k.Block, 6f, 0.5f);
            Add(steps, k.Block, 7f, 0.5f);
            Add(steps, k.Block, 10f, 0.5f);
            Add(steps, k.Block, 10f, 1.5f);
            Add(steps, k.Block, 11f, 0.5f);
            Add(steps, k.Block, 11f, 1.5f);
            Add(steps, k.Coin, 6.5f, 1.5f);
            Add(steps, k.Coin, 10.5f, 2.5f);
            chunks.Add(Save(b, steps));

            GameObject lowBar = Chunk("Chunk Low Bar", 20f, 0.3f, 1f);
            Ground(lowBar, 0f, 20f);
            Add(lowBar, k.LowBar, 10f, 1.2f);
            chunks.Add(Save(b, lowBar));

            GameObject doubleGap = Chunk("Chunk Double Gap", 24f, 0.3f, 1f);
            Ground(doubleGap, 0f, 7f);
            Ground(doubleGap, 10f, 5f);
            Ground(doubleGap, 18f, 6f);
            chunks.Add(Save(b, doubleGap));

            GameObject spikes = Chunk("Chunk Spikes", 20f, 0.6f, 1f);
            Ground(spikes, 0f, 20f);
            Add(spikes, k.Spikes, 5f, 0.5f);
            Add(spikes, k.Spikes, 11f, 0.5f);
            Add(spikes, k.Spikes, 17f, 0.5f);
            Add(spikes, k.Magnet, 10f, 3f);
            chunks.Add(Save(b, spikes));

            GameObject platforms = Chunk("Chunk Platforms", 24f, 0.6f, 1f);
            Ground(platforms, 0f, 6f);
            Ground(platforms, 20f, 4f);
            // Platform tops at heights 1, 2, then 1 again (the collider is the sprite's top half).
            foreach (float x in new[] { 7.5f, 8.5f, 9.5f }) Add(platforms, k.OneWay, x, 0.5f);
            foreach (float x in new[] { 12.5f, 13.5f, 14.5f }) Add(platforms, k.OneWay, x, 1.5f);
            foreach (float x in new[] { 17.5f, 18.5f }) Add(platforms, k.OneWay, x, 0.5f);
            Add(platforms, k.Coin, 13.5f, 2.5f);
            chunks.Add(Save(b, platforms));

            GameObject gauntlet = Chunk("Chunk Gauntlet", 24f, 0.8f, 1f);
            Ground(gauntlet, 0f, 24f);
            Add(gauntlet, k.Crate, 5f, 0.5f);
            Add(gauntlet, k.Spikes, 10f, 0.5f);
            Add(gauntlet, k.LowBar, 15f, 1.2f);
            Add(gauntlet, k.Crate, 20f, 0.5f);
            chunks.Add(Save(b, gauntlet));

            return first;
        }

        // ---------------------------------------------------------------- Part 4 — Runner Systems

        static GameObject RunnerSystems(SampleBuild b, Kit k, TrackChunk first, List<TrackChunk> chunks)
        {
            GameObject root = new GameObject("Runner Systems");

            GameObject track = Child(root, "Track");
            track.transform.localPosition = new Vector3(-10f, 0f, 0f);
            TrackSpawner2D spawner = track.AddComponent<TrackSpawner2D>();
            Set(spawner, "firstChunk", first);
            Set(spawner, "chunks", chunks);
            Set(spawner, "buildAhead", 40f);
            Set(spawner, "removeBehind", 20f);

            GameObject pit = Child(root, "Pit");
            pit.transform.localPosition = new Vector3(0f, -8f, 0f);
            TriggerBox(pit, new Vector2(100000f, 2f), Vector2.zero);
            pit.AddComponent<KillZone>();

            // Three background layers, far to near. Each is wide enough that the view never reaches an edge.
            GameObject background = Child(root, "Background");
            Layer(background, "Far Clouds", Background("tile_0008"), 2.5f, new Vector2(0f, 3.2f), 1, 0.9f, -30);
            Layer(background, "Far Haze", Background("tile_0016"), 2.5f, new Vector2(0f, -8.45f), 6, 0.9f, -30);
            Layer(background, "Middle Trees", Background("tile_0011"), 2f, new Vector2(0f, 1.2f), 1, 0.7f, -20);
            Layer(background, "Near Bushes", Background("tile_0014"), 1.5f, new Vector2(0f, 0.7f), 1, 0.4f, -10);

            GameObject hud = Child(root, "HUD");
            b.Document(hud, KitFolder + "/UI/RunnerHUD.uxml", 0);
            Bind(hud, ("distance-label", k.Distance, "{0} m"), ("best-label", k.Best, "Best: {0} m"), ("coins-label", k.Coins, "Coins: {0}"));

            HighScore best = Child(root, "Best").AddComponent<HighScore>();
            Set(best, "score", k.Distance);
            Set(best, "best", k.Best);
            Set(best, "saveName", "Runner");

            Resetter(Child(root, "Reset"), true, k.Distance, k.Coins);

            Systems s = b.StandardSystems(root, MenuName);
            b.EndScreen(s, "Game Over", false, k.Died, new (string, VariableBase, string)[] { ("score-label", k.Distance, "{0} m") }, null);
            return b.Prefab(root);
        }

        static void Layer(GameObject parent, string layerName, Sprite sprite, float scale, Vector2 position, int tilesHigh, float distance, int order)
        {
            float tile = sprite.bounds.size.x;
            GameObject layer = TiledSprite(layerName, sprite, order, new Vector2(tile * 30f, tile * tilesHigh));
            layer.transform.SetParent(parent.transform, false);
            layer.transform.localPosition = position;
            layer.transform.localScale = new Vector3(scale, scale, 1f);
            Set(layer.AddComponent<Parallax2D>(), "distance", distance);
        }
    }
}
