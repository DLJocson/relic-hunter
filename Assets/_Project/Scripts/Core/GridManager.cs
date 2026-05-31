using System.Collections.Generic;
using RelicHunter.Enemy;
using RelicHunter.Player;
using UnityEngine;

namespace RelicHunter.Core
{
    /// <summary>
    /// Grid logic, barricades, and round entity spawning; maze visuals come from <see cref="MazeGridBridge"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid Dimensions")]
        [SerializeField] private int width = 9;
        [SerializeField] private int height = 9;

        public int Width => width;
        public int Height => height;

        [Header("Visual Prefabs")]
        [SerializeField] private GameObject mazeBarricadePrefab;
        public GameObject exitPrefab;
        public Transform gridVisualParent;

        [Header("Maze Integration")]
        [SerializeField] private MazeGridBridge mazeBridge;

        [Header("Entity Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject guardPrefab;

        [Header("Round Entities")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private GuardController guardController;

        private GameObject currentPlayer;
        private GameObject currentGuard;

        public Dictionary<Vector2Int, int> activeBarricades = new Dictionary<Vector2Int, int>();
        public Dictionary<Vector2Int, GameObject> visualBarricades = new Dictionary<Vector2Int, GameObject>();

        [Header("Rules & Limits")]
        public int maxBarricadesAllowed = 3;
        public int barricadeDuration = 4;

        [Header("Runtime Tracker Positions")]
        public Vector2Int playerPos = new Vector2Int(0, 0);
        public Vector2Int guardPos = new Vector2Int(8, 8);
        public Vector2Int exitPos = new Vector2Int(8, 0);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            ResolveMazeBridge();
            ResolveRoundEntities();
            DeactivateRoundEntities();
        }

        public void ResolveRoundEntities()
        {
            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

            if (guardController == null)
                guardController = FindFirstObjectByType<GuardController>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// Hides Player and Guard until the maze for the current round is ready.
        /// </summary>
        public void DeactivateRoundEntities()
        {
            ResolveRoundEntities();
            SetRoundEntityVisibility(false);
        }

        /// <summary>
        /// Shows Player and Guard after maze generation and spawn placement are complete.
        /// </summary>
        public void ActivateRoundEntities()
        {
            ResolveRoundEntities();
            SetRoundEntityVisibility(true);
        }

        /// <summary>
        /// Called before each maze build so entities stay hidden during generation.
        /// </summary>
        public void PrepareForRoundGeneration()
        {
            DeactivateRoundEntities();
            DestroyRoundEntities();
        }

        /// <summary>
        /// Clears round entities before a full match restart to avoid duplicate visuals.
        /// </summary>
        public void ResetForMatchRestart()
        {
            DeactivateRoundEntities();
            DestroyRoundEntities();
            ClearAllBarricades();
            playerPos = Vector2Int.zero;
            guardPos = Vector2Int.zero;
        }

        public void ConfigureEntityPrefabs(GameObject player, GameObject guard)
        {
            if (player != null)
                playerPrefab = player;

            if (guard != null)
                guardPrefab = guard;
        }

        public void DestroyRoundEntities()
        {
            DestroyEntityVisualChildren();

            if (currentPlayer != null)
            {
                Destroy(currentPlayer);
                currentPlayer = null;
            }

            if (currentGuard != null)
            {
                Destroy(currentGuard);
                currentGuard = null;
            }

            playerController = null;
            guardController = null;
        }

        /// <summary>
        /// Instantiates player and guard prefabs at round spawn positions.
        /// </summary>
        public void SpawnRoundEntities()
        {
            DestroyRoundEntities();

            GameObject resolvedPlayerPrefab = ResolvePlayerPrefab();
            GameObject resolvedGuardPrefab = ResolveGuardPrefab();

            if (resolvedPlayerPrefab == null || resolvedGuardPrefab == null)
            {
                Debug.LogError("[GridManager] Player or Guard prefab is missing. Assign prefabs on GridManager or GameManager.");
                return;
            }

            currentPlayer = Instantiate(resolvedPlayerPrefab, transform);
            currentPlayer.name = "Player";

            currentGuard = Instantiate(resolvedGuardPrefab, transform);
            currentGuard.name = "Guard";

            playerController = currentPlayer.GetComponent<PlayerController>();
            guardController = currentGuard.GetComponent<GuardController>();

            if (playerController == null || guardController == null)
            {
                Debug.LogError("[GridManager] Spawned prefabs are missing PlayerController or GuardController.");
                DestroyRoundEntities();
                return;
            }

            Vector2Int playerStart = playerPos;
            if (playerStart.x < 0 || playerStart.x >= width || playerStart.y < 0 || playerStart.y >= height)
                playerStart = Vector2Int.zero;

            Vector2Int guardStart = guardPos;
            if (guardStart.x < 0 || guardStart.x >= width || guardStart.y < 0 || guardStart.y >= height)
                guardStart = new Vector2Int(width - 1, height - 1);

            playerController.gridPosition = playerStart;
            currentPlayer.transform.position = GetWorldPositionForCell(playerStart);
            playerPos = playerStart;

            guardController.ResetToPosition(guardStart);
            currentGuard.transform.position = GetWorldPositionForCell(guardStart);
            guardPos = guardStart;

            DeactivateRoundEntities();

            Debug.Log($"[GridManager] Spawned Player at {playerStart} and Guard at {guardStart}.");
        }

        private GameObject ResolvePlayerPrefab()
        {
            if (playerPrefab == null)
                Debug.LogWarning("[GridManager] Player prefab is not assigned on GridManager.");
            return playerPrefab;
        }

        private GameObject ResolveGuardPrefab()
        {
            if (guardPrefab == null)
                Debug.LogWarning("[GridManager] Guard prefab is not assigned on GridManager.");
            return guardPrefab;
        }

        private void SetRoundEntityVisibility(bool visible)
        {
            SetEntityRootVisibility(playerController, visible);
            SetEntityRootVisibility(guardController, visible);
        }

        private static void SetEntityRootVisibility(MonoBehaviour entity, bool visible)
        {
            if (entity == null)
                return;

            // Root placeholder sprites stay off; character art lives on PlayerVisual / GuardVisual children.
            SpriteRenderer placeholder = entity.GetComponent<SpriteRenderer>();
            if (placeholder != null)
                placeholder.enabled = false;

            foreach (Transform child in entity.transform)
            {
                if (child != null)
                    child.gameObject.SetActive(visible);
            }
        }

        private void DestroyEntityVisualChildren()
        {
            DestroyNamedChild(playerController, "PlayerVisual");
            DestroyNamedChild(guardController, "GuardVisual");
        }

        private static void DestroyNamedChild(MonoBehaviour entity, string childName)
        {
            if (entity == null)
                return;

            Transform child = entity.transform.Find(childName);
            if (child != null)
                Destroy(child.gameObject);
        }

        private void ResolveMazeBridge()
        {
            if (mazeBridge == null)
                mazeBridge = MazeGridBridge.Instance ?? FindFirstObjectByType<MazeGridBridge>();
        }

        public void UpdateGridDimensions(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;
            ClearPhysicalGridVisuals();
        }

        private void ClearPhysicalGridVisuals()
        {
            ClearAllBarricades();

            if (gridVisualParent != null)
            {
                foreach (Transform child in gridVisualParent)
                {
                    if (child != null)
                        Destroy(child.gameObject);
                }
            }
        }

        public void SetGuardPosition(Vector2Int newPos)
        {
            guardPos = newPos;
        }

        public void ApplyRoundSettings(int maxB, int duration)
        {
            maxBarricadesAllowed = maxB;
            barricadeDuration = duration;
        }

        public bool CanEnterCell(Vector2Int from, Vector2Int to, bool isGuard = false)
        {
            if (to.x < 0 || to.x >= width || to.y < 0 || to.y >= height) return false;
            if (activeBarricades.ContainsKey(to)) return false;
            if (isGuard && to == exitPos) return false;

            ResolveMazeBridge();

            if (mazeBridge == null)
            {
                Debug.LogError("[GridManager] MazeGridBridge is missing.");
                return false;
            }

            return mazeBridge.CanMoveBetween(from, to);
        }

        /// <summary>
        /// Guard may leap 2 cells in a straight line over the exit when the corridor continues on the far side.
        /// </summary>
        public bool CanGuardLeapTo(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            int manhattan = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
            if (manhattan != 2) return false;
            if (delta.x != 0 && delta.y != 0) return false;

            Vector2Int dir = new Vector2Int(
                delta.x != 0 ? (delta.x > 0 ? 1 : -1) : 0,
                delta.y != 0 ? (delta.y > 0 ? 1 : -1) : 0);

            Vector2Int overExit = from + dir;
            if (overExit != exitPos) return false;

            if (to.x < 0 || to.x >= width || to.y < 0 || to.y >= height) return false;
            if (to == exitPos) return false;
            if (activeBarricades.ContainsKey(overExit) || activeBarricades.ContainsKey(to)) return false;

            if (!CanEnterCell(from, overExit, isGuard: false)) return false;
            if (!CanEnterCell(overExit, to, isGuard: false)) return false;
            return CanEnterCell(overExit, to, isGuard: true);
        }

        public bool TryPlaceBarricade(Vector2Int position, out bool wouldTrapPlayer)
        {
            wouldTrapPlayer = false;

            if (activeBarricades.Count >= maxBarricadesAllowed)
            {
                Debug.Log("Barricade placement denied: Max cap reached!");
                return false;
            }

            if (!IsTileWalkable(position.x, position.y))
            {
                Debug.Log("Barricade placement denied: Tile is blocked or out of bounds!");
                return false;
            }

            if (position == playerPos || position == guardPos || position == exitPos)
            {
                Debug.Log("Barricade placement denied: Tile is occupied!");
                return false;
            }

            if (activeBarricades.ContainsKey(position))
                return false;

            if (!WouldHavePathToExitAfterBarricade(position))
            {
                wouldTrapPlayer = true;
                Debug.Log("Barricade placement denied: Would block all paths to the exit!");
                return false;
            }

            activeBarricades.Add(position, barricadeDuration);
            Debug.Log($"Engine: Barricade placed at {position} for {barricadeDuration} turns.");

            GameObject prefab = ResolveBarricadePrefab();
            if (prefab != null)
            {
                Vector3 spawnPos = GetWorldPositionForCell(position);
                Transform parent = gridVisualParent != null ? gridVisualParent : transform;
                GameObject visualObj = Instantiate(prefab, spawnPos, Quaternion.identity, parent);
                visualObj.SetActive(true);
                visualObj.transform.position = spawnPos;
                ConfigureBarricadeVisual(visualObj);
                visualBarricades[position] = visualObj;
            }

            if (RelicHunter.UI.UIManager.Instance != null)
                RelicHunter.UI.UIManager.Instance.UpdateBarricades(activeBarricades.Count, maxBarricadesAllowed);

            return true;
        }

        public bool WouldHavePathToExitAfterBarricade(Vector2Int position)
        {
            var hypotheticalBlocked = new HashSet<Vector2Int> { position };
            return HasPathFromPlayerToExit(hypotheticalBlocked);
        }

        private bool HasPathFromPlayerToExit(HashSet<Vector2Int> extraBlocked)
        {
            return AStar.PathExists(this, playerPos, exitPos, isGuard: false, extraBlocked);
        }

        private GameObject ResolveBarricadePrefab()
        {
            if (mazeBarricadePrefab == null)
                Debug.LogWarning("[GridManager] Maze barricade prefab is not assigned on GridManager.");
            return mazeBarricadePrefab;
        }

        private static void ConfigureBarricadeVisual(GameObject visualObj)
        {
            if (visualObj == null) return;

            foreach (SpriteRenderer renderer in visualObj.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingOrder = 6;
            }
        }

        public Vector3 GetWorldPositionForCell(Vector2Int cell)
        {
            ResolveMazeBridge();

            if (mazeBridge != null)
                return mazeBridge.GridToWorld(cell);

            Debug.LogError("[GridManager] MazeGridBridge is missing.");
            return new Vector3(cell.x, cell.y, 0f);
        }

        public bool IsTileWalkable(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return false;

            Vector2Int target = new Vector2Int(x, y);
            return !activeBarricades.ContainsKey(target);
        }

        public void ClearAllBarricades()
        {
            foreach (var kvp in visualBarricades)
            {
                if (kvp.Value != null) Destroy(kvp.Value);
            }

            activeBarricades.Clear();
            visualBarricades.Clear();
        }
    }
}
