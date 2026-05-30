using UnityEngine;

namespace RelicHunter.Maze
{
public class PlayerStatus : MonoBehaviour
{
    public GameObject idleSprite;
    public GameObject winSprite;
    public GameObject loseSprite;

    void Start()
    {
        ShowIdle();
    }

    public void ShowIdle()
    {
        idleSprite.SetActive(true);
        winSprite.SetActive(false);
        loseSprite.SetActive(false);
    }

    public void ShowWin()
    {
        idleSprite.SetActive(false);
        winSprite.SetActive(true);
        loseSprite.SetActive(false);
        Debug.Log("Nanalo ka!");
    }

    public void ShowLose()
    {
        idleSprite.SetActive(false);
        winSprite.SetActive(false);
        loseSprite.SetActive(true);
        Debug.Log("Natalo ka!");
    }
}
}