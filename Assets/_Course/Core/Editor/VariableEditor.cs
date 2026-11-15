using CollinGame.Core;
using UnityEditor;
using UnityEngine;

namespace CollinGame.EditorTools
{
    /// <summary>
    /// Adds a "Current Value" line to the Inspector of every variable asset
    /// while the game is running, so you can watch the value change.
    /// </summary>
    [CustomEditor(typeof(VariableBase), true)]
    public class VariableEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                VariableBase variable = (VariableBase)target;
                EditorGUILayout.LabelField("Current Value", variable.ValueAsText, EditorStyles.boldLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("Press Play to see the current value here.", MessageType.Info);
            }
        }

        // Redraw all the time in Play mode so the value stays up to date.
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
