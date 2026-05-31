using System;
using System.Collections.Generic;
using UnityEngine;
using RelicHunter.Core;
using RelicHunter.AI;

/// <summary>
/// A* pathfinding for grid traversal with guard leap support.
/// </summary>
public static class AStar
{
    private static readonly Vector2Int[] CardinalDirs = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// Finds a shortest path respecting barricades, maze walls, and guard-specific leap rules.
    /// </summary>
    public static List<Vector2Int> FindPath(
        GridManager grid,
        Vector2Int startPos,
        Vector2Int targetPos,
        bool isGuard,
        HashSet<Vector2Int> extraBlocked)
    {
        List<Vector2Int> computedPath = new List<Vector2Int>();

        if (grid == null)
            return computedPath;

        if (startPos == targetPos)
        {
            computedPath.Add(startPos);
            return computedPath;
        }

        int width = grid.Width;
        int height = grid.Height;
        int estimatedCapacity = Mathf.Max(16, Mathf.Min(width * height, 256));

        MinHeap openSet = new MinHeap(estimatedCapacity);
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>(estimatedCapacity);
        Dictionary<Vector2Int, int> bestGScore = new Dictionary<Vector2Int, int>(estimatedCapacity);
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        int startH = Heuristics.GetManhattanDistance(startPos, targetPos);
        bestGScore[startPos] = 0;
        openSet.Push(new QueueEntry(startPos, 0, startH));

        List<Vector2Int> adjacentBuffer = new List<Vector2Int>(4);

        while (openSet.Count > 0)
        {
            QueueEntry current = openSet.Pop();

            if (closedSet.Contains(current.Position))
                continue;

            if (!bestGScore.TryGetValue(current.Position, out int knownBestG) || knownBestG != current.GScore)
                continue;

            if (current.Position == targetPos)
            {
                return ReconstructPath(cameFrom, startPos, targetPos);
            }

            closedSet.Add(current.Position);

            GetMovementNeighbors(grid, current.Position, isGuard, extraBlocked, adjacentBuffer);

            for (int i = 0; i < adjacentBuffer.Count; i++)
            {
                Vector2Int neighborPos = adjacentBuffer[i];

                if (closedSet.Contains(neighborPos))
                    continue;

                int edgeCost = Heuristics.GetManhattanDistance(current.Position, neighborPos);
                int tentativeGScore = current.GScore + Mathf.Max(1, edgeCost);

                if (bestGScore.TryGetValue(neighborPos, out int existingGScore) &&
                    tentativeGScore >= existingGScore)
                {
                    continue;
                }

                cameFrom[neighborPos] = current.Position;
                bestGScore[neighborPos] = tentativeGScore;

                int hScore = Heuristics.GetManhattanDistance(neighborPos, targetPos);
                openSet.Push(new QueueEntry(neighborPos, tentativeGScore, hScore));
            }
        }

        return computedPath;
    }

    /// <summary>
    /// Checks whether a path exists between two cells.
    /// </summary>
    public static bool PathExists(
        GridManager grid,
        Vector2Int startPos,
        Vector2Int targetPos,
        bool isGuard,
        HashSet<Vector2Int> extraBlocked)
    {
        List<Vector2Int> path = FindPath(grid, startPos, targetPos, isGuard, extraBlocked);
        return path != null && path.Count > 0;
    }

    /// <summary>
    /// Builds valid neighboring positions for either guard or regular traversal.
    /// </summary>
    private static void GetMovementNeighbors(
        GridManager grid,
        Vector2Int from,
        bool isGuard,
        HashSet<Vector2Int> extraBlocked,
        List<Vector2Int> neighbors)
    {
        neighbors.Clear();

        for (int i = 0; i < CardinalDirs.Length; i++)
        {
            Vector2Int dir = CardinalDirs[i];
            Vector2Int adjacent = from + dir;

            // Guard leap over the exit tile.
            if (isGuard && adjacent == grid.exitPos)
            {
                Vector2Int jumpTo = from + dir * 2;

                if ((extraBlocked == null || !extraBlocked.Contains(jumpTo)) &&
                    grid.CanGuardLeapTo(from, jumpTo))
                {
                    neighbors.Add(jumpTo);
                }

                continue;
            }

            if (extraBlocked != null && extraBlocked.Contains(adjacent))
                continue;

            if (grid.CanEnterCell(from, adjacent, isGuard))
            {
                neighbors.Add(adjacent);
            }
        }
    }

    private static List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int startPos,
        Vector2Int targetPos)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        Vector2Int current = targetPos;
        path.Add(current);

        while (current != startPos && cameFrom.TryGetValue(current, out Vector2Int parent))
        {
            current = parent;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private struct QueueEntry
    {
        public Vector2Int Position;
        public int GScore;
        public int HScore;

        public int FScore => GScore + HScore;

        public QueueEntry(Vector2Int position, int gScore, int hScore)
        {
            Position = position;
            GScore = gScore;
            HScore = hScore;
        }
    }

    // =========================================================================
    // HIGH-PERFORMANCE PRIORITY QUEUE (GC-OPTIMIZED MIN-HEAP)
    // =========================================================================
    private sealed class MinHeap
    {
        private QueueEntry[] _items;
        private int _size;

        public int Count => _size;

        public MinHeap(int capacity)
        {
            _items = new QueueEntry[Math.Max(4, capacity)];
            _size = 0;
        }

        public void Push(QueueEntry item)
        {
            if (_size == _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }

            _items[_size] = item;
            BubbleUp(_size);
            _size++;
        }

        public QueueEntry Pop()
        {
            if (_size == 0)
                throw new InvalidOperationException("Heap is empty.");

            QueueEntry head = _items[0];
            _size--;

            if (_size > 0)
            {
                _items[0] = _items[_size];
                BubbleDown(0);
            }

            _items[_size] = default;
            return head;
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (!IsBetter(_items[index], _items[parent]))
                    break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int left = (index << 1) + 1;
                int right = left + 1;
                int best = index;

                if (left < _size && IsBetter(_items[left], _items[best]))
                    best = left;

                if (right < _size && IsBetter(_items[right], _items[best]))
                    best = right;

                if (best == index)
                    break;

                Swap(index, best);
                index = best;
            }
        }

        private static bool IsBetter(QueueEntry a, QueueEntry b)
        {
            if (a.FScore != b.FScore)
                return a.FScore < b.FScore;

            if (a.HScore != b.HScore)
                return a.HScore < b.HScore;

            return a.GScore < b.GScore;
        }

        private void Swap(int a, int b)
        {
            QueueEntry temp = _items[a];
            _items[a] = _items[b];
            _items[b] = temp;
        }
    }
}
