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

            turnManager = GameObject.FindFirstObjectByType<TurnManager>();

            if (turnManager == null)
            {
                // Try searching by the literal name of the Hierarchy object instead!
                GameObject tmObject = GameObject.Find("TurnManager");
                if (tmObject != null)
                {
                    turnManager = tmObject.GetComponent<TurnManager>();
                }
            }

            if (gridManager != null)
            {
                gridManager.playerPos = gridPosition;
            }
            Debug.Log("PlayerController loaded");

            if (turnManager != null)
            {
                Debug.Log("<color=green>SUCCESS: PlayerController successfully linked to TurnManager object!</color>");
            }
            else
            {
                Debug.LogError("QA Alert: Player cannot see the TurnManager script component!");
            }
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
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                TryDropBarricade(Vector2Int.up);
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                TryDropBarricade(Vector2Int.down);
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                TryDropBarricade(Vector2Int.left);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                TryDropBarricade(Vector2Int.right);
        }

        private void TryMove(Vector2Int direction)
        {
            Vector2Int target = gridPosition + direction;

            if (gridManager != null && gridManager.IsTileWalkable(target.x, target.y))
            {
                gridPosition = target;
                transform.position = new Vector3(gridPosition.x, gridPosition.y, 0);

                gridManager.playerPos = gridPosition;

                turnManager.EndPlayerTurn();
            }
        }

        private void TryDropBarricade(Vector2Int direction)
        {
            // Target coordinate relative to where the player is currently standing
            Vector2Int targetPos = gridPosition + direction;

            if (gridManager != null)
            {
                // Ask GridManager to mathematically register the barricade
                bool success = gridManager.TryPlaceBarricade(targetPos);

                if (success)
                {
                    // Dropping a barricade counts as your action, so end the turn!
                    turnManager.EndPlayerTurn();
                }
            }
        }
    }
}
