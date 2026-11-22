using System.Collections.Generic;
using CollinGame.Core;
using CollinGame.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using Yarn.Unity.Editor;
using static CollinGame.EditorTools.SampleBuild;

namespace CollinGame.Kits.VisualNovel.EditorTools
{
    /// <summary>
    /// Builds the Visual Novel Kit's sample, Lantern Festival, following
    /// "Visual Novel Kit — Building the Sample". The story script,
    /// Sample/Story/LanternFestival.yarn, is kept.
    /// Menu: Collin Game → Kit Samples → Build Visual Novel Sample.
    /// </summary>
    public static class VisualNovelSampleBuilder
    {
        const string KitFolder = "Assets/_Course/Kits/VisualNovel";
        const string Dungeon = "kenney_tiny-dungeon";
        const string MenuScene = "Story_Menu";
        const string GameScene = "Story_Game";

        [MenuItem("Collin Game/Kit Samples/Build Visual Novel Sample")]
        static void Build()
        {
            Run("Visual Novel", KitFolder, new[] { "Data", "Characters", "Prefabs", "Scenes" },
                new[] { Kenney + Dungeon }, new[] { "Story/LanternFestival.yarn" }, BuildSample);
        }

        static void BuildSample(SampleBuild b)
        {
            // Part 1 — The Yarn Project, beside the story
            string projectPath = b.SampleFolder + "/Story/LanternFestival.yarnproject";
            Yarn.Compiler.Project project = new Yarn.Compiler.Project();
            project.ExcludeFilePatterns = new[] { "**/*~/*" };
            YarnEditorUtility.CreateYarnProject(projectPath, project);
            YarnProject yarnProject = AssetDatabase.LoadAssetAtPath<YarnProject>(projectPath);

            if (yarnProject == null)
            {
                throw new SampleBuildProblem($"The Yarn Project at {projectPath} didn't import. Click it to see Yarn's errors.");
            }

            // Part 2 — Data
            IntVariable ren = b.Int("RenAffection");
            IntVariable mika = b.Int("MikaAffection");
            VoidEventChannel renEnding = b.Channel("RenEnding");
            VoidEventChannel mikaEnding = b.Channel("MikaEnding");
            VoidEventChannel aloneEnding = b.Channel("AloneEnding");

            // Part 3 — Characters
            CharacterData Character(string characterName, Color color, string portrait, StagePosition position)
            {
                CharacterData character = b.Asset<CharacterData>("Characters", characterName);
                Set(character, "characterName", characterName);
                Set(character, "nameColor", color);
                Set(character, "portraits", new List<CharacterData.Portrait>
                {
                    new CharacterData.Portrait { mood = "neutral", sprite = KenneySprite(Dungeon, "Tiles/" + portrait) }
                });
                Set(character, "defaultPosition", position);
                return character;
            }

            CharacterData renCharacter = Character("Ren", new Color(1f, 0.62f, 0.42f), "tile_0099", StagePosition.Left);
            CharacterData mikaCharacter = Character("Mika", new Color(0.5f, 0.78f, 1f), "tile_0085", StagePosition.Right);

            // Part 4 — Story Systems
            GameObject root = new GameObject("Story Systems");
            Systems systems = b.StandardSystems(root, MenuScene);

            GameObject dialogue = Child(root, "Dialogue");
            DialogueRunner runner = dialogue.AddComponent<DialogueRunner>();
            Set(runner, "yarnProject", yarnProject);
            runner.startNode = "Start";
            runner.autoStart = true;
            b.Document(dialogue, KitFolder + "/UI/VisualNovel.uxml", 0);

            UIDialoguePresenter presenter = dialogue.AddComponent<UIDialoguePresenter>();
            Set(presenter, "lettersPerSecond", 45f);
            Set(presenter, "showUnavailableChoices", true);
            Set(presenter, "stopPlayerWhileTalking", true);

            VisualNovelStage stage = dialogue.AddComponent<VisualNovelStage>();
            Set(presenter, "stage", stage);
            Set(runner, "dialoguePresenters", new List<DialoguePresenterBase> { presenter });
            Set(stage, "runner", runner);
            Set(stage, "characters", new List<CharacterData> { renCharacter, mikaCharacter });
            Set(stage, "dimOthers", true);
            Set(stage, "backgrounds", new List<VisualNovelStage.Background>
            {
                new VisualNovelStage.Background { name = "Square", color = Hex("2B2547") },
                new VisualNovelStage.Background { name = "Stalls", color = Hex("5A2E2E") },
                new VisualNovelStage.Background { name = "Bridge", color = Hex("16304A") },
            });
            Set(stage, "startingBackground", "Square");
            Set(stage, "events", new List<VisualNovelStage.NamedEvent>
            {
                new VisualNovelStage.NamedEvent { name = "RenEnding", channel = renEnding },
                new VisualNovelStage.NamedEvent { name = "MikaEnding", channel = mikaEnding },
                new VisualNovelStage.NamedEvent { name = "AloneEnding", channel = aloneEnding },
            });
            Set(stage, "sounds", new List<VisualNovelStage.NamedClip>());
            Set(stage, "music", new List<VisualNovelStage.NamedClip>());

            YarnVariableLink link = dialogue.AddComponent<YarnVariableLink>();
            Set(link, "runner", runner);
            Set(link, "links", new List<YarnVariableLink.Link>
            {
                new YarnVariableLink.Link { yarnVariable = "$ren", variable = ren },
                new YarnVariableLink.Link { yarnVariable = "$mika", variable = mika },
            });
            Set(link, "twoWay", false);

            UIRelationshipMeters meters = dialogue.AddComponent<UIRelationshipMeters>();
            Set(meters, "meters", new List<UIRelationshipMeters.Meter>
            {
                new UIRelationshipMeters.Meter { character = renCharacter, value = ren, max = 6 },
                new UIRelationshipMeters.Meter { character = mikaCharacter, value = mika, max = 6 },
            });
            Set(meters, "showNumbers", true);

            VariableBase[] all = { ren, mika };
            Resetter(Child(root, "Reset"), true, all);

            b.EndScreen(systems, "Ren Ending", true, renEnding, null, all, "Ren's Lantern", "You and Ren, side by side.");
            b.EndScreen(systems, "Mika Ending", true, mikaEnding, null, all, "Mika's Lantern", "The bridge lantern stayed lit.");
            b.EndScreen(systems, "Alone Ending", true, aloneEnding, null, all, "A Quiet Night", "Maybe next year.");

            GameObject systemsPrefab = b.Prefab(root);

            // Part 5 — Scenes
            b.MenuScene(MenuScene, "Lantern Festival", "Visual Novel Kit sample", GameScene, all, Hex("2B2547"));

            Scene game = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, Color.black);
            Place(systemsPrefab, Vector3.zero);
            b.SaveScene(game, GameScene);
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
