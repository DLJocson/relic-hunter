using System;
using System.Collections;
using UnityEngine;
using RelicHunter.Core;
using RelicHunter.Player;
using RelicHunter.Enemy;

/// <summary>
/// Match and round lifecycle: scoring, maze generation handoff, and game-over flow.
/// </summary>
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
    [SerializeField] private MazeGridBridge mazeGridBridge;
    [SerializeField] private RoundFeedbackController roundFeedback;

    [Header("Entity Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject guardPrefab;

    [Header("Round Definitions")]
    [SerializeField] private RoundDefinition[] rounds = new RoundDefinition[3];

    [Header("Match Tracking")]
    [SerializeField] private int currentRoundIndex = 1;
    [SerializeField] private int playerWins = 0;
    [SerializeField] private int guardWins = 0;

    [Header("Runtime Round Settings")]
    public int CurrentMinimaxDepth { get; private set; } = 1;
    public float CurrentGuardSpeed { get; private set; } = 1f;
    public int CurrentBarricadeDuration { get; private set; } = 6;
    public int CurrentMaxBarricades { get; private set; } = 4;

    public MatchState CurrentMatchState { get; private set; } = MatchState.NotStarted;

    public event Action<int, RoundDefinition> OnRoundStarted;
    public event Action<int, bool, int, int> OnRoundCompleted;
    public event Action<bool, int, int> OnMatchCompleted;

    private bool waitingForMazeGeneration;
    private Coroutine roundTransitionCoroutine;
    private bool isStartingGameSequence;

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

        if (gridManager != null)
            gridManager.DeactivateRoundEntities();

        if (RelicHunter.UI.UIManager.Instance != null)
            RelicHunter.UI.UIManager.Instance.ShowMainMenu();
    }

    private void EnsureDefaultRounds()
    {
        if (rounds == null || rounds.Length < 3)
            rounds = new RoundDefinition[3];

        SetDefaultRoundIfEmpty(0, "Round 1 (Easy)", 9, 9, 1f, 6, 4, 1);
        SetDefaultRoundIfEmpty(1, "Round 2 (Medium)", 12, 12, 1.25f, 5, 3, 2);
        SetDefaultRoundIfEmpty(2, "Round 3 (Difficult)", 15, 15, 1.34f, 4, 2, 3);
    }

    private void SetDefaultRoundIfEmpty(
        int index,
        string name,
        int width,
        int height,
        float speed,
        int duration,
        int maxB,
        int depth)
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
            minimaxDepth = depth
        };
    }

    /// <summary>
    /// Begins a new match from the main menu (round-start UI, then maze generation).
    /// </summary>
    public void StartGameSequence()
    {
        ResolveSceneReferences();

        if (CurrentMatchState != MatchState.NotStarted || isStartingGameSequence)
            return;

        isStartingGameSequence = true;

        if (roundTransitionCoroutine != null)
        {
            StopCoroutine(roundTransitionCoroutine);
            roundTransitionCoroutine = null;
        }

        if (RelicHunter.UI.UIManager.Instance != null)
            RelicHunter.UI.UIManager.Instance.ShowRoundStart();

        StartCoroutine(GenerateAndStartMatchRoutine());
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void RestartMatch()
    {
        if (roundTransitionCoroutine != null)
        {
            StopCoroutine(roundTransitionCoroutine);
            roundTransitionCoroutine = null;
        }

        StopAllCoroutines();

        playerWins = 0;
        guardWins = 0;
        currentRoundIndex = 1;
        CurrentMatchState = MatchState.NotStarted;
        waitingForMazeGeneration = false;
        isStartingGameSequence = false;

        if (RelicHunter.UI.UIManager.Instance != null)
            RelicHunter.UI.UIManager.Instance.UpdateScore(0, 0);

        ResolveSceneReferences();

        if (gridManager != null)
            gridManager.ResetForMatchRestart();

        StartGameSequence();
    }

    private IEnumerator GenerateAndStartMatchRoutine()
    {
        yield return new WaitForSeconds(3f);

        playerWins = 0;
        guardWins = 0;
        currentRoundIndex = 1;

        StartRound();
        isStartingGameSequence = false;
    }

    public void StartRound()
    {
        ResolveSceneReferences();

        int index = currentRoundIndex - 1;
        if (index < 0 || index >= rounds.Length)
            return;

        RoundDefinition round = SanitizeRound(rounds[index], index);
        rounds[index] = round;
        CurrentMatchState = MatchState.RoundTransition;
        waitingForMazeGeneration = mazeGridBridge != null;

        CurrentGuardSpeed = round.guardSpeed;
        CurrentBarricadeDuration = round.barricadeDuration;
        CurrentMaxBarricades = round.maxBarricades;
        CurrentMinimaxDepth = round.minimaxDepth;

        if (turnManager != null)
            turnManager.currentTurn = TurnManager.TurnState.Processing;

        if (mazeGridBridge == null)
        {
            Debug.LogError("[GameManager] MazeGridBridge is required for round start.");
            return;
        }

        if (gridManager != null)
        {
            gridManager.ConfigureEntityPrefabs(playerPrefab, guardPrefab);
            gridManager.PrepareForRoundGeneration();
        }

        int seed = BuildRoundSeed(currentRoundIndex);
        mazeGridBridge.BeginRound(round, seed, () => CompleteRoundStart(round));
    }

    private void CompleteRoundStart(RoundDefinition round)
    {
        waitingForMazeGeneration = false;
        CurrentMatchState = MatchState.RoundActive;

        if (gridManager != null)
            gridManager.ApplyRoundSettings(round.maxBarricades, round.barricadeDuration);

        ResolveSceneReferences();

        if (mazeGridBridge != null)
            mazeGridBridge.RefreshEntityControllerReferences();

        ResetPositionsForNewRound();

        if (gridManager != null)
            gridManager.ActivateRoundEntities();

        if (guardController != null)
            guardController.SetGuardSpeed(round.guardSpeed);

        if (RelicHunter.UI.UIManager.Instance != null)
        {
            RelicHunter.UI.UIManager.Instance.UpdateRound(
                currentRoundIndex,
                GetRoundDifficultyLabel(round.gridWidth, round.gridHeight),
                round.gridWidth,
                round.gridHeight,
                round.guardSpeed,
                round.barricadeDuration);
            RelicHunter.UI.UIManager.Instance.UpdateScore(playerWins, guardWins);
            RelicHunter.UI.UIManager.Instance.UpdateBarricades(0, round.maxBarricades);
            RelicHunter.UI.UIManager.Instance.ShowHUD();
        }

        OnRoundStarted?.Invoke(currentRoundIndex, round);

        Debug.Log($"<color=orange><b>===================================================</b></color>");
        Debug.Log($"<color=lime><b>[STARTING {round.roundName.ToUpper()}]</b></color>");
        Debug.Log($"<color=yellow>Grid Dimensions: {round.gridWidth} x {round.gridHeight}</color>");
        if (gridManager != null)
            Debug.Log($"<color=yellow>Exit Tile: {gridManager.exitPos}</color>");
        Debug.Log($"<color=white>Current Match Score -> Player: {playerWins} | Guard AI: {guardWins}</color>");
        Debug.Log($"<color=orange><b>===================================================</b></color>");

        if (turnManager != null)
        {
            turnManager.currentTurn = TurnManager.TurnState.PlayerTurn;
            RelicHunter.UI.UIManager.Instance?.UpdateTurnNotice(turnManager.currentTurn);
        }
    }

    private RoundDefinition SanitizeRound(RoundDefinition round, int index)
    {
        if (round.gridWidth >= 3 && round.gridHeight >= 3)
            return round;

        EnsureDefaultRounds();
        if (index >= 0 && index < rounds.Length)
        {
            RoundDefinition fallback = rounds[index];
            if (fallback.gridWidth >= 3 && fallback.gridHeight >= 3)
                return fallback;
        }

        Debug.LogWarning($"[GameManager] Round {index + 1} had invalid dimensions; using 9x9 defaults.");
        return new RoundDefinition
        {
            roundName = $"Round {index + 1}",
            gridWidth = 9,
            gridHeight = 9,
            guardSpeed = 1f,
            barricadeDuration = 6,
            maxBarricades = 4,
            minimaxDepth = 1
        };
    }

    private int BuildRoundSeed(int roundIndex)
    {
        unchecked
        {
            int timeSeed = (int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF);
            return timeSeed ^ (roundIndex * 7919);
        }
    }

    private static string GetRoundDifficultyLabel(int gridWidth, int gridHeight)
    {
        if (gridWidth >= 15 || gridHeight >= 15)
            return "HARD";

        if (gridWidth >= 12 || gridHeight >= 12)
            return "MEDIUM";

        return "EASY";
    }

    private void ResetPositionsForNewRound()
    {
        if (gridManager == null)
            return;

        if (playerController != null)
        {
            Vector2Int playerStart = gridManager.playerPos;
            playerController.gridPosition = playerStart;
            playerController.SnapTransformToGrid();
        }

        Vector2Int guardStart = gridManager.guardPos;
        if (guardStart.x < 0 || guardStart.x >= gridManager.Width || guardStart.y < 0 || guardStart.y >= gridManager.Height)
            guardStart = new Vector2Int(gridManager.Width - 1, gridManager.Height - 1);

        if (guardController != null)
            guardController.ResetToPosition(guardStart);

        gridManager.guardPos = guardStart;
    }

    public void EndRound(bool playerWon)
    {
        if (CurrentMatchState == MatchState.MatchOver)
            return;

        if (waitingForMazeGeneration)
            return;

        CurrentMatchState = MatchState.RoundTransition;

        int index = currentRoundIndex - 1;
        string activeRoundName = (index >= 0 && index < rounds.Length) ? rounds[index].roundName : $"Round {currentRoundIndex}";

        if (playerWon) playerWins++;
        else guardWins++;

        if (RelicHunter.UI.UIManager.Instance != null)
        {
            RelicHunter.UI.UIManager.Instance.ShowRoundWinner(playerWon);
            RelicHunter.UI.UIManager.Instance.UpdateScore(playerWins, guardWins); // Updates numbers instantly
        }

        OnRoundCompleted?.Invoke(currentRoundIndex, playerWon, playerWins, guardWins);

        string roundWinner = playerWon ? "PLAYER (THIEF)" : "GUARD AI";
        string roundColor = playerWon ? "green" : "red";
        Debug.Log($"<color={roundColor}><b>[ROUND COMPLETE]</b> {roundWinner} has won {activeRoundName}!</color>");

        if (playerWins >= 2 || guardWins >= 2)
        {
            CurrentMatchState = MatchState.MatchOver;

            if (turnManager != null)
                turnManager.currentTurn = TurnManager.TurnState.Processing;

            bool playerMatchWinner = playerWins >= 2;

            if (RelicHunter.UI.UIManager.Instance != null)
            {
                RelicHunter.UI.UIManager.Instance.ShowGameOver(playerMatchWinner);
            }

            Debug.Log($"<color=cyan><b>===================================================</b></color>");
            Debug.Log($"<color=cyan><b>[MATCH OVER - FINAL SERIES WINNER]</b></color>");
            Debug.Log($"<color=cyan><b>Final Winner: {(playerMatchWinner ? "PLAYER (THIEF)" : "GUARD AI")}</b></color>");
            Debug.Log($"<color=cyan><b>Final Series Score -> Player: {playerWins} | Guard: {guardWins}</b></color>");
            Debug.Log($"<color=cyan><b>===================================================</b></color>");

            OnMatchCompleted?.Invoke(playerMatchWinner, playerWins, guardWins);
            return;
        }

        if (roundTransitionCoroutine != null)
            StopCoroutine(roundTransitionCoroutine);

        roundTransitionCoroutine = StartCoroutine(TransitionToNextRoundAfterResultAudio(playerWon));
    }

    private IEnumerator TransitionToNextRoundAfterResultAudio(bool playerWonRound)
    {
        float waitDuration = GetResultAudioDuration(playerWonRound);
        if (waitDuration > 0f)
            yield return new WaitForSeconds(waitDuration);

        currentRoundIndex++;
        ResolveSceneReferences();
        roundFeedback?.StartBackgroundMusic();
        StartRound();
        roundTransitionCoroutine = null;
    }

    private float GetResultAudioDuration(bool playerWonRound)
    {
        ResolveSceneReferences();
        if (roundFeedback != null)
            return roundFeedback.GetResultClipPlayDuration(playerWonRound);

        return 0f;
    }

    private void ResolveSceneReferences()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
        gridManager?.ConfigureEntityPrefabs(playerPrefab, guardPrefab);

        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (guardController == null)
            guardController = FindFirstObjectByType<GuardController>(FindObjectsInactive.Include);
        if (mazeGridBridge == null) mazeGridBridge = FindFirstObjectByType<MazeGridBridge>();
        if (roundFeedback == null) roundFeedback = FindFirstObjectByType<RoundFeedbackController>();
    }
}
