// =============================================================================
// Minimax.cs  —  v5 (Absolute Final Integrated Version)
// Relic Hunter: The Escaping Thief
// COSC 304 – Introduction to Artificial Intelligence | AY 2025-2026
//
// Implements:  Minimax Search with Alpha-Beta Pruning (Section 3.2.2)
// Role:        Static utility — call GetBestGuardMove() from GuardController.
//
// Final Features Included:
//   1. Alpha-Beta Pruning: Eliminates branches that cannot affect final decision.
//   2. Empty-move Safety: Returns a static evaluation if no moves exist.
//   3. TRAPPED Sentinel: Returns (-1, -1) when Guard has no legal moves.
//   4. Barricade Limits: Enforces TTLs and max active barricade constraints.
//   5. Defensive API: Null-safe collections and depth validation.
//   6. Root Terminal Checks: Exits early if game is already decided.
//   7. Exit Tile Protection: Prevents Thief from barricading the escape route.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

public static class Minimax
{
    // =========================================================================
    // SENTINELS & CONSTANTS
    // =========================================================================

    /// <summary>
    /// Returned by GetBestGuardMove() when the Guard is completely trapped (no legal moves).
    /// TurnManager should check: if (result == Minimax.TRAPPED) { skip guard's turn; }
    /// </summary>
    public static readonly Vector2Int TRAPPED = new Vector2Int(-1, -1);

    /// <summary>
    /// Terminal evaluation scores. Large magnitude ensures terminal states dominate
    /// non-terminal evaluations in move selection.
    /// </summary>
    private const int SCORE_GUARD_WINS = 100_000;
    private const int SCORE_THIEF_WINS = -100_000;

    /// <summary>
    /// Infinity bounds for alpha-beta pruning. Using int.MinValue/2 and int.MaxValue/2
    /// prevents overflow when comparing scores in recursive calls.
    /// </summary>
    private const int NEG_INF = int.MinValue / 2;
    private const int POS_INF = int.MaxValue / 2;

    // =========================================================================
    // MOVEMENT DIRECTIONS
    // =========================================================================

    /// <summary>
    /// Cardinal directions (up, down, left, right) for Guard movement.
    /// Guards move in 4-connected grids.
    /// </summary>
    private static readonly Vector2Int[] CardinalDirs = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    /// <summary>
    /// All 8 directions (cardinal + diagonals) for Thief barricade placement.
    /// Thief can place barricades in adjacent 8-connected neighborhoods.
    /// </summary>
    private static readonly Vector2Int[] AllEightDirs = {
        Vector2Int.up,               Vector2Int.down,
        Vector2Int.left,             Vector2Int.right,
        new Vector2Int( 1,  1),      new Vector2Int( 1, -1),
        new Vector2Int(-1,  1),      new Vector2Int(-1, -1)
    };

    // =========================================================================
    // INTERNAL TYPES
    // =========================================================================

    /// <summary>
    /// Classifies Thief actions: moving to an adjacent tile or placing a barricade.
    /// </summary>
    private enum MoveType { Walk, PlaceBarricade }

    /// <summary>
    /// Represents a Thief action: either a walk to Position or a barricade placement.
    /// </summary>
    private struct MinimaxMove
    {
        public MoveType Type;
        public Vector2Int Position;
    }

    /// <summary>
    /// Immutable snapshot of the game state during search. Includes entity positions,
    /// static obstacles, active barricades with remaining TTLs, and grid dimensions.
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

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Returns the Guard's best next move via minimax search with alpha-beta pruning.
    /// 
    /// Key Safety Features:
    ///  - Validates all parameters and substitutes sensible defaults for null collections.
    ///  - Checks if the game is already terminal (Thief escaped or caught).
    ///  - Returns TRAPPED (-1, -1) if the Guard has no legal moves.
    ///
    /// Parameters:
    ///  - guardPos, thiefPos, exitPos: Current entity positions.
    ///  - obstacles, barricadeTTLs: Static and dynamic obstacles; null-safe.
    ///  - gridWidth, gridHeight: Grid dimensions for bounds checking.
    ///  - maxDepth: Search tree depth (larger = deeper lookahead, more computation).
    ///  - barricadeDuration: TTL assigned to each newly placed barricade.
    ///  - maxBarricades: Maximum concurrent barricades allowed on the board.
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
        // Defensive initialization: substitute safe defaults for null inputs.
        obstacles ??= new HashSet<Vector2Int>();
        barricadeTTLs ??= new Dictionary<Vector2Int, int>();
        maxDepth = Mathf.Max(0, maxDepth);
        barricadeDuration = Mathf.Max(0, barricadeDuration);
        maxBarricades = Mathf.Max(0, maxBarricades);

