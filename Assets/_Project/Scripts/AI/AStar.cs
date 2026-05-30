// =============================================================================
// AStar.cs — Unified structural routing & path safety evaluation component
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using RelicHunter.Core;
using RelicHunter.AI;

public static class AStar
{
    private static readonly Vector2Int[] CardinalDirs = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// Computes an optimal, complete point-to-point sequence of grid positions.
    /// Safely wraps custom GridManager traversal constraints and handles exceptional Guard mechanics (Leaping).
    /// </summary>
    public static List<Vector2Int> FindPath(
        GridManager grid,
        Vector2Int startPos,
        Vector2Int targetPos,
        bool isGuard,
        HashSet<Vector2Int> extraBlocked)
    {
        List<Vector2Int> computedPath = new List<Vector2Int>();

        if (grid == null) return computedPath;
        if (startPos == targetPos)
        {
            computedPath.Add(startPos);
            return computedPath;
        }

        int width = grid.Width;
        int height = grid.Height;

        // Bounded capacity allocation matrix to avoid garbage collection allocation cycles
        int estimatedCapacity = Mathf.Min(width * height, 256);
        SimplePriorityQueue<PathNode> openSet = new SimplePriorityQueue<PathNode>(estimatedCapacity);
        Dictionary<Vector2Int, PathNode> allNodes = new Dictionary<Vector2Int, PathNode>(estimatedCapacity);

        PathNode startNode = new PathNode(startPos)
        {
            GScore = 0,
            HScore = Heuristics.GetManhattanDistance(startPos, targetPos)
        };

        allNodes[startPos] = startNode;
        openSet.Enqueue(startNode);

        bool destinationReached = false;
        List<Vector2Int> adjacentBuffer = new List<Vector2Int>(4);

        while (openSet.Count > 0)
        {
            PathNode current = openSet.Dequeue();

            if (current.Position == targetPos)
            {
                destinationReached = true;
                break;
            }

            // Extract dynamic context neighbors using existing structural layout paths
            GetMovementNeighbors(grid, current.Position, isGuard, extraBlocked, adjacentBuffer);

            for (int i = 0; i < adjacentBuffer.Count; i++)
            {
                Vector2Int neighborPos = adjacentBuffer[i];
                
                // Calculate actual path cost step changes (Leaps count as double steps but evaluate normally)
                int edgeCost = Heuristics.GetManhattanDistance(current.Position, neighborPos);
                int tentativeGScore = current.GScore + edgeCost;

                if (!allNodes.TryGetValue(neighborPos, out PathNode neighborNode))
                {
                    neighborNode = new PathNode(neighborPos);
                    allNodes[neighborPos] = neighborNode;
                }

                if (tentativeGScore < neighborNode.GScore)
                {
                    neighborNode.Parent = current;
                    neighborNode.GScore = tentativeGScore;
                    neighborNode.HScore = Heuristics.GetManhattanDistance(neighborPos, targetPos);

                    if (!openSet.Contains(neighborNode))
                    {
                        openSet.Enqueue(neighborNode);
                    }
                }
            }
        }

        if (destinationReached)
        {
            // Unwind parent nodes backwards into an ordered sequence
            PathNode trace = allNodes[targetPos];
            while (trace != null)
            {
                computedPath.Add(trace.Position);
                trace = trace.Parent;
            }
            computedPath.Reverse();
        }

        return computedPath;
    }

    /// <summary>
    /// Interface compliance fallback for GridManager path presence analysis calls.
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
    /// Intercepts valid step configurations by combining regular card paths with custom Guard jumping matrices.
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

            // Handle special Guard Leap actions over exit locations
            if (isGuard && adjacent == grid.exitPos)
            {
                Vector2Int jumpTo = from + dir * 2;
                if ((extraBlocked == null || !extraBlocked.Contains(jumpTo)) && grid.CanGuardLeapTo(from, jumpTo))
                {
                    neighbors.Add(jumpTo);
                }
                continue;
            }

            // Normal step evaluation logic
            if (extraBlocked != null && extraBlocked.Contains(adjacent))
                continue;

            if (grid.CanEnterCell(from, adjacent, isGuard))
            {
                neighbors.Add(adjacent);
            }
        }
    }

    // =========================================================================
    // HIGH-PERFORMANCE PRIORITY QUEUE (GC-OPTIMIZED MIN-HEAP)
    // =========================================================================
    private sealed class SimplePriorityQueue<T> where T : IComparable<T>
    {
        private T[] _items;
        private int _size;
        private readonly HashSet<T> _set;

        public int Count => _size;

        public SimplePriorityQueue(int capacity)
        {
            _items = new T[capacity];
            _size = 0;
            _set = new HashSet<T>();
        }

        public bool Contains(T item) => _set.Contains(item);

        public void Enqueue(T item)
        {
            if (_size == _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2);
            }
            _items[_size] = item;
            _set.Add(item);
            BubbleUp(_size);
            _size++;
        }

        public T Dequeue()
        {
            if (_size == 0) throw new InvalidOperationException("Queue is empty.");
            T head = _items[0];
            _size--;
            if (_size > 0)
            {
                _items[0] = _items[_size];
                TrickleDown(0);
            }
            _items[_size] = default;
            _set.Remove(head);
            return head;
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                if (_items[index].CompareTo(_items[parent]) >= 0) break;
                T swap = _items[index]; _items[index] = _items[parent]; _items[parent] = swap;
                index = parent;
            }
        }

        private void TrickleDown(int index)
        {
            int middle = _size >> 1;
            while (index < middle)
            {
                int left = (index << 1) + 1;
                int right = left + 1;
                int best = left;
                if (right < _size && _items[right].CompareTo(_items[left]) < 0) best = right;
                if (_items[index].CompareTo(_items[best]) <= 0) break;
                T swap = _items[index]; _items[index] = _items[best]; _items[best] = swap;
                index = best;
            }
        }
    }
}