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
    ///   Collin Game > UI > Repair Course Panel Settings
    ///       creates the asset if it's missing, and puts back its settings.
    ///
    /// A new UI Document also gets it by itself, when its Panel Settings is empty.
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
