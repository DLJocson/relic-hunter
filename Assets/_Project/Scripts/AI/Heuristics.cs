using UnityEngine;

namespace RelicHunter.AI
{
    /// <summary>
    /// Grid distance heuristics for pathfinding.
    /// </summary>
    public static class Heuristics
    {
        /// <summary>
        /// Calculates the Manhattan distance between two grid points.
        /// Perfect for 4-directional cardinal movement systems.
        /// </summary>
        public static int GetManhattanDistance(Vector2Int current, Vector2Int target)
        {
            return Mathf.Abs(current.x - target.x) + Mathf.Abs(current.y - target.y);
        }
    }
}