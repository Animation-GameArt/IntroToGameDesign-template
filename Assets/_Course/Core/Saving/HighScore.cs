using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Keeps the best score ever reached, saved on this computer.
    ///
    /// It watches a Score variable and copies it into a Best variable whenever the
    /// score is higher. Show the Best variable on the HUD or the end screen.
    /// </summary>
    [AddComponentMenu("Collin Game/Saving/High Score")]
    public class HighScore : MonoBehaviour
    {
        [Tooltip("The score to watch.")]
        [SerializeField] IntVariable score;

        [Tooltip("The variable that holds the best score.")]
        [SerializeField] IntVariable best;

        [Tooltip("A name for this high score, if the game has more than one.")]
        [SerializeField] string saveName = "HighScore";

        [Tooltip("Runs the first time the score beats the best during this scene.")]
        [SerializeField] UnityEvent onNewBest;

        bool announced;

        string Key => "CollinGame.HighScore." + saveName;

        void Awake()
        {
            if (best != null)
            {
                best.SetValue(PlayerPrefs.GetInt(Key, best.InitialValue));
            }
        }

        void OnEnable()
        {
            announced = false;

            if (score == null || best == null)
            {
                Debug.LogWarning($"High Score on \"{name}\": set both Score and Best.", this);
                return;
            }

            score.Changed += Check;
        }

        void OnDisable()
        {
            if (score != null)
            {
                score.Changed -= Check;
            }

            PlayerPrefs.Save();
        }

        void Check(int value)
        {
            if (value <= best.Value)
            {
                return;
            }

            best.SetValue(value);
            PlayerPrefs.SetInt(Key, value);

            if (!announced)
            {
                announced = true;
                onNewBest?.Invoke();
            }
        }

        /// <summary>Sets the best score back to 0 and deletes it from this computer.</summary>
        public void ResetBest()
        {
            PlayerPrefs.DeleteKey(Key);

            if (best != null)
            {
                best.SetValue(0);
            }
        }
    }
}
