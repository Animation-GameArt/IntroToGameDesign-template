using System;
using System.Text;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.Farming
{
    /// <summary>
    /// The days of a farming game. A day ends when the player sleeps (call
    /// Sleep from a bed), or when the clock runs out. Then the night passes:
    /// watered crops grow, the soil dries, the day number goes up, the player
    /// wakes at the bed, and a short report of the night is shown.
    ///
    /// The clock stops while the game is paused, a shop is open, or the
    /// report is showing. After Last Day, time is up.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Farming Kit/Day Cycle")]
    public class DayCycle : MonoBehaviour
    {
        [Header("Days")]

        [Tooltip("A number variable for the day, for the HUD.")]
        [SerializeField] IntVariable day;

        [Tooltip("The last day. When it ends, time is up. 0 means the game never ends by itself.")]
        [SerializeField, Min(0)] int lastDay = 14;

        [Header("Clock")]

        [Tooltip("Real seconds in a day. 0 means the clock doesn't run, and days end only by sleeping.")]
        [SerializeField, Min(0f)] float secondsPerDay = 240f;

        [Tooltip("The hour the day starts, on a 24-hour clock.")]
        [SerializeField, Range(0, 23)] int startHour = 6;

        [Tooltip("The hour the player falls asleep wherever they are. Past midnight is 24 and more: 26 is 2 AM.")]
        [SerializeField, Range(1, 30)] int endHour = 26;

        [Tooltip("Optional. A text variable for the time, such as \"6:40 PM\", for the HUD.")]
        [SerializeField] StringVariable clock;

        [Tooltip("Optional. A sprite covering the view, tinted by Overlay Color through the day, such as clear at noon and dark blue at night.")]
        [SerializeField] SpriteRenderer overlay;

        [Tooltip("The overlay's color from the start of the day (left) to the end (right). Use the alpha to make it see-through.")]
        [SerializeField] Gradient overlayColor = DefaultOverlay();

        static Gradient DefaultOverlay()
        {
            // Clear all day, turning orange in the evening and dark blue at night.
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.85f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.3f), 0.6f),
                    new GradientColorKey(new Color(0.1f, 0.12f, 0.35f), 0.8f),
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.1f, 0f),
                    new GradientAlphaKey(0f, 0.15f),
                    new GradientAlphaKey(0f, 0.5f),
                    new GradientAlphaKey(0.25f, 0.65f),
                    new GradientAlphaKey(0.5f, 1f)
                });
            return gradient;
        }

        [Header("Morning")]

        [Tooltip("Optional. The player, moved to Wake Up Point every morning.")]
        [SerializeField] Transform player;

        [Tooltip("Optional. Where the player wakes up, such as beside the bed.")]
        [SerializeField] Transform wakeUpPoint;

        [Tooltip("Optional. Variables set back to their starting value every morning, such as energy.")]
        [SerializeField] VariableBase[] resetEachMorning = new VariableBase[0];

        [Header("Results")]

        [Tooltip("Optional. Raised when the last day ends.")]
        [SerializeField] VoidEventChannel raiseWhenTimeUp;

        [Tooltip("Runs every night, after the crops have grown.")]
        [SerializeField] UnityEvent onDayEnded;

        [Tooltip("Runs when the last day ends.")]
        [SerializeField] UnityEvent onTimeUp;

        readonly StringBuilder notes = new StringBuilder();
        InputReader input;
        float elapsed;
        bool over;

        /// <summary>The day cycle in the open scene.</summary>
        public static DayCycle Instance { get; private set; }

        /// <summary>Raised every night with the new day number. The Farm Field grows its crops here.</summary>
        public event Action<int> DayEnded;

        /// <summary>Raised with the night's report. If something listens, the clock waits until Continue is called.</summary>
        public event Action<string> ReportReady;

        /// <summary>The day being played, counting from 1.</summary>
        public int Day { get; private set; } = 1;

        /// <summary>How far through the day it is, from 0 to 1.</summary>
        public float DayProgress => secondsPerDay > 0f ? Mathf.Clamp01(elapsed / secondsPerDay) : 0f;

        /// <summary>True while the night's report is showing.</summary>
        public bool WaitingForReport { get; private set; }

        /// <summary>True once time is up or the game has been won.</summary>
        public bool IsOver => over;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        void Awake()
        {
            Instance = this;
            input = InputReader.FindInProject();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            SetDay();
            ShowTime();
        }

        void Update()
        {
            if (over || WaitingForReport || secondsPerDay <= 0f || PauseController.GamePaused)
            {
                return;
            }

            // The clock stops while the player can't move, such as while a shop is open.
            if (input != null && !input.PlayerInputEnabled)
            {
                return;
            }

            elapsed += Time.deltaTime;
            ShowTime();

            if (elapsed >= secondsPerDay)
            {
                AddNote("You were too tired to stay up, and fell asleep where you stood.");
                EndDay();
            }
        }

        /// <summary>Ends the day. Wire a bed's Interact Zone to this.</summary>
        public void Sleep()
        {
            if (!over && !WaitingForReport)
            {
                EndDay();
            }
        }

        /// <summary>Adds a line to tonight's report. The Farm Field uses it, such as for rain.</summary>
        public void AddNote(string line)
        {
            if (!string.IsNullOrEmpty(line))
            {
                notes.AppendLine(line);
            }
        }

        /// <summary>Hides the report and starts the new day's clock. The report's Continue button calls this.</summary>
        public void Continue()
        {
            WaitingForReport = false;

            if (input != null && !over)
            {
                input.EnablePlayerInput();
            }
        }

        /// <summary>Stops the days, such as when the player has won.</summary>
        public void StopGame()
        {
            over = true;
        }

        void EndDay()
        {
            int ended = Day;
            Day++;
            elapsed = 0f;

            DayEnded?.Invoke(Day);
            onDayEnded?.Invoke();

            foreach (VariableBase variable in resetEachMorning)
            {
                if (variable != null)
                {
                    variable.ResetToInitial();
                }
            }

            if (player != null && wakeUpPoint != null)
            {
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();

                if (body != null)
                {
                    body.position = wakeUpPoint.position;
                    body.linearVelocity = Vector2.zero;
                }

                player.position = wakeUpPoint.position;
            }

            bool timeUp = lastDay > 0 && ended >= lastDay && !over;
            string text = $"Day {ended} is over.\n\n{notes}".TrimEnd();
            notes.Clear();

            if (!timeUp)
            {
                SetDay();
            }

            ShowTime();

            if (timeUp)
            {
                over = true;

                if (raiseWhenTimeUp != null)
                {
                    raiseWhenTimeUp.Raise();
                }

                onTimeUp?.Invoke();
                return;
            }

            if (ReportReady != null && !over)
            {
                WaitingForReport = true;

                if (input != null)
                {
                    input.DisablePlayerInput();
                }

                ReportReady.Invoke(text);
            }
        }

        void SetDay()
        {
            if (day != null)
            {
                day.SetValue(Day);
            }
        }

        void ShowTime()
        {
            float progress = DayProgress;

            if (clock != null)
            {
                float hours = Mathf.Lerp(startHour, endHour, progress);
                int totalMinutes = Mathf.FloorToInt(hours * 6f) * 10; // Steps of 10 minutes
                int hour = totalMinutes / 60 % 24;
                int minute = totalMinutes % 60;
                string half = hour < 12 ? "AM" : "PM";
                int shown = hour % 12 == 0 ? 12 : hour % 12;
                clock.SetValue($"{shown}:{minute:00} {half}");
            }

            if (overlay != null)
            {
                overlay.color = overlayColor.Evaluate(progress);
            }
        }
    }
}
