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

            // Movement Keys
            if (Input.GetKeyDown(KeyCode.W))      TryMove(Vector2Int.up);
            else if (Input.GetKeyDown(KeyCode.S)) TryMove(Vector2Int.down);
            else if (Input.GetKeyDown(KeyCode.A)) TryMove(Vector2Int.left);
            else if (Input.GetKeyDown(KeyCode.D)) TryMove(Vector2Int.right);
            
            // Barricade Keys
            else if (Input.GetKeyDown(KeyCode.UpArrow))    TryDropBarricade(Vector2Int.up);
            else if (Input.GetKeyDown(KeyCode.DownArrow))  TryDropBarricade(Vector2Int.down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow))  TryDropBarricade(Vector2Int.left);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) TryDropBarricade(Vector2Int.right);
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