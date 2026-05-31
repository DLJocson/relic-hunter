using UnityEngine;

namespace RelicHunter.Maze
{
    /// <summary>
    /// Swaps player visual sprites for idle, win, and lose states.
    /// </summary>
    public class PlayerStatus : MonoBehaviour
    {
        public GameObject idleSprite;
        public GameObject winSprite;
        public GameObject loseSprite;

        void Start()
        {
            ShowIdle();
        }

        /// <summary>Shows the idle pose sprite.</summary>
        public void ShowIdle()
        {
            idleSprite.SetActive(true);
            winSprite.SetActive(false);
            loseSprite.SetActive(false);
        }

        /// <summary>Shows the round-win pose sprite.</summary>
        public void ShowWin()
        {
            idleSprite.SetActive(false);
            winSprite.SetActive(true);
            loseSprite.SetActive(false);
            Debug.Log("Nanalo ka!");
        }

        /// <summary>Shows the round-loss pose sprite.</summary>
        public void ShowLose()
        {
            idleSprite.SetActive(false);
            winSprite.SetActive(false);
            loseSprite.SetActive(true);
            Debug.Log("Natalo ka!");
        }
    }
}
