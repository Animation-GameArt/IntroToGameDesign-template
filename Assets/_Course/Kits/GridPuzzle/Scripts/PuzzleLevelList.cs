using System.Collections.Generic;
using UnityEngine;

namespace CollinGame.Kits.GridPuzzle
{
    /// <summary>
    /// The levels of a puzzle game, in order, and which ones the player has
    /// solved. Solving a level unlocks the next. Progress is saved between sessions.
    ///
    /// Create one with Create → Collin Game → Grid Puzzle Kit → Level List, and
    /// type each level's scene name. Every scene must also be in the Scene List.
    /// </summary>
    [CreateAssetMenu(menuName = "Collin Game/Grid Puzzle Kit/Level List", fileName = "Level List", order = 1)]
    public class PuzzleLevelList : ScriptableObject
    {
        [Tooltip("A short name for saving progress. Change it if a game has more than one list.")]
        [SerializeField] string saveName = "Puzzle";

        [Tooltip("The level scene names, in order.")]
        [SerializeField] List<string> levels = new List<string>();

        [Tooltip("Optional. A name to show for each level, in the same order. Leave a line empty to show its number.")]
        [SerializeField] List<string> titles = new List<string>();

        [Tooltip("Ticked: every level can be played from the start. Handy while testing.")]
        [SerializeField] bool unlockAll = false;

        /// <summary>How many levels there are.</summary>
        public int Count => levels.Count;

        /// <summary>The scene name of a level.</summary>
        public string SceneName(int index) => index >= 0 && index < levels.Count ? levels[index] : null;

        /// <summary>The name shown for a level: its title, or "Level 3".</summary>
        public string Title(int index)
        {
            string title = index >= 0 && index < titles.Count ? titles[index] : null;
            return string.IsNullOrWhiteSpace(title) ? $"Level {index + 1}" : title;
        }

        /// <summary>The position of a scene in the list, or -1.</summary>
        public int IndexOf(string sceneName) => levels.IndexOf(sceneName);

        /// <summary>True if the level has been solved.</summary>
        public bool IsSolved(int index) => index >= 0 && index < levels.Count && PlayerPrefs.GetInt(Key(levels[index]), 0) == 1;

        /// <summary>True if the level can be played: the first level, any level after a solved one, or everything with Unlock All.</summary>
        public bool IsUnlocked(int index) => unlockAll || index == 0 || IsSolved(index - 1) || IsSolved(index);

        /// <summary>Records a level as solved. The Grid Board calls this.</summary>
        public void MarkSolved(string sceneName)
        {
            if (levels.Contains(sceneName))
            {
                PlayerPrefs.SetInt(Key(sceneName), 1);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Forgets every solved level.</summary>
        public void ClearProgress()
        {
            foreach (string level in levels)
            {
                PlayerPrefs.DeleteKey(Key(level));
            }

            PlayerPrefs.Save();
        }

        string Key(string sceneName) => $"CollinGame.Puzzle.{saveName}.{sceneName}";
    }
}
