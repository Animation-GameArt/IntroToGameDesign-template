using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CollinGame.Kits.GridPuzzle
{
    /// <summary>
    /// Anything that sits on the puzzle grid: the player, a box, a rock, a key.
    /// Pieces snap to the middle of a cell when the level starts, and slide one
    /// cell at a time.
    ///
    ///   Player     moved by the arrow keys or stick. A level can have several.
    ///   Pushable   moves one cell when pushed, if the cell behind it is free.
    ///   Solid      never moves, and blocks the way. For rocks and statues.
    ///   Item       the player walks onto it to pick it up, such as a gem.
    ///
    /// No colliders or Rigidbodies are needed: the Grid Board works everything out.
    /// </summary>
    [AddComponentMenu("Collin Game/Grid Puzzle Kit/Grid Piece")]
    public class GridPiece : MonoBehaviour
    {
        /// <summary>What the piece does.</summary>
        public enum Role
        {
            Player,
            Pushable,
            Solid,
            Item
        }

        [Tooltip("What the piece does.")]
        [SerializeField] Role role = Role.Pushable;

        [Tooltip("Optional. A word that Grid Goals can ask for, such as red or crate. Leave empty to match any goal for its role.")]
        [SerializeField] string kind = "";

        [Tooltip("Player only: ticked, the player can push Pushable pieces.")]
        [SerializeField] bool canPush = true;

        [Tooltip("Seconds a one-cell slide takes. 0 jumps straight there.")]
        [SerializeField, Min(0f)] float moveTime = 0.1f;

        [Tooltip("Runs each time the piece moves. Use it for a sound.")]
        [SerializeField] UnityEvent onMoved;

        [Tooltip("Item only: runs when the player picks it up.")]
        [SerializeField] UnityEvent onCollected;

        Coroutine sliding;

        /// <summary>What the piece does.</summary>
        public Role PieceRole => role;

        /// <summary>The word Grid Goals can ask for. May be empty.</summary>
        public string Kind => kind;

        /// <summary>True if this player piece can push Pushable pieces.</summary>
        public bool CanPush => canPush;

        /// <summary>The cell the piece is in.</summary>
        public Vector2Int Cell { get; private set; }

        /// <summary>True while the piece is sliding to a new cell.</summary>
        public bool IsMoving => sliding != null;

        /// <summary>True for pieces that take up their cell: everything but items.</summary>
        public bool BlocksCell => role != Role.Item && gameObject.activeInHierarchy;

        GridBoard board;

        void Start()
        {
            board = GridBoard.Instance;

            if (board == null)
            {
                Debug.LogWarning($"Grid Piece on \"{name}\": there is no Grid Board in the scene.", this);
                return;
            }

            board.Register(this);
            Cell = board.WorldToCell(transform.position);
            transform.position = board.CellToWorld(Cell, transform.position.z);
        }

        void OnDestroy()
        {
            if (board != null)
            {
                board.Unregister(this);
            }
        }

        /// <summary>Slides the piece to a cell. The Grid Board calls this; use the board to move pieces.</summary>
        public void MoveTo(Vector2Int cell)
        {
            Cell = cell;
            Vector3 target = board.CellToWorld(cell, transform.position.z);

            if (sliding != null)
            {
                StopCoroutine(sliding);
                sliding = null;
            }

            if (moveTime <= 0f || !isActiveAndEnabled)
            {
                transform.position = target;
            }
            else
            {
                sliding = StartCoroutine(Slide(target));
            }

            onMoved?.Invoke();
        }

        /// <summary>Puts the piece in a cell at once, with no slide. Used by undo.</summary>
        public void PlaceAt(Vector2Int cell)
        {
            if (sliding != null)
            {
                StopCoroutine(sliding);
                sliding = null;
            }

            Cell = cell;
            transform.position = board.CellToWorld(cell, transform.position.z);
        }

        /// <summary>Picks up an item: it disappears until an undo brings it back.</summary>
        public void Collect()
        {
            onCollected?.Invoke();
            gameObject.SetActive(false);
        }

        IEnumerator Slide(Vector3 target)
        {
            Vector3 from = transform.position;

            for (float t = 0f; t < moveTime; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, target, t / moveTime);
                yield return null;
            }

            transform.position = target;
            sliding = null;
        }
    }
}
