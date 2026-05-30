using System;
using System.Collections.Generic;
using RelicHunter.Core;
using UnityEngine;

namespace RelicHunter.AI
{
    /// <summary>
    /// Allocation-optimized, production-grade A* pathfinding system for grid-based traversal.
    /// Operates independently of monolithic engines via static data-driven injection.
    /// </summary>
    public static class AStar
    {
        private static readonly Vector2Int[] CardinalDirections = new Vector2Int[4]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        /// <summary>
        /// Computes an optimal point-to-point sequence using the heuristic state space.
        /// </summary>
        /// <param name="grid">The engine grid manager abstraction context.</param>
        /// <param name="start">Origin point.</param>
        /// <param name="goal">Target node.</param>
        /// <param name="isGuard">Determines if agent-specific traversal constraints apply.</param>
        /// <param name="extraBlocked">An optional localized runtime blacklist containing temporary hazards.</param>
        /// <returns>A clean sequence mapping path progression. Returns an empty list if a solution does not exist.</returns>
        public static List<Vector2Int> FindPath(
            GridManager grid,
            Vector2Int start,
            Vector2Int goal,
            bool isGuard,
            HashSet<Vector2Int> extraBlocked = null)
        {
            List<Vector2Int> operationalPath = new List<Vector2Int>();

            if (grid == null)
            {
                return operationalPath;
            }

            if (start == goal)
            {
                operationalPath.Add(start);
                return operationalPath;
            }

            // Allocation-mitigated structural collections
            PriorityQueue openSet = new PriorityQueue(grid.Width * grid.Height);
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, int> gScore = new Dictionary<Vector2Int, int>();

            gScore[start] = 0;
            openSet.Enqueue(start, Heuristic(start, goal));

            while (openSet.Count > 0)
            {
                Vector2Int current = openSet.Dequeue();

                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }

                closedSet.Add(current);

                // Inline neighbor evaluation to satisfy single-context stability
                for (int i = 0; i < CardinalDirections.Length; i++)
                {
                    Vector2Int neighbor = current + CardinalDirections[i];

                    if (closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    if (extraBlocked != null && extraBlocked.Contains(neighbor))
                    {
                        continue;
                    }

                    // Strict topology parsing matching underlying engine hooks
                    if (!grid.CanEnterCell(current, neighbor, isGuard))
                    {
                        continue;
                    }

                    int tentativeGScore = gScore[current] + 1;

                    if (gScore.TryGetValue(neighbor, out int currentGScore) && tentativeGScore >= currentGScore)
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    int fScore = tentativeGScore + Heuristic(neighbor, goal);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor, fScore);
                    }
                }
            }

            return operationalPath;
        }

        /// <summary>
        /// Computes whether a valid route is available within the current matrix.
        /// </summary>
        public static bool PathExists(
            GridManager grid,
            Vector2Int start,
            Vector2Int goal,
            bool isGuard,
            HashSet<Vector2Int> extraBlocked = null)
        {
            List<Vector2Int> evaluatedRoute = FindPath(grid, start, goal, isGuard, extraBlocked);
            return evaluatedRoute != null && evaluatedRoute.Count > 0;
        }

        /// <summary>
        /// Manhattan metric representing the grid topology heuristic formula f(n) = g(n) + h(n).
        /// </summary>
        private static int Heuristic(Vector2Int current, Vector2Int destination)
        {
            return Mathf.Abs(current.x - destination.x) + Mathf.Abs(current.y - destination.y);
        }

        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            List<Vector2Int> structuralPath = new List<Vector2Int> { current };

            while (cameFrom.TryGetValue(current, out Vector2Int sourceNode))
            {
                current = sourceNode;
                structuralPath.Add(current);
            }

            structuralPath.Reverse();
            return structuralPath;
        }

        /// <summary>
        /// Highly deterministic inline PriorityQueue implementation structured explicitly 
        /// to eliminate dynamic memory allocations during path exploration loops.
        /// </summary>
        private sealed class PriorityQueue
        {
            private struct QueueElement
            {
                public Vector2Int Node;
                public int Priority;
            }

            private readonly QueueElement[] heapArray;
            private readonly HashSet<Vector2Int> trackingSet;
            private int elementCount;

            public int Count => elementCount;

            public PriorityQueue(int capacity)
            {
                heapArray = new QueueElement[capacity];
                trackingSet = new HashSet<Vector2Int>();
                elementCount = 0;
            }

            public bool Contains(Vector2Int node)
            {
                return trackingSet.Contains(node);
            }

            public void Enqueue(Vector2Int node, int priority)
            {
                trackingSet.Add(node);
                QueueElement newElement = new QueueElement { Node = node, Priority = priority };
                heapArray[elementCount] = newElement;
                SiftUp(elementCount);
                elementCount++;
            }

            public Vector2Int Dequeue()
            {
                if (elementCount == 0)
                {
                    throw new InvalidOperationException("Queue underflow exception during path traversal serialization.");
                }

                Vector2Int trackedNode = heapArray[0].Node;
                trackingSet.Remove(trackedNode);

                elementCount--;
                heapArray[0] = heapArray[elementCount];
                SiftDown(0);

                return trackedNode;
            }

            private void SiftUp(int elementIndex)
            {
                while (elementIndex > 0)
                {
                    int parentIndex = (elementIndex - 1) / 2;
                    if (heapArray[elementIndex].Priority >= heapArray[parentIndex].Priority)
                    {
                        break;
                    }

                    SwapElements(elementIndex, parentIndex);
                    elementIndex = parentIndex;
                }
            }

            private void SiftDown(int elementIndex)
            {
                while (true)
                {
                    int leftChildIndex = (elementIndex * 2) + 1;
                    int rightChildIndex = (elementIndex * 2) + 2;
                    int smallestValueIndex = elementIndex;

                    if (leftChildIndex < elementCount && heapArray[leftChildIndex].Priority < heapArray[smallestValueIndex].Priority)
                    {
                        smallestValueIndex = leftChildIndex;
                    }

                    if (rightChildIndex < elementCount && heapArray[rightChildIndex].Priority < heapArray[smallestValueIndex].Priority)
                    {
                        smallestValueIndex = rightChildIndex;
                    }

                    if (smallestValueIndex == elementIndex)
                    {
                        break;
                    }

                    SwapElements(elementIndex, smallestValueIndex);
                    elementIndex = smallestValueIndex;
                }
            }

            private void SwapElements(int sourceIndex, int targetIndex)
            {
                QueueElement temporaryElement = heapArray[sourceIndex];
                heapArray[sourceIndex] = heapArray[targetIndex];
                heapArray[targetIndex] = temporaryElement;
            }
        }
    }
}