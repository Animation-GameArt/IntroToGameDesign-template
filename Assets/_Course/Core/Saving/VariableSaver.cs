using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Core
{
    /// <summary>
    /// Saves variable assets on this computer and loads them again, so progress
    /// survives closing the game: levels unlocked, coins collected, a high score.
    ///
    /// List the variables to save. Call Save from an event, such as reaching a
    /// checkpoint or finishing a level, or tick Save When Scene Ends. Values are
    /// loaded when the scene starts.
    ///
    /// Works in desktop builds and in Web builds.
    /// </summary>
    [AddComponentMenu("Collin Game/Saving/Variable Saver")]
    public class VariableSaver : MonoBehaviour
    {
        [Serializable]
        class SaveFile
        {
            public List<Entry> entries = new List<Entry>();
        }

        [Serializable]
        class Entry
        {
            public string name;
            public string value;
        }

        [Tooltip("A name for this save. Savers with the same name share one save, so use one name per game.")]
        [SerializeField] string saveName = "Progress";

        [Tooltip("The variables to save. Each must have a different asset name.")]
        [SerializeField] VariableBase[] variables;

        [Tooltip("Load the saved values as soon as the scene starts.")]
        [SerializeField] bool loadWhenSceneStarts = true;

        [Tooltip("Save automatically when the scene ends or the game closes.")]
        [SerializeField] bool saveWhenSceneEnds;

        string Key => "CollinGame.Save." + saveName;

        /// <summary>True if there is a save to load.</summary>
        public bool HasSave => PlayerPrefs.HasKey(Key);

        void Awake()
        {
            if (loadWhenSceneStarts)
            {
                Load();
            }
        }

        void OnDestroy()
        {
            // OnDestroy also runs when the scene changes and when the game closes.
            if (saveWhenSceneEnds && Application.isPlaying)
            {
                Save();
            }
        }

        /// <summary>Saves the current value of every listed variable.</summary>
        public void Save()
        {
            if (variables == null)
            {
                return;
            }

            SaveFile file = new SaveFile();

            for (int i = 0; i < variables.Length; i++)
            {
                if (variables[i] != null)
                {
                    file.entries.Add(new Entry { name = variables[i].name, value = variables[i].ToSaveText() });
                }
            }

            PlayerPrefs.SetString(Key, JsonUtility.ToJson(file));
            PlayerPrefs.Save();
        }

        /// <summary>Loads the saved values. Variables that were never saved keep their current value.</summary>
        public void Load()
        {
            if (variables == null || !PlayerPrefs.HasKey(Key))
            {
                return;
            }

            SaveFile file;

            try
            {
                file = JsonUtility.FromJson<SaveFile>(PlayerPrefs.GetString(Key));
            }
            catch (Exception)
            {
                Debug.LogWarning($"Variable Saver on \"{name}\": the save \"{saveName}\" couldn't be read, so nothing was loaded.", this);
                return;
            }

            if (file == null || file.entries == null)
            {
                return;
            }

            for (int i = 0; i < variables.Length; i++)
            {
                VariableBase variable = variables[i];

                if (variable == null)
                {
                    continue;
                }

                Entry entry = file.entries.Find(e => e.name == variable.name);

                if (entry != null && !variable.LoadFromSaveText(entry.value))
                {
                    Debug.LogWarning($"Variable Saver on \"{name}\": couldn't load \"{variable.name}\" from the save.", this);
                }
            }
        }

        /// <summary>Deletes the save. Use it for a "New Game" button.</summary>
        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
