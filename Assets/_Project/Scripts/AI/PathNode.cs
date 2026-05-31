using System;
using UnityEngine;

namespace RelicHunter.AI
{
    /// <summary>
    /// A* search node with G/H/F costs and parent chain for path reconstruction.
    /// </summary>
    public sealed class PathNode : IComparable<PathNode>
    {
        public Vector2Int Position { get; private set; }
        public PathNode Parent { get; set; }

        // Cost from start to this cell.
        public int GScore { get; set; }

        // Heuristic estimate from this cell to the goal.
        public int HScore { get; set; }

        // Total estimated cost (G + H) used to order the open set.
        public int FScore => GScore + HScore;

        public PathNode(Vector2Int position)
        {
            Position = position;
            Parent = null;
            GScore = int.MaxValue;
            HScore = 0;
        }

        public int CompareTo(PathNode other)
        {
            if (other == null) return 1;
            
            int compare = FScore.CompareTo(other.FScore);
            if (compare == 0)
            {
                // Tie-breaker: Prioritize nodes that are closer to the target (lower H)
                return HScore.CompareTo(other.HScore);
            }
            return compare;
        }
    }
}