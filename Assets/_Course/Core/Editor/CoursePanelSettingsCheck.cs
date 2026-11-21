using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace CollinGame.EditorTools
{
    /// <summary>
    /// A UI Document without Panel Settings draws nothing, so its screen is
    /// silently missing. This runs on every scene as it's played in the Editor
    /// or put in a build, gives such documents the course Panel Settings for
    /// that run, and warns, naming them. Use Collin Game > UI > Use Course Panel
    /// Settings in All Scenes and Prefabs to fix them for good.
    /// </summary>
    public class CoursePanelSettingsCheck : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(CoursePanelSettingsTool.PanelSettingsPath);

            if (settings == null)
            {
                Debug.LogWarning($"Course Panel Settings: {CoursePanelSettingsTool.PanelSettingsPath} is missing or broken. Run Collin Game > UI > Repair Course Panel Settings.");
                return;
            }

            System.Text.StringBuilder names = new System.Text.StringBuilder();
            int fixedCount = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (UIDocument document in root.GetComponentsInChildren<UIDocument>(true))
                {
                    if (document.panelSettings == null)
                    {
                        document.panelSettings = settings;
                        names.Append(fixedCount == 0 ? "" : ", ").Append(document.gameObject.name);
                        fixedCount++;
                    }
                }
            }

            if (fixedCount > 0)
            {
                Debug.LogWarning($"Course Panel Settings: in \"{scene.name}\", {fixedCount} UI Document(s) had no Panel Settings and would have shown nothing: {names}. They use the course Panel Settings for now. To fix them for good, run Collin Game > UI > Use Course Panel Settings in All Scenes and Prefabs.");
            }
        }
    }
}
