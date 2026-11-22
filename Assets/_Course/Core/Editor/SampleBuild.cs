using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CollinGame.Core;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CollinGame.EditorTools
{
    /// <summary>A problem that stops a sample build, with a message for the instructor.</summary>
    public class SampleBuildProblem : Exception
    {
        public SampleBuildProblem(string message) : base(message) { }
    }

    /// <summary>
    /// Shared parts of the kits' "Build ... Sample" menu commands: creating the
    /// Sample folder, data assets, prefabs and scenes; the standard screens (pause,
    /// settings, won, lost, the menu scene); wiring buttons and events; and setting
    /// private Inspector fields. Each kit's builder follows its "Building the
    /// Sample" guide using these.
    /// </summary>
    public sealed class SampleBuild
    {
        public const string CoreUI = "Assets/_Course/Core/UI/";
        public const string InputReaderPath = "Assets/_Course/Core/Input/InputReader.asset";
        public const string CreditsPath = "Assets/_Course/Credits/Course Credits.txt";
        public const string Kenney = "Assets/_Course/Art/Kenney/";

        public readonly string Title;
        public readonly string KitFolder;
        public readonly string SampleFolder;
        public InputReader Input { get; private set; }
        public PanelSettings Panel { get; private set; }

        readonly List<string> scenes = new List<string>();
        readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

        SampleBuild(string title, string kitFolder)
        {
            Title = title;
            KitFolder = kitFolder;
            SampleFolder = kitFolder + "/Sample";
        }

        /// <summary>The scenes saved so far, in order. They're added to the Scene List at the end.</summary>
        public IReadOnlyList<string> Scenes => scenes;

        // ---------------------------------------------------------------- Running a build

        /// <summary>
        /// Asks to continue, replaces the Sample folder, runs the kit's build, adds its
        /// scenes to the Scene List, and opens the first scene.
        /// </summary>
        public static void Run(string title, string kitFolder, string[] folders, string[] artNeeded, Action<SampleBuild> build) =>
            Run(title, kitFolder, folders, artNeeded, new string[0], build);

        /// <summary>
        /// As Run, keeping some files already in the Sample folder (paths inside it, such as
        /// "Story/LanternFestival.yarn"): they're moved aside while the folder is replaced,
        /// then put back with the same GUIDs.
        /// </summary>
        public static void Run(string title, string kitFolder, string[] folders, string[] artNeeded, string[] keep, Action<SampleBuild> build)
        {
            SampleBuild b = new SampleBuild(title, kitFolder);
            string dialogTitle = $"Build {title} Sample";

            if (!EditorUtility.DisplayDialog(dialogTitle,
                    $"This creates the {title} Kit's sample in {b.SampleFolder}: its data, prefabs and scenes, and adds the scenes to the Scene List.\n\n" +
                    (AssetDatabase.IsValidFolder(b.SampleFolder) ? "The existing Sample folder will be replaced, including anything built there by hand.\n\n" : "") +
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
                foreach (string art in artNeeded)
                {
                    if (!AssetDatabase.IsValidFolder(art))
                    {
                        throw new SampleBuildProblem($"The art isn't at {art}. Pull the template project, which includes it.");
                    }
                }

                // Start from an empty scene, so no open scene is inside the folder being replaced.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // Move the files to keep out of the way.
                string holding = kitFolder + "/KeptWhileBuilding";
                List<string> kept = new List<string>();

                foreach (string relative in keep)
                {
                    string from = $"{b.SampleFolder}/{relative}";

                    if (AssetDatabase.LoadMainAssetAtPath(from) == null)
                    {
                        throw new SampleBuildProblem($"{from} is missing. Pull the template project, which includes it.");
                    }

                    if (!AssetDatabase.IsValidFolder(holding))
                    {
                        AssetDatabase.CreateFolder(kitFolder, "KeptWhileBuilding");
                    }

                    string error = AssetDatabase.MoveAsset(from, $"{holding}/{Path.GetFileName(relative)}");

                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new SampleBuildProblem($"Couldn't move {from} aside: {error}");
                    }

                    kept.Add(relative);
                }

                if (AssetDatabase.IsValidFolder(b.SampleFolder))
                {
                    AssetDatabase.DeleteAsset(b.SampleFolder);
                }

                AssetDatabase.CreateFolder(kitFolder, "Sample");

                // Put them back.
                foreach (string relative in kept)
                {
                    string folder = Path.GetDirectoryName(relative)?.Replace('\\', '/');

                    if (!string.IsNullOrEmpty(folder))
                    {
                        b.Folder(folder);
                    }

                    AssetDatabase.MoveAsset($"{holding}/{Path.GetFileName(relative)}", $"{b.SampleFolder}/{relative}");
                }

                if (AssetDatabase.IsValidFolder(holding))
                {
                    AssetDatabase.DeleteAsset(holding);
                }

                foreach (string folder in folders)
                {
                    b.Folder(folder);
                }

                b.Input = Load<InputReader>(InputReaderPath);
                b.Panel = CoursePanelSettingsTool.GetOrCreate();

                build(b);

                b.AddScenesToList();
                AssetDatabase.SaveAssets();

                if (b.scenes.Count > 0)
                {
                    EditorSceneManager.OpenScene(b.scenes[0]);
                }

                Debug.Log($"{title} sample built in {b.SampleFolder}. Press Play to start from the menu, and check it against the Test part of \"{title} Kit — Building the Sample\".");
                EditorUtility.DisplayDialog(dialogTitle, "Done. The menu scene is open: press Play.\n\nIts scenes are at the end of the Scene List.", "OK");
            }
            catch (SampleBuildProblem problem)
            {
                Debug.LogError($"{title} sample: {problem.Message}");
                EditorUtility.DisplayDialog(dialogTitle, problem.Message, "OK");
            }
        }

        /// <summary>Creates a folder inside Sample (and any folders above it).</summary>
        public string Folder(string relative)
        {
            string path = SampleFolder;

            foreach (string part in relative.Split('/'))
            {
                string next = path + "/" + part;

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(path, part);
                }

                path = next;
            }

            return path;
        }

        // ---------------------------------------------------------------- Assets

        /// <summary>Creates a ScriptableObject asset in a Sample subfolder.</summary>
        public T Asset<T>(string folder, string assetName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, $"{Folder(folder)}/{assetName}.asset");
            return asset;
        }

        public IntVariable Int(string assetName, int initial = 0)
        {
            IntVariable v = Asset<IntVariable>("Data", assetName);
            Set(v, "initialValue", initial);
            return v;
        }

        public FloatVariable Float(string assetName, float initial = 0f)
        {
            FloatVariable v = Asset<FloatVariable>("Data", assetName);
            Set(v, "initialValue", initial);
            return v;
        }

        public StringVariable Text(string assetName, string initial = "")
        {
            StringVariable v = Asset<StringVariable>("Data", assetName);
            Set(v, "initialValue", initial);
            return v;
        }

        public VoidEventChannel Channel(string assetName) => Asset<VoidEventChannel>("Data", assetName);

        /// <summary>Saves a GameObject as a prefab in Sample/Prefabs, destroys the scene copy, and returns the prefab.</summary>
        public GameObject Prefab(GameObject go)
        {
            string path = $"{Folder("Prefabs")}/{go.name}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            prefabs[saved.name] = saved;
            return saved;
        }

        public GameObject GetPrefab(string prefabName)
        {
            if (!prefabs.TryGetValue(prefabName, out GameObject prefab))
            {
                throw new SampleBuildProblem($"The builder has no prefab called {prefabName}.");
            }

            return prefab;
        }

        /// <summary>Places a prefab instance in the open scene.</summary>
        public static GameObject Place(GameObject prefab, Vector3 position, Transform parent = null)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            instance.transform.position = position;
            return instance;
        }

        /// <summary>
        /// Writes a copy of a UXML file into Sample/UI with some text replaced (such as a
        /// title), and returns it.
        /// </summary>
        public VisualTreeAsset UxmlCopy(string sourcePath, string newName, params (string from, string to)[] replacements)
        {
            string text = File.ReadAllText(sourcePath);

            foreach ((string from, string to) in replacements)
            {
                if (!text.Contains(from))
                {
                    throw new SampleBuildProblem($"{sourcePath} doesn't contain \"{from}\".");
                }

                text = text.Replace(from, to);
            }

            string path = $"{Folder("UI")}/{newName}.uxml";
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path);
            return Load<VisualTreeAsset>(path);
        }

        // ---------------------------------------------------------------- Scenes

        public static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        public string ScenePath(string sceneName) => $"{SampleFolder}/Scenes/{sceneName}.unity";

        /// <summary>Saves the scene in Sample/Scenes, and remembers it for the Scene List.</summary>
        public void SaveScene(Scene scene, string sceneName)
        {
            Folder("Scenes");
            string path = ScenePath(sceneName);

            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new SampleBuildProblem($"Couldn't save {sceneName}.");
            }

            if (!scenes.Contains(path))
            {
                scenes.Add(path);
            }
        }

        void AddScenesToList()
        {
            List<EditorBuildSettingsScene> list = EditorBuildSettings.scenes.Where(s => !scenes.Contains(s.path)).ToList();
            list.AddRange(scenes.Select(p => new EditorBuildSettingsScene(p, true)));
            EditorBuildSettings.scenes = list.ToArray();
        }

        /// <summary>An orthographic Main Camera for a 2D scene.</summary>
        public static Camera Camera2D(Vector3 position, float size, Color background)
        {
            GameObject go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = position;
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            go.AddComponent<AudioListener>();
            return camera;
        }

        /// <summary>A Global Light 2D. The Light 2D type is found by name, as its assembly has been renamed between versions.</summary>
        public static void GlobalLight2D()
        {
            Type lightType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("UnityEngine.Rendering.Universal.Light2D", false))
                .FirstOrDefault(type => type != null);

            if (lightType == null)
            {
                Debug.LogWarning("Sample build: couldn't find the Light 2D component. Add one with GameObject → Light → Global Light 2D.");
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

        // ---------------------------------------------------------------- Screens

        public UIDocument Document(GameObject go, string uxmlPath, int sortOrder) => Document(go, Load<VisualTreeAsset>(uxmlPath), sortOrder);

        public UIDocument Document(GameObject go, VisualTreeAsset uxml, int sortOrder)
        {
            UIDocument document = go.AddComponent<UIDocument>();
            document.panelSettings = Panel;
            document.visualTreeAsset = uxml;
            document.sortingOrder = sortOrder;
            return document;
        }

        public static UIScreen MakeScreen(GameObject go, bool hiddenAtStart, string selectWhenShown)
        {
            UIScreen screen = go.AddComponent<UIScreen>();
            Set(screen, "hiddenAtStart", hiddenAtStart);
            Set(screen, "selectWhenShown", selectWhenShown);
            return screen;
        }

        /// <summary>A Settings screen (volume and full screen), hidden until shown; Back hides it.</summary>
        public UIScreen Settings(Transform parent, int sortOrder)
        {
            GameObject settings = new GameObject("Settings");

            if (parent != null)
            {
                settings.transform.SetParent(parent, false);
            }

            Document(settings, CoreUI + "Settings.uxml", sortOrder);
            UIScreen screen = MakeScreen(settings, true, "music-slider");
            settings.AddComponent<UISettings>();
            AddButton(settings.AddComponent<UIButtonEvents>(), "back-button", Call(screen.Hide));
            return screen;
        }

        /// <summary>The objects every game scene's Systems has: Scene Changer, To Menu, Pause Menu and Settings.</summary>
        public sealed class Systems
        {
            public GameObject Root;
            public SceneChanger SceneChanger;
            public SceneChanger ToMenu;
            public PauseController Pause;
        }

        public Systems StandardSystems(GameObject root, string menuScene)
        {
            Systems s = new Systems { Root = root };
            s.SceneChanger = Child(root, "Scene Changer").AddComponent<SceneChanger>();
            s.ToMenu = Child(root, "To Menu").AddComponent<SceneChanger>();
            Set(s.ToMenu, "sceneName", menuScene);

            GameObject pauseMenu = Child(root, "Pause Menu");
            Document(pauseMenu, CoreUI + "PauseMenu.uxml", 10);
            UIScreen pauseScreen = MakeScreen(pauseMenu, true, "resume-button");
            s.Pause = pauseMenu.AddComponent<PauseController>();
            Set(s.Pause, "input", Input);
            Set(s.Pause, "pauseScreen", pauseScreen);

            UIScreen settings = Settings(root.transform, 11);

            UIButtonEvents buttons = pauseMenu.AddComponent<UIButtonEvents>();
            AddButton(buttons, "resume-button", Call(s.Pause.Resume));
            AddButton(buttons, "settings-button", Call(settings.Show));
            AddButton(buttons, "restart-button", Call(s.SceneChanger.ReloadScene));
            AddButton(buttons, "menu-button", Call(s.ToMenu.Load));
            return s;
        }

        /// <summary>
        /// A hidden end screen (Core's EndScreen for a win, GameOver for a loss), shown when
        /// the channel is raised, which also turns off the player's controls. The first
        /// button (Play Again or Try Again) resets the given variables and reloads the scene;
        /// Main Menu goes to the menu. Extra wiring runs when it's shown.
        /// </summary>
        public UIScreen EndScreen(Systems systems, string objectName, bool won, VoidEventChannel showOn,
            (string label, VariableBase variable, string format)[] bindings, VariableBase[] resetOnRetry,
            string title = null, string subtitle = null, params Action<UnityEvent>[] alsoWhenShown)
        {
            GameObject go = Child(systems.Root, objectName);
            string source = CoreUI + (won ? "EndScreen.uxml" : "GameOver.uxml");
            string firstButton = won ? "play-again-button" : "try-again-button";
            bool bound = bindings != null && bindings.Length > 0;

            // The screen's title, and the line under it (unless a variable is shown there).
            List<(string, string)> changes = new List<(string, string)>();

            if (title != null)
            {
                changes.Add((won ? "text=\"You Win!\"" : "text=\"Game Over\"", $"text=\"{title}\""));
            }

            if (!bound)
            {
                changes.Add(("text=\"Score: 0\"", $"text=\"{subtitle ?? ""}\""));
            }

            if (changes.Count > 0)
            {
                Document(go, UxmlCopy(source, $"{SceneManager.GetActiveScene().name}{objectName}".Replace(" ", ""), changes.ToArray()), 12);
            }
            else
            {
                Document(go, source, 12);
            }

            UIScreen screen = MakeScreen(go, true, firstButton);

            if (bound)
            {
                Bind(go, bindings);
            }

            List<Action<UnityEvent>> onShow = new List<Action<UnityEvent>> { Call(screen.Show), Call(Input.DisablePlayerInput) };
            onShow.AddRange(alsoWhenShown);
            Listen(go, showOn, onShow.ToArray());

            UIButtonEvents buttons = go.AddComponent<UIButtonEvents>();
            List<Action<UnityEvent>> retry = new List<Action<UnityEvent>>();

            if (resetOnRetry != null && resetOnRetry.Length > 0)
            {
                VariableResetter resetter = go.AddComponent<VariableResetter>();
                Set(resetter, "variables", resetOnRetry);
                Set(resetter, "resetWhenSceneStarts", false);
                retry.Add(Call(resetter.ResetAll));
            }

            retry.Add(Call(systems.SceneChanger.ReloadScene));
            AddButton(buttons, firstButton, retry.ToArray());
            AddButton(buttons, "menu-button", Call(systems.ToMenu.Load));
            return screen;
        }

        /// <summary>
        /// The menu scene: a title, Play (loading the first game scene), Settings, Credits
        /// and Quit, and a Variable Resetter that starts a new game on every visit.
        /// </summary>
        public void MenuScene(string sceneName, string title, string subtitle, string playScene, VariableBase[] newGame, Color background)
        {
            Scene scene = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, background);

            GameObject credits = new GameObject("Credits");
            Document(credits, CoreUI + "Credits.uxml", 1);
            UIScreen creditsScreen = MakeScreen(credits, true, "back-button");
            Set(credits.AddComponent<UICredits>(), "creditFiles", new List<TextAsset> { Load<TextAsset>(CreditsPath) });
            AddButton(credits.AddComponent<UIButtonEvents>(), "back-button", Call(creditsScreen.Hide));

            UIScreen settings = Settings(null, 2);

            GameObject menu = new GameObject("Main Menu");
            VisualTreeAsset uxml = UxmlCopy(CoreUI + "MainMenu.uxml", sceneName,
                ("text=\"Game Title\"", $"text=\"{title}\""),
                ("text=\"A game by Your Name\"", $"text=\"{subtitle}\""));
            Document(menu, uxml, 0);
            MakeScreen(menu, false, "play-button");
            SceneChanger play = menu.AddComponent<SceneChanger>();
            Set(play, "sceneName", playScene);
            UIButtonEvents buttons = menu.AddComponent<UIButtonEvents>();
            AddButton(buttons, "play-button", Call(play.Load));
            AddButton(buttons, "settings-button", Call(settings.Show));
            AddButton(buttons, "credits-button", Call(creditsScreen.Show));
            AddButton(buttons, "quit-button", Call(play.QuitGame));

            if (newGame != null && newGame.Length > 0)
            {
                VariableResetter resetter = new GameObject("Game Rules").AddComponent<VariableResetter>();
                Set(resetter, "variables", newGame);
                Set(resetter, "resetWhenSceneStarts", true);
            }

            SaveScene(scene, sceneName);
        }

        /// <summary>
        /// A standalone win scene: a camera and Core's end screen with a title and a line,
        /// Play Again loading the first game scene (after resetting the variables) and
        /// Main Menu loading the menu.
        /// </summary>
        public void WinScene(string sceneName, string title, string subtitle, string firstScene, string menuScene,
            VariableBase[] newGame, Color background, params (string label, VariableBase variable, string format)[] bindings)
        {
            Scene scene = NewScene();
            Camera2D(new Vector3(0f, 0f, -10f), 5f, background);

            SceneChanger toFirst = new GameObject("To First Level").AddComponent<SceneChanger>();
            Set(toFirst, "sceneName", firstScene);
            SceneChanger toMenu = new GameObject("To Menu").AddComponent<SceneChanger>();
            Set(toMenu, "sceneName", menuScene);

            GameObject win = new GameObject("Win Screen");
            List<(string, string)> changes = new List<(string, string)> { ("text=\"You Win!\"", $"text=\"{title}\"") };

            if (bindings.Length == 0)
            {
                changes.Add(("text=\"Score: 0\"", $"text=\"{subtitle}\""));
            }

            Document(win, UxmlCopy(CoreUI + "EndScreen.uxml", sceneName, changes.ToArray()), 0);
            MakeScreen(win, false, "play-again-button");

            if (bindings.Length > 0)
            {
                Bind(win, bindings);
            }

            UIButtonEvents buttons = win.AddComponent<UIButtonEvents>();
            List<Action<UnityEvent>> again = new List<Action<UnityEvent>>();

            if (newGame != null && newGame.Length > 0)
            {
                VariableResetter resetter = Resetter(new GameObject("Game Rules"), false, newGame);
                again.Add(Call(resetter.ResetAll));
            }

            again.Add(Call(toFirst.Load));
            AddButton(buttons, "play-again-button", again.ToArray());
            AddButton(buttons, "menu-button", Call(toMenu.Load));
            SaveScene(scene, sceneName);
        }

        /// <summary>
        /// A plain white square sprite, one unit across, made once in Sample/Art. For health
        /// bars, cursors, overlays and placeholder shapes; tint it with the Sprite Renderer's color.
        /// </summary>
        public Sprite WhiteSquare()
        {
            string path = $"{Folder("Art")}/Square.png";

            if (!File.Exists(path))
            {
                Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                Color32[] pixels = Enumerable.Repeat(new Color32(255, 255, 255, 255), 256).ToArray();
                texture.SetPixels32(pixels);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return Load<Sprite>(path);
        }

        /// <summary>Saves changes made to a prefab instance in the scene, so they're kept as overrides.</summary>
        public static void KeepChanges(Object component) => PrefabUtility.RecordPrefabInstancePropertyModifications(component);

        // ---------------------------------------------------------------- Wiring

        public static Action<UnityEvent> Call(UnityAction action) => e => UnityEventTools.AddVoidPersistentListener(e, action);
        public static Action<UnityEvent> Call(UnityAction<int> action, int argument) => e => UnityEventTools.AddIntPersistentListener(e, action, argument);
        public static Action<UnityEvent> Call(UnityAction<float> action, float argument) => e => UnityEventTools.AddFloatPersistentListener(e, action, argument);
        public static Action<UnityEvent> Call(UnityAction<bool> action, bool argument) => e => UnityEventTools.AddBoolPersistentListener(e, action, argument);
        public static Action<UnityEvent> Call(UnityAction<string> action, string argument) => e => UnityEventTools.AddStringPersistentListener(e, action, argument);

        /// <summary>Adds listeners to a component's UnityEvent field.</summary>
        public static void On(Object target, string eventField, params Action<UnityEvent>[] wiring)
        {
            UnityEvent unityEvent = GetEvent(target, eventField);

            foreach (Action<UnityEvent> wire in wiring)
            {
                wire(unityEvent);
            }

            EditorUtility.SetDirty(target);
        }

        public static void AddButton(UIButtonEvents buttons, string buttonName, params Action<UnityEvent>[] wiring)
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

        public static VoidEventListener Listen(GameObject go, VoidEventChannel channel, params Action<UnityEvent>[] wiring)
        {
            VoidEventListener listener = go.AddComponent<VoidEventListener>();
            Set(listener, "channel", channel);
            On(listener, "response", wiring);
            return listener;
        }

        public static void Bind(GameObject go, params (string label, VariableBase variable, string format)[] bindings)
        {
            UIVariableBinder binder = go.AddComponent<UIVariableBinder>();
            List<UIVariableBinder.Binding> list = new List<UIVariableBinder.Binding>();

            foreach ((string label, VariableBase variable, string format) in bindings)
            {
                list.Add(new UIVariableBinder.Binding { labelName = label, variable = variable, format = format });
            }

            Set(binder, "bindings", list);
        }

        /// <summary>An Int Variable Condition that raises a channel when its comparison becomes true.</summary>
        public static IntVariableCondition Condition(GameObject go, IntVariable variable, string comparison, int value, VoidEventChannel raise)
        {
            IntVariableCondition condition = go.AddComponent<IntVariableCondition>();
            Set(condition, "variable", variable);
            SetEnum(condition, "comparison", comparison);
            Set(condition, "value", value);
            Set(condition, "onlyOnce", true);
            Set(condition, "raiseEvent", raise);
            return condition;
        }

        public static VariableResetter Resetter(GameObject go, bool whenSceneStarts, params VariableBase[] variables)
        {
            VariableResetter resetter = go.AddComponent<VariableResetter>();
            Set(resetter, "variables", variables);
            Set(resetter, "resetWhenSceneStarts", whenSceneStarts);
            return resetter;
        }

        // ---------------------------------------------------------------- Objects

        public static GameObject Child(GameObject parent, string childName)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        public static GameObject SpriteObject(string objectName, Sprite sprite, int order)
        {
            GameObject go = new GameObject(objectName);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return go;
        }

        public static CircleCollider2D TriggerCircle(GameObject go, float radius)
        {
            CircleCollider2D circle = go.AddComponent<CircleCollider2D>();
            circle.radius = radius;
            circle.isTrigger = true;
            return circle;
        }

        public static BoxCollider2D TriggerBox(GameObject go, Vector2 size, Vector2 offset)
        {
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.offset = offset;
            box.isTrigger = true;
            return box;
        }

        /// <summary>A Kenney sprite, by pack and path inside it, such as ("kenney_tiny-town", "Tiles/tile_0017").</summary>
        public static Sprite KenneySprite(string pack, string pathInPack) => Load<Sprite>($"{Kenney}{pack}/{pathInPack}.png");

        // ---------------------------------------------------------------- Assets and fields

        public static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset == null)
            {
                throw new SampleBuildProblem($"Couldn't find {path}.");
            }

            return asset;
        }

        /// <summary>Sets a field the Inspector shows, even when it's private.</summary>
        public static void Set(Object target, string fieldName, object value)
        {
            FindField(target.GetType(), fieldName).SetValue(target, value);
            EditorUtility.SetDirty(target);
        }

        public static void SetEnum(Object target, string fieldName, string valueName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            field.SetValue(target, Enum.Parse(field.FieldType, valueName));
            EditorUtility.SetDirty(target);
        }

        /// <summary>Parses an enum value for a field, for building lists of serializable classes.</summary>
        public static object EnumValue(Type owner, string fieldName, string valueName) =>
            Enum.Parse(FindField(owner, fieldName).FieldType, valueName);

        public static object Get(Object target, string fieldName) => FindField(target.GetType(), fieldName).GetValue(target);

        /// <summary>Gets a UnityEvent field, creating it if needed.</summary>
        public static UnityEvent GetEvent(Object target, string fieldName)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);

            if (!(field.GetValue(target) is UnityEvent unityEvent))
            {
                unityEvent = new UnityEvent();
                field.SetValue(target, unityEvent);
            }

            return unityEvent;
        }

        public static FieldInfo FindField(Type type, string fieldName)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null)
                {
                    return field;
                }
            }

            throw new SampleBuildProblem($"{type.Name} has no field \"{fieldName}\". The kit's scripts and this builder are out of step; tell Claude.");
        }
    }
}
