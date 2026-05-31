using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chooses the guard's best move using minimax with alpha-beta pruning.
/// </summary>
public static class Minimax
{
    // ---------------------------------------------------------------------
    // CONSTANTS
    // ---------------------------------------------------------------------

    public static readonly Vector2Int TRAPPED = new Vector2Int(-1, -1);
    private const int SCORE_GUARD_WINS = 100_000;
    private const int SCORE_THIEF_WINS = -100_000;
    private const int NEG_INF = int.MinValue / 2;
    private const int POS_INF = int.MaxValue / 2;

    private static readonly Vector2Int[] CardinalDirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    private static readonly Vector2Int[] AllEightDirs =
    {
        Vector2Int.up,               Vector2Int.down,
        Vector2Int.left,             Vector2Int.right,
        new Vector2Int( 1,  1),      new Vector2Int( 1, -1),
        new Vector2Int(-1,  1),      new Vector2Int(-1, -1)
    };

    private enum MoveType
    {
        Walk,
        PlaceBarricade
    }

    private struct MinimaxMove
    {
        public MoveType Type;
        public Vector2Int Position;
    }

    private struct GameState
    {
        public Vector2Int GuardPos;
        public Vector2Int ThiefPos;
        public Vector2Int ExitPos;
        public HashSet<Vector2Int> Obstacles;
        public Dictionary<Vector2Int, int> BarricadeTTLs;
        public int GridWidth;
        public int GridHeight;

        public Func<Vector2Int, Vector2Int, bool> CanGuardMoveBetween;
        public Func<Vector2Int, Vector2Int, bool> CanThiefMoveBetween;
        public Func<Vector2Int, Vector2Int, bool> CanGuardLeapTo;
    }

