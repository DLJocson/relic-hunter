using System.Collections.Generic;
using UnityEngine;
using RelicHunter.Core;
using RelicHunter.AI;

namespace RelicHunter.Enemy
{
    /// <summary>
    /// Controls execution steps, processing loops, and spatial tracking for Guard agents.
    /// Integrates Minimax decisions with an optimization-validated A* backup path pipeline.
    /// </summary>
    public class GuardController : MonoBehaviour
    {
        [Header("Starting Position")]
        [SerializeField] private Vector2Int startGridPos = new Vector2Int(8, 8);

        [Header("Movement Configuration")]
        private float guardSpeed = 1f;
        private float movementAccumulator = 0f;

        public Vector2Int CurrentGridPos { get; private set; }

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

            if (turnManager != null && turnManager.currentTurn != TurnManager.TurnState.Processing)
            {
                EndGuardTurnSafely();
            }
        }

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

            // Attempt Minimax search vector calculation
            Vector2Int nextStep = DetermineBestGuardMove(thiefPos);

            // Hybrid Search Pipeline Intercept:
            // Fall back immediately to global multi-step A* if Minimax fails, stalls, or traps out.
            if (nextStep == CurrentGridPos || nextStep == Minimax.TRAPPED)
            {
                List<Vector2Int> clearPath = AStar.FindPath(
                    gridManager,
                    CurrentGridPos,
                    thiefPos,
                    isGuard: true,
                    extraBlocked: null
                );

                // If path exists, select the step immediately following our current node
                if (clearPath != null && clearPath.Count > 1)
                {
                    nextStep = clearPath[1];
                }
                else
                {
                    nextStep = CurrentGridPos; // Structural fallback if completely blocked
                }
            }

            // Engine constraint safety validation prior to simulation integration
            if (!CanStep(CurrentGridPos, nextStep))
            {
                Debug.LogWarning($"[GuardController] Aborted illegal physical structural move to grid coordinates: {nextStep}. Locking transform state.");
                nextStep = CurrentGridPos;
            }

            ApplyGuardPosition(nextStep);

            if (turnManager != null)
            {
                bool gameEnded = turnManager.CheckWinLossConditions();
                if (gameEnded) 
                {
                    return false;
                }
            }

            return true;
        }

        private Vector2Int DetermineBestGuardMove(Vector2Int thiefPos)
        {
            if (gridManager == null)
            {
                return CurrentGridPos;
            }

            int maxDepth = 1;
            if (GameManager.Instance != null)
            {
                maxDepth = Mathf.Max(0, GameManager.Instance.CurrentMinimaxDepth);
            }

            Vector2Int bestStep = Minimax.GetBestGuardMove(
                CurrentGridPos,
                thiefPos,
                gridManager.exitPos,
                new HashSet<Vector2Int>(),
                new Dictionary<Vector2Int, int>(gridManager.activeBarricades),
                gridManager.Width,
                gridManager.Height,
                maxDepth,
                gridManager.barricadeDuration,
                gridManager.maxBarricadesAllowed,
                (from, to) => CanTraverseForAi(from, to)
            );

            return bestStep;
        }

        private bool CanStep(Vector2Int from, Vector2Int to)
        {
            if (gridManager == null) 
            {
                return false;
            }

            int manhattan = Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
            if (manhattan == 2)
            {
                return gridManager.CanGuardLeapTo(from, to);
            }
            if (manhattan == 1)
            {
                return gridManager.CanEnterCell(from, to, isGuard: true);
            }

            return false;
        }

        private bool CanTraverseForAi(Vector2Int from, Vector2Int to)
        {
            if (gridManager == null) 
            {
                return false;
            }

            int manhattan = Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
            if (manhattan == 2)
            {
                return gridManager.CanGuardLeapTo(from, to);
            }
            if (manhattan == 1)
            {
                return gridManager.CanEnterCell(from, to, isGuard: false);
            }

            return false;
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
            if (mazeBridge != null)
            {
                transform.position = mazeBridge.GridToWorld(CurrentGridPos);
            }
            else if (gridManager != null)
            {
                transform.position = gridManager.GetWorldPositionForCell(CurrentGridPos);
            }
            else
            {
                transform.position = new Vector3(CurrentGridPos.x, CurrentGridPos.y, 0f);
            }
        }

        private void RegisterGuardPosition()
        {
            if (gridManager != null)
            {
                gridManager.SetGuardPosition(CurrentGridPos);
            }
        }

        private void ResolveSceneReferences()
        {
            if (gridManager == null)
            {
                gridManager = GridManager.Instance != null ? GridManager.Instance : FindFirstObjectByType<GridManager>();
            }

            if (turnManager == null)
            {
                turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindFirstObjectByType<TurnManager>();
            }

            if (mazeBridge == null)
            {
                mazeBridge = MazeGridBridge.Instance != null ? MazeGridBridge.Instance : FindFirstObjectByType<MazeGridBridge>();
            }
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