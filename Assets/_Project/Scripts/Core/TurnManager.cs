using UnityEngine;
using System.Collections.Generic;
using RelicHunter.Enemy; // Connects to the GuardController's namespace folder

namespace RelicHunter.Core
{
    /// <summary>
    /// Manages turn-based game logic and turn transitions.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        public enum TurnState
        {
            PlayerTurn,
            GuardTurn,
            Processing
        }

        public TurnState currentTurn = TurnState.PlayerTurn;
        private GridManager gridManager;
        private GuardController guardController;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            gridManager = Object.FindFirstObjectByType<GridManager>();
            guardController = Object.FindFirstObjectByType<GuardController>();
            
            currentTurn = TurnState.PlayerTurn;
            Debug.Log("<color=cyan>TurnManager: System initialized. It is now the Player's Turn.</color>");
        }

        public void EndPlayerTurn()
        {
            currentTurn = TurnState.Processing;
            
            CheckWinLossConditions();
            if (currentTurn == TurnState.Processing) return; // Stop if game has resolved!

            if (gridManager != null)
            {
                TickBarricades();
            }

            currentTurn = TurnState.GuardTurn;
            Debug.Log("TurnManager: Handed turn over to Guard AI.");
            
            // Execute your leader's master AI pathfinding loop
            if (guardController != null)
            {
                guardController.TakeTurn();
            }
            else
            {
                // Fallback check to find the guard if it spawned late
                guardController = Object.FindFirstObjectByType<GuardController>();
                if (guardController != null)
                {
                    guardController.TakeTurn();
                }
                else
                {
                    Debug.LogError("TurnManager Critical Error: GuardController could not be found in the scene!");
                    EndGuardTurn();
                }
            }
        }

        /// <summary>
        /// Public sync hook called by GuardController to track movement progression
        /// </summary>
        public void RegisterGuardPosition(Vector2Int newPos)
        {
            CheckWinLossConditions();
        }

        public void EndGuardTurn()
        {
            currentTurn = TurnState.PlayerTurn;
            Debug.Log("<color=yellow>TurnManager: Refreshed back to Player's Turn.</color>");
        }

        private void TickBarricades()
        {
            if (gridManager == null) return;

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

        public void CheckWinLossConditions()
        {
            if (gridManager == null) return;

            // Condition A: Thief shares a tile with the Exit = THIEF WINS
            if (gridManager.playerPos == gridManager.exitPos)
            {
                currentTurn = TurnState.Processing;
                Debug.Log("<color=green>GAME OVER: The Thief has successfully escaped with the relic!</color>");
                return;
            }

            // Condition B: Guard catches the Thief on the same tile = GUARD WINS
            if (gridManager.playerPos == gridManager.guardPos)
            {
                currentTurn = TurnState.Processing;
                Debug.Log("<color=red>GAME OVER: Caught by the Guard! The Thief was captured.</color>");
                return;
            }
        }
    }
}