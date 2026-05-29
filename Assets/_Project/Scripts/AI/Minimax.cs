// =============================================================================
// Minimax.cs
// Chooses the Guard's best move using minimax with alpha-beta pruning.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

public static class Minimax
{
    // ---------------------------------------------------------------------
    // CONSTANTS
    // ---------------------------------------------------------------------

    /// <summary>
    /// Returned when the Guard has no legal move.
    /// The game logic can use this to skip the Guard's turn.
    /// </summary>
    public static readonly Vector2Int TRAPPED = new Vector2Int(-1, -1);

    /// <summary>Score used when the Guard has already won.</summary>
    private const int SCORE_GUARD_WINS = 100_000;

    /// <summary>Score used when the Thief has already won.</summary>
    private const int SCORE_THIEF_WINS = -100_000;

    /// <summary>
    /// Very large values used by alpha-beta pruning.
    /// </summary>
    private const int NEG_INF = int.MinValue / 2;
    private const int POS_INF = int.MaxValue / 2;

    // ---------------------------------------------------------------------
    // DIRECTION SETS
    // ---------------------------------------------------------------------

    /// <summary>
    /// The Guard can only move up, down, left, or right.
    /// </summary>
    private static readonly Vector2Int[] CardinalDirs = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// The Thief can place barricades in any adjacent tile, including diagonals.
    /// </summary>
    private static readonly Vector2Int[] AllEightDirs = {
        Vector2Int.up,               Vector2Int.down,
        Vector2Int.left,             Vector2Int.right,
        new Vector2Int( 1,  1),      new Vector2Int( 1, -1),
        new Vector2Int(-1,  1),      new Vector2Int(-1, -1)
    };

    // ---------------------------------------------------------------------
    // INTERNAL DATA TYPES
    // ---------------------------------------------------------------------

    /// <summary>
    /// The Thief can either move or place a barricade.
    /// </summary>
    private enum MoveType { Walk, PlaceBarricade }

    /// <summary>
    /// Stores one possible Thief action.
    /// </summary>
    private struct MinimaxMove
    {
        public MoveType Type;
        public Vector2Int Position;
    }

    /// <summary>
    /// Represents one snapshot of the game state during search.
    /// </summary>
    private struct GameState
    {
        public Vector2Int GuardPos;
        public Vector2Int ThiefPos;
        public Vector2Int ExitPos;
        public HashSet<Vector2Int> Obstacles;
        public Dictionary<Vector2Int, int> BarricadeTTLs;
        public int GridWidth;
        public int GridHeight;
    }

    // ---------------------------------------------------------------------
    // PUBLIC ENTRY POINT
    // ---------------------------------------------------------------------

