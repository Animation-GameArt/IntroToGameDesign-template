using CollinGame.Core;
using UnityEditor;
using UnityEngine;

namespace CollinGame.EditorTools
{
    /// <summary>
    /// Adds a "Raise" button to the Inspector of every Void Event Channel, so you
    /// can test what happens when the event is raised, without playing through
    /// the game to make it happen.
    /// </summary>
    [CustomEditor(typeof(VoidEventChannel))]
    public class VoidEventChannelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Raise"))
                {
                    ((VoidEventChannel)target).Raise();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Press Play to use the Raise button.", MessageType.Info);
            }
        }
    }
}