        // Early termination: if the game is already decided, no need to simulate.
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

        // If Guard is fully trapped, signal the TurnManager to skip the turn.
        if (guardMoves.Count == 0)
            return TRAPPED;

        // Select the move with the highest minimax score after recursion.
        Vector2Int bestMove = guardMoves[0];
        int bestScore = NEG_INF;

        foreach (Vector2Int move in guardMoves)
        {
            GameState child = ApplyGuardMove(root, move);
            // Minimizing layer (Thief's perspective): Thief seeks to minimize Guard's advantage.
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

    // =========================================================================
    // CORE MINIMAX RECURSION WITH ALPHA-BETA PRUNING
    // =========================================================================

    /// <summary>
    /// Recursive minimax search with alpha-beta pruning.
    /// 
    /// Alternates between Guard maximization and Thief minimization layers.
    /// Alpha-beta pruning skips branches that provably cannot influence the final choice.
    /// 
    /// Alpha: Highest score found on the maximizing (Guard) path so far.
    /// Beta:  Lowest score found on the minimizing (Thief) path so far.
    /// When alpha >= beta, prune remaining branches (they are irrelevant).
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
        // Terminal state checks: game outcome is decided.
        if (state.GuardPos == state.ThiefPos) return SCORE_GUARD_WINS;
        if (state.ThiefPos == state.ExitPos) return SCORE_THIEF_WINS;
        if (depth <= 0) return Evaluate(state);

        if (isMaximizing)
        {
            // ---- Guard's Turn (Maximizing Layer) ----
            // Guard tries to find the move that maximizes the score.
            List<Vector2Int> moves = GetGuardMoves(state);

            // Empty-move safety: if Guard is trapped during search, return static eval.
            if (moves.Count == 0) return Evaluate(state);

            int best = NEG_INF;

            foreach (Vector2Int move in moves)
            {
                GameState child = ApplyGuardMove(state, move);
                int score = MinimaxSearch(child, depth - 1, isMaximizing: false,
                                          alpha, beta, barricadeDuration, maxBarricades);

                if (score > best) best = score;
                if (score > alpha) alpha = score;

                // Prune: if alpha >= beta, remaining siblings cannot improve this branch.
                if (alpha >= beta) break;
            }

            return best;
        }
        else
        {
            // ---- Thief's Turn (Minimizing Layer) ----
            // Barricade TTLs tick down at the start of the Thief's turn.
            state = TickBarricadeTTLs(state);

            // Thief considers both movement and barricade placement.
            List<MinimaxMove> moves = GetThiefMoves(state, barricadeDuration, maxBarricades);

            // Empty-move safety: if Thief has no moves, return static eval.
            if (moves.Count == 0) return Evaluate(state);

            int best = POS_INF;

            foreach (MinimaxMove move in moves)
            {
                GameState child = ApplyThiefMove(state, move, barricadeDuration);
                int score = MinimaxSearch(child, depth - 1, isMaximizing: true,
                                          alpha, beta, barricadeDuration, maxBarricades);

                if (score < best) best = score;
                if (score < beta) beta = score;

                // Prune: if alpha >= beta, remaining siblings cannot improve this branch.
                if (alpha >= beta) break;
            }

            return best;
        }
    }

    // =========================================================================
    // HEURISTIC EVALUATION
    // =========================================================================

    /// <summary>
    /// Static evaluation of a non-terminal state: negative Manhattan distance.
    /// Closer Guard → higher score (Guard advantage). Farther Guard → lower score (Thief advantage).
    /// Manhattan distance is admissible and consistent for grid-based pursuit.
    /// </summary>
    private static int Evaluate(GameState state)
    {
        int manhattan = Mathf.Abs(state.GuardPos.x - state.ThiefPos.x)
                      + Mathf.Abs(state.GuardPos.y - state.ThiefPos.y);
        return -manhattan;
    }

