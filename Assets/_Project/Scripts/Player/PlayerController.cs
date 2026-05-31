using UnityEngine;
using RelicHunter.Core;

namespace RelicHunter.Player
{
    /// <summary>
    /// Player input, grid movement, and barricade placement on the active maze.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Grid Setup")]
        public GridManager gridManager;
        public TurnManager turnManager;
        public Vector2Int gridPosition;

        [Header("Movement")]
        [SerializeField] private float repeatDelay = 0.5f;
        private Vector2Int lastDirection = Vector2Int.zero;
        private float timeSinceLastMove = 0f;

        private MazeGridBridge mazeBridge;
        private RoundFeedbackController roundFeedback;

        private void Start()
        {
            ResolveSceneReferences();
            SnapTransformToGrid();
            RegisterPlayerPosition();
        }

        private void Update()
        {
            if (turnManager == null || turnManager.currentTurn != TurnManager.TurnState.PlayerTurn)
                return;

            HandleBarricadeInput();
            HandleMovementInput();
        }

        private void HandleMovementInput()
        {
            Vector2Int keyDownDirection = GetKeyDownInput();
            if (keyDownDirection != Vector2Int.zero)
            {
                TryMove(keyDownDirection);
                lastDirection = keyDownDirection;
                timeSinceLastMove = 0f;
            }
            else if (lastDirection != Vector2Int.zero)
            {
                Vector2Int heldDirection = GetDirectionalInput();
                if (heldDirection == lastDirection)
                {
                    timeSinceLastMove += Time.deltaTime;
                    if (timeSinceLastMove >= repeatDelay)
                    {
                        TryMove(heldDirection);
                        timeSinceLastMove = 0f;
                    }
                }
                else
                {
                    lastDirection = Vector2Int.zero;
                    timeSinceLastMove = 0f;
                }
            }
        }

        private void HandleBarricadeInput()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) TryDropBarricade(Vector2Int.up);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) TryDropBarricade(Vector2Int.down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) TryDropBarricade(Vector2Int.left);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) TryDropBarricade(Vector2Int.right);
        }

        private Vector2Int GetKeyDownInput()
        {
            if (Input.GetKeyDown(KeyCode.W)) return Vector2Int.up;
            if (Input.GetKeyDown(KeyCode.S)) return Vector2Int.down;
            if (Input.GetKeyDown(KeyCode.A)) return Vector2Int.left;
            if (Input.GetKeyDown(KeyCode.D)) return Vector2Int.right;
            return Vector2Int.zero;
        }

        private Vector2Int GetDirectionalInput()
        {
            if (Input.GetKey(KeyCode.W)) return Vector2Int.up;
            if (Input.GetKey(KeyCode.S)) return Vector2Int.down;
            if (Input.GetKey(KeyCode.A)) return Vector2Int.left;
            if (Input.GetKey(KeyCode.D)) return Vector2Int.right;
            return Vector2Int.zero;
        }

        private void TryMove(Vector2Int direction)
        {
            Vector2Int target = gridPosition + direction;

            if (!CanStep(gridPosition, target))
                return;

            gridPosition = target;
            SnapTransformToGrid();
            RegisterPlayerPosition();

            if (turnManager != null)
            {
                bool gameHasEnded = turnManager.CheckWinLossConditions();
                if (gameHasEnded) return;
            }

            if (turnManager != null)
                turnManager.EndPlayerTurn();
        }

        private bool CanStep(Vector2Int from, Vector2Int to)
        {
            if (gridManager == null) return false;

            if (mazeBridge != null)
            {
                if (gridManager.activeBarricades.ContainsKey(to)) return false;
                return mazeBridge.CanMoveBetween(from, to);
            }

            return gridManager.CanEnterCell(from, to);
        }

        private void TryDropBarricade(Vector2Int direction)
        {
            Vector2Int targetPos = gridPosition + direction;

            if (gridManager != null)
            {
                bool success = gridManager.TryPlaceBarricade(targetPos, out bool wouldTrapPlayer);
                if (success)
                {
                    if (turnManager != null)
                        turnManager.EndPlayerTurn();
                }
                else if (wouldTrapPlayer)
                {
                    roundFeedback?.PlayBarricadeDeniedSound();
                }
            }
        }

        public void SnapTransformToGrid()
        {
            if (mazeBridge != null)
                transform.position = mazeBridge.GridToWorld(gridPosition);
            else if (gridManager != null)
                transform.position = gridManager.GetWorldPositionForCell(gridPosition);
            else
                transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);
        }

        public void RefreshAfterMazeReady()
        {
            SnapTransformToGrid();
            RegisterPlayerPosition();
        }

        private void RegisterPlayerPosition()
        {
            if (gridManager != null)
                gridManager.playerPos = gridPosition;
        }

        private void ResolveSceneReferences()
        {
            if (turnManager == null)
                turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindFirstObjectByType<TurnManager>();

            if (gridManager == null)
                gridManager = GridManager.Instance != null ? GridManager.Instance : FindFirstObjectByType<GridManager>();

            if (mazeBridge == null)
                mazeBridge = MazeGridBridge.Instance != null ? MazeGridBridge.Instance : FindFirstObjectByType<MazeGridBridge>();

            if (roundFeedback == null)
                roundFeedback = FindFirstObjectByType<RoundFeedbackController>();
        }
    }
}

