using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.GridPuzzle
{
    /// <summary>
    /// A target cell: a spot a box must be pushed onto, or an exit the player
    /// must reach. The level is solved when every goal is filled.
    ///
    /// Setup: a sprite on the floor, drawn under the pieces. It snaps to the
    /// middle of its cell when the level starts.
    /// </summary>
    [AddComponentMenu("Collin Game/Grid Puzzle Kit/Grid Goal")]
    public class GridGoal : MonoBehaviour
    {
        [Tooltip("Which role of piece fills this goal: Pushable for box targets, Player for an exit.")]
        [SerializeField] GridPiece.Role filledBy = GridPiece.Role.Pushable;

        [Tooltip("Optional. Only a piece with this Kind fills it, such as red for a red box. Leave empty to accept any.")]
        [SerializeField] string kind = "";

        [Header("Looks")]

        [Tooltip("Optional. This sprite changes color when the goal is filled.")]
        [SerializeField] SpriteRenderer indicator;

        [Tooltip("The indicator's color while filled.")]
        [SerializeField] Color filledColor = new Color(0.4f, 1f, 0.5f);

        [Tooltip("Runs when the goal becomes filled. Use it for a click sound.")]
        [SerializeField] UnityEvent onFilled;

        [Tooltip("Runs when the goal stops being filled.")]
        [SerializeField] UnityEvent onEmptied;

        Color emptyColor;

        /// <summary>The cell the goal is in.</summary>
        public Vector2Int Cell { get; private set; }

        /// <summary>True while a matching piece is on the goal.</summary>
        public bool IsFilled { get; private set; }

        void Start()
        {
            GridBoard board = GridBoard.Instance;

            if (board == null)
            {
                Debug.LogWarning($"Grid Goal on \"{name}\": there is no Grid Board in the scene.", this);
                return;
            }

            Cell = board.WorldToCell(transform.position);
            transform.position = board.CellToWorld(Cell, transform.position.z);

            if (indicator == null)
            {
                indicator = GetComponent<SpriteRenderer>();
            }

            if (indicator != null)
            {
                emptyColor = indicator.color;
            }

            board.Register(this);
        }

        /// <summary>Checks whether a matching piece is on the goal. The Grid Board calls this after each move.</summary>
        public void Refresh(GridBoard board)
        {
            bool filled = false;

            foreach (GridPiece piece in board.PiecesAt(Cell))
            {
                if (piece.PieceRole == filledBy && (string.IsNullOrEmpty(kind) || piece.Kind == kind))
                {
                    filled = true;
                    break;
                }
            }

            if (filled == IsFilled)
            {
                return;
            }

            IsFilled = filled;

            if (indicator != null)
            {
                indicator.color = filled ? filledColor : emptyColor;
            }

            if (filled)
            {
                onFilled?.Invoke();
            }
            else
            {
                onEmptied?.Invoke();
            }
        }
    }
}
