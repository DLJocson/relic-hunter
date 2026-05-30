// =============================================================================
// MazeGridBridge.cs — Syncs MazeGen visuals with Relic Hunter grid logic.
// =============================================================================

using System;
using UnityEngine;
using RelicHunter.Player;
using RelicHunter.Enemy;

namespace RelicHunter.Core
{
    public class MazeGridBridge : MonoBehaviour
    {
        public static MazeGridBridge Instance { get; private set; }

        [Header("Maze")]
        [SerializeField] private GenerateMaze generateMaze;

        [Header("Scene References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private GuardController guardController;

        [Header("Entity Visual Prefabs")]
        [SerializeField] private GameObject playerVisualPrefab;
        [SerializeField] private GameObject guardVisualPrefab;
        [SerializeField] private GameObject exitVisualPrefab;

        private GameObject exitVisualInstance;
        private GameObject playerVisualInstance;
        private GameObject guardVisualInstance;
        private PlayerStatus playerStatus;

        public Vector2Int ExitCell => generateMaze != null ? generateMaze.ExitCell : Vector2Int.zero;
        public PlayerStatus PlayerStatus => playerStatus;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);

            ResolveReferences();
        }

        public void BeginRound(GameManager.RoundDefinition round, int seed, Action onComplete)
        {
            ResolveReferences();

            if (generateMaze == null)
            {
                Debug.LogError("[MazeGridBridge] GenerateMaze reference is missing.");
                onComplete?.Invoke();
                return;
            }

            SetRoundEntitiesVisible(false);
            FrameCameraForRound(round.gridWidth, round.gridHeight);

            void OnMazeReady()
            {
                generateMaze.OnGenerationComplete -= OnMazeReady;
                ApplyMazeToGrid(round);
                SetRoundEntitiesVisible(true);
                onComplete?.Invoke();
            }

            generateMaze.OnGenerationComplete += OnMazeReady;
            generateMaze.GenerateForRound(round.gridWidth, round.gridHeight, seed);
        }

        private void ApplyMazeToGrid(GameManager.RoundDefinition round)
        {
            if (gridManager == null) return;

            gridManager.UpdateGridDimensions(round.gridWidth, round.gridHeight);
            gridManager.exitPos = generateMaze.ExitCell;
            gridManager.ClearAllBarricades();

            CleanupStrayExitMarkers();
            SpawnExitVisual();
            EnsureEntityVisuals();
            RefreshEntityTransforms();
            FrameCamera();

            NotifyPlayerStatusReady();
        }

        public bool CanMoveBetween(Vector2Int from, Vector2Int to)
        {
            if (generateMaze == null || !generateMaze.IsReady) return false;
            return generateMaze.CanPass(from, to);
        }

        public Vector3 GridToWorld(Vector2Int cell)
        {
            if (generateMaze == null || !generateMaze.IsReady)
                return new Vector3(cell.x, cell.y, 0f);
            return generateMaze.GridToWorld(cell);
        }

        public void SpawnExitVisual()
        {
            if (gridManager == null || generateMaze == null || !generateMaze.IsReady) return;

            generateMaze.ClearExitRoomFloorTint();

            GameObject prefab = ResolveExitPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[MazeGridBridge] No exit prefab assigned on GridManager or MazeGridBridge.");
                return;
            }

            if (exitVisualInstance != null) Destroy(exitVisualInstance);

            Vector3 pos = GridToWorld(gridManager.exitPos);
            Transform parent = gridManager.gridVisualParent != null
                ? gridManager.gridVisualParent
                : transform;

            exitVisualInstance = Instantiate(prefab, pos, Quaternion.identity, parent);
            exitVisualInstance.SetActive(true);
            exitVisualInstance.tag = "Exit";
            exitVisualInstance.transform.position = pos;
            SetVisualSorting(exitVisualInstance, 10);
        }

        private GameObject ResolveExitPrefab()
        {
            if (exitVisualPrefab != null)
                return exitVisualPrefab;

            if (gridManager != null && gridManager.exitPrefab != null)
                return gridManager.exitPrefab;

            return Resources.Load<GameObject>("Prefabs/Exit Tile");
        }

        public void EnsureEntityVisuals()
        {
            ResolveReferences();

            if (playerController != null && playerVisualPrefab != null)
            {
                DestroyVisualInstance(ref playerVisualInstance);
                DisablePlaceholderRenderer(playerController.gameObject);

                playerVisualInstance = Instantiate(playerVisualPrefab, playerController.transform);
                playerVisualInstance.name = "PlayerVisual";
                playerVisualInstance.transform.localPosition = Vector3.zero;
                playerVisualInstance.transform.localRotation = Quaternion.identity;
                SetVisualSorting(playerVisualInstance, 5);

                playerStatus = playerVisualInstance.GetComponentInChildren<PlayerStatus>(true);
                if (playerStatus == null)
                    playerStatus = playerVisualInstance.GetComponent<PlayerStatus>();
            }

            if (guardController != null && guardVisualPrefab != null)
            {
                DestroyVisualInstance(ref guardVisualInstance);
                DisablePlaceholderRenderer(guardController.gameObject);

                guardVisualInstance = Instantiate(guardVisualPrefab, guardController.transform);
                guardVisualInstance.name = "GuardVisual";
                guardVisualInstance.transform.localPosition = Vector3.zero;
                guardVisualInstance.transform.localRotation = Quaternion.identity;
                SetVisualSorting(guardVisualInstance, 5);
            }
        }

        private static void SetVisualSorting(GameObject visualRoot, int order)
        {
            if (visualRoot == null) return;

            foreach (SpriteRenderer renderer in visualRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingOrder = order;
            }
        }

        public void RefreshEntityTransforms()
        {
            ResolveReferences();
            if (gridManager == null) return;

            if (playerController != null)
            {
                playerController.RefreshAfterMazeReady();
            }

            Vector2Int guardStart = new Vector2Int(gridManager.Width - 1, gridManager.Height - 1);
            if (guardController != null)
            {
                guardController.ResetToPosition(guardStart);
                guardController.RefreshAfterMazeReady();
            }

            gridManager.guardPos = guardStart;

            if (exitVisualInstance != null)
                exitVisualInstance.transform.position = GridToWorld(gridManager.exitPos);
        }

        public void CleanupStrayExitMarkers()
        {
            GameObject[] exits = GameObject.FindGameObjectsWithTag("Exit");
            foreach (GameObject exitObj in exits)
            {
                if (exitObj == null) continue;
                if (exitVisualInstance != null && exitObj == exitVisualInstance) continue;
                Destroy(exitObj);
            }
        }

        public void FrameCamera()
        {
            generateMaze?.FrameCamera();
        }

        public void FrameCameraForRound(int gridWidth, int gridHeight)
        {
            if (generateMaze == null) return;

            generateMaze.PrepareCameraForDimensions(gridWidth, gridHeight);
        }

        private void SetRoundEntitiesVisible(bool visible)
        {
            if (!visible)
            {
                if (playerVisualInstance != null)
                    playerVisualInstance.SetActive(false);
                if (guardVisualInstance != null)
                    guardVisualInstance.SetActive(false);

                if (playerController != null)
                    SetPlaceholderRenderersVisible(playerController.gameObject, false);
                if (guardController != null)
                    SetPlaceholderRenderersVisible(guardController.gameObject, false);
                return;
            }

            if (playerVisualInstance != null)
                playerVisualInstance.SetActive(true);
            else if (playerController != null)
                SetPlaceholderRenderersVisible(playerController.gameObject, true);

            if (guardVisualInstance != null)
                guardVisualInstance.SetActive(true);
            else if (guardController != null)
                SetPlaceholderRenderersVisible(guardController.gameObject, true);
        }

        private static void SetPlaceholderRenderersVisible(GameObject root, bool visible)
        {
            if (root == null) return;

            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.enabled = visible;
        }

        private void NotifyPlayerStatusReady()
        {
            if (playerStatus == null) return;

            RoundFeedbackController feedback = FindFirstObjectByType<RoundFeedbackController>();
            feedback?.SetPlayerStatus(playerStatus);
        }

        private static void DestroyVisualInstance(ref GameObject instance)
        {
            if (instance == null) return;
            Destroy(instance);
            instance = null;
        }

        private static void DisablePlaceholderRenderer(GameObject root)
        {
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.enabled = false;
        }

        private void ResolveReferences()
        {
            if (generateMaze == null) generateMaze = GetComponent<GenerateMaze>();
            if (generateMaze == null) generateMaze = FindFirstObjectByType<GenerateMaze>();
            if (gridManager == null) gridManager = GridManager.Instance ?? FindFirstObjectByType<GridManager>();
            if (gameManager == null) gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
            if (guardController == null) guardController = FindFirstObjectByType<GuardController>();
        }
    }
}
