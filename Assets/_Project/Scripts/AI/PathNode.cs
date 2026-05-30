// =============================================================================
// PathNode.cs — Discrete node representation for pathfinding space search
// =============================================================================

using System;
using UnityEngine;

namespace RelicHunter.AI
{
    public sealed class PathNode : IComparable<PathNode>
    {
        public Vector2Int Position { get; private set; }
        public PathNode Parent { get; set; }

        // G Score: Cost from the start node to this node
        public int GScore { get; set; }
        
        // H Score: Estimated cost from this node to the end node (Heuristic)
        public int HScore { get; set; }

        // F Score: Total estimated cost (G + H)
        public int FScore => GScore + HScore;

        public PathNode(Vector2Int position)
        {
            Position = position;
            Parent = null;
            GScore = int.MaxValue;
            HScore = 0;
        }

        public void Reset()
        {
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