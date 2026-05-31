using System.Collections.Generic;
using UnityEngine;
using RelicHunter.Core;
using RelicHunter.AI;

namespace RelicHunter.Enemy
{
    /// <summary>
    /// Controls execution steps, processing loops, and spatial tracking for Guard agents.
    /// Integrates Minimax decisions with an optimization-validated A* path pipeline.
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

            Vector2Int strategicTarget = DetermineBestGuardMove(thiefPos);
            Vector2Int nextStep = CurrentGridPos;

            // Primary execution path: use A* to follow the Minimax-selected target.
            if (strategicTarget != Minimax.TRAPPED && strategicTarget != CurrentGridPos)
            {
                List<Vector2Int> plannedPath = AStar.FindPath(
                    gridManager,
                    CurrentGridPos,
                    strategicTarget,
                    isGuard: true,
                    extraBlocked: null
                );

                if (plannedPath != null && plannedPath.Count > 1)
                {
                    nextStep = plannedPath[1];
                }
            }

            // Fallback: if the strategic target is not reachable, pursue the thief directly.
            if (nextStep == CurrentGridPos)
            {
                List<Vector2Int> fallbackPath = AStar.FindPath(
                    gridManager,
                    CurrentGridPos,
                    thiefPos,
                    isGuard: true,
                    extraBlocked: null
                );

                if (fallbackPath != null && fallbackPath.Count > 1)
                {
                    nextStep = fallbackPath[1];
                }
            }

            if (!CanStep(CurrentGridPos, nextStep))
            {
                Debug.LogWarning(
                    $"[GuardController] Aborted illegal physical structural move to grid coordinates: {nextStep}. Locking transform state.");
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
                canGuardMoveBetween: CanTraverseForGuardAi,
                canThiefMoveBetween: CanTraverseForThiefAi,
                canGuardLeapTo: CanGuardLeapForAi
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

        private bool CanTraverseForGuardAi(Vector2Int from, Vector2Int to)
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

        private bool CanTraverseForThiefAi(Vector2Int from, Vector2Int to)
        {
            if (gridManager == null)
            {
                return false;
            }

            int manhattan = Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);

            if (manhattan != 1)
            {
                return false;
            }

            return gridManager.CanEnterCell(from, to, isGuard: false);
        }

        private bool CanGuardLeapForAi(Vector2Int from, Vector2Int to)
        {
            if (gridManager == null)
            {
                return false;
            }

            return gridManager.CanGuardLeapTo(from, to);
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
                gridManager = GridManager.Instance != null
                    ? GridManager.Instance
                    : FindFirstObjectByType<GridManager>();
            }

            if (turnManager == null)
            {
                turnManager = TurnManager.Instance != null
                    ? TurnManager.Instance
                    : FindFirstObjectByType<TurnManager>();
            }

            if (mazeBridge == null)
            {
                mazeBridge = MazeGridBridge.Instance != null
                    ? MazeGridBridge.Instance
                    : FindFirstObjectByType<MazeGridBridge>();
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