using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace CollinGame.Core
{
    /// <summary>
    /// Plays a Timeline as a cutscene: the player's controls are switched off while
    /// it plays and switched back on when it ends. The player can skip it with the
    /// Pause button (Esc, or Start on a gamepad).
    ///
    /// Setup: add this to the GameObject that has the Playable Director playing your
    /// Timeline. The Director's own Play On Awake is switched off for you.
    /// </summary>
    [RequireComponent(typeof(PlayableDirector))]
    [AddComponentMenu("Collin Game/Effects/Cutscene Player")]
    public class CutscenePlayer : MonoBehaviour
    {
        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("Play the cutscene as soon as the scene starts.")]
        [SerializeField] bool playOnStart = true;

        [Tooltip("Let the player skip the cutscene with the Pause button.")]
        [SerializeField] bool canSkip = true;

        [Header("Events")]

        [Tooltip("Optional. An event to raise when the cutscene ends or is skipped.")]
        [SerializeField] VoidEventChannel raiseOnFinished;

        [Tooltip("Runs when the cutscene starts.")]
        [SerializeField] UnityEvent onStarted;

        [Tooltip("Runs when the cutscene ends or is skipped.")]
        [SerializeField] UnityEvent onFinished;

        static int playingCount;

        PlayableDirector director;
        bool playing;

        /// <summary>True while any cutscene is playing. The Pause Controller doesn't pause during one.</summary>
        public static bool AnyPlaying => playingCount > 0;

        /// <summary>True while this cutscene is playing.</summary>
        public bool IsPlaying => playing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            playingCount = 0;
        }

        void Awake()
        {
            director = GetComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
        }

        void OnEnable()
        {
            director.stopped += OnDirectorStopped;

            if (input != null)
            {
                input.PausePressed += OnSkipPressed;
            }
        }

        void OnDisable()
        {
            director.stopped -= OnDirectorStopped;

            if (input != null)
            {
                input.PausePressed -= OnSkipPressed;
            }

            if (playing)
            {
                Finish();
            }
        }

        void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        /// <summary>Plays the cutscene from the beginning.</summary>
        public void Play()
        {
            if (playing)
            {
                return;
            }

            if (director.playableAsset == null)
            {
                Debug.LogError($"Cutscene Player on \"{name}\": the Playable Director has no Timeline. Drag a Timeline asset into its Playable field.", this);
                return;
            }

            playing = true;
            playingCount++;

            if (input != null)
            {
                input.DisablePlayerInput();
            }

            onStarted?.Invoke();

            director.time = 0;
            director.Play();
        }

        /// <summary>Jumps to the end of the cutscene and finishes it.</summary>
        public void Skip()
        {
            if (!playing)
            {
                return;
            }

            // Show the last frame, so everything ends up where the cutscene leaves it.
            director.time = director.duration;
            director.Evaluate();
            director.Stop();
        }

        void OnSkipPressed()
        {
            if (canSkip)
            {
                Skip();
            }
        }

        void OnDirectorStopped(PlayableDirector stoppedDirector)
        {
            if (playing)
            {
                Finish();
            }
        }

        void Finish()
        {
            playing = false;
            playingCount = Mathf.Max(0, playingCount - 1);

            if (input != null)
            {
                input.EnablePlayerInput();
            }

            if (raiseOnFinished != null)
            {
                raiseOnFinished.Raise();
            }

            onFinished?.Invoke();
        }

#if UNITY_EDITOR
        // Runs when the component is first added: fills in the Input field for you.
        void Reset()
        {
            input = InputReader.FindInProject();
        }
#endif
    }
}
