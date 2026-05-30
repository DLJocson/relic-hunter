// =============================================================================
// RoundFeedbackController.cs — Player sprites and round/match audio feedback.
// =============================================================================

using UnityEngine;
using RelicHunter.Core;
using RelicHunter.Maze;

public class RoundFeedbackController : MonoBehaviour
{
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [SerializeField] private AudioClip barricadeDeniedSound;

    [Header("Result Audio")]
    [Tooltip("Seconds to skip at the start of the win clip (leading silence).")]
    [SerializeField] private float winSoundStartOffset = 1.25f;

    private void Start()
    {
        if (playerStatus == null) playerStatus = FindFirstObjectByType<PlayerStatus>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRoundStarted += HandleRoundStarted;
            GameManager.Instance.OnRoundCompleted += HandleRoundCompleted;
            GameManager.Instance.OnMatchCompleted += HandleMatchCompleted;
        }

        StartBackgroundMusic();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnRoundStarted -= HandleRoundStarted;
        GameManager.Instance.OnRoundCompleted -= HandleRoundCompleted;
        GameManager.Instance.OnMatchCompleted -= HandleMatchCompleted;
    }

    private void HandleRoundStarted(int roundIndex, GameManager.RoundDefinition round)
    {
        if (playerStatus != null) playerStatus.ShowIdle();
        StartBackgroundMusic();
    }

    private void HandleRoundCompleted(int roundIndex, bool playerWon, int playerWins, int guardWins)
    {
        if (playerStatus != null)
        {
            if (playerWon) playerStatus.ShowWin();
            else playerStatus.ShowLose();
        }

        StopBackgroundMusic();

        if (playerWon)
            PlayResultClip(winSound, winSoundStartOffset);
        else
            PlayResultClip(loseSound, 0f);
    }

    private void HandleMatchCompleted(bool playerMatchWinner, int playerWins, int guardWins)
    {
        if (playerStatus != null)
        {
            if (playerMatchWinner) playerStatus.ShowWin();
            else playerStatus.ShowLose();
        }
    }

    private void StartBackgroundMusic()
    {
        if (audioSource == null || backgroundMusic == null) return;

        bool alreadyPlayingBgm = audioSource.isPlaying
            && audioSource.clip == backgroundMusic
            && audioSource.loop;

        if (alreadyPlayingBgm)
            return;

        audioSource.clip = backgroundMusic;
        audioSource.loop = true;

        if (!audioSource.isPlaying)
        {
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    private void StopBackgroundMusic()
    {
        if (audioSource == null) return;

        audioSource.Stop();
        audioSource.loop = false;
    }

    private void PlayResultClip(AudioClip clip, float startTime)
    {
        if (audioSource == null || clip == null) return;

        audioSource.clip = clip;
        audioSource.loop = false;
        audioSource.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.01f));
        audioSource.Play();
    }

    public float GetResultClipPlayDuration(bool playerWon)
    {
        AudioClip clip = playerWon ? winSound : loseSound;
        if (clip == null) return 0f;

        float startOffset = playerWon ? winSoundStartOffset : 0f;
        return Mathf.Max(0f, clip.length - startOffset);
    }

    public void SetPlayerStatus(PlayerStatus status)
    {
        if (status != null) playerStatus = status;
    }

    public void PlayBarricadeDeniedSound()
    {
        if (audioSource == null || barricadeDeniedSound == null) return;
        audioSource.PlayOneShot(barricadeDeniedSound);
    }
}
