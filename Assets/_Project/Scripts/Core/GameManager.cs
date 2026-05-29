// =============================================================================
// GameManager.cs — Handles game mechanics
// =============================================================================

using System;
using UnityEngine;
using RelicHunter.Core;
using RelicHunter.Player;

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
    [SerializeField] private int currentRoundIndex = 1; 
    [SerializeField] private int playerWins = 0;
    [SerializeField] private int guardWins = 0;

    public MatchState CurrentMatchState { get; private set; } = MatchState.NotStarted;

    public int CurrentGuardSpeed { get; private set; }
    public int CurrentMinimaxDepth { get; private set; }

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

        SetDefaultRoundIfEmpty(0, "Round 1 (Easy)", 9, 9, 1, 4, 3, 1);
        SetDefaultRoundIfEmpty(1, "Round 2 (Medium)", 13, 13, 1, 3, 2, 2);
        SetDefaultRoundIfEmpty(2, "Round 3 (Hard)", 19, 19, 2, 2, 1, 3);
    }

    private void SetDefaultRoundIfEmpty(int index, string name, int width, int height, int speed, int duration, int maxB, int depth)
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

        CurrentGuardSpeed = round.guardSpeed;
        CurrentMinimaxDepth = round.minimaxDepth;

        Debug.Log($"[GameManager] Match state activated: {round.roundName}");
        
        if (turnManager != null)
        {
            turnManager.currentTurn = TurnManager.TurnState.PlayerTurn;
        }
    }

    public void EndRound(bool playerWon)
    {
        if (CurrentMatchState == MatchState.MatchOver) return;
        CurrentMatchState = MatchState.RoundTransition;

        if (playerWon) playerWins++;
        else guardWins++;

        Debug.Log($"[GameManager] Round complete. Score: Player {playerWins} - Guard {guardWins}");

        if (playerWins >= 2 || guardWins >= 2)
        {
            CurrentMatchState = MatchState.MatchOver;
            Debug.Log("[GameManager] Match has resolved a final winner.");
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
    }
}