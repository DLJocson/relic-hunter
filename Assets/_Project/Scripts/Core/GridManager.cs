using UnityEngine;
using System.Collections.Generic;

namespace RelicHunter.Core
{
    /// <summary>
    /// Manages the game grid structure and grid-based operations.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public int width = 9;
        public int height = 9;
        public GameObject tilePrefab;
        public Transform gridVisualParent;
        private bool[,] walls;
        private GameObject[,] tiles;
        public HashSet<Vector2Int> permanentWalls = new HashSet<Vector2Int>();
        public Dictionary<Vector2Int, int> activeBarricades = new Dictionary<Vector2Int, int>();

        private void Awake()
        {
            walls = new bool[width, height];
            tiles = new GameObject[width, height];
            
            Debug.Log("GridManager initialized");
            SpawnGrid();
            AutoCenterCamera();
        }

        private void SpawnGrid()
        {
            float offsetX = (width - 1) / 2f;
            float offsetY = (height - 1) / 2f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GameObject tileObj = Instantiate(tilePrefab, new Vector3(x, y, 0), Quaternion.identity, gridVisualParent);
                    tiles[x, y] = tileObj;

                    // Pass coordinates to the individual tile script if you created it
                    Tile tileScript = tileObj.GetComponent<Tile>();
                    if (tileScript != null)
                    {
                        tileScript.Setup(x, y);
                    }
                }
            }
            Debug.Log("Grid spawned successfully.");
        }

        private void AutoCenterCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogWarning("AutoCenterCamera: No Main Camera found in the scene!");
                return;
            }

            float centerX = (width - 1) / 2f;
            float centerY = (height - 1) / 2f;

            mainCam.transform.position = new Vector3(centerX, centerY, -10f);

            float totalMaxDimension = Mathf.Max(width, height);
            mainCam.orthographicSize = (totalMaxDimension / 2f) + 1f;
        }

        public bool IsInsideGrid(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
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
