// =============================================================================
// GameManager.cs — Handles game mechanics
// =============================================================================

using System;
using UnityEngine;
using RelicHunter.Core;
using RelicHunter.Player;
using RelicHunter.Enemy;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum MatchState
    {
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
        public float guardSpeed;
        public int barricadeDuration;
        public int maxBarricades;
        public int minimaxDepth;
    }

    [Header("Scene References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GuardController guardController;

    [Header("Round Definitions")]
    [SerializeField] private RoundDefinition[] rounds = new RoundDefinition[3];

    [Header("Match Tracking")]
    [SerializeField] private int currentRoundIndex = 1;
    [SerializeField] private int playerWins = 0;
    [SerializeField] private int guardWins = 0;

    public MatchState CurrentMatchState { get; private set; } = MatchState.NotStarted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureDefaultRounds();
    }

    private void Start()
    {
        ResolveSceneReferences();
        if (CurrentMatchState == MatchState.NotStarted)
        {
            StartMatch();
        }
    }

    private void EnsureDefaultRounds()
    {
        if (rounds == null || rounds.Length < 3)
            rounds = new RoundDefinition[3];

        SetDefaultRoundIfEmpty(0, "Round 1 (Easy)", 9, 9, 1f, 4, 3, 1);
        SetDefaultRoundIfEmpty(1, "Round 2 (Medium)", 12, 12, 1.5f, 3, 2, 2);
        SetDefaultRoundIfEmpty(2, "Round 3 (Hard)", 15, 15, 2f, 2, 1, 3);
    }

    private void SetDefaultRoundIfEmpty(int index, string name, int width, int height, float speed, int duration, int maxB, int depth)
    {
        if (index < 0 || index >= rounds.Length || !string.IsNullOrWhiteSpace(rounds[index].roundName)) return;
        rounds[index] = new RoundDefinition { roundName = name, gridWidth = width, gridHeight = height, guardSpeed = speed, barricadeDuration = duration, maxBarricades = maxB, minimaxDepth = depth };
    }

    public void StartMatch()
    {
        playerWins = 0;
        guardWins = 0;
        currentRoundIndex = 1;
        StartRound();
    }

    public void StartRound()
    {
        ResolveSceneReferences();
        int index = currentRoundIndex - 1;
        if (index < 0 || index >= rounds.Length) return;

        RoundDefinition round = rounds[index];
        CurrentMatchState = MatchState.RoundActive;

        // 1. Reinitialize and rebuild the board dimensions dynamically
        if (gridManager != null)
        {
            gridManager.UpdateGridDimensions(round.gridWidth, round.gridHeight);
            gridManager.ApplyRoundSettings(round.maxBarricades, round.barricadeDuration);
        }

        // 2. Teleport characters based on the new boundaries
        ResetPositionsForNewRound();

        // 3. Set guard speed for this round
        if (guardController != null)
        {
            guardController.SetGuardSpeed(round.guardSpeed);
        }

        // ADDED EDITS: Log details for round start, type, dimension settings, and series scores
        Debug.Log($"<color=orange><b>===================================================</b></color>");
        Debug.Log($"<color=lime><b>[STARTING {round.roundName.ToUpper()}]</b></color>");
        Debug.Log($"<color=yellow>📐 Grid Dimensions: {round.gridWidth} x {round.gridHeight}</color>");
        Debug.Log($"<color=white>📊 Current Match Score -> 👤 Player: {playerWins} | 🤖 Guard AI: {guardWins}</color>");
        Debug.Log($"<color=orange><b>===================================================</b></color>");

        if (turnManager != null)
        {
            turnManager.currentTurn = TurnManager.TurnState.PlayerTurn;
        }
    }

    private void ResetPositionsForNewRound()
    {
        if (gridManager == null) return;

        if (playerController != null)
        {
            playerController.gridPosition = new Vector2Int(0, 0);
            playerController.SnapTransformToGrid();
            gridManager.playerPos = new Vector2Int(0, 0);
        }

        Vector2Int guardStart = new Vector2Int(gridManager.Width - 1, gridManager.Height - 1);
        if (guardController != null)
        {
            guardController.ResetToPosition(guardStart);
        }
        gridManager.guardPos = guardStart;
    }

    public void EndRound(bool playerWon)
    {
        if (CurrentMatchState == MatchState.MatchOver) return;
        CurrentMatchState = MatchState.RoundTransition;

        int index = currentRoundIndex - 1;
        string activeRoundName = (index >= 0 && index < rounds.Length) ? rounds[index].roundName : $"Round {currentRoundIndex}";

        if (playerWon) playerWins++;
        else guardWins++;

        // ADDED EDITS: Cleanly print the concrete winner of this round to the console
        string roundWinner = playerWon ? "👤 PLAYER (THIEF)" : "🤖 GUARD AI";
        string roundColor = playerWon ? "green" : "red";
        Debug.Log($"<color={roundColor}><b>[ROUND COMPLETE]</b> {roundWinner} has won {activeRoundName}!</color>");

        if (playerWins >= 2 || guardWins >= 2)
        {
            CurrentMatchState = MatchState.MatchOver;

            if (turnManager != null)
                turnManager.currentTurn = TurnManager.TurnState.Processing; // Lock turn system permanently

            string absoluteWinner = playerWins >= 2 ? "PLAYER (THIEF)" : "GUARD AI";

            // ADDED EDITS: Enhanced prominence for the final championship message block
            Debug.Log($"<color=cyan><b>===================================================</b></color>");
            Debug.Log($"<color=cyan><b>🏆🏆🏆 [MATCH OVER - FINAL SERIES WINNER] 🏆🏆🏆</b></color>");
            Debug.Log($"<color=cyan><b>👑 Final Winner: {absoluteWinner}</b></color>");
            Debug.Log($"<color=cyan><b>💯 Final Series Score -> Player: {playerWins} | Guard: {guardWins}</b></color>");
            Debug.Log($"<color=cyan><b>===================================================</b></color>");
            return;
        }

        currentRoundIndex++;
        StartRound();
    }

    private void ResolveSceneReferences()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (guardController == null) guardController = FindFirstObjectByType<GuardController>();
    }
}