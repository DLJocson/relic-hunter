using UnityEngine;

namespace RelicHunter.Core
{
    /// <summary>
    /// Manages turn-based game logic and turn transitions.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public enum TurnState
        {
            PlayerTurn,
            GuardTurn
        }

        public TurnState currentTurn = TurnState.PlayerTurn;

        private void Start()
        {
            Debug.Log("TurnManager loaded");
        }

        public void EndPlayerTurn()
        {
            currentTurn = TurnState.GuardTurn;
        }

        public void EndGuardTurn()
        {
            currentTurn = TurnState.PlayerTurn;
        }
    }
}
