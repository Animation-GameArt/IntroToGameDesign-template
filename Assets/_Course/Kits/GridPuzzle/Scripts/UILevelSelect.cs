using UnityEngine;
using UnityEngine.UIElements;

namespace CollinGame.Kits.GridPuzzle
{
    /// <summary>
    /// Fills a level select screen with one button per level in a Level List.
    /// Locked levels are greyed out; solved levels get the level-button--solved style.
    ///
    /// Add it to the same GameObject as a UI Document using LevelSelect (from
    /// this kit's UI folder), with a Puzzle Level Loader.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [AddComponentMenu("Collin Game/Grid Puzzle Kit/UI Level Select")]
    public class UILevelSelect : MonoBehaviour
    {
        [Tooltip("The Puzzle Level Loader that loads the chosen level.")]
        [SerializeField] PuzzleLevelLoader loader;

        [Tooltip("The name of the empty element that holds the level buttons.")]
        [SerializeField] string containerName = "levels";

        void OnEnable()
        {
            if (loader == null)
            {
                loader = GetComponent<PuzzleLevelLoader>();
            }

            VisualElement container = GetComponent<UIDocument>().rootVisualElement?.Q<VisualElement>(containerName);

            if (container == null || loader == null || loader.LevelList == null)
            {
                Debug.LogWarning($"UI Level Select on \"{name}\": it needs an element named \"{containerName}\" and a Puzzle Level Loader with a Level List.", this);
                return;
            }

            PuzzleLevelList list = loader.LevelList;
            container.Clear();
            Button first = null;

            for (int i = 0; i < list.Count; i++)
            {
                int index = i;
                bool solved = list.IsSolved(i);
                bool unlocked = list.IsUnlocked(i);

                Button button = new Button(() => loader.LoadLevel(index))
                {
                    text = list.Title(i)
                };

                button.AddToClassList("level-button");
                button.EnableInClassList("level-button--solved", solved);
                button.SetEnabled(unlocked);
                container.Add(button);

                if (first == null && unlocked && !solved)
                {
                    first = button;
                }
            }

            first?.Focus();
        }
    }
}