    // =========================================================================
    // MOVE GENERATORS
    // =========================================================================

    /// <summary>
    /// Returns all walkable adjacent tiles from the Guard's current position.
    /// Guard moves are 4-connected (cardinal directions only).
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
    /// Returns all available Thief actions: walking to adjacent tiles or placing barricades.
    /// 
    /// Walk moves: Any adjacent walkable tile (4-connected).
    /// Barricade moves: Any adjacent tile (8-connected) that is unoccupied, not an obstacle,
    ///   not occupied by Guard/Thief, and not the Exit (protected from barricading).
    ///   Barricade placement is only offered if there are slots remaining (count < maxBarricades)
    ///   and barricade duration is positive.
    /// </summary>
    private static List<MinimaxMove> GetThiefMoves(GameState state, int barricadeDuration, int maxBarricades)
    {
        List<MinimaxMove> moves = new List<MinimaxMove>(12);

        // Walk candidates: Thief moves to an adjacent walkable tile.
        foreach (Vector2Int dir in CardinalDirs)
        {
            Vector2Int tile = state.ThiefPos + dir;
            if (IsWalkable(tile, state))
                moves.Add(new MinimaxMove { Type = MoveType.Walk, Position = tile });
        }

        // Barricade placement candidates: Thief can place a barricade around itself (8-connected).
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

    // =========================================================================
    // MOVE APPLICATION
    // =========================================================================

    /// <summary>
    /// Applies a Guard move (position update) to the current state.
    /// Returns a new immutable state object (no side effects).
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
    /// Applies a Thief move (walk or barricade placement) to the current state.
    /// 
    /// Walk: Update ThiefPos only.
    /// Barricade: Add the tile to Obstacles and initialize its TTL in BarricadeTTLs.
    ///           Both collections are copied to avoid mutation.
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
            // Barricade placement: add to obstacles and track TTL.
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

    // =========================================================================
    // BARRICADE DECAY SIMULATION
    // =========================================================================

    /// <summary>
    /// Decrements all barricade TTLs at the start of the Thief's turn.
    /// Barricades with TTL <= 0 are removed from both BarricadeTTLs and Obstacles.
    /// 
    /// This models barricade expiration: temporary obstacles decay over time,
    /// reflecting real-world deterioration or maintenance intervals.
    /// </summary>
    private static GameState TickBarricadeTTLs(GameState state)
    {
        if (state.BarricadeTTLs == null || state.BarricadeTTLs.Count == 0)
            return state;

        // Identify barricades that will expire after this tick.
        List<Vector2Int> expired = null;
        foreach (KeyValuePair<Vector2Int, int> entry in state.BarricadeTTLs)
        {
            if (entry.Value - 1 <= 0)
            {
                if (expired == null) expired = new List<Vector2Int>(2);
                expired.Add(entry.Key);
            }
        }

        // Decrement all TTLs and remove expired entries.
        Dictionary<Vector2Int, int> newTTLs = new Dictionary<Vector2Int, int>(state.BarricadeTTLs);
        HashSet<Vector2Int> newObs = null;

        foreach (Vector2Int tile in new List<Vector2Int>(newTTLs.Keys))
        {
            newTTLs[tile]--;
            if (newTTLs[tile] <= 0)
                newTTLs.Remove(tile);
        }

        // Remove expired barricades from the obstacle set.
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

    // =========================================================================
    // GRID VALIDATION HELPERS
    // =========================================================================

    /// <summary>
    /// Checks if a position is walkable: in bounds and not blocked by a static or dynamic obstacle.
    /// </summary>
    private static bool IsWalkable(Vector2Int pos, GameState state)
    {
        return IsInBounds(pos, state) && !state.Obstacles.Contains(pos);
    }

    /// <summary>
    /// Checks if a position is eligible for barricade placement:
    ///  - In bounds
    ///  - Not already an obstacle
    ///  - Not occupied by Guard or Thief
    ///  - Not the Exit tile (protected from barricading)
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
    /// Checks if a position is within the grid boundaries [0, width) × [0, height).
    /// </summary>
    private static bool IsInBounds(Vector2Int pos, GameState state)
    {
        return pos.x >= 0 && pos.x < state.GridWidth
            && pos.y >= 0 && pos.y < state.GridHeight;
    }
}
