using System.Collections;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.PhysicsPuzzle
{
    /// <summary>
    /// Decides when a physics puzzle level is won or lost.
    ///
    /// Won: no targets are left.
    /// Lost: the last shot has finished and targets are still left.
    ///
    /// It sets Targets Left to 0 and Shots Left to Starting Shots when the scene
    /// starts, so no Variable Resetter is needed for them.
    /// </summary>
    [AddComponentMenu("Collin Game/Physics Puzzle Kit/Puzzle Rules")]
    public class PuzzleRules : MonoBehaviour
    {
        [Tooltip("The number variable the Puzzle Targets count themselves in.")]
        [SerializeField] IntVariable targetsLeft;

        [Tooltip("The number variable the Launcher takes shots from.")]
        [SerializeField] IntVariable shotsLeft;

        [Tooltip("How many shots the player has in this level.")]
        [SerializeField, Min(1)] int startingShots = 3;

        [Tooltip("The event the Launcher raises when a shot is finished.")]
        [SerializeField] VoidEventChannel shotFinished;

        [Tooltip("Seconds to wait after the last target goes, before the level counts as won.")]
        [SerializeField, Min(0f)] float winDelay = 1f;

        [Header("Results")]

        [Tooltip("Optional. Raised when the level is won.")]
        [SerializeField] VoidEventChannel raiseOnWon;

        [Tooltip("Optional. Raised when the level is lost.")]
        [SerializeField] VoidEventChannel raiseOnLost;

        [Tooltip("Optional. Each unused shot adds this to the score when the level is won.")]
        [SerializeField] IntVariable score;

        [Tooltip("Points for each unused shot.")]
        [SerializeField] int pointsPerUnusedShot = 500;

        [Tooltip("Runs when the level is won.")]
        [SerializeField] UnityEvent onWon;

        [Tooltip("Runs when the level is lost.")]
        [SerializeField] UnityEvent onLost;

        bool started;
        bool over;

        void Awake()
        {
            if (targetsLeft != null)
            {
                targetsLeft.SetValue(0);
            }

            if (shotsLeft != null)
            {
                shotsLeft.SetValue(startingShots);
            }
        }

        void OnEnable()
        {
            if (targetsLeft != null)
            {
                targetsLeft.Changed += OnTargetsChanged;
            }

            if (shotFinished != null)
            {
                shotFinished.Raised += OnShotFinished;
            }
        }

        void OnDisable()
        {
            if (targetsLeft != null)
            {
                targetsLeft.Changed -= OnTargetsChanged;
            }

            if (shotFinished != null)
            {
                shotFinished.Raised -= OnShotFinished;
            }
        }

        IEnumerator Start()
        {
            // Wait one frame, so every target has counted itself.
            yield return null;
            started = true;

            if (targetsLeft == null || targetsLeft.Value <= 0)
            {
                Debug.LogWarning($"Puzzle Rules on \"{name}\": no targets were counted. Give each target a Puzzle Target 2D that uses the same Targets Left variable.", this);
            }
        }

        void OnTargetsChanged(int left)
        {
            if (started && !over && left <= 0 && isActiveAndEnabled)
            {
                StartCoroutine(WinAfterDelay());
            }
        }

        IEnumerator WinAfterDelay()
        {
            over = true;

            // Always wait at least one frame. If the scene is closing, this
            // object is gone by then and nothing is announced.
            yield return null;

            if (winDelay > 0f)
            {
                yield return new WaitForSeconds(winDelay);
            }

            if (score != null && shotsLeft != null)
            {
                score.Add(shotsLeft.Value * pointsPerUnusedShot);
            }

            if (raiseOnWon != null)
            {
                raiseOnWon.Raise();
            }

            onWon?.Invoke();
        }

        void OnShotFinished()
        {
            if (over || shotsLeft == null || targetsLeft == null)
            {
                return;
            }

            if (shotsLeft.Value <= 0 && targetsLeft.Value > 0)
            {
                over = true;

                if (raiseOnLost != null)
                {
                    raiseOnLost.Raise();
                }

                onLost?.Invoke();
            }
        }
    }
}
