using UnityEngine;
using System.Collections.Generic;
using RelicHunter.Enemy;
using RelicHunter.Player;

namespace RelicHunter.Core
{
    /// <summary>
    /// Alternates player and guard turns and coordinates barricade decay.
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
        private GameManager gameManager;
        private RelicHunter.UI.UIManager uiManager;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            ResolveReferences();
            currentTurn = TurnState.Processing;
            if (uiManager != null)
                uiManager.UpdateTurnNotice(currentTurn);
            Debug.Log("<color=cyan>TurnManager: System initialized. Awaiting match start.</color>");
        }

        private void ResolveReferences()
        {
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (guardController == null) guardController = FindFirstObjectByType<GuardController>();
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<RelicHunter.UI.UIManager>();
        }

        public void EndPlayerTurn()
        {
            ResolveReferences();

            if (currentTurn == TurnState.Processing) return;

            currentTurn = TurnState.Processing;

            if (uiManager != null) uiManager.UpdateTurnNotice(currentTurn);

            if (CheckWinLossConditions()) return;

            try
            {
                TickBarricades();

                if (gridManager != null && uiManager != null)
                {
                    uiManager.UpdateBarricades(gridManager.activeBarricades.Count, gridManager.maxBarricadesAllowed);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TurnManager Error] Inside TickBarricades: {ex.Message}");
            }

            currentTurn = TurnState.GuardTurn;

            if (uiManager != null) uiManager.UpdateTurnNotice(currentTurn);

            if (guardController != null)
            {
                guardController.TakeTurn();
            }
            else
            {
                EndGuardTurn();
            }
        }

        public void EndGuardTurn()
        {
            ResolveReferences();
            if (currentTurn == TurnState.Processing) return;

            currentTurn = TurnState.PlayerTurn;

            if (uiManager != null) uiManager.UpdateTurnNotice(currentTurn);

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

        /// <summary>
        /// Evaluates coordinates to see if the round should terminate.
        /// Returns true if the round has ended.
        /// </summary>
        public bool CheckWinLossConditions()
        {
            ResolveReferences();
            if (gridManager == null) return false;

            if (gridManager.playerPos == gridManager.guardPos)
            {
                currentTurn = TurnState.Processing;
                Debug.Log("<color=red><b>[GAME OVER]</b> The Guard caught the player!</color>");

                if (uiManager != null)
                    uiManager.UpdateTurnNotice(currentTurn);

                if (gameManager != null)
                {
                    gameManager.EndRound(false);
                }
                return true;
            }

            if (gridManager.playerPos == gridManager.exitPos)
            {
                currentTurn = TurnState.Processing;
                Debug.Log("<color=green><b>[VICTORY]</b> The Thief escaped through the exit!</color>");

                if (gameManager != null)
                {
                    gameManager.EndRound(true);
                }
                return true;
            }

            return false;
        }
    }
}