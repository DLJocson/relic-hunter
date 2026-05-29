// =============================================================================
// GuardController.cs — Controls Guard movement and AI turn execution.
// Temp Patch: Minimax and A* references bypassed until integrated by Leader.
// =============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RelicHunter.Core; 

namespace RelicHunter.Enemy
{
    public class GuardController : MonoBehaviour
    {
        // =========================================================================
        // STARTING POSITION
        // =========================================================================

        [Header("Starting Position")]
        [Tooltip("Set the Guard's initial tile on the grid.")]
        [SerializeField] private Vector2Int startGridPos = new Vector2Int(8, 8);

        // =========================================================================
        // MOVEMENT SETTINGS
        // =========================================================================

        [Header("Movement")]
        [Tooltip("How long it takes the guard to slide from one tile to the next.")]
        [SerializeField] private float moveDuration = 0.15f;

        [Tooltip("Small pause when the guard is trapped, so the player can notice it.")]
        [SerializeField] private float trappedPause = 0.35f;

        public Vector2Int CurrentGridPos { get; private set; }

        private GridManager gridManager;
        private TurnManager turnManager;
        private GameManager gameManager;

        private Coroutine turnRoutine;
        private bool isTakingTurn;

        // =========================================================================
        // UNITY LIFECYCLE
        // =========================================================================

        private void Awake()
        {
            CurrentGridPos = startGridPos;
            ResolveSceneReferences();
        }

        private void Start()
        {
            SnapTransformToGrid();
            RegisterGuardPosition();
        }

        // =========================================================================
        // TURN FLOW
        // =========================================================================

        public void TakeTurn()
        {
            if (isTakingTurn)
                return;

            if (!isActiveAndEnabled)
                return;

            turnRoutine = StartCoroutine(ExecuteGuardTurnRoutine());
        }

        private IEnumerator ExecuteGuardTurnRoutine()
        {
            isTakingTurn = true;

            if (!TryGetTurnData(
                out Vector2Int thiefPos,
                out Vector2Int exitPos,
                out HashSet<Vector2Int> obstacles,
                out Dictionary<Vector2Int, int> barricades,
                out int gridWidth,
                out int gridHeight,
                out int guardSpeed,
                out int minimaxDepth,
                out int barricadeDuration,
                out int maxBarricades))
            {
                Debug.LogWarning("[GuardController] Missing game references. Ending guard turn safely.");
                EndGuardTurnSafely();
                yield break;
            }

            // -----------------------------------------------------------------
            // TEMP PATCH CODE: Bypassing missing Minimax/AStar files
            // -----------------------------------------------------------------
            Vector2Int nextStep = CurrentGridPos;

            if (nextStep.x > thiefPos.x) nextStep.x--;
            else if (nextStep.x < thiefPos.x) nextStep.x++;
            else if (nextStep.y > thiefPos.y) nextStep.y--;
            else if (nextStep.y < thiefPos.y) nextStep.y++;

            // Smoothly slide to the calculated step
            yield return SlideToPosition(nextStep);

            CurrentGridPos = nextStep;
            RegisterGuardPosition();
            // -----------------------------------------------------------------

            Debug.Log($"[GuardController] Guard moved to {CurrentGridPos}. Ending turn.");
            EndGuardTurnSafely();
        }

        private bool TryGetTurnData(
            out Vector2Int thiefPos,
            out Vector2Int exitPos,
            out HashSet<Vector2Int> obstacles,
            out Dictionary<Vector2Int, int> barricades,
            out int gridWidth,
            out int gridHeight,
            out int guardSpeed,
            out int minimaxDepth,
            out int barricadeDuration,
            out int maxBarricades)
        {
            thiefPos = default;
            exitPos = default;
            obstacles = new HashSet<Vector2Int>();
            barricades = new Dictionary<Vector2Int, int>();
            gridWidth = 0;
            gridHeight = 0;
            guardSpeed = 1;
            minimaxDepth = 1;
            barricadeDuration = 4;
            maxBarricades = 3;

            if (gridManager == null)
                ResolveSceneReferences();

            if (gridManager == null)
                return false;

            thiefPos = gridManager.playerPos;
            exitPos = gridManager.exitPos;
            gridWidth = gridManager.Width;
            gridHeight = gridManager.Height;

            if (gridManager.permanentWalls != null)
                obstacles = new HashSet<Vector2Int>(gridManager.permanentWalls);

            if (gridManager.activeBarricades != null)
                barricades = new Dictionary<Vector2Int, int>(gridManager.activeBarricades);

            if (gameManager != null && gameManager.TryGetCurrentRound(out var round))
            {
                guardSpeed = round.guardSpeed;
                minimaxDepth = round.minimaxDepth;
                barricadeDuration = round.barricadeDuration;
                maxBarricades = round.maxBarricades;
            }

            return true;
        }

        // =========================================================================
        // MOVEMENT
        // =========================================================================

        private IEnumerator SlideToPosition(Vector2Int targetPos)
        {
            float duration = Mathf.Max(0.01f, moveDuration);

            Vector3 startPos = transform.position;
            Vector3 endPos = new Vector3(targetPos.x, targetPos.y, startPos.z);
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                transform.position = Vector3.Lerp(startPos, endPos, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos;
        }

        private void SnapTransformToGrid()
        {
            transform.position = new Vector3(CurrentGridPos.x, CurrentGridPos.y, 0f);
        }

        // =========================================================================
        // SYNC HELPERS
        // =========================================================================

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

            if (gameManager == null)
                gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        }

        private void EndGuardTurnSafely()
        {
            isTakingTurn = false;
            turnRoutine = null;

            if (turnManager != null)
                turnManager.EndGuardTurn();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector2Int gizmoPos = Application.isPlaying ? CurrentGridPos : startGridPos;

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawCube(new Vector3(gizmoPos.x, gizmoPos.y, 0f), Vector3.one);
        }
#endif
    }
}