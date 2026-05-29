// =============================================================================
// GuardController.cs - Handles guard movement and behavior
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
            ExecuteGuardTurnWithBarricadeChecks();
        }

        /// <summary>
        /// Public helper called by GameManager to teleport guard during round changes
        /// </summary>
        public void ResetToPosition(Vector2Int newGridPos)
        {
            CurrentGridPos = newGridPos;
            SnapTransformToGrid();
            RegisterGuardPosition();
        }

        private void ExecuteGuardTurnWithBarricadeChecks()
        {
            ResolveSceneReferences();

            if (gridManager == null)
            {
                EndGuardTurnSafely();
                return;
            }

            Vector2Int thiefPos = gridManager.playerPos;
            Vector2Int bestStep = CurrentGridPos;
            
            int dx = System.Math.Sign(thiefPos.x - CurrentGridPos.x);
            int dy = System.Math.Sign(thiefPos.y - CurrentGridPos.y);

            bool stepTaken = false;

            if (dx != 0)
            {
                Vector2Int testStep = CurrentGridPos + new Vector2Int(dx, 0);
                if (gridManager.IsTileWalkable(testStep.x, testStep.y))
                {
                    bestStep = testStep;
                    stepTaken = true;
                }
            }

            if (!stepTaken && dy != 0)
            {
                Vector2Int testStep = CurrentGridPos + new Vector2Int(0, dy);
                if (gridManager.IsTileWalkable(testStep.x, testStep.y))
                {
                    bestStep = testStep;
                    stepTaken = true;
                }
            }

            if (!stepTaken)
            {
                Vector2Int[] fallbackDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                foreach (Vector2Int dir in fallbackDirections)
                {
                    Vector2Int testStep = CurrentGridPos + dir;
                    if (gridManager.IsTileWalkable(testStep.x, testStep.y))
                    {
                        bestStep = testStep;
                        stepTaken = true;
                        break;
                    }
                }
            }

            CurrentGridPos = bestStep;
            SnapTransformToGrid();
            RegisterGuardPosition();

            if (turnManager != null)
            {
                bool gameEnded = turnManager.CheckWinLossConditions();
                if (gameEnded) return; 
            }

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