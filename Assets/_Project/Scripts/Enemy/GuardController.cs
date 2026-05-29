// =============================================================================
// GuardController.cs — Controlled Snap Variant to prevent state freezes.
// =============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RelicHunter.Core; 

namespace RelicHunter.Enemy
{
    public class GuardController : MonoBehaviour
    {
        [Header("Starting Position")]
        [SerializeField] private Vector2Int startGridPos = new Vector2Int(8, 8);

        public Vector2Int CurrentGridPos { get; private set; }

        private GridManager gridManager;
        private TurnManager turnManager;

        private void Awake()
        {
            CurrentGridPos = startGridPos;
        }

        private void Start()
        {
            ResolveSceneReferences();
            SnapTransformToGrid();
            RegisterGuardPosition();
        }

        public void TakeTurn()
        {
            // Forces the turn execution to run immediately and safely without frame locks
            ExecuteGuardTurnInstant();
        }

        private void ExecuteGuardTurnInstant()
        {
            ResolveSceneReferences();

            if (gridManager == null)
            {
                Debug.LogWarning("[GuardController] Missing GridManager reference! Releasing turn loop control cleanly.");
                EndGuardTurnSafely();
                return;
            }

            Vector2Int thiefPos = gridManager.playerPos;
            Vector2Int nextStep = CurrentGridPos;

            // Simple direct tracking logic
            if (nextStep.x > thiefPos.x) nextStep.x--;
            else if (nextStep.x < thiefPos.x) nextStep.x++;
            else if (nextStep.y > thiefPos.y) nextStep.y--;
            else if (nextStep.y < thiefPos.y) nextStep.y++;

            // Update local coordinates
            CurrentGridPos = nextStep;
            
            // Instantly move the sprite physically on screen (removes Coroutine yield risks)
            SnapTransformToGrid();
            
            // Register positions with engine trackers
            RegisterGuardPosition();

            Debug.Log($"[GuardController] Guard snapped to {CurrentGridPos}. Completing turn block.");
            
            // Absolutely forces TurnManager back to PlayerTurn state instantly
            EndGuardTurnSafely();
        }

        private void SnapTransformToGrid()
        {
            transform.position = new Vector3(CurrentGridPos.x, CurrentGridPos.y, 0f);
        }

        private void RegisterGuardPosition()
        {
            if (gridManager != null)
                gridManager.SetGuardPosition(CurrentGridPos);

            if (turnManager != null)
                turnManager.RegisterGuardPosition(CurrentGridPos);
        }

        private void ResolveSceneReferences()
        {
            if (gridManager == null)
                gridManager = GridManager.Instance != null ? GridManager.Instance : FindFirstObjectByType<GridManager>();

            if (turnManager == null)
                turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindFirstObjectByType<TurnManager>();
        }

        private void EndGuardTurnSafely()
        {
            if (turnManager != null)
            {
                turnManager.EndGuardTurn();
            }
        }
    }
}