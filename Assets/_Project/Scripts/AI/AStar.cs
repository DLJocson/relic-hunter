using System.Collections.Generic;
using RelicHunter.Core;
using UnityEngine;

namespace RelicHunter.AI
{
    /// <summary>
    /// A* pathfinding for grid-based movement. Uses GridManager.CanEnterCell for walkability.
    /// </summary>
    public static class AStar
    {
        private static readonly Vector2Int[] CardinalDirs = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public static bool PathExists(
            GridManager grid,
            Vector2Int start,
            Vector2Int goal,
            bool isGuard,
            HashSet<Vector2Int> extraBlocked = null)
        {
            return FindPath(grid, start, goal, isGuard, extraBlocked).Count > 0;
        }

        private static List<Vector2Int> FindPath(
            GridManager grid,
            Vector2Int start,
            Vector2Int goal,
            bool isGuard,
            HashSet<Vector2Int> extraBlocked = null)
        {
            var emptyPath = new List<Vector2Int>();
            if (grid == null) return emptyPath;
            if (start == goal) return new List<Vector2Int> { start };

            var openSet = new List<Vector2Int> { start };
            var neighborBuffer = new List<Vector2Int>(4);
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
            var fScore = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, goal) };

            while (openSet.Count > 0)
            {
                Vector2Int current = PopLowestFScore(openSet, fScore);

                if (current == goal)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

                if (isGuard)
                    grid.CollectGuardNeighborCells(current, neighborBuffer);
                else
                    CollectPlayerNeighborCells(grid, current, isGuard, extraBlocked, neighborBuffer);

                foreach (Vector2Int neighbor in neighborBuffer)
                {
                    if (extraBlocked != null && extraBlocked.Contains(neighbor))
                        continue;

                    int tentativeG = gScore[current] + 1;
                    if (gScore.TryGetValue(neighbor, out int existingG) && tentativeG >= existingG)
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }

            return emptyPath;
        }

        private static void CollectPlayerNeighborCells(
            GridManager grid,
            Vector2Int current,
            bool isGuard,
            HashSet<Vector2Int> extraBlocked,
            List<Vector2Int> neighbors)
        {
            neighbors.Clear();

            foreach (Vector2Int dir in CardinalDirs)
            {
                Vector2Int neighbor = current + dir;
                if (extraBlocked != null && extraBlocked.Contains(neighbor))
                    continue;
                if (!grid.CanEnterCell(current, neighbor, isGuard))
                    continue;
                neighbors.Add(neighbor);
            }
        }

        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static Vector2Int PopLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, int> fScore)
        {
            Vector2Int best = openSet[0];
            int bestScore = fScore.TryGetValue(best, out int score) ? score : int.MaxValue;

            for (int i = 1; i < openSet.Count; i++)
            {
                Vector2Int candidate = openSet[i];
                int candidateScore = fScore.TryGetValue(candidate, out int candidateF) ? candidateF : int.MaxValue;
                if (candidateScore < bestScore)
                {
                    best = candidate;
                    bestScore = candidateScore;
                }
            }

            return best;
        }

        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            while (cameFrom.TryGetValue(current, out Vector2Int previous))
            {
                current = previous;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}
