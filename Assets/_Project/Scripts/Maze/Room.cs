using System.Collections.Generic;
using UnityEngine;

namespace RelicHunter.Maze
{
public class Room : MonoBehaviour
{
    public enum Directions { TOP, RIGHT, BOTTOM, LEFT, NONE }

    [SerializeField] GameObject topWall;
    [SerializeField] GameObject rightWall;
    [SerializeField] GameObject bottomWall;
    [SerializeField] GameObject leftWall;

    Dictionary<Directions, GameObject> walls = new Dictionary<Directions, GameObject>();
    
    public Vector2Int Index { get; set; }
    public bool visited { get; set; } = false;

    private void Awake()
    {
        if (!topWall) topWall = transform.Find("Top")?.gameObject;
        if (!rightWall) rightWall = transform.Find("Right")?.gameObject;
        if (!bottomWall) bottomWall = transform.Find("Bottom")?.gameObject;
        if (!leftWall) leftWall = transform.Find("Left")?.gameObject;

        walls[Directions.TOP] = topWall;
        walls[Directions.RIGHT] = rightWall;
        walls[Directions.BOTTOM] = bottomWall;
        walls[Directions.LEFT] = leftWall;
    }

    public void SetDirFlag(Directions dir, bool flag)
    {
        if (walls.ContainsKey(dir) && walls[dir] != null)
            walls[dir].SetActive(flag);
    }

    public bool IsWallActive(Directions dir)
    {
    return walls.ContainsKey(dir) && walls[dir] != null && walls[dir].activeSelf;
    }
}
}