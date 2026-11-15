using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Core
{
    /// <summary>
    /// Runs a turn-based battle. It keeps the turn order, tells each Turn Taker
    /// when its turn starts, and decides when the battle is won or lost.
    ///
    /// The battle is won when no opponents can act, and lost when nobody on the
    /// player's side can act.
    /// </summary>
    [AddComponentMenu("Collin Game/Turn Based/Turn Manager")]
    public class TurnManager : MonoBehaviour
    {
        [Tooltip("Everyone in the battle, in the order they take turns.")]
        [SerializeField] List<TurnTaker> turnOrder = new List<TurnTaker>();

        [Tooltip("Start the battle as soon as the scene starts.")]
        [SerializeField] bool startAutomatically = true;

        [Tooltip("Seconds to wait between one turn and the next.")]
        [SerializeField, Min(0f)] float pauseBetweenTurns = 0.6f;

        [Header("Share with other objects")]

        [Tooltip("Optional. A text variable that says whose turn it is, for a label to show.")]
        [SerializeField] StringVariable statusText;

        [Tooltip("Optional. A number variable that holds the current round.")]
        [SerializeField] IntVariable roundVariable;

        [Tooltip("Optional. An event to raise when the player's side wins.")]
        [SerializeField] VoidEventChannel raiseOnPlayerWon;

        [Tooltip("Optional. An event to raise when the player's side loses.")]
        [SerializeField] VoidEventChannel raiseOnPlayerLost;

        [Header("Events")]

        [Tooltip("Runs when the battle starts.")]
        [SerializeField] UnityEvent onBattleStarted;

        [Tooltip("Runs when the player's side wins.")]
        [SerializeField] UnityEvent onPlayerWon;

        [Tooltip("Runs when the player's side loses.")]
        [SerializeField] UnityEvent onPlayerLost;

        int index;
        int turnsTaken;

        /// <summary>Whose turn it is. Empty between turns.</summary>
        public TurnTaker Current { get; private set; }

        /// <summary>The current round. A round is one pass through the turn order.</summary>
        public int Round { get; private set; }

        /// <summary>True from the start of the battle until it is won or lost.</summary>
        public bool BattleRunning { get; private set; }

        void Start()
        {
            if (startAutomatically)
            {
                StartBattle();
            }
        }

        /// <summary>Starts the battle from the first in the turn order.</summary>
        public void StartBattle()
        {
            if (turnOrder.Count == 0)
            {
                Debug.LogError($"Turn Manager \"{name}\": Turn Order is empty. Add the Turn Takers that are in the battle.", this);
                return;
            }

            for (int i = 0; i < turnOrder.Count; i++)
            {
                if (turnOrder[i] != null)
                {
                    turnOrder[i].Manager = this;
                }
            }

            StopAllCoroutines();

            index = -1;
            turnsTaken = 0;
            Current = null;
            BattleRunning = true;
            SetRound(1);

            onBattleStarted?.Invoke();
            StartCoroutine(NextTurn(0f));
        }

        /// <summary>Ends the current turn. Only the Turn Taker whose turn it is can end it.</summary>
        public void EndTurn(TurnTaker taker)
        {
            if (!BattleRunning || taker == null || taker != Current)
            {
                return;
            }

            Current.FinishTurn();
            Current = null;

            if (!BattleIsOver())
            {
                StartCoroutine(NextTurn(pauseBetweenTurns));
            }
        }

        IEnumerator NextTurn(float wait)
        {
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            if (!BattleRunning || BattleIsOver())
            {
                yield break;
            }

            // Find the next in the order who can still act.
            for (int checkedCount = 0; checkedCount < turnOrder.Count; checkedCount++)
            {
                index = (index + 1) % turnOrder.Count;

                if (index == 0 && turnsTaken > 0)
                {
                    SetRound(Round + 1);
                }

                TurnTaker next = turnOrder[index];

                if (next != null && next.CanAct)
                {
                    Current = next;
                    turnsTaken++;
                    SetStatus($"Round {Round}: {next.DisplayName}'s turn");
                    next.BeginTurn();
                    yield break;
                }
            }
        }

        // Checks both sides, and finishes the battle if one of them is out.
        bool BattleIsOver()
        {
            bool playersLeft = false;
            bool opponentsLeft = false;

            for (int i = 0; i < turnOrder.Count; i++)
            {
                TurnTaker taker = turnOrder[i];

                if (taker == null || !taker.CanAct)
                {
                    continue;
                }

                if (taker.PlayerSide)
                {
                    playersLeft = true;
                }
                else
                {
                    opponentsLeft = true;
                }
            }

            if (playersLeft && opponentsLeft)
            {
                return false;
            }

            BattleRunning = false;

            if (playersLeft)
            {
                SetStatus("You win!");

                if (raiseOnPlayerWon != null)
                {
                    raiseOnPlayerWon.Raise();
                }

                onPlayerWon?.Invoke();
            }
            else
            {
                SetStatus("You lose.");

                if (raiseOnPlayerLost != null)
                {
                    raiseOnPlayerLost.Raise();
                }

                onPlayerLost?.Invoke();
            }

            return true;
        }

        void SetRound(int round)
        {
            Round = round;

            if (roundVariable != null)
            {
                roundVariable.SetValue(round);
            }
        }

        void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.SetValue(message);
            }
        }
    }
}
