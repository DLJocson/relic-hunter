using UnityEngine;

namespace RelicHunter.Core
{
    /// <summary>
    /// Manages the game grid structure and grid-based operations.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public int width = 9;
        public int height = 9;
        private bool[,] walls;

        private void Awake()
        {
            walls = new bool[width, height];
            Debug.Log("GridManager loaded");
        }

        public bool IsInsideGrid(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool IsWalkable(int x, int y)
        {
            if (!IsInsideGrid(x, y)) return false;
            return !walls[x, y];
        }
    }
}
