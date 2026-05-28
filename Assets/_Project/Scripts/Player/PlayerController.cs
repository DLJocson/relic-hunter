using UnityEngine;
using RelicHunter.Core;

namespace RelicHunter.Player
{
    /// <summary>
    /// Handles player input and character movement logic.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public GridManager gridManager;
        public TurnManager turnManager;
        public Vector2Int gridPosition;

        private void Start()
        {
            transform.position = new Vector3(gridPosition.x, gridPosition.y, 0);
            Debug.Log("PlayerController loaded");
        }

        private void Update()
        {
            if (turnManager == null || turnManager.currentTurn != TurnManager.TurnState.PlayerTurn)
                return;
            if (Input.GetKeyDown(KeyCode.W))
                TryMove(Vector2Int.up);
            else if (Input.GetKeyDown(KeyCode.S))
                TryMove(Vector2Int.down);
            else if (Input.GetKeyDown(KeyCode.A))
                TryMove(Vector2Int.left);
            else if (Input.GetKeyDown(KeyCode.D))
                TryMove(Vector2Int.right);
        }

        private void TryMove(Vector2Int direction)
        {
            Vector2Int target = gridPosition + direction;

            if (gridManager != null && gridManager.IsTileWalkable(target.x, target.y))
            {
                gridPosition = target;
                transform.position = new Vector3(gridPosition.x, gridPosition.y, 0);
                
                turnManager.EndPlayerTurn();
            }
        }
    }
}
