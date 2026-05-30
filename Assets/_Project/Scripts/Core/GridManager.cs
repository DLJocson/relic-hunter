// =============================================================================
// GridManager.cs — Handles playing ground
// =============================================================================

using System;
using System.Collections.Generic;
using RelicHunter.AI;
using UnityEngine;

namespace RelicHunter.Core
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid Dimensions")]
        [SerializeField] private int width = 9;
        [SerializeField] private int height = 9;

        public int Width => width;
        public int Height => height;

        [Header("Visual Prefabs")]
        public GameObject tilePrefab;
        public GameObject barricadePrefab;
        [SerializeField] private GameObject mazeBarricadePrefab;
        public GameObject exitPrefab;
        public Transform gridVisualParent;

        [Header("Maze Integration")]
        [SerializeField] private bool useMazeVisuals = true;
        [SerializeField] private MazeGridBridge mazeBridge;

        public HashSet<Vector2Int> permanentWalls = new HashSet<Vector2Int>();
        public Dictionary<Vector2Int, int> activeBarricades = new Dictionary<Vector2Int, int>();
        public Dictionary<Vector2Int, GameObject> visualBarricades = new Dictionary<Vector2Int, GameObject>();

        [Header("Rules & Limits")]
        public int maxBarricadesAllowed = 3;
        public int barricadeDuration = 4;

        [Header("Runtime Tracker Positions")]
        public Vector2Int playerPos = new Vector2Int(0, 0);
        public Vector2Int guardPos = new Vector2Int(8, 8);
        public Vector2Int exitPos = new Vector2Int(8, 0);

        public bool UseMazeVisuals => useMazeVisuals;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            ResolveMazeBridge();

            if (!useMazeVisuals)
            {
                SpawnGrid();
                SpawnExitVisual();
                AutoCenterCamera();
            }

            Debug.Log("GridManager initialized");
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

            if (!useMazeVisuals)
            {
                SpawnGrid();
                SpawnExitVisual();
                AutoCenterCamera();
            }
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

            permanentWalls.Clear();
        }

        private void SpawnGrid()
        {
            if (tilePrefab == null) return;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity, gridVisualParent);
                }
            }

            Debug.Log($"Grid spawned successfully at size: {width}x{height}");
        }

        private void AutoCenterCamera()
        {
            if (useMazeVisuals) return;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            float centerX = (width - 1) / 2f;
            float centerY = (height - 1) / 2f;

            mainCam.transform.position = new Vector3(centerX, centerY, -10f);
            float totalMaxDimension = Mathf.Max(width, height);
            mainCam.orthographicSize = (totalMaxDimension / 2f) + 1f;
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

        public void GenerateProceduralWalls(int seed, float wallDensity, Vector2Int startTile, Vector2Int guardTile, Vector2Int exitTile)
        {
            permanentWalls.Clear();
        }

        public bool CanEnterCell(Vector2Int from, Vector2Int to, bool isGuard = false)
        {
            if (to.x < 0 || to.x >= width || to.y < 0 || to.y >= height) return false;
            if (activeBarricades.ContainsKey(to)) return false;
            if (isGuard && to == exitPos) return false;

            ResolveMazeBridge();

            if (useMazeVisuals)
            {
                if (mazeBridge == null)
                {
                    Debug.LogError("[GridManager] useMazeVisuals is enabled but MazeGridBridge is missing.");
                    return false;
                }
                return mazeBridge.CanMoveBetween(from, to);
            }

            if (permanentWalls.Contains(to)) return false;
            return true;
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

        public void CollectGuardNeighborCells(Vector2Int from, List<Vector2Int> neighbors)
        {
            neighbors.Clear();

            foreach (Vector2Int dir in CardinalDirections)
            {
                Vector2Int adjacent = from + dir;

                if (adjacent == exitPos)
                {
                    Vector2Int jumpTo = from + dir * 2;
                    if (CanGuardLeapTo(from, jumpTo))
                        neighbors.Add(jumpTo);
                    continue;
                }

                if (CanEnterCell(from, adjacent, isGuard: true))
                    neighbors.Add(adjacent);
            }
        }

        private static readonly Vector2Int[] CardinalDirections = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

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

            return true;
        }

        /// <summary>
        /// Returns true if the player could still reach the exit with an extra barricade at <paramref name="position"/>.
        /// </summary>
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
            if (useMazeVisuals)
            {
                if (mazeBarricadePrefab != null) return mazeBarricadePrefab;

                GameObject loaded = Resources.Load<GameObject>("Prefabs/Barricade");
                if (loaded != null) return loaded;

                if (barricadePrefab != null) return barricadePrefab;

                Debug.LogWarning("[GridManager] No MazeGen barricade prefab assigned.");
                return null;
            }

            return barricadePrefab;
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

            if (useMazeVisuals)
            {
                if (mazeBridge != null)
                    return mazeBridge.GridToWorld(cell);

                Debug.LogError("[GridManager] useMazeVisuals is enabled but MazeGridBridge is missing.");
                return new Vector3(cell.x, cell.y, 0f);
            }

            return new Vector3(cell.x, cell.y, 0f);
        }

        private void SpawnExitVisual()
        {
            if (useMazeVisuals) return;
            if (exitPrefab == null) return;

            exitPos = new Vector2Int(width - 1, 0);
            Vector3 spawnPos = GetWorldPositionForCell(exitPos);

            GameObject oldExit = GameObject.FindWithTag("Exit");
            if (oldExit != null) Destroy(oldExit);

            GameObject exitObj = Instantiate(exitPrefab, spawnPos, Quaternion.identity, gridVisualParent);
            exitObj.tag = "Exit";
        }

        public bool IsTileWalkable(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return false;

            Vector2Int target = new Vector2Int(x, y);
            if (activeBarricades.ContainsKey(target)) return false;

            if (useMazeVisuals)
                return true;

            if (permanentWalls.Contains(target)) return false;
            return true;
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
