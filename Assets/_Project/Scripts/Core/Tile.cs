using UnityEngine;

public class Tile : MonoBehaviour
{
    public int xCoordinate;
    public int yCoordinate;

    public void Setup(int x, int y)
    {
        this.xCoordinate = x;
        this.yCoordinate = y;
        gameObject.name = $"Tile ({x}, {y})";
    }
}