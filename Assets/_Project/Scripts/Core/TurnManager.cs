// =============================================================================
// TurnManager.cs — Bulletproof State Router Variant
// =============================================================================

using UnityEngine;
using System.Collections.Generic;
using RelicHunter.Enemy; 

namespace RelicHunter.Core
{
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
            ResolveReferences();
            currentTurn = TurnState.PlayerTurn;
            Debug.Log("<color=cyan>TurnManager: System initialized. Player Turn Active.</color>");
        }

        private void ResolveReferences()
        {
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (guardController == null) guardController = FindFirstObjectByType<GuardController>();
        }

        public void EndPlayerTurn()
        {
            ResolveReferences();
            currentTurn = TurnState.Processing;
            
            CheckWinLossConditions();
            
            try 
            {
                TickBarricades();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TurnManager Critical Catch] Error inside TickBarricades: {ex.Message}");
            }

            currentTurn = TurnState.GuardTurn;
            
            if (guardController != null)
            {
                guardController.TakeTurn();
            }
            else
            {
                Debug.LogWarning("[TurnManager] GuardController component not detected in scene. Resetting back to Player turn cleanly.");
                EndGuardTurn();
            }
        }

        public void RegisterGuardPosition(Vector2Int newPos)
        {
            CheckWinLossConditions();
        }

        public void EndGuardTurn()
        {
            currentTurn = TurnState.PlayerTurn;
            Debug.Log("<color=yellow>TurnManager: System refreshed back to Player's Turn.</color>");
        }

        private void TickBarricades()
        {
            if (gridManager == null || gridManager.activeBarricades == null) return;

            List<Vector2Int> keysToRemove = new List<Vector2Int>();
            List<Vector2Int> keysToUpdate = new List<Vector2Int>(gridManager.activeBarricades.Keys);

            foreach (Vector2Int key in keysToUpdate)
            {
                gridManager.activeBarricades[key]--;
                if (gridManager.activeBarricades[key] <= 0)
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (Vector2Int key in keysToRemove)
            {
                gridManager.activeBarricades.Remove(key);
                Debug.Log($"Engine: Barricade at {key} expired.");

                if (gridManager.visualBarricades.ContainsKey(key))
                {
                    if (gridManager.visualBarricades[key] != null)
                    {
                        Destroy(gridManager.visualBarricades[key]);
                    }
                    gridManager.visualBarricades.Remove(key);
                }
            }
        }

        public void CheckWinLossConditions()
        {
            if (gridManager == null) return;

            if (gridManager.playerPos == gridManager.exitPos)
            {
                Debug.Log("<color=green>Match Event: Thief reached exit tile.</color>");
            }

            if (gridManager.playerPos == gridManager.guardPos)
            {
                Debug.Log("<color=red>Match Event: Guard reached Thief tile.</color>");
            }
        }
    }
}