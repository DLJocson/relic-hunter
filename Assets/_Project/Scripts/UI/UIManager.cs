// =============================================================================
// UIManager.cs — Updates HUD canvas elements using TextMeshPro
// =============================================================================

using UnityEngine;
using TMPro;
using RelicHunter.Core;

namespace RelicHunter.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("TextMeshPro UI References")]
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI barricadeText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void UpdateScoreboard(int playerWins, int guardWins)
        {
            if (scoreText != null)
            {
                scoreText.text = $"<color=#00FFCC><b>THIEF:</b></color> {playerWins}   <color=#FF3366><b>GUARD:</b></color> {guardWins}";
            }
        }

        public void UpdateRoundInfo(string roundName, int width, int height)
        {
            if (roundText != null)
            {
                roundText.text = $"<b>{roundName.ToUpper()}</b>\n<size=18><color=#FFCC00>Size: {width}x{height}</color></size>";
            }
        }

        public void UpdateTurnNotice(TurnManager.TurnState activeTurn)
        {
            if (turnText == null) return;

            switch (activeTurn)
            {
                case TurnManager.TurnState.PlayerTurn:
                    turnText.text = "<color=#00FF99><b>YOUR TURN</b></color>";
                    break;
                case TurnManager.TurnState.GuardTurn:
                    turnText.text = "<color=#FFCC00><b>GUARD IS MOVING...</b></color>";
                    break;
                case TurnManager.TurnState.Processing:
                    turnText.text = "<color=#999999>PROCESSING...</color>";
                    break;
            }
        }

        public void UpdateBarricadeCount(int currentPlaced, int maxAllowed)
        {
            if (barricadeText != null)
            {
                int remaining = maxAllowed - currentPlaced;
                barricadeText.text = $"BARRICADES: <b>{remaining}/{maxAllowed}</b>";
            }
        }

        public void DisplayMatchWinner(string absoluteWinner)
        {
            if (turnText != null)
            {
                turnText.text = $"<color=cyan><b>MATCH OVER\nWINNER: {absoluteWinner}</b></color>";
            }
        }
    }
}