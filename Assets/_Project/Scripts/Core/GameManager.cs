using UnityEngine;

namespace RelicHunter.Core
{
    /// <summary>
    /// Main game manager that handles overall game state and initialization.
    /// </summary>
    public class GameManager : MonoBehaviour
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
    }
}
