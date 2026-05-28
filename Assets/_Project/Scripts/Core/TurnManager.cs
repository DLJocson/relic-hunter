using UnityEngine;
using System.Collections.Generic;

namespace RelicHunter.Core
{
    /// <summary>
    /// Manages turn-based game logic and turn transitions.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public enum TurnState
        {
            PlayerTurn,
            GuardTurn,
            Processing
        }

        public TurnState currentTurn = TurnState.PlayerTurn;
        private GridManager gridManager;

        private void Start()
        {
            gridManager = Object.FindFirstObjectByType<GridManager>();
            Debug.Log("TurnManager: Game initialized on Player Turn.");
        }

        public void EndPlayerTurn()
        {
            currentTurn = TurnState.Processing;
            
            if (gridManager != null)
            {
                TickBarricades();
            }

            currentTurn = TurnState.GuardTurn;
            Debug.Log("TurnManager: It is now the Guard's Turn.");
            
            EndGuardTurn(); 
        }

        public void EndGuardTurn()
        {
            currentTurn = TurnState.PlayerTurn;
            Debug.Log("TurnManager: It is now the Player's Turn.");
        }

        private void TickBarricades()
        {
            List<Vector2Int> keys = new List<Vector2Int>(gridManager.activeBarricades.Keys);
            foreach (Vector2Int key in keys)
            {
                gridManager.activeBarricades[key]--;
                if (gridManager.activeBarricades[key] <= 0)
                {
                    gridManager.activeBarricades.Remove(key);
                    Debug.Log($"Engine: Barricade data at {key} expired.");

                    if (gridManager.visualBarricades.ContainsKey(key))
                    {
                        Destroy(gridManager.visualBarricades[key]);
                        gridManager.visualBarricades.Remove(key);
                        Debug.Log($"Visuals: Barricade square at {key} removed from scene.");
                    }
                }
            }
        }
    }
}
