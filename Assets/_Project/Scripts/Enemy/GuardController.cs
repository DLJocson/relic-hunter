// =============================================================================
// GuardController.cs - Handles guard movement and behavior
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RelicHunter.Core;

namespace RelicHunter.Enemy
{
    public class GuardController : MonoBehaviour
    {
        [Header("Starting Position")]
        [SerializeField] private Vector2Int startGridPos = new Vector2Int(8, 8);

        [Header("Movement Speed")]
        private float guardSpeed = 1f;
        private float movementAccumulator = 0f;

        [Header("Fallback Behavior")]
        [SerializeField] private bool allowGreedyFallback = true;

        public Vector2Int CurrentGridPos { get; private set; }

        public event Action<Vector2Int> GuardMoved;

        private GridManager gridManager;
        private TurnManager turnManager;
        private MazeGridBridge mazeBridge;

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

        public void SetGuardSpeed(float speed)
        {
            guardSpeed = Mathf.Max(0f, speed);
            movementAccumulator = 0f;
        }

        public void TakeTurn()
        {
            ResolveSceneReferences();

            movementAccumulator += guardSpeed;

            // Move tiles equal to accumulated movement
            while (movementAccumulator >= 1f)
            {
                bool keepGoing = ExecuteStrategicGuardMove();
                movementAccumulator -= 1f;

                if (!keepGoing)
                {
                    break;
                }

                if (turnManager != null && turnManager.currentTurn == TurnManager.TurnState.Processing)
                {
                    break;
                }
            }

            // Signal end of guard turn
            if (turnManager != null && turnManager.currentTurn != TurnManager.TurnState.Processing)
            {
                EndGuardTurnSafely();
            }
        }

        /// <summary>
        /// Public helper called by GameManager to teleport guard during round changes
        /// </summary>
        public void ResetToPosition(Vector2Int newGridPos)
        {
            CurrentGridPos = newGridPos;
            movementAccumulator = 0f;
            SnapTransformToGrid();
            RegisterGuardPosition();
        }

        private bool ExecuteStrategicGuardMove()
        {
            ResolveSceneReferences();

            if (gridManager == null)
            {
                EndGuardTurnSafely();
                return false;
            }

            Vector2Int thiefPos = gridManager.playerPos;

            if (CurrentGridPos == thiefPos)
            {
                RegisterGuardPosition();
                return true;
            }

            Vector2Int nextStep = DetermineBestGuardMove(thiefPos);

            if (nextStep == CurrentGridPos && allowGreedyFallback)
            {
                nextStep = FindGreedyFallbackMove(thiefPos);
            }

            if (!CanStep(CurrentGridPos, nextStep))
            {
                Debug.LogWarning("[GuardController] Blocked an illegal AI move! Guard is holding position.");
                nextStep = CurrentGridPos;
            }

            ApplyGuardPosition(nextStep);
            GuardMoved?.Invoke(CurrentGridPos);

            if (turnManager != null)
            {
                bool gameEnded = turnManager.CheckWinLossConditions();
                if (gameEnded) return false;
            }

            return true;
        }

        private Vector2Int DetermineBestGuardMove(Vector2Int thiefPos)
        {
            if (gridManager == null)
                return CurrentGridPos;

            int maxDepth = 1;
            if (GameManager.Instance != null)
            {
                maxDepth = Mathf.Max(0, GameManager.Instance.CurrentMinimaxDepth);
            }

            Vector2Int bestStep = Minimax.GetBestGuardMove(
                CurrentGridPos,
                thiefPos,
                gridManager.exitPos,
                new HashSet<Vector2Int>(gridManager.permanentWalls),
                new Dictionary<Vector2Int, int>(gridManager.activeBarricades),
                gridManager.Width,
                gridManager.Height,
                maxDepth,
                gridManager.barricadeDuration,
                gridManager.maxBarricadesAllowed,
                (from, to) => CanTraverseForAi(from, to)
            );

            if (bestStep == Minimax.TRAPPED)
                return CurrentGridPos;

            return bestStep;
        }

        private Vector2Int FindGreedyFallbackMove(Vector2Int thiefPos)
        {
            if (gridManager == null)
                return CurrentGridPos;

            Vector2Int bestStep = CurrentGridPos;

            int dx = System.Math.Sign(thiefPos.x - CurrentGridPos.x);
            int dy = System.Math.Sign(thiefPos.y - CurrentGridPos.y);

            bool stepTaken = false;

            if (dx != 0)
            {
                TryPickGreedyStep(CurrentGridPos + new Vector2Int(dx, 0), ref bestStep, ref stepTaken);
                if (!stepTaken)
                    TryPickGreedyStep(CurrentGridPos + new Vector2Int(dx * 2, 0), ref bestStep, ref stepTaken);
            }

            if (!stepTaken && dy != 0)
            {
                TryPickGreedyStep(CurrentGridPos + new Vector2Int(0, dy), ref bestStep, ref stepTaken);
                if (!stepTaken)
                    TryPickGreedyStep(CurrentGridPos + new Vector2Int(0, dy * 2), ref bestStep, ref stepTaken);
            }

            if (!stepTaken)
            {
                Vector2Int[] fallbackDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                foreach (Vector2Int dir in fallbackDirections)
                {
                    Vector2Int testStep = CurrentGridPos + dir;
                    if (TryPickGreedyStep(testStep, ref bestStep, ref stepTaken))
                        break;

                    Vector2Int leapStep = CurrentGridPos + dir * 2;
                    if (TryPickGreedyStep(leapStep, ref bestStep, ref stepTaken))
                        break;
                }
            }

            return bestStep;
        }

        private bool CanStep(Vector2Int from, Vector2Int to)
        {
            if (gridManager == null) return false;

            int manhattan = Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
            if (manhattan == 2)
                return gridManager.CanGuardLeapTo(from, to);
            if (manhattan == 1)
                return gridManager.CanEnterCell(from, to, isGuard: true);

            return false;
        }

        /// <summary>Topology-only check for minimax simulation (exit leap handled in Minimax).</summary>
        private bool CanTraverseForAi(Vector2Int from, Vector2Int to)
        {
            if (gridManager == null) return false;

            int manhattan = Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
            if (manhattan == 2)
                return gridManager.CanGuardLeapTo(from, to);
            if (manhattan == 1)
                return gridManager.CanEnterCell(from, to, isGuard: false);

            return false;
        }

        private bool TryPickGreedyStep(Vector2Int testStep, ref Vector2Int bestStep, ref bool stepTaken)
        {
            if (!CanStep(CurrentGridPos, testStep))
                return false;

            bestStep = testStep;
            stepTaken = true;
            return true;
        }

        private void ApplyGuardPosition(Vector2Int gridPos)
        {
            CurrentGridPos = gridPos;
            SnapTransformToGrid();
            RegisterGuardPosition();
        }

        public void RefreshAfterMazeReady()
        {
            SnapTransformToGrid();
            RegisterGuardPosition();
        }

        private void SnapTransformToGrid()
        {
            if (mazeBridge != null && gridManager != null && gridManager.UseMazeVisuals)
                transform.position = mazeBridge.GridToWorld(CurrentGridPos);
            else if (gridManager != null)
                transform.position = gridManager.GetWorldPositionForCell(CurrentGridPos);
            else
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

            if (mazeBridge == null)
                mazeBridge = MazeGridBridge.Instance != null ? MazeGridBridge.Instance : FindFirstObjectByType<MazeGridBridge>();
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