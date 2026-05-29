using UnityEngine;
using System.Collections.Generic;

namespace RelicHunter.Core
{
    /// <summary>
    /// Manages the game grid structure and grid-based operations.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid Dimensions")]
        [SerializeField] private int width = 9;
        [SerializeField] private int height = 9;

        // Properties that GuardController uses to check grid size
        public int Width => width;
        public int Height => height;

        public GameObject tilePrefab;
        public Transform gridVisualParent;
        private bool[,] walls;
        private GameObject[,] tiles;
        
        public HashSet<Vector2Int> permanentWalls = new HashSet<Vector2Int>();
        public Dictionary<Vector2Int, int> activeBarricades = new Dictionary<Vector2Int, int>();
        
        [Header("Rules & Limits")]
        public int maxBarricadesAllowed = 3; 
        public int barricadeDuration = 4;    
        public GameObject barricadePrefab;
        public Dictionary<Vector2Int, GameObject> visualBarricades = new Dictionary<Vector2Int, GameObject>();
        
        [Header("Runtime Tracker Positions")]
        public Vector2Int playerPos = new Vector2Int(0, 0);
        public Vector2Int guardPos = new Vector2Int(8, 8);
        public Vector2Int exitPos = new Vector2Int(8, 0);
        
        public GameObject exitPrefab;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            walls = new bool[width, height];
            tiles = new GameObject[width, height];
            
            Debug.Log("GridManager initialized");
            SpawnGrid();
            SpawnExitVisual();
            AutoCenterCamera();
        }

        private void SpawnGrid()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GameObject tileObj = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity, gridVisualParent);
                    tiles[x, y] = tileObj;
                }
            }
            Debug.Log("Grid spawned successfully.");
        }

        private void AutoCenterCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            float centerX = (width - 1) / 2f;
            float centerY = (height - 1) / 2f;

            mainCam.transform.position = new Vector3(centerX, centerY, -10f);
            float totalMaxDimension = Mathf.Max(width, height);
            mainCam.orthographicSize = (totalMaxDimension / 2f) + 1f;
        }

        /// <summary>
        /// Public method used by GuardController to register its current position.
        /// </summary>
        public void SetGuardPosition(Vector2Int newPos)
        {
            guardPos = newPos;
        }

        public bool TryPlaceBarricade(Vector2Int position)
        {
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

            activeBarricades.Add(position, barricadeDuration);
            Debug.Log($"Engine: Barricade placed at {position} for {barricadeDuration} turns.");

            if (barricadePrefab != null)
            {
                Vector3 spawnPos = new Vector3(position.x, position.y, 0);
                GameObject visualObj = Instantiate(barricadePrefab, spawnPos, Quaternion.identity);
                visualBarricades.Add(position, visualObj);
            }

            return true;
        }

        private void SpawnExitVisual()
        {
            if (exitPrefab != null)
            {
                exitPos = new Vector2Int(width - 1, 0);
                Vector3 spawnPos = new Vector3(exitPos.x, exitPos.y, 0);
                Instantiate(exitPrefab, spawnPos, Quaternion.identity, gridVisualParent);
            }
        }

        public bool IsTileWalkable(int x, int y)
        {
            Vector2Int target = new Vector2Int(x, y);
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            if (permanentWalls.Contains(target)) return false;
            if (activeBarricades.ContainsKey(target)) return false;

            return true;
        }
    }
}