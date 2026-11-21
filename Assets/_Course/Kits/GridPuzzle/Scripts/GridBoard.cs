using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace CollinGame.Kits.GridPuzzle
{
    /// <summary>
    /// The rules of a grid puzzle level. It knows where every Grid Piece and
    /// Grid Goal is, decides whether a move is allowed (and what gets pushed),
    /// remembers every move for undo, and announces when the level is solved.
    ///
    /// Setup: one per level scene. Paint walls on a Tilemap and drag it into
    /// Walls: any cell with a tile is a wall. Put pieces and goals on the cells.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Collin Game/Grid Puzzle Kit/Grid Board")]
    public class GridBoard : MonoBehaviour
    {
        [Tooltip("The Tilemap of walls. Any cell with a tile blocks movement.")]
        [SerializeField] Tilemap walls;

        [Tooltip("How many pushable pieces a push can move in a row. 1 is classic Sokoban: two boxes in a row can't be pushed.")]
        [SerializeField, Min(1)] int maxPushedInARow = 1;

        [Tooltip("Ticked: the level is only solved once every Item has been picked up, as well as every goal filled.")]
        [SerializeField] bool needAllItems = false;

        [Header("Showing progress")]

        [Tooltip("Optional. A number variable set to the moves made, for the HUD.")]
        [SerializeField] IntVariable moves;

        [Tooltip("Optional. The level list this level belongs to, so solving it is saved and unlocks the next.")]
        [SerializeField] PuzzleLevelList levelList;

        [Tooltip("Optional. A text variable set to this level's title from the Level List, for the HUD.")]
        [SerializeField] StringVariable levelTitle;

        [Header("Events")]

        [Tooltip("Optional. An event to raise when the level is solved.")]
        [SerializeField] VoidEventChannel raiseWhenSolved;

        [Tooltip("Runs when the level is solved.")]
        [SerializeField] UnityEvent onSolved;

        [Tooltip("Runs when a move is blocked. Use it for a bump sound.")]
        [SerializeField] UnityEvent onBlocked;

        [Tooltip("Runs on every undo.")]
        [SerializeField] UnityEvent onUndo;

        struct PieceState
        {
            public GridPiece piece;
            public Vector2Int cell;
            public bool active;
        }

        readonly List<GridPiece> pieces = new List<GridPiece>();
        readonly List<GridGoal> goals = new List<GridGoal>();
        readonly Stack<List<PieceState>> history = new Stack<List<PieceState>>();
        readonly List<GridPiece> toMove = new List<GridPiece>();
        List<PieceState> startState;
        Grid grid;
        int moveCount;

        /// <summary>The board in the open scene.</summary>
        public static GridBoard Instance { get; private set; }

        /// <summary>True once the level is solved. No more moves are taken.</summary>
        public bool Solved { get; private set; }

        /// <summary>True while any piece is still sliding.</summary>
        public bool IsBusy
        {
            get
            {
                foreach (GridPiece piece in pieces)
                {
                    if (piece != null && piece.IsMoving)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>The moves made so far.</summary>
        public int Moves => moveCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        void Awake()
        {
            Instance = this;
            grid = walls != null ? walls.layoutGrid : FindFirstObjectByType<Grid>();
            SetMoves(0);

            if (levelTitle != null && levelList != null)
            {
                int index = levelList.IndexOf(SceneManager.GetActiveScene().name);
                levelTitle.SetValue(index >= 0 ? levelList.Title(index) : SceneManager.GetActiveScene().name);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ---------------------------------------------------------------- Cells

        /// <summary>The cell a world position is in.</summary>
        public Vector2Int WorldToCell(Vector3 position)
        {
            if (grid != null)
            {
                Vector3Int cell = grid.WorldToCell(position);
                return new Vector2Int(cell.x, cell.y);
            }

            return new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        }

        /// <summary>The world position of a cell's middle.</summary>
        public Vector3 CellToWorld(Vector2Int cell, float z = 0f)
        {
            Vector3 world = grid != null
                ? grid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0))
                : new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);

            world.z = z;
            return world;
        }

        /// <summary>True if the cell has a wall tile.</summary>
        public bool IsWall(Vector2Int cell)
        {
            return walls != null && walls.HasTile(new Vector3Int(cell.x, cell.y, 0));
        }

        /// <summary>The piece taking up a cell (not an item), or null.</summary>
        public GridPiece BlockerAt(Vector2Int cell)
        {
            foreach (GridPiece piece in pieces)
            {
                if (piece != null && piece.BlocksCell && piece.Cell == cell)
                {
                    return piece;
                }
            }

            return null;
        }

        /// <summary>Every active piece in a cell.</summary>
        public IEnumerable<GridPiece> PiecesAt(Vector2Int cell)
        {
            foreach (GridPiece piece in pieces)
            {
                if (piece != null && piece.gameObject.activeInHierarchy && piece.Cell == cell)
                {
                    yield return piece;
                }
            }
        }

        // ---------------------------------------------------------------- Registration

        /// <summary>Called by each Grid Piece when the level starts.</summary>
        public void Register(GridPiece piece)
        {
            if (!pieces.Contains(piece))
            {
                pieces.Add(piece);
            }
        }

        /// <summary>Called by a Grid Piece when it's destroyed.</summary>
        public void Unregister(GridPiece piece)
        {
            pieces.Remove(piece);
        }

        /// <summary>Called by each Grid Goal when the level starts.</summary>
        public void Register(GridGoal goal)
        {
            if (!goals.Contains(goal))
            {
                goals.Add(goal);
            }
        }

        void LateUpdate()
        {
            // Every piece has registered by the end of the first frame: remember the start for Restart.
            if (startState == null && pieces.Count > 0)
            {
                startState = Capture();
                RefreshGoals();
            }
        }

        // ---------------------------------------------------------------- Moving

        /// <summary>
        /// Moves every Player piece one cell, pushing what's in the way if allowed.
        /// Returns true if anything moved.
        /// </summary>
        public bool TryMovePlayers(Vector2Int direction)
        {
            if (Solved || direction == Vector2Int.zero)
            {
                return false;
            }

            List<PieceState> before = Capture();

            // Move the front-most players first, so players in a line don't block each other.
            List<GridPiece> players = pieces.FindAll(p => p != null && p.gameObject.activeInHierarchy && p.PieceRole == GridPiece.Role.Player);
            players.Sort((a, b) => Vector2Int.Dot(b.Cell, direction).CompareTo(Vector2Int.Dot(a.Cell, direction)));

            bool anyMoved = false;

            foreach (GridPiece player in players)
            {
                toMove.Clear();

                if (!CanMove(player, direction, maxPushedInARow))
                {
                    continue;
                }

                // toMove lists the front of the chain first, so nothing moves into an occupied cell.
                foreach (GridPiece piece in toMove)
                {
                    piece.MoveTo(piece.Cell + direction);
                }

                CollectItems(player);
                anyMoved = true;
            }

            if (!anyMoved)
            {
                onBlocked?.Invoke();
                return false;
            }

            history.Push(before);
            SetMoves(moveCount + 1);
            RefreshGoals();
            CheckSolved();
            return true;
        }

        bool CanMove(GridPiece piece, Vector2Int direction, int pushesLeft)
        {
            Vector2Int target = piece.Cell + direction;

            if (IsWall(target))
            {
                return false;
            }

            GridPiece blocker = BlockerAt(target);

            if (blocker == null)
            {
                toMove.Add(piece);
                return true;
            }

            bool pusherCanPush = piece.PieceRole != GridPiece.Role.Player || piece.CanPush;

            if (blocker.PieceRole == GridPiece.Role.Pushable && pusherCanPush && pushesLeft > 0
                && CanMove(blocker, direction, pushesLeft - 1))
            {
                toMove.Add(piece);
                return true;
            }

            return false;
        }

        void CollectItems(GridPiece player)
        {
            foreach (GridPiece piece in pieces)
            {
                if (piece != null && piece.PieceRole == GridPiece.Role.Item && piece.gameObject.activeInHierarchy && piece.Cell == player.Cell)
                {
                    piece.Collect();
                }
            }
        }

        // ---------------------------------------------------------------- Undo and restart

        /// <summary>Takes back the last move.</summary>
        public void Undo()
        {
            if (Solved || history.Count == 0)
            {
                return;
            }

            Restore(history.Pop());
            SetMoves(moveCount - 1);
            RefreshGoals();
            onUndo?.Invoke();
        }

        /// <summary>Puts every piece back where it started. Can't be undone.</summary>
        public void Restart()
        {
            if (Solved || startState == null)
            {
                return;
            }

            Restore(startState);
            history.Clear();
            SetMoves(0);
            RefreshGoals();
        }

        List<PieceState> Capture()
        {
            List<PieceState> state = new List<PieceState>(pieces.Count);

            foreach (GridPiece piece in pieces)
            {
                if (piece != null)
                {
                    state.Add(new PieceState { piece = piece, cell = piece.Cell, active = piece.gameObject.activeSelf });
                }
            }

            return state;
        }

        void Restore(List<PieceState> state)
        {
            foreach (PieceState saved in state)
            {
                if (saved.piece == null)
                {
                    continue;
                }

                saved.piece.gameObject.SetActive(saved.active);
                saved.piece.PlaceAt(saved.cell);
            }
        }

        // ---------------------------------------------------------------- Solving

        void RefreshGoals()
        {
            foreach (GridGoal goal in goals)
            {
                if (goal != null)
                {
                    goal.Refresh(this);
                }
            }
        }

        void CheckSolved()
        {
            bool anyGoal = false;

            foreach (GridGoal goal in goals)
            {
                if (goal == null)
                {
                    continue;
                }

                anyGoal = true;

                if (!goal.IsFilled)
                {
                    return;
                }
            }

            bool itemsLeft = pieces.Exists(p => p != null && p.PieceRole == GridPiece.Role.Item && p.gameObject.activeSelf);

            if (needAllItems && itemsLeft)
            {
                return;
            }

            if (!anyGoal && !(needAllItems && !itemsLeft))
            {
                return;
            }

            Solved = true;

            if (levelList != null)
            {
                levelList.MarkSolved(SceneManager.GetActiveScene().name);
            }

            if (raiseWhenSolved != null)
            {
                raiseWhenSolved.Raise();
            }

            onSolved?.Invoke();
        }

        void SetMoves(int value)
        {
            moveCount = Mathf.Max(0, value);

            if (moves != null)
            {
                moves.SetValue(moveCount);
            }
        }
    }
}
