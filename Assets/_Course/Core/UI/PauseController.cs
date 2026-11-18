using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Pauses and un-pauses the game when the player presses Pause
    /// (Esc on the keyboard, Start on a gamepad).
    ///
    /// While paused, time stops, the player's controls are switched off, and the
    /// pause screen is shown. Call Resume from the pause screen's Resume button.
    /// </summary>
    [AddComponentMenu("Collin Game/UI/Pause Controller")]
    public class PauseController : MonoBehaviour
    {
        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("The screen to show while paused. It should have Hidden At Start ticked.")]
        [SerializeField] UIScreen pauseScreen;

        [Tooltip("Optional. A true/false variable that is kept equal to whether the game is paused, so other objects can react.")]
        [SerializeField] BoolVariable pausedVariable;

        [Tooltip("Runs when the game is paused.")]
        [SerializeField] UnityEvent onPaused;

        [Tooltip("Runs when the game continues.")]
        [SerializeField] UnityEvent onResumed;

        /// <summary>True while the game is paused.</summary>
        public bool IsPaused { get; private set; }

        /// <summary>True while any Pause Controller has the game paused.</summary>
        public static bool GamePaused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            GamePaused = false;
        }

        void Awake()
        {
            if (input == null)
            {
                Debug.LogError($"Pause Controller on \"{name}\": Input is empty. Drag the InputReader asset from Assets/_Course/Core/Input into the Input field.", this);
            }
        }

        void OnEnable()
        {
            if (input != null)
            {
                input.PausePressed += TogglePause;
            }
        }

        void OnDisable()
        {
            if (input != null)
            {
                input.PausePressed -= TogglePause;
            }

            // If the scene is closed while paused, don't leave the game frozen.
            if (IsPaused)
            {
                IsPaused = false;
                GamePaused = false;
                Time.timeScale = 1f;

                if (input != null)
                {
                    input.EnablePlayerInput();
                }

                if (pausedVariable != null)
                {
                    pausedVariable.SetValue(false);
                }
            }
        }

        /// <summary>Pauses if the game is running, and resumes if it is paused.</summary>
        public void TogglePause()
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        /// <summary>Stops time, switches off the player's controls and shows the pause screen.</summary>
        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            // Don't pause in the middle of a scene change or a cutscene.
            if ((SceneLoader.Instance != null && SceneLoader.Instance.IsLoading) || CutscenePlayer.AnyPlaying)
            {
                return;
            }

            IsPaused = true;
            GamePaused = true;
            Time.timeScale = 0f;

            if (input != null)
            {
                input.DisablePlayerInput();
            }

            if (pauseScreen != null)
            {
                pauseScreen.Show();
            }

            if (pausedVariable != null)
            {
                pausedVariable.SetValue(true);
            }

            onPaused?.Invoke();
        }

        /// <summary>Starts time again, switches the player's controls back on and hides the pause screen.</summary>
        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            GamePaused = false;
            Time.timeScale = 1f;

            if (input != null)
            {
                input.EnablePlayerInput();
            }

            if (pauseScreen != null)
            {
                pauseScreen.Hide();
            }

            if (pausedVariable != null)
            {
                pausedVariable.SetValue(false);
            }

            onResumed?.Invoke();
        }

#if UNITY_EDITOR
        // Runs when the component is first added: fills in the fields it can.
        void Reset()
        {
            input = InputReader.FindInProject();
            pauseScreen = GetComponent<UIScreen>();
        }
#endif
    }
}
