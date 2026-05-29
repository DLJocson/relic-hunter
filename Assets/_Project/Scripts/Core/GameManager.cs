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
        public float wallDensity;
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

    [Header("Runtime Round Settings")]
    public int CurrentMinimaxDepth { get; private set; } = 1;
    public float CurrentGuardSpeed { get; private set; } = 1f;
    public int CurrentBarricadeDuration { get; private set; } = 4;
    public int CurrentMaxBarricades { get; private set; } = 3;
    public float CurrentWallDensity { get; private set; } = 0.18f;

    public MatchState CurrentMatchState { get; private set; } = MatchState.NotStarted;

    public event Action<int, RoundDefinition> OnRoundStarted;
    public event Action<int, bool, int, int> OnRoundCompleted;
    public event Action<bool, int, int> OnMatchCompleted;

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

        SetDefaultRoundIfEmpty(0, "Round 1 (Easy)", 9, 9, 1f, 4, 3, 1, 0.12f);
        SetDefaultRoundIfEmpty(1, "Round 2 (Medium)", 12, 12, 1.5f, 3, 2, 2, 0.18f);
        SetDefaultRoundIfEmpty(2, "Round 3 (Hard)", 15, 15, 2f, 2, 1, 3, 0.24f);
    }

    private void SetDefaultRoundIfEmpty(
        int index,
        string name,
        int width,
        int height,
        float speed,
        int duration,
        int maxB,
        int depth,
        float wallDensity)
    {
        if (index < 0 || index >= rounds.Length || !string.IsNullOrWhiteSpace(rounds[index].roundName))
            return;

        rounds[index] = new RoundDefinition
        {
            roundName = name,
            gridWidth = width,
            gridHeight = height,
            guardSpeed = speed,
            barricadeDuration = duration,
            maxBarricades = maxB,
            minimaxDepth = depth,
            wallDensity = wallDensity
        };
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
        if (index < 0 || index >= rounds.Length)
            return;

        RoundDefinition round = rounds[index];
        CurrentMatchState = MatchState.RoundActive;

        CurrentGuardSpeed = round.guardSpeed;
        CurrentBarricadeDuration = round.barricadeDuration;
        CurrentMaxBarricades = round.maxBarricades;
        CurrentMinimaxDepth = round.minimaxDepth;
        CurrentWallDensity = round.wallDensity;

        if (gridManager != null)
        {
            gridManager.UpdateGridDimensions(round.gridWidth, round.gridHeight);
            gridManager.ApplyRoundSettings(round.maxBarricades, round.barricadeDuration);

            Vector2Int playerStart = new Vector2Int(0, 0);
            Vector2Int guardStart = new Vector2Int(round.gridWidth - 1, round.gridHeight - 1);
            Vector2Int exitTile = new Vector2Int(round.gridWidth - 1, 0);

            int seed = BuildRoundSeed(currentRoundIndex);
            gridManager.GenerateProceduralWalls(seed, round.wallDensity, playerStart, guardStart, exitTile);
        }

        ResetPositionsForNewRound();

        if (guardController != null)
        {
            guardController.SetGuardSpeed(round.guardSpeed);
        }

        OnRoundStarted?.Invoke(currentRoundIndex, round);

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

    private int BuildRoundSeed(int roundIndex)
    {
        unchecked
        {
            int timeSeed = (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);
            return timeSeed ^ (roundIndex * 7919);
        }
    }

    private void ResetPositionsForNewRound()
    {
        if (gridManager == null)
            return;

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
        if (CurrentMatchState == MatchState.MatchOver)
            return;

        CurrentMatchState = MatchState.RoundTransition;

        int index = currentRoundIndex - 1;
        string activeRoundName = (index >= 0 && index < rounds.Length) ? rounds[index].roundName : $"Round {currentRoundIndex}";

        if (playerWon) playerWins++;
        else guardWins++;

        OnRoundCompleted?.Invoke(currentRoundIndex, playerWon, playerWins, guardWins);

        string roundWinner = playerWon ? "👤 PLAYER (THIEF)" : "🤖 GUARD AI";
        string roundColor = playerWon ? "green" : "red";
        Debug.Log($"<color={roundColor}><b>[ROUND COMPLETE]</b> {roundWinner} has won {activeRoundName}!</color>");

        if (playerWins >= 2 || guardWins >= 2)
        {
            CurrentMatchState = MatchState.MatchOver;

            if (turnManager != null)
                turnManager.currentTurn = TurnManager.TurnState.Processing;

            bool playerMatchWinner = playerWins >= 2;
            string absoluteWinner = playerMatchWinner ? "PLAYER (THIEF)" : "GUARD AI";

            Debug.Log($"<color=cyan><b>===================================================</b></color>");
            Debug.Log($"<color=cyan><b>🏆🏆🏆 [MATCH OVER - FINAL SERIES WINNER] 🏆🏆🏆</b></color>");
            Debug.Log($"<color=cyan><b>👑 Final Winner: {absoluteWinner}</b></color>");
            Debug.Log($"<color=cyan><b>💯 Final Series Score -> Player: {playerWins} | Guard: {guardWins}</b></color>");
            Debug.Log($"<color=cyan><b>===================================================</b></color>");

            OnMatchCompleted?.Invoke(playerMatchWinner, playerWins, guardWins);
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