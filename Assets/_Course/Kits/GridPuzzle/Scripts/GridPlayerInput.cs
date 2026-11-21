using CollinGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CollinGame.Kits.GridPuzzle
{
    /// <summary>
    /// Turns the Move input into one-cell steps on the Grid Board, and adds undo
    /// and restart buttons. Holding a direction keeps stepping after a short pause.
    ///
    /// | Action  | Keyboard                | Gamepad        |
    /// | Move    | WASD or arrow keys      | Left stick, D-pad |
    /// | Undo    | Z or Backspace          | West button    |
    /// | Restart | R                       | North button   |
    ///
    /// Setup: put one in each level scene, for example on the Grid Board.
    /// </summary>
    [AddComponentMenu("Collin Game/Grid Puzzle Kit/Grid Player Input")]
    public class GridPlayerInput : MonoBehaviour
    {
        [Tooltip("The Input Reader asset, from Assets/_Course/Core/Input.")]
        [SerializeField] InputReader input;

        [Tooltip("Seconds a direction must be held before it starts repeating.")]
        [SerializeField, Min(0f)] float repeatDelay = 0.3f;

        [Tooltip("Seconds between repeated steps while a direction is held.")]
        [SerializeField, Min(0.02f)] float repeatEvery = 0.12f;

        Vector2Int heldDirection;
        float nextRepeat;

        void Update()
        {
            GridBoard board = GridBoard.Instance;

            if (board == null || board.Solved || PauseController.GamePaused)
            {
                return;
            }

            if (UndoPressed())
            {
                board.Undo();
                return;
            }

            if (RestartPressed())
            {
                board.Restart();
                return;
            }

            Vector2Int direction = ReadDirection();

            if (direction == Vector2Int.zero)
            {
                heldDirection = Vector2Int.zero;
                return;
            }

            bool newPress = direction != heldDirection;
            bool repeat = !newPress && Time.time >= nextRepeat;

            if ((!newPress && !repeat) || board.IsBusy)
            {
                return;
            }

            board.TryMovePlayers(direction);
            heldDirection = direction;
            nextRepeat = Time.time + (newPress ? repeatDelay : repeatEvery);
        }

        // The stick or keys as one of four directions.
        Vector2Int ReadDirection()
        {
            Vector2 move = input != null ? input.Move : Vector2.zero;

            if (move.magnitude < 0.5f)
            {
                return Vector2Int.zero;
            }

            return Mathf.Abs(move.x) > Mathf.Abs(move.y)
                ? new Vector2Int(move.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, move.y > 0f ? 1 : -1);
        }

        static bool UndoPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            return (keyboard != null && (keyboard.zKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame))
                   || (pad != null && pad.buttonWest.wasPressedThisFrame);
        }

        static bool RestartPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            return (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                   || (pad != null && pad.buttonNorth.wasPressedThisFrame);
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