    /// <summary>
    /// Finds the best move for the Guard.
    /// 
    /// Returns TRAPPED (-1, -1) if the Guard cannot move.
    /// </summary>
    public static Vector2Int GetBestGuardMove(
        Vector2Int guardPos,
        Vector2Int thiefPos,
        Vector2Int exitPos,
        HashSet<Vector2Int> obstacles,
        Dictionary<Vector2Int, int> barricadeTTLs,
        int gridWidth,
        int gridHeight,
        int maxDepth,
        int barricadeDuration,
        int maxBarricades)
    {
        obstacles ??= new HashSet<Vector2Int>();
        barricadeTTLs ??= new Dictionary<Vector2Int, int>();
        maxDepth = Mathf.Max(0, maxDepth);
        barricadeDuration = Mathf.Max(0, barricadeDuration);
        maxBarricades = Mathf.Max(0, maxBarricades);

        // Stop early if the game is already over.
        if (guardPos == thiefPos || thiefPos == exitPos)
            return guardPos;

        GameState root = new GameState
        {
            GuardPos = guardPos,
            ThiefPos = thiefPos,
            ExitPos = exitPos,
            Obstacles = obstacles,
            BarricadeTTLs = barricadeTTLs,
            GridWidth = gridWidth,
            GridHeight = gridHeight
        };

        List<Vector2Int> guardMoves = GetGuardMoves(root);

        if (guardMoves.Count == 0)
            return TRAPPED;

        Vector2Int bestMove = guardMoves[0];
        int bestScore = NEG_INF;

        foreach (Vector2Int move in guardMoves)
        {
            GameState child = ApplyGuardMove(root, move);
            int score = MinimaxSearch(child, maxDepth - 1, isMaximizing: false,
                                      NEG_INF, POS_INF, barricadeDuration, maxBarricades);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    // ---------------------------------------------------------------------
    // MINIMAX SEARCH
    // ---------------------------------------------------------------------

    /// <summary>
    /// Looks ahead through future moves and scores each possible outcome.
    /// The Guard tries to maximize the score, while the Thief tries to lower it.
    /// </summary>
    private static int MinimaxSearch(
        GameState state,
        int depth,
        bool isMaximizing,
        int alpha,
        int beta,
        int barricadeDuration,
        int maxBarricades)
    {
        if (state.GuardPos == state.ThiefPos) return SCORE_GUARD_WINS;
        if (state.ThiefPos == state.ExitPos) return SCORE_THIEF_WINS;
        if (depth <= 0) return Evaluate(state);

        if (isMaximizing)
        {
            // Guard turn: choose the move with the highest score.
            List<Vector2Int> moves = GetGuardMoves(state);
            if (moves.Count == 0) return Evaluate(state);

            int best = NEG_INF;
            foreach (Vector2Int move in moves)
            {
                GameState child = ApplyGuardMove(state, move);
                int score = MinimaxSearch(child, depth - 1, isMaximizing: false,
                                          alpha, beta, barricadeDuration, maxBarricades);

                if (score > best) best = score;
                if (score > alpha) alpha = score;
                if (alpha >= beta) break;
            }
            return best;
        }
        else
        {
            // Thief turn: choose the move that gives the Guard the worst result.
            state = TickBarricadeTTLs(state);
            List<MinimaxMove> moves = GetThiefMoves(state, barricadeDuration, maxBarricades);
            if (moves.Count == 0) return Evaluate(state);

            int best = POS_INF;
            foreach (MinimaxMove move in moves)
            {
                GameState child = ApplyThiefMove(state, move, barricadeDuration);
                int score = MinimaxSearch(child, depth - 1, isMaximizing: true,
                                          alpha, beta, barricadeDuration, maxBarricades);

                if (score < best) best = score;
                if (score < beta) beta = score;
                if (alpha >= beta) break;
            }
            return best;
        }
    }

    // ---------------------------------------------------------------------
    // HEURISTIC
    // ---------------------------------------------------------------------

    /// <summary>
    /// Gives a simple score for non-final positions.
    /// Smaller distance between Guard and Thief is better for the Guard.
    /// </summary>
    private static int Evaluate(GameState state)
    {
        int manhattan = Mathf.Abs(state.GuardPos.x - state.ThiefPos.x)
                      + Mathf.Abs(state.GuardPos.y - state.ThiefPos.y);
        return -manhattan;
    }

    // ---------------------------------------------------------------------
    // MOVE GENERATION
    // ---------------------------------------------------------------------

    /// <summary>
    /// Finds all legal moves for the Guard.
    /// </summary>
    private static List<Vector2Int> GetGuardMoves(GameState state)
    {
        List<Vector2Int> moves = new List<Vector2Int>(4);
        foreach (Vector2Int dir in CardinalDirs)
        {
            Vector2Int tile = state.GuardPos + dir;
            if (IsWalkable(tile, state))
                moves.Add(tile);
        }
        return moves;
    }

    /// <summary>
    /// Finds all legal Thief actions:
    /// moving to a nearby tile or placing a barricade.
    /// </summary>
    private static List<MinimaxMove> GetThiefMoves(GameState state, int barricadeDuration, int maxBarricades)
    {
        List<MinimaxMove> moves = new List<MinimaxMove>(12);

        // Thief movement.
        foreach (Vector2Int dir in CardinalDirs)
        {
            Vector2Int tile = state.ThiefPos + dir;
            if (IsWalkable(tile, state))
                moves.Add(new MinimaxMove { Type = MoveType.Walk, Position = tile });
        }

        // Thief barricades.
        int currentActiveBarricades = state.BarricadeTTLs != null ? state.BarricadeTTLs.Count : 0;
        if (barricadeDuration > 0 && currentActiveBarricades < maxBarricades)
        {
            foreach (Vector2Int dir in AllEightDirs)
            {
                Vector2Int tile = state.ThiefPos + dir;
                if (IsEligibleBarricadeTile(tile, state))
                    moves.Add(new MinimaxMove { Type = MoveType.PlaceBarricade, Position = tile });
            }
        }

        return moves;
    }

    // ---------------------------------------------------------------------
    // APPLYING MOVES
    // ---------------------------------------------------------------------

    /// <summary>
    /// Creates a new state after the Guard moves.
    /// </summary>
    private static GameState ApplyGuardMove(GameState state, Vector2Int newPos)
    {
        return new GameState
        {
            GuardPos = newPos,
            ThiefPos = state.ThiefPos,
            ExitPos = state.ExitPos,
            Obstacles = state.Obstacles,
            BarricadeTTLs = state.BarricadeTTLs,
            GridWidth = state.GridWidth,
            GridHeight = state.GridHeight
        };
    }

    /// <summary>
    /// Creates a new state after the Thief either moves or places a barricade.
    /// </summary>
    private static GameState ApplyThiefMove(GameState state, MinimaxMove move, int barricadeDuration)
    {
        if (move.Type == MoveType.Walk)
        {
            return new GameState
            {
                GuardPos = state.GuardPos,
                ThiefPos = move.Position,
                ExitPos = state.ExitPos,
                Obstacles = state.Obstacles,
                BarricadeTTLs = state.BarricadeTTLs,
                GridWidth = state.GridWidth,
                GridHeight = state.GridHeight
            };
        }
        else
        {
            HashSet<Vector2Int> newObs = new HashSet<Vector2Int>(state.Obstacles) { move.Position };

            Dictionary<Vector2Int, int> newTTLs = new Dictionary<Vector2Int, int>(
                state.BarricadeTTLs != null ? state.BarricadeTTLs : new Dictionary<Vector2Int, int>())
            {
                [move.Position] = barricadeDuration
            };

            return new GameState
            {
                GuardPos = state.GuardPos,
                ThiefPos = state.ThiefPos,
                ExitPos = state.ExitPos,
                Obstacles = newObs,
                BarricadeTTLs = newTTLs,
                GridWidth = state.GridWidth,
                GridHeight = state.GridHeight
            };
        }
    }

    // ---------------------------------------------------------------------
    // BARRICADE TIMER
    // ---------------------------------------------------------------------

    /// <summary>
    /// Decreases the life of each barricade.
    /// Expired barricades are removed from the board.
    /// </summary>
    private static GameState TickBarricadeTTLs(GameState state)
    {
        if (state.BarricadeTTLs == null || state.BarricadeTTLs.Count == 0)
            return state;

        List<Vector2Int> expired = null;
        foreach (KeyValuePair<Vector2Int, int> entry in state.BarricadeTTLs)
        {
            if (entry.Value - 1 <= 0)
            {
                if (expired == null) expired = new List<Vector2Int>(2);
                expired.Add(entry.Key);
            }
        }

        Dictionary<Vector2Int, int> newTTLs = new Dictionary<Vector2Int, int>(state.BarricadeTTLs);
        HashSet<Vector2Int> newObs = null;

        foreach (Vector2Int tile in new List<Vector2Int>(newTTLs.Keys))
        {
            newTTLs[tile]--;
            if (newTTLs[tile] <= 0)
                newTTLs.Remove(tile);
        }

        if (expired != null)
        {
            newObs = new HashSet<Vector2Int>(state.Obstacles);
            foreach (Vector2Int tile in expired)
                newObs.Remove(tile);
        }

        return new GameState
        {
            GuardPos = state.GuardPos,
            ThiefPos = state.ThiefPos,
            ExitPos = state.ExitPos,
            Obstacles = newObs ?? state.Obstacles,
            BarricadeTTLs = newTTLs,
            GridWidth = state.GridWidth,
            GridHeight = state.GridHeight
        };
    }

    // ---------------------------------------------------------------------
    // VALIDATION HELPERS
    // ---------------------------------------------------------------------

    /// <summary>
    /// Checks whether a tile is inside the grid and not blocked.
    /// </summary>
    private static bool IsWalkable(Vector2Int pos, GameState state)
    {
        return IsInBounds(pos, state) && !state.Obstacles.Contains(pos);
    }

    /// <summary>
    /// Checks whether the Thief can place a barricade on a tile.
    /// </summary>
    private static bool IsEligibleBarricadeTile(Vector2Int pos, GameState state)
    {
        return IsInBounds(pos, state)
            && !state.Obstacles.Contains(pos)
            && pos != state.GuardPos
            && pos != state.ThiefPos
            && pos != state.ExitPos;
    }

    /// <summary>
    /// Checks whether a position is inside the grid.
    /// </summary>
    private static bool IsInBounds(Vector2Int pos, GameState state)
    {
        return pos.x >= 0 && pos.x < state.GridWidth
            && pos.y >= 0 && pos.y < state.GridHeight;
    }
}