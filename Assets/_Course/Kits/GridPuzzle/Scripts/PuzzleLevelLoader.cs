using CollinGame.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollinGame.Kits.GridPuzzle
{
    /// <summary>
    /// Loads levels from a Level List: the next level, the first unsolved one,
    /// or a level by number. Call its methods from buttons and events, as with
    /// a Scene Changer.
    /// </summary>
    [AddComponentMenu("Collin Game/Grid Puzzle Kit/Puzzle Level Loader")]
    public class PuzzleLevelLoader : MonoBehaviour
    {
        [Tooltip("The Level List asset.")]
        [SerializeField] PuzzleLevelList levelList;

        [Tooltip("The scene to go to after the last level, such as a win screen. Leave empty for the first scene in the Scene List.")]
        [SerializeField] string afterLastLevel = "";

        /// <summary>The Level List this loader uses.</summary>
        public PuzzleLevelList LevelList => levelList;

        /// <summary>Loads the level after the open one, or After Last Level.</summary>
        public void LoadNextLevel()
        {
            if (!Ready())
            {
                return;
            }

            int index = levelList.IndexOf(SceneManager.GetActiveScene().name);
            string next = levelList.SceneName(index + 1);

            if (next == null)
            {
                next = string.IsNullOrWhiteSpace(afterLastLevel) ? FirstSceneInList() : afterLastLevel;
            }

            SceneLoader.Instance.LoadScene(next);
        }

        /// <summary>Loads the first level the player hasn't solved, or the first level.</summary>
        public void LoadFirstUnsolved()
        {
            if (!Ready())
            {
                return;
            }

            for (int i = 0; i < levelList.Count; i++)
            {
                if (!levelList.IsSolved(i))
                {
                    LoadLevel(i);
                    return;
                }
            }

            LoadLevel(0);
        }

        /// <summary>Loads a level by its position in the list, counting from 0.</summary>
        public void LoadLevel(int index)
        {
            if (!Ready())
            {
                return;
            }

            string sceneName = levelList.SceneName(index);

            if (sceneName == null)
            {
                Debug.LogWarning($"Puzzle Level Loader on \"{name}\": the level list has no level {index + 1}.", this);
                return;
            }

            SceneLoader.Instance.LoadScene(sceneName);
        }

        bool Ready()
        {
            if (levelList == null)
            {
                Debug.LogWarning($"Puzzle Level Loader on \"{name}\": Level List is empty. Drag in a Level List asset.", this);
                return false;
            }

            if (SceneLoader.Instance == null)
            {
                Debug.LogWarning($"Puzzle Level Loader on \"{name}\": the course's Scene Loader isn't running. Start the game from a scene, not with a script.", this);
                return false;
            }

            return true;
        }

        static string FirstSceneInList()
        {
            string path = SceneUtility.GetScenePathByBuildIndex(0);
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }
}
