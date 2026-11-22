using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.GridPuzzle.EditorTools
{
    /// <summary>
    /// Builds the Grid Puzzle Kit's sample, eight Sokoban levels, following
    /// "Grid Puzzle Kit — Building the Sample". The levels are drawn as the text
    /// maps below; change one and run the command again to rebuild.
    /// Menu: Collin Game → Kit Samples → Build Grid Puzzle Sample.
    /// </summary>
    public static class GridPuzzleSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/GridPuzzle";
        const string Sokoban = "kenney_sokoban-pack";
        const string MenuScene = "Puzzle_Menu";
        const string SelectScene = "Puzzle_Select";
        static readonly Color Background = new Color(0.12f, 0.13f, 0.17f);

        // # wall  . floor  P player  B box  T target  * box on a target  R red box  r red target
        // O rock  G gem  E exit. The last value is Need All Items.
        static readonly (string title, bool needAllItems, string[] map)[] Levels =
        {
            ("First Push", false, new[] { "#######", "#.....#", "#.PB.T#", "#.....#", "#######" }),
            ("Around the Corner", false, new[] { "#######", "#..T..#", "#..#..#", "#.PB..#", "#.....#", "#######" }),
            ("Two Boxes", false, new[] { "########", "#......#", "#.B..T.#", "#.P##..#", "#.B..T.#", "#......#", "########" }),
            ("Corners Are Traps", false, new[] { "#######", "#T...##", "#.#B..#", "#..P..#", "##.B.T#", "#######" }),
            ("Rocks", false, new[] { "########", "#..O..T#", "#.B.O..#", "#.P..B.#", "#O...O.#", "#T.....#", "########" }),
            ("Red Box", false, new[] { "########", "#r....T#", "#..R.B.#", "#.P....#", "########" }),
            ("Collect the Gems", true, new[] { "########", "#G..B.E#", "#.##.#.#", "#P.B..G#", "########" }),
            ("Tight Room", false, new[] { "#######", "##..T##", "#.B.B.#", "#T.P.T#", "#.B...#", "##....#", "#######" }),
        };

        [MenuItem("Collin Game/Kit Samples/Build Grid Puzzle Sample")]
        static void Build()
        {
            Run("Grid Puzzle", KitFolder, new[] { "Data", "Prefabs", "Tiles", "Scenes" },
                new[] { Kenney + Sokoban }, BuildSample);
        }

        static string LevelScene(int index) => $"Puzzle_{index + 1:00}";

        static Sprite Art(string path) => KenneySprite(Sokoban, "PNG/Default size/" + path);

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — Data
            IntVariable moves = b.Int("Moves");
            StringVariable levelTitle = b.Text("LevelTitle");
            VoidEventChannel solved = b.Channel("LevelSolved");

            PuzzleLevelList levelList = b.Asset<PuzzleLevelList>("Data", "Puzzle Levels");
            Set(levelList, "saveName", "SampleSokoban");
            List<string> sceneNames = new List<string>();
            List<string> titles = new List<string>();

            for (int i = 0; i < Levels.Length; i++)
            {
                sceneNames.Add(LevelScene(i));
                titles.Add(Levels[i].title);
            }

            Set(levelList, "levels", sceneNames);
            Set(levelList, "titles", titles);

            // Part 2 — Prefabs
            GameObject Piece(string pieceName, string sprite, GridPiece.Role role, string kind, int order)
            {
                GameObject go = SpriteObject(pieceName, Art(sprite), order);
                GridPiece piece = go.AddComponent<GridPiece>();
                Set(piece, "role", role);
                Set(piece, "kind", kind);
                Set(piece, "moveTime", 0.1f);
                return b.Prefab(go);
            }

            GameObject Goal(string goalName, string sprite, GridPiece.Role filledBy, string kind)
            {
                GameObject go = SpriteObject(goalName, Art(sprite), -5);
                GridGoal goal = go.AddComponent<GridGoal>();
                Set(goal, "filledBy", filledBy);
                Set(goal, "kind", kind);
                Set(goal, "indicator", go.GetComponent<SpriteRenderer>());
                Set(goal, "filledColor", new Color(0.4f, 1f, 0.5f));
                return b.Prefab(go);
            }

            Dictionary<char, GameObject> pieces = new Dictionary<char, GameObject>
            {
                ['P'] = Piece("Player", "Player/player_05", GridPiece.Role.Player, "", 5),
                ['B'] = Piece("Box", "Crates/crate_02", GridPiece.Role.Pushable, "", 0),
                ['R'] = Piece("Red Box", "Crates/crate_03", GridPiece.Role.Pushable, "red", 0),
                ['O'] = Piece("Rock", "Blocks/block_02", GridPiece.Role.Solid, "", 0),
                ['G'] = Piece("Gem", "Environment/environment_11", GridPiece.Role.Item, "", 0),
            };

            Dictionary<char, GameObject> goals = new Dictionary<char, GameObject>
            {
                ['T'] = Goal("Target", "Environment/environment_02", GridPiece.Role.Pushable, ""),
                ['r'] = Goal("Red Target", "Environment/environment_05", GridPiece.Role.Pushable, "red"),
                ['E'] = Goal("Exit", "Environment/environment_13", GridPiece.Role.Player, ""),
            };

            Tile floorTile = b.TileFrom(Art("Ground/ground_01"), "Floor");
            Tile wallTile = b.TileFrom(Art("Blocks/block_06"), "Wall");

            // Part 3 — Puzzle Systems
            GameObject root = new GameObject("Puzzle Systems");
            Systems systems = b.StandardSystems(root, SelectScene);

            GameObject boardObject = Child(root, "Board");
            GridBoard board = boardObject.AddComponent<GridBoard>();
            Set(board, "maxPushedInARow", 1);
            Set(board, "moves", moves);
            Set(board, "levelList", levelList);
            Set(board, "levelTitle", levelTitle);
            Set(board, "raiseWhenSolved", solved);
            Set(boardObject.AddComponent<GridPlayerInput>(), "input", b.Input);

            GameObject hud = Child(root, "HUD");
            b.Document(hud, KitFolder + "/UI/PuzzleHUD.uxml", 0);
            Bind(hud, ("level-label", levelTitle, "{0}"), ("moves-label", moves, "Moves: {0}"));

            GameObject loaderObject = Child(root, "Loader");
            PuzzleLevelLoader loader = loaderObject.AddComponent<PuzzleLevelLoader>();
            Set(loader, "levelList", levelList);
            Set(loader, "afterLastLevel", SelectScene);
            SceneChanger toSelect = loaderObject.AddComponent<SceneChanger>();
            Set(toSelect, "sceneName", SelectScene);

            GameObject solvedObject = Child(root, "Solved");
            b.Document(solvedObject, KitFolder + "/UI/PuzzleSolved.uxml", 12);
            UIScreen solvedScreen = MakeScreen(solvedObject, true, "next-button");
            Listen(solvedObject, solved, Call(solvedScreen.Show));
            Bind(solvedObject, ("moves-label", moves, "Solved in {0} moves"));
            UIButtonEvents solvedButtons = solvedObject.AddComponent<UIButtonEvents>();
            AddButton(solvedButtons, "next-button", Call(loader.LoadNextLevel));
            AddButton(solvedButtons, "select-button", Call(toSelect.Load));

            GameObject systemsPrefab = b.Prefab(root);

            // Part 6 — Menu and level select (first, for the Scene List)
            b.MenuScene(MenuScene, "Crate Mover", "Push every crate onto its target, in as few moves as you can.", null, null, Background,
                "Push every crate onto a target to solve the level.\n\n" +
                "Move with the arrow keys, WASD, or the left stick. Walk into a crate to push it. You can only push one crate at a time, and you can't pull.\n\n" +
                "A crate pushed into a corner is stuck. Press Z (or Backspace) to undo a move, or R to restart the level.\n\n" +
                "Red crates only fit red targets. In some levels, collect every coin and reach the exit instead.\n\n" +
                "Solving a level unlocks the next. Choose any unlocked level from Levels.",
                new[] { ("levels-button", "Levels") },
                (menu, buttons) =>
                {
                    PuzzleLevelLoader menuLoader = menu.AddComponent<PuzzleLevelLoader>();
                    Set(menuLoader, "levelList", levelList);
                    SceneChanger levels = menu.AddComponent<SceneChanger>();
                    Set(levels, "sceneName", SelectScene);
                    AddButton(buttons, "play-button", Call(menuLoader.LoadFirstUnsolved));
                    AddButton(buttons, "levels-button", Call(levels.Load));
                });

            Scene select = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, Background);
            GameObject selectScreen = new GameObject("Level Select");
            b.Document(selectScreen, KitFolder + "/UI/LevelSelect.uxml", 0);
            PuzzleLevelLoader selectLoader = selectScreen.AddComponent<PuzzleLevelLoader>();
            Set(selectLoader, "levelList", levelList);
            Set(selectScreen.AddComponent<UILevelSelect>(), "loader", selectLoader);
            SceneChanger back = selectScreen.AddComponent<SceneChanger>();
            Set(back, "sceneName", MenuScene);
            AddButton(selectScreen.AddComponent<UIButtonEvents>(), "back-button", Call(back.Load));
            b.SaveScene(select, SelectScene);

            // Parts 4 and 5 — the levels
            for (int i = 0; i < Levels.Length; i++)
            {
                BuildLevel(b, Levels[i].map, Levels[i].needAllItems, LevelScene(i), systemsPrefab, pieces, goals, floorTile, wallTile);
            }
        }

        static void BuildLevel(SampleBuild b, string[] map, bool needAllItems, string sceneName, GameObject systemsPrefab,
            Dictionary<char, GameObject> pieces, Dictionary<char, GameObject> goals, Tile floorTile, Tile wallTile)
        {
            Scene scene = NewScene();
            int height = map.Length;
            int width = map[0].Length;
            float size = Mathf.Max(height * 0.5f + 1f, (width * 0.5f + 1f) / (16f / 9f)) + 0.5f;
            Camera2D(new Vector3(width / 2f, height / 2f - 0.4f, -10f), size, Background);

            GameObject grid = GridObject();
            Tilemap floor = TilemapObject(grid, "Floor", -10);
            Tilemap walls = TilemapObject(grid, "Walls", -1);
            GameObject things = new GameObject("Pieces");

            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = map[row][x];
                    int y = height - 1 - row;
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    Vector3 center = new Vector3(x + 0.5f, y + 0.5f, 0f);

                    if (c == '#')
                    {
                        walls.SetTile(cell, wallTile);
                        continue;
                    }

                    floor.SetTile(cell, floorTile);

                    if (c == '*')
                    {
                        Place(goals['T'], center, things.transform);
                        Place(pieces['B'], center, things.transform);
                    }
                    else if (goals.TryGetValue(c, out GameObject goal))
                    {
                        Place(goal, center, things.transform);
                    }
                    else if (pieces.TryGetValue(c, out GameObject piece))
                    {
                        Place(piece, center, things.transform);
                    }
                    else if (c != '.')
                    {
                        throw new SampleBuildProblem($"{sceneName}'s map has an unknown character '{c}'.");
                    }
                }
            }

            GameObject systems = Place(systemsPrefab, Vector3.zero);
            GridBoard board = systems.GetComponentInChildren<GridBoard>();
            Set(board, "walls", walls);
            Set(board, "needAllItems", needAllItems);
            KeepChanges(board);
            b.SaveScene(scene, sceneName);
        }
    }
}