    /// <summary>
    /// Backward-compatible overload.
    /// Uses the same movement callback for both guard and thief unless a more specific overload is used.
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
        int maxBarricades,
        Func<Vector2Int, Vector2Int, bool> canMoveBetween = null,
        Func<Vector2Int, Vector2Int, bool> canGuardLeapTo = null)
    {
        return GetBestGuardMove(
            guardPos,
            thiefPos,
            exitPos,
            obstacles,
            barricadeTTLs,
            gridWidth,
            gridHeight,
            maxDepth,
            barricadeDuration,
            maxBarricades,
            canGuardMoveBetween: canMoveBetween,
            canThiefMoveBetween: canMoveBetween,
            canGuardLeapTo: canGuardLeapTo);
    }

    /// <summary>
    /// Preferred overload.
    /// Separate callbacks let the simulation follow guard and thief rules independently.
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
        int maxBarricades,
        Func<Vector2Int, Vector2Int, bool> canGuardMoveBetween,
        Func<Vector2Int, Vector2Int, bool> canThiefMoveBetween,
        Func<Vector2Int, Vector2Int, bool> canGuardLeapTo = null)
    {
        obstacles ??= new HashSet<Vector2Int>();
        barricadeTTLs ??= new Dictionary<Vector2Int, int>();
        maxDepth = Mathf.Max(0, maxDepth);
        barricadeDuration = Mathf.Max(0, barricadeDuration);
        maxBarricades = Mathf.Max(0, maxBarricades);

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
            GridHeight = gridHeight,
            CanGuardMoveBetween = canGuardMoveBetween,
            CanThiefMoveBetween = canThiefMoveBetween,
            CanGuardLeapTo = canGuardLeapTo
        };

        List<Vector2Int> guardMoves = GetGuardMoves(root);

        if (guardMoves.Count == 0)
            return TRAPPED;

        Vector2Int bestMove = guardMoves[0];
        int bestScore = NEG_INF;

        foreach (Vector2Int move in guardMoves)
        {
            GameState child = ApplyGuardMove(root, move);
            int score = MinimaxSearch(
                child,
                maxDepth - 1,
                isMaximizing: false,
                alpha: NEG_INF,
                beta: POS_INF,
                barricadeDuration: barricadeDuration,
                maxBarricades: maxBarricades);

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

    private static int MinimaxSearch(
        GameState state,
        int depth,
        bool isMaximizing,
        int alpha,
        int beta,
        int barricadeDuration,
        int maxBarricades)
    {
        if (state.GuardPos == state.ThiefPos)
            return SCORE_GUARD_WINS;

        if (state.ThiefPos == state.ExitPos)
            return SCORE_THIEF_WINS;

        if (depth <= 0)
            return Evaluate(state);

        if (isMaximizing)
        {
            List<Vector2Int> moves = GetGuardMoves(state);
            if (moves.Count == 0)
                return Evaluate(state);

            int best = NEG_INF;

            foreach (Vector2Int move in moves)
            {
                GameState child = ApplyGuardMove(state, move);
                int score = MinimaxSearch(
                    child,
                    depth - 1,
                    isMaximizing: false,
                    alpha: alpha,
                    beta: beta,
                    barricadeDuration: barricadeDuration,
                    maxBarricades: maxBarricades);

                if (score > best) best = score;
                if (score > alpha) alpha = score;
                if (alpha >= beta) break;
            }

            return best;
        }
        else
        {
            state = TickBarricadeTTLs(state);

            List<MinimaxMove> moves = GetThiefMoves(state, barricadeDuration, maxBarricades);
            if (moves.Count == 0)
                return Evaluate(state);

            int best = POS_INF;

            foreach (MinimaxMove move in moves)
            {
                GameState child = ApplyThiefMove(state, move, barricadeDuration);
                int score = MinimaxSearch(
                    child,
                    depth - 1,
                    isMaximizing: true,
                    alpha: alpha,
                    beta: beta,
                    barricadeDuration: barricadeDuration,
                    maxBarricades: maxBarricades);

                if (score < best) best = score;
                if (score < beta) beta = score;
                if (alpha >= beta) break;
            }

            return best;
        }
    }

    private static int Evaluate(GameState state)
    {
        int manhattan = Mathf.Abs(state.GuardPos.x - state.ThiefPos.x)
                      + Mathf.Abs(state.GuardPos.y - state.ThiefPos.y);

        return -manhattan;
    }

    // ---------------------------------------------------------------------
    // MOVE GENERATION
    // ---------------------------------------------------------------------

    private static List<Vector2Int> GetGuardMoves(GameState state)
    {
        List<Vector2Int> moves = new List<Vector2Int>(4);

        foreach (Vector2Int dir in CardinalDirs)
        {
            Vector2Int adjacent = state.GuardPos + dir;

            if (adjacent == state.ExitPos)
            {
                Vector2Int jumpTo = state.GuardPos + dir * 2;
                if (CanGuardReachTile(state.GuardPos, jumpTo, state))
                    moves.Add(jumpTo);

                continue;
            }

            if (CanMoveTo(state.GuardPos, adjacent, state, isGuard: true))
                moves.Add(adjacent);
        }

        return moves;
    }

    private static List<MinimaxMove> GetThiefMoves(GameState state, int barricadeDuration, int maxBarricades)
    {
        List<MinimaxMove> moves = new List<MinimaxMove>(12);

        foreach (Vector2Int dir in CardinalDirs)
        {
            Vector2Int tile = state.ThiefPos + dir;
            if (CanMoveTo(state.ThiefPos, tile, state, isGuard: false))
            {
                moves.Add(new MinimaxMove
                {
                    Type = MoveType.Walk,
                    Position = tile
                });
            }
        }

        int currentActiveBarricades = state.BarricadeTTLs != null ? state.BarricadeTTLs.Count : 0;
        if (barricadeDuration > 0 && currentActiveBarricades < maxBarricades)
        {
            foreach (Vector2Int dir in AllEightDirs)
            {
                Vector2Int tile = state.ThiefPos + dir;
                if (IsEligibleBarricadeTile(tile, state))
                {
                    moves.Add(new MinimaxMove
                    {
                        Type = MoveType.PlaceBarricade,
                        Position = tile
                    });
                }
            }
        }

        return moves;
    }

    // ---------------------------------------------------------------------
    // APPLYING MOVES
    // ---------------------------------------------------------------------

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
            GridHeight = state.GridHeight,
            CanGuardMoveBetween = state.CanGuardMoveBetween,
            CanThiefMoveBetween = state.CanThiefMoveBetween,
            CanGuardLeapTo = state.CanGuardLeapTo
        };
    }

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
                GridHeight = state.GridHeight,
                CanGuardMoveBetween = state.CanGuardMoveBetween,
                CanThiefMoveBetween = state.CanThiefMoveBetween,
                CanGuardLeapTo = state.CanGuardLeapTo
            };
        }

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
            Obstacles = state.Obstacles,
            BarricadeTTLs = newTTLs,
            GridWidth = state.GridWidth,
            GridHeight = state.GridHeight,
            CanGuardMoveBetween = state.CanGuardMoveBetween,
            CanThiefMoveBetween = state.CanThiefMoveBetween,
            CanGuardLeapTo = state.CanGuardLeapTo
        };
    }

    // ---------------------------------------------------------------------
    // BARRICADE TIMER
    // ---------------------------------------------------------------------

    private static GameState TickBarricadeTTLs(GameState state)
    {
        if (state.BarricadeTTLs == null || state.BarricadeTTLs.Count == 0)
            return state;

        Dictionary<Vector2Int, int> newTTLs = new Dictionary<Vector2Int, int>(state.BarricadeTTLs);
        List<Vector2Int> keys = new List<Vector2Int>(newTTLs.Keys);

        foreach (Vector2Int key in keys)
        {
            newTTLs[key]--;
            if (newTTLs[key] <= 0)
            {
                newTTLs.Remove(key);
            }
        }

        return new GameState
        {
            GuardPos = state.GuardPos,
            ThiefPos = state.ThiefPos,
            ExitPos = state.ExitPos,
            Obstacles = state.Obstacles,
            BarricadeTTLs = newTTLs,
            GridWidth = state.GridWidth,
            GridHeight = state.GridHeight,
            CanGuardMoveBetween = state.CanGuardMoveBetween,
            CanThiefMoveBetween = state.CanThiefMoveBetween,
            CanGuardLeapTo = state.CanGuardLeapTo
        };
    }

    // ---------------------------------------------------------------------
    // VALIDATION HELPERS
    // ---------------------------------------------------------------------

    private static bool CanMoveTo(Vector2Int from, Vector2Int to, GameState state, bool isGuard)
    {
        if (!IsInBounds(to, state))
            return false;

        bool isBarricade = state.BarricadeTTLs != null && state.BarricadeTTLs.ContainsKey(to);
        if (isBarricade)
            return false;

        bool isWall = state.Obstacles != null && state.Obstacles.Contains(to);
        if (isWall)
            return false;

        if (isGuard)
        {
            if (to == state.ExitPos)
                return false;

            int manhattan = Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);

            if (manhattan == 2)
                return CanGuardReachTile(from, to, state);

            if (manhattan != 1)
                return false;

            if (state.CanGuardMoveBetween != null && !state.CanGuardMoveBetween(from, to))
                return false;
        }
        else
        {
            int manhattan = Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
            if (manhattan != 1)
                return false;

            if (state.CanThiefMoveBetween != null && !state.CanThiefMoveBetween(from, to))
                return false;
        }

        return true;
    }

    private static bool CanGuardReachTile(Vector2Int from, Vector2Int to, GameState state)
    {
        if (state.CanGuardLeapTo != null)
            return state.CanGuardLeapTo(from, to);

        Vector2Int delta = to - from;
        int manhattan = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
        if (manhattan != 2)
            return false;

        if (delta.x != 0 && delta.y != 0)
            return false;

        Vector2Int dir = new Vector2Int(
            delta.x != 0 ? (delta.x > 0 ? 1 : -1) : 0,
            delta.y != 0 ? (delta.y > 0 ? 1 : -1) : 0);

        Vector2Int overExit = from + dir;
        if (overExit != state.ExitPos)
            return false;

        if (to == state.ExitPos)
            return false;

        if (!IsInBounds(to, state))
            return false;

        bool barricadeOnPath = state.BarricadeTTLs != null
            && (state.BarricadeTTLs.ContainsKey(overExit) || state.BarricadeTTLs.ContainsKey(to));
        if (barricadeOnPath)
            return false;

        bool wallOnPath = state.Obstacles != null
            && (state.Obstacles.Contains(overExit) || state.Obstacles.Contains(to));
        if (wallOnPath)
            return false;

        return true;
    }

    private static bool IsEligibleBarricadeTile(Vector2Int pos, GameState state)
    {
        bool isWall = state.Obstacles != null && state.Obstacles.Contains(pos);
        bool isBarricade = state.BarricadeTTLs != null && state.BarricadeTTLs.ContainsKey(pos);

        return IsInBounds(pos, state)
            && !isWall
            && !isBarricade
            && pos != state.GuardPos
            && pos != state.ThiefPos
            && pos != state.ExitPos;
    }

    private static bool IsInBounds(Vector2Int pos, GameState state)
    {
        return pos.x >= 0 && pos.x < state.GridWidth
            && pos.y >= 0 && pos.y < state.GridHeight;
    }
}