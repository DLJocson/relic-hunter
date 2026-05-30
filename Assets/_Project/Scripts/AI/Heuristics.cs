// =============================================================================
// Heuristics.cs — Mathematical distance estimation functions
// =============================================================================

using System;
using UnityEngine;

namespace RelicHunter.AI
{
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

        /// <summary>
        /// Calculates the Chebyshev distance between two grid points.
        /// Use this if you ever enable 8-directional king-movement vectors.
        /// </summary>
        public static int GetChebyshevDistance(Vector2Int current, Vector2Int target)
        {
            return Mathf.Max(Mathf.Abs(current.x - target.x), Mathf.Abs(current.y - target.y));
        }
    }
}