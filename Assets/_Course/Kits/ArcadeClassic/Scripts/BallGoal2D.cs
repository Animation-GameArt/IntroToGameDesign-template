using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.ArcadeClassic
{
    /// <summary>
    /// An area the ball must not reach: the gap below the paddle in Breakout,
    /// or the goal behind each paddle in Pong. When the ball enters, a number
    /// changes (a life lost, a point scored) and the ball goes back to be served.
    ///
    /// Setup: a Box Collider 2D with Is Trigger ticked, just past the edge of
    /// the play area.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Collin Game/Arcade Classic Kit/Ball Goal 2D")]
    public class BallGoal2D : MonoBehaviour
    {
        [Tooltip("Optional. A number to change when the ball enters: Lives in Breakout, the other player's score in Pong.")]
        [SerializeField] IntVariable changeVariable;

        [Tooltip("How much to change it by. Use -1 to lose a life, 1 to score a point.")]
        [SerializeField] int amount = -1;

        [Tooltip("Ticked: the ball goes back to be served again.")]
        [SerializeField] bool resetBall = true;

        [Tooltip("The direction of the next serve. Zero uses the ball's own Serve Direction. In Pong, point it toward the player who just lost the point.")]
        [SerializeField] Vector2 nextServeDirection = Vector2.zero;

        [Tooltip("Optional. An event to raise when the ball enters.")]
        [SerializeField] VoidEventChannel raiseEvent;

        [Tooltip("Runs when the ball enters. Use it for a sound.")]
        [SerializeField] UnityEvent onBallEntered;

        void Awake()
        {
            Collider2D area = GetComponent<Collider2D>();

            if (!area.isTrigger)
            {
                Debug.LogWarning($"Ball Goal 2D on \"{name}\": the collider should have Is Trigger ticked, or the ball bounces off it. Ticking it for you.", this);
                area.isTrigger = true;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Ball2D ball = other.GetComponentInParent<Ball2D>();

            // A ball that is already going back to be served only counts once.
            if (ball == null || !ball.InPlay)
            {
                return;
            }

            if (changeVariable != null)
            {
                changeVariable.Add(amount);
            }

            if (raiseEvent != null)
            {
                raiseEvent.Raise();
            }

            onBallEntered?.Invoke();

            if (resetBall)
            {
                ball.ResetBall(nextServeDirection);
            }
        }
    }
}
