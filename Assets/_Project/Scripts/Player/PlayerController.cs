// =============================================================================
// PlayerController.cs — Handles player input and character grid-movement.
// =============================================================================

using UnityEngine;
using RelicHunter.Core;

namespace RelicHunter.Player
{
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

        private void Start()
        {
            SnapTransformToGrid();
            ResolveSceneReferences();
            RegisterPlayerPosition();
        }

        private void Update()
        {
            if (turnManager == null || turnManager.currentTurn != TurnManager.TurnState.PlayerTurn)
                return;

            // Movement Keys - initial input uses GetKeyDown, repeat uses held key + delay
            Vector2Int keyDownDirection = GetKeyDownInput();
            if (keyDownDirection != Vector2Int.zero)
            {
                // First keypress of this frame - move immediately
                TryMove(keyDownDirection);
                lastDirection = keyDownDirection;
                timeSinceLastMove = 0f;
            }
            else if (lastDirection != Vector2Int.zero)
            {
                // Check if same direction is still held
                Vector2Int heldDirection = GetDirectionalInput();
                if (heldDirection == lastDirection)
                {
                    // Same direction still held - repeat after delay
                    timeSinceLastMove += Time.deltaTime;
                    if (timeSinceLastMove >= repeatDelay)
                    {
                        TryMove(heldDirection);
                        timeSinceLastMove = 0f;
                    }
                }
                else
                {
                    // Direction released or changed
                    lastDirection = Vector2Int.zero;
                    timeSinceLastMove = 0f;
                }
            }

            // Barricade Keys
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

        private void TryMove(Vector2Int direction)
        {
            Vector2Int target = gridPosition + direction;

            if (gridManager != null && gridManager.IsTileWalkable(target.x, target.y))
            {
                gridPosition = target;
                SnapTransformToGrid();
                RegisterPlayerPosition();

                Debug.Log($"[PlayerController] Player stepped onto {gridPosition}. Intercepting condition checks.");

                if (turnManager != null)
                {
                    bool gameHasEnded = turnManager.CheckWinLossConditions();
                    if (gameHasEnded)
                    {
                        return;
                    }
                }

                if (turnManager != null)
                {
                    turnManager.EndPlayerTurn();
                }
            }
        }

        private void TryDropBarricade(Vector2Int direction)
        {
            Vector2Int targetPos = gridPosition + direction;

            if (gridManager != null)
            {
                bool success = gridManager.TryPlaceBarricade(targetPos);
                if (success && turnManager != null)
                {
                    turnManager.EndPlayerTurn();
                }
            }
        }

        private Vector2Int GetDirectionalInput()
        {
            if (Input.GetKey(KeyCode.W)) return Vector2Int.up;
            if (Input.GetKey(KeyCode.S)) return Vector2Int.down;
            if (Input.GetKey(KeyCode.A)) return Vector2Int.left;
            if (Input.GetKey(KeyCode.D)) return Vector2Int.right;
            return Vector2Int.zero;
        }

        public void SnapTransformToGrid()
        {
            transform.position = new Vector3(gridPosition.x, gridPosition.y, 0f);
        }

        private void RegisterPlayerPosition()
        {
            if (gridManager != null)
            {
                gridManager.playerPos = gridPosition;
            }
        }

        private void ResolveSceneReferences()
        {
            if (turnManager == null)
                turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindFirstObjectByType<TurnManager>();

            if (gridManager == null)
                gridManager = GridManager.Instance != null ? GridManager.Instance : FindFirstObjectByType<GridManager>();
        }
    }
}