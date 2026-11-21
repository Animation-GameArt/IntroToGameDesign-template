using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.EditorTools
{
    /// <summary>
    /// Keeps every course screen on one shared Panel Settings asset, so all
    /// screens scale the same way: CoursePanelSettings, in Assets/_Course/Core/UI.
    ///
    ///   Collin Game > UI > Use Course Panel Settings in Open Scenes
    ///       sets it on every UI Document in the open scenes.
    ///   Collin Game > UI > Use Course Panel Settings in All Scenes and Prefabs
    ///       does the same in every scene and prefab in the project, and saves them.
    ///   Collin Game > UI > Repair Course Panel Settings
    ///       creates the asset if it's missing or broken, and puts back its settings.
    ///
    /// A new UI Document also gets it by itself, when its Panel Settings is empty.
    /// And when a scene is played or built, any UI Document still without Panel
    /// Settings gets it for that run, with a warning (see CoursePanelSettingsCheck).
    /// </summary>
    [InitializeOnLoad]
    public static class CoursePanelSettingsTool
    {
        /// <summary>Where the shared Panel Settings asset lives.</summary>
        public const string PanelSettingsPath = "Assets/_Course/Core/UI/CoursePanelSettings.asset";

        /// <summary>The theme the shared Panel Settings uses.</summary>
        public const string ThemePath = "Assets/_Course/Core/UI/CourseRuntimeTheme.tss";

        static CoursePanelSettingsTool()
        {
            ObjectFactory.componentWasAdded += OnComponentAdded;
        }

        // Fills in the Panel Settings of a UI Document added in the Editor.
        static void OnComponentAdded(Component component)
        {
            if (component is UIDocument document && document.panelSettings == null)
            {
                PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);

                if (settings != null)
                {
                    document.panelSettings = settings;
                }
            }
        }

        [MenuItem("Collin Game/UI/Use Course Panel Settings in Open Scenes")]
        static void UseInOpenScenes()
        {
            PanelSettings settings = GetOrCreate();
            int changed = 0;

            UIDocument[] documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (UIDocument document in documents)
            {
                if (document.panelSettings == settings)
                {
                    continue;
                }

                Undo.RecordObject(document, "Use Course Panel Settings");
                document.panelSettings = settings;
                EditorUtility.SetDirty(document);
                changed++;
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkAllScenesDirty();
            }

            Debug.Log($"Course Panel Settings: set on {changed} UI Document(s). {documents.Length - changed} already used it. Save the scene to keep the change.");
        }

        [MenuItem("Collin Game/UI/Use Course Panel Settings in All Scenes and Prefabs")]
        static void UseEverywhere()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            PanelSettings settings = GetOrCreate();
            string[] folders = { "Assets" };
            int prefabsChanged = 0;
            int scenesChanged = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!path.StartsWith("Assets/"))
                {
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);

                if (Assign(root.GetComponentsInChildren<UIDocument>(true), settings) > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabsChanged++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            string startScene = EditorSceneManager.GetActiveScene().path;

            foreach (string guid in AssetDatabase.FindAssets("t:Scene", folders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int changed = 0;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    changed += Assign(root.GetComponentsInChildren<UIDocument>(true), settings);
                }

                if (changed > 0)
                {
                    EditorSceneManager.SaveScene(scene);
                    scenesChanged++;
                }
            }

            if (!string.IsNullOrEmpty(startScene))
            {
                EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
            }

            Debug.Log($"Course Panel Settings: fixed {prefabsChanged} prefab(s) and {scenesChanged} scene(s).");
        }

        /// <summary>Sets the course Panel Settings on documents that have none. Returns how many changed.</summary>
        public static int Assign(UIDocument[] documents, PanelSettings settings)
        {
            int changed = 0;

            foreach (UIDocument document in documents)
            {
                if (document.panelSettings == null)
                {
                    document.panelSettings = settings;
                    EditorUtility.SetDirty(document);
                    changed++;
                }
            }

            return changed;
        }

        [MenuItem("Collin Game/UI/Repair Course Panel Settings")]
        static void Repair()
        {
            PanelSettings settings = GetOrCreate();
            ApplyCourseSettings(settings);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log("Course Panel Settings: settings put back (Scale With Screen Size, 1920 × 1080, the course theme).", settings);
        }

        /// <summary>Loads the shared Panel Settings, creating it if it's missing.</summary>
        public static PanelSettings GetOrCreate()
        {
            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);

            if (settings == null && System.IO.File.Exists(PanelSettingsPath))
            {
                // The file is there but Unity can't read it as Panel Settings. Write a
                // good one over it, keeping its .meta (and so every reference to it).
                PanelSettings rebuilt = ScriptableObject.CreateInstance<PanelSettings>();
                rebuilt.name = "CoursePanelSettings";
                ApplyCourseSettings(rebuilt);
                UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { rebuilt }, PanelSettingsPath, true);
                AssetDatabase.ImportAsset(PanelSettingsPath, ImportAssetOptions.ForceUpdate);
                settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
                Debug.LogWarning($"Course Panel Settings: {PanelSettingsPath} couldn't be read, so it was rebuilt.", settings);
            }

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                ApplyCourseSettings(settings);
                AssetDatabase.CreateAsset(settings, PanelSettingsPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Course Panel Settings: created {PanelSettingsPath}.", settings);
            }
            else if (settings.themeStyleSheet == null)
            {
                ApplyCourseSettings(settings);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        static void ApplyCourseSettings(PanelSettings settings)
        {
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.themeStyleSheet = FindTheme();

            if (settings.themeStyleSheet == null)
            {
                Debug.LogWarning($"Course Panel Settings: no theme found at {ThemePath}. Text may not show. Create one with Create → UI Toolkit → TSS Theme File.");
            }

            EditorUtility.SetDirty(settings);
        }

        static ThemeStyleSheet FindTheme()
        {
            ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);

            if (theme != null)
            {
                return theme;
            }

            // Fall back to any theme in the project, such as Unity's default runtime theme.
            string[] guids = AssetDatabase.FindAssets("t:ThemeStyleSheet");
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }
    }
}
