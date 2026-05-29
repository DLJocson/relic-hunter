// =============================================================================
// GameManager.cs — Manages match progression, rounds, and scoring.
// =============================================================================

using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // =========================================================================
    // SINGLETON AND MATCH STATE
    // =========================================================================

    public static GameManager Instance { get; private set; }

    public enum MatchState
    {
        public static GameManager Instance { get; private set; }

        [System.Serializable]
        public struct RoundData
        {
            public int guardSpeed;
            public int minimaxDepth;
            public int barricadeDuration;
            public int maxBarricades;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        /// <summary>
        /// Placeholder helper that provides default round data until team structures configuration menus
        /// </summary>
        public bool TryGetCurrentRound(out RoundData round)
        {
            round = new RoundData
            {
                guardSpeed = 1,        // Guard steps 1 tile per turn
                minimaxDepth = 2,      // Alpha-Beta Search calculation layer depth
                barricadeDuration = 4, // Turns barricades last
                maxBarricades = 3      // Maximum placement limits
            };
            return true;
        }
        
        NotStarted,
        RoundActive,
        RoundTransition,
        MatchOver
    }

    [Serializable]
    public struct RoundDefinition
    {
        public string roundName;
        public int gridWidth;
        public int gridHeight;
        public int guardSpeed;
        public int barricadeDuration;
        public int maxBarricades;
        public int minimaxDepth;
    }

    [Header("Scene References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private PlayerController playerController;

    [Header("Round Definitions")]
    [SerializeField] private RoundDefinition[] rounds = new RoundDefinition[3];

    [Header("Match Tracking")]
    [SerializeField] private int currentRoundIndex = 1; // 1-based: 1..3
    [SerializeField] private int playerWins = 0;
    [SerializeField] private int guardWins = 0;

    public MatchState CurrentMatchState { get; private set; } = MatchState.NotStarted;

    // =========================================================================
    // PUBLIC PROPERTIES
    // =========================================================================

    public int CurrentRoundIndex => currentRoundIndex;
    public int PlayerWins => playerWins;
    public int GuardWins => guardWins;

    public int CurrentGuardSpeed { get; private set; }
    public int CurrentMinimaxDepth { get; private set; }

    public RoundDefinition? CurrentRound
    {
        get
        {
            int index = currentRoundIndex - 1;
            if (rounds == null || index < 0 || index >= rounds.Length)
                return null;

            return rounds[index];
        }
    }

    // =========================================================================
    // UNITY LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        EnsureDefaultRounds();
        ResolveSceneReferences();
    }

    private void Start()
    {
        EnsureDefaultRounds();

        if (CurrentMatchState == MatchState.NotStarted)
        {
            StartMatch();
        }
    }

    private void Reset()
    {
        EnsureDefaultRounds();
    }

    private void OnValidate()
    {
        EnsureDefaultRounds();
    }

    // =========================================================================
    // ROUND SETUP AND VALIDATION
    // =========================================================================

    /// <summary>
    /// Fills missing round slots with default values.
    /// </summary>
    private void EnsureDefaultRounds()
    {
        if (rounds == null || rounds.Length < 3)
            rounds = new RoundDefinition[3];

        SetDefaultRoundIfEmpty(0, "Round 1 (Easy)", 9, 9, 1, 4, 3, 1);
        SetDefaultRoundIfEmpty(1, "Round 2 (Medium)", 13, 13, 1, 3, 2, 2);
        SetDefaultRoundIfEmpty(2, "Round 3 (Hard)", 19, 19, 2, 2, 1, 3);
    }

    /// <summary>
    /// Writes a default round only when the slot is empty.
    /// </summary>
    private void SetDefaultRoundIfEmpty(
        int index,
        string name,
        int width,
        int height,
        int guardSpeed,
        int barricadeDuration,
        int maxBarricades,
        int minimaxDepth)
    {
        if (index < 0 || index >= rounds.Length)
            return;

        if (!string.IsNullOrWhiteSpace(rounds[index].roundName))
            return;

        rounds[index] = new RoundDefinition
        {
            roundName = name,
            gridWidth = width,
            gridHeight = height,
            guardSpeed = guardSpeed,
            barricadeDuration = barricadeDuration,
            maxBarricades = maxBarricades,
            minimaxDepth = minimaxDepth
        };
    }

    /// <summary>
    /// Checks whether the round list exists and has at least 3 entries.
    /// </summary>
    private bool HasValidRoundDefinitions()
    {
        return rounds != null && rounds.Length >= 3;
    }

    // =========================================================================
    // SCENE REFERENCES
    // =========================================================================

    /// <summary>
    /// Finds scene objects if they were not assigned in the Inspector.
    /// </summary>
    private void ResolveSceneReferences()
    {
        if (gridManager == null)
            gridManager = GridManager.Instance != null ? GridManager.Instance : FindFirstObjectByType<GridManager>();

        if (turnManager == null)
            turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindFirstObjectByType<TurnManager>();

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    // =========================================================================
    // MATCH CONTROL
    // =========================================================================

    /// <summary>
    /// Starts a fresh best-of-3 match.
    /// </summary>
    public void StartMatch()
    {
        ResolveSceneReferences();

        EnsureDefaultRounds();

        playerWins = 0;
        guardWins = 0;
        currentRoundIndex = 1;
        CurrentMatchState = MatchState.RoundActive;

        Debug.Log("[GameManager] Match started. Best 2 out of 3.");

        StartRound();
    }

    /// <summary>
    /// Starts the current round and applies its difficulty settings.
    /// </summary>
    public void StartRound()
    {
        ResolveSceneReferences();

        if (CurrentMatchState == MatchState.MatchOver)
        {
            Debug.LogWarning("[GameManager] StartRound() ignored because the match is already over.");
            return;
        }

        if (!TryGetCurrentRound(out RoundDefinition round))
        {
            Debug.LogError($"[GameManager] Cannot start round. Invalid round index: {currentRoundIndex}.");
            return;
        }

        CurrentMatchState = MatchState.RoundActive;

        Debug.Log($"[GameManager] Starting {round.roundName}.");
        Debug.Log(
            $"[GameManager] Grid {round.gridWidth}x{round.gridHeight}, " +
            $"GuardSpeed={round.guardSpeed}, BarricadeTTL={round.barricadeDuration}, " +
            $"MaxBarricades={round.maxBarricades}, MinimaxDepth={round.minimaxDepth}");

        if (gridManager != null)
        {
            gridManager.ClearAllBarricades();
            gridManager.ApplyRoundSettings(round.maxBarricades, round.barricadeDuration);
        }
        else
        {
            Debug.LogWarning("[GameManager] GridManager reference missing. Cannot apply grid settings.");
        }

        if (playerController != null)
        {
            playerController.ApplyRoundDifficulty(round.maxBarricades, round.barricadeDuration);
        }
        else
        {
            Debug.LogWarning("[GameManager] PlayerController reference missing. Cannot apply player difficulty settings.");
        }

        CurrentGuardSpeed = round.guardSpeed;
        CurrentMinimaxDepth = round.minimaxDepth;

        // TODO (Level Loader):
        // Use round.gridWidth and round.gridHeight to build or load the correct
        // maze for this round. A future level loader should:
        //   1. Replace the current tiles.
        //   2. Spawn the correct grid size.
        //   3. Place permanent walls, start points, and the exit.
        //   4. Register those positions with GridManager.

        // TODO (GuardController):
        // Pass CurrentGuardSpeed and CurrentMinimaxDepth to the GuardController
        // so the AI uses the correct difficulty for this round.

        if (turnManager != null)
        {
            turnManager.SetPlayerTurn(true);
        }
        else if (playerController != null)
        {
            playerController.SetPlayerTurn(true);
        }
    }

    /// <summary>
    /// Ends the current round, updates the score, and checks for a match winner.
    /// </summary>
    public void EndRound(bool playerWon)
    {
        ResolveSceneReferences();

        if (CurrentMatchState == MatchState.MatchOver)
        {
            Debug.LogWarning("[GameManager] EndRound() ignored because the match is already over.");
            return;
        }

        CurrentMatchState = MatchState.RoundTransition;

        if (playerWon)
        {
            playerWins++;
            Debug.Log($"[GameManager] Round {currentRoundIndex} won by PLAYER. Score: Player {playerWins} - Guard {guardWins}");
        }
        else
        {
            guardWins++;
            Debug.Log($"[GameManager] Round {currentRoundIndex} won by GUARD. Score: Player {playerWins} - Guard {guardWins}");
        }

        if (HasMatchWinner(out string winner))
        {
            CurrentMatchState = MatchState.MatchOver;
            Debug.Log($"[GameManager] MATCH OVER — {winner} wins the match.");
            return;
        }

        currentRoundIndex++;

        if (currentRoundIndex > 3)
        {
            CurrentMatchState = MatchState.MatchOver;
            Debug.LogWarning("[GameManager] Round index exceeded 3 without a match winner. Ending match defensively.");
            return;
        }

        Debug.Log($"[GameManager] Preparing next round: Round {currentRoundIndex}.");

        StartRound();
    }

    /// <summary>
    /// Returns true when either side has reached 2 wins.
    /// </summary>
    public bool HasMatchWinner(out string winner)
    {
        if (playerWins >= 2)
        {
            winner = "PLAYER";
            return true;
        }

        if (guardWins >= 2)
        {
            winner = "GUARD";
            return true;
        }

        winner = string.Empty;
        return false;
    }

    /// <summary>
    /// Gets the round data for the current round index.
    /// </summary>
    public bool TryGetCurrentRound(out RoundDefinition round)
    {
        round = default;

        if (!HasValidRoundDefinitions())
            return false;

        int index = currentRoundIndex - 1;
        if (index < 0 || index >= rounds.Length)
            return false;

        round = rounds[index];
        return true;
    }

    /// <summary>
    /// Restarts the match from the beginning.
    /// </summary>
    public void RestartMatch()
    {
        Debug.Log("[GameManager] RestartMatch() called.");
        StartMatch();
    }

    /// <summary>
    /// Ends the match immediately.
    /// </summary>
    public void ForceMatchOver()
    {
        CurrentMatchState = MatchState.MatchOver;
        Debug.Log("[GameManager] Match forced to end.");
    }
}