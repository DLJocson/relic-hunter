using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMaze : MonoBehaviour
{
    public enum Difficulty { Easy, Medium, Hard }
    
    [Header("Settings")]
    [SerializeField] Difficulty currentDifficulty = Difficulty.Easy;
    [SerializeField] GameObject roomPrefab;
    
    Room[,] rooms = null;
    int numX, numY;
    float roomWidth, roomHeight;
    Stack<Room> stack = new Stack<Room>();
    bool generating = false;

    private void Start()
    {
        GetRoomSize();
        CreateMaze();
    }

    private void GetRoomSize()
    {
        SpriteRenderer[] spriteRenderers = roomPrefab.GetComponentsInChildren<SpriteRenderer>();
        Vector3 minBounds = Vector3.positiveInfinity;
        Vector3 maxBounds = Vector3.negativeInfinity;
        foreach (SpriteRenderer ren in spriteRenderers)
        {
            minBounds = Vector3.Min(minBounds, ren.bounds.min);
            maxBounds = Vector3.Max(maxBounds, ren.bounds.max);
        }
        roomWidth = (maxBounds.x - minBounds.x); 
        roomHeight = (maxBounds.y - minBounds.y);
    }

    private void UpdateGridSize()
    {
        switch (currentDifficulty)
        {
            case Difficulty.Easy:   numX = 9;  numY = 9;  break;
            case Difficulty.Medium: numX = 12; numY = 12; break;
            case Difficulty.Hard:   numX = 15; numY = 15; break;
        }
    }

    public void CreateMaze()
    {
        if (generating) return;
        UpdateGridSize();
        
        foreach (Transform child in transform) Destroy(child.gameObject);
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

        rooms = new Room[numX, numY];
        for (int i = 0; i < numX; ++i)
        {
            for (int j = 0; j < numY; ++j)
            {
                GameObject room = Instantiate(roomPrefab, new Vector3(i * roomWidth, j * roomHeight, 0.0f), Quaternion.identity, transform);
                room.name = $"Room_{i}_{j}";
                rooms[i, j] = room.GetComponent<Room>();
                rooms[i, j].Index = new Vector2Int(i, j);
            }
        }
        
        foreach(var room in rooms) {
            room.SetDirFlag(Room.Directions.TOP, true);
            room.SetDirFlag(Room.Directions.RIGHT, true);
            room.SetDirFlag(Room.Directions.BOTTOM, true);
            room.SetDirFlag(Room.Directions.LEFT, true);
        }

        rooms[0, 0].SetDirFlag(Room.Directions.BOTTOM, false);
        rooms[numX - 1, numY - 1].SetDirFlag(Room.Directions.TOP, false);

        stack.Clear();
        stack.Push(rooms[0, 0]);
        rooms[0, 0].visited = true;

        SetCamera();
        StartCoroutine(Coroutine_Generate());
    }

    private void SetCamera()
    {
        float centerX = ((numX - 1) * roomWidth) / 2f;
        float centerY = ((numY - 1) * roomHeight) / 2f;
        Camera.main.transform.position = new Vector3(centerX, centerY, -10f);
        float maxDim = Mathf.Max(numX * roomWidth, numY * roomHeight);
        Camera.main.orthographicSize = maxDim * 0.6f;
    }

    private void RemoveRoomWall(int x, int y, Room.Directions dir)
    {
        bool isStart = (x == 0 && y == 0 && dir == Room.Directions.BOTTOM);
        bool isExit = (x == numX - 1 && y == numY - 1 && dir == Room.Directions.TOP);

        if (!isStart && !isExit)
        {
            if (dir == Room.Directions.BOTTOM && y == 0) return;
            if (dir == Room.Directions.TOP && y == numY - 1) return;
            if (dir == Room.Directions.LEFT && x == 0) return;
            if (dir == Room.Directions.RIGHT && x == numX - 1) return;
        }

        rooms[x, y].SetDirFlag(dir, false);
        int nx = x, ny = y;
        Room.Directions opp = Room.Directions.NONE;

        if (dir == Room.Directions.TOP && y < numY - 1) { opp = Room.Directions.BOTTOM; ny++; }
        else if (dir == Room.Directions.RIGHT && x < numX - 1) { opp = Room.Directions.LEFT; nx++; }
        else if (dir == Room.Directions.BOTTOM && y > 0) { opp = Room.Directions.TOP; ny--; }
        else if (dir == Room.Directions.LEFT && x > 0) { opp = Room.Directions.RIGHT; nx--; }

        if (opp != Room.Directions.NONE) rooms[nx, ny].SetDirFlag(opp, false);
    }

    IEnumerator Coroutine_Generate()
    {
        generating = true;
        while (!GenerateStep()) yield return null;
        
        
        int extraConnections = (numX * numY) / 2; 
        for(int i = 0; i < extraConnections; i++)
        {
            int x = UnityEngine.Random.Range(1, numX - 1);
            int y = UnityEngine.Random.Range(1, numY - 1);
            RemoveRoomWall(x, y, (Room.Directions)UnityEngine.Random.Range(0, 4));
        }

        
        for(int i = 0; i < 3; i++)
        {
            int rx = UnityEngine.Random.Range(1, numX - 2);
            int ry = UnityEngine.Random.Range(1, numY - 2);
            for(int x = rx; x <= rx + 1; x++)
            {
                for(int y = ry; y <= ry + 1; y++)
                {
                    rooms[x, y].SetDirFlag(Room.Directions.TOP, false);
                    rooms[x, y].SetDirFlag(Room.Directions.RIGHT, false);
                    rooms[x, y].SetDirFlag(Room.Directions.BOTTOM, false);
                    rooms[x, y].SetDirFlag(Room.Directions.LEFT, false);
                }
            }
        }

        
        EnsureMazeFullySafe();

        generating = false;
    }

    public bool TryPlaceBarricade(int x, int y, Room.Directions dir)
    {
        if (!IsValidWallTarget(x, y, dir)) return false;
        if (IsSpecialOpening(x, y, dir)) return false;

        if (rooms[x, y].IsWallActive(dir)) return true;

        if (!WouldKeepRequiredOpenExitsIfWallStateChanged(x, y, dir, true)) return false;
        if (!WouldRemainConnectedIfWallStateChanged(x, y, dir, true)) return false;

        SetWallState(x, y, dir, true);
        return true;
    }

    private void EnsureMazeFullySafe()
    {
        int maxPasses = numX * numY * 8;

        for (int pass = 0; pass < maxPasses; pass++)
        {
            bool changed = false;
            changed |= EnsureMinimumOpenExits();
            changed |= EnsureNoBridges();
            changed |= EnsureMazeConnected();

            if (IsMazeSafe()) return;
            if (!changed) break;
        }

        EnsureMinimumOpenExits();
        EnsureNoBridges();
        EnsureMazeConnected();
    }

    private bool EnsureMazeConnected()
    {
        bool changed = false;
        while (!IsMazeConnected())
        {
            bool[,] reachable = GetReachableFromStart();
            bool repaired = false;

            for (int x = 0; x < numX && !repaired; x++)
            {
                for (int y = 0; y < numY && !repaired; y++)
                {
                    if (!reachable[x, y]) continue;

                    foreach (Room.Directions dir in new[] { Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT })
                    {
                        int nx, ny;
                        if (!TryGetNeighbor(x, y, dir, out nx, out ny)) continue;
                        if (reachable[nx, ny]) continue;

                        SetWallState(x, y, dir, false);
                        repaired = true;
                        changed = true;
                        break;
                    }
                }
            }

            if (!repaired) break;
        }

        return changed;
    }

    private bool EnsureNoBridges()
    {
        int passLimit = numX * numY * 4;
        bool changed = false;

        for (int pass = 0; pass < passLimit; pass++)
        {
            List<BridgeEdge> bridges = FindBridges();
            if (bridges.Count == 0) return changed;

            bool repaired = false;
            foreach (BridgeEdge bridge in bridges)
            {
                if (OpenSupportAroundBridge(bridge))
                {
                    repaired = true;
                    changed = true;
                    break;
                }
            }

            if (!repaired) break;
        }

        return changed;
    }

    private bool OpenSupportAroundBridge(BridgeEdge bridge)
    {
        int x = bridge.x;
        int y = bridge.y;
        Room.Directions dir = bridge.dir;

        foreach (Room.Directions candidate in new[] { Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT })
        {
            if (candidate == dir || candidate == GetOppositeDirection(dir)) continue;

            int nx, ny;
            if (!TryGetNeighbor(x, y, candidate, out nx, out ny)) continue;
            if (!rooms[x, y].IsWallActive(candidate)) continue;

            SetWallState(x, y, candidate, false);
            if (FindBridges().Count < 1) return true;
            SetWallState(x, y, candidate, true);
        }

        int bx, by;
        if (!TryGetNeighbor(x, y, dir, out bx, out by)) return false;

        foreach (Room.Directions candidate in new[] { Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT })
        {
            if (candidate == GetOppositeDirection(dir)) continue;

            int nx, ny;
            if (!TryGetNeighbor(bx, by, candidate, out nx, out ny)) continue;
            if (!rooms[bx, by].IsWallActive(candidate)) continue;

            SetWallState(bx, by, candidate, false);
            if (FindBridges().Count < 1) return true;
            SetWallState(bx, by, candidate, true);
        }

        return false;
    }

    private List<BridgeEdge> FindBridges()
    {
        int nodeCount = numX * numY;
        int[] discovery = new int[nodeCount];
        int[] low = new int[nodeCount];
        int[] parent = new int[nodeCount];
        for (int i = 0; i < nodeCount; i++) parent[i] = -1;

        int time = 0;
        List<BridgeEdge> bridges = new List<BridgeEdge>();

        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                int node = ToNodeIndex(x, y);
                if (discovery[node] == 0)
                {
                    BridgeDfs(x, y, ref time, discovery, low, parent, bridges);
                }
            }
        }

        return bridges;
    }

    private void BridgeDfs(int x, int y, ref int time, int[] discovery, int[] low, int[] parent, List<BridgeEdge> bridges)
    {
        int current = ToNodeIndex(x, y);
        discovery[current] = low[current] = ++time;

        foreach (Room.Directions dir in new[] { Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT })
        {
            if (rooms[x, y].IsWallActive(dir)) continue;

            int nx, ny;
            if (!TryGetNeighbor(x, y, dir, out nx, out ny)) continue;

            int next = ToNodeIndex(nx, ny);
            if (discovery[next] == 0)
            {
                parent[next] = current;
                BridgeDfs(nx, ny, ref time, discovery, low, parent, bridges);
                low[current] = Mathf.Min(low[current], low[next]);

                if (low[next] > discovery[current])
                {
                    bridges.Add(new BridgeEdge(x, y, dir));
                }
            }
            else if (next != parent[current])
            {
                low[current] = Mathf.Min(low[current], discovery[next]);
            }
        }
    }

    private int ToNodeIndex(int x, int y)
    {
        return x * numY + y;
    }

    private struct BridgeEdge
    {
        public int x;
        public int y;
        public Room.Directions dir;

        public BridgeEdge(int x, int y, Room.Directions dir)
        {
            this.x = x;
            this.y = y;
            this.dir = dir;
        }
    }

    public bool IsMazeConnected()
    {
        if (rooms == null) return false;

        bool[,] reachable = GetReachableFromStart();
        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                if (!reachable[x, y]) return false;
            }
        }

        return true;
    }

    private bool[,] GetReachableFromStart()
    {
        bool[,] visited = new bool[numX, numY];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(new Vector2Int(0, 0));
        visited[0, 0] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int x = current.x;
            int y = current.y;

            foreach (Room.Directions dir in new[] { Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT })
            {
                if (rooms[x, y].IsWallActive(dir)) continue;

                int nx, ny;
                if (!TryGetNeighbor(x, y, dir, out nx, out ny)) continue;
                if (visited[nx, ny]) continue;

                visited[nx, ny] = true;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        return visited;
    }

    private bool WouldRemainConnectedIfWallStateChanged(int x, int y, Room.Directions dir, bool active)
    {
        int nx, ny;
        if (!TryGetNeighbor(x, y, dir, out nx, out ny)) return false;

        bool originalHere = rooms[x, y].IsWallActive(dir);
        Room.Directions opposite = GetOppositeDirection(dir);
        bool originalThere = rooms[nx, ny].IsWallActive(opposite);

        SetWallState(x, y, dir, active);
        bool connected = IsMazeConnected();
        SetWallState(x, y, dir, originalHere);
        SetWallState(nx, ny, opposite, originalThere);

        return connected;
    }

    private bool WouldKeepRequiredOpenExitsIfWallStateChanged(int x, int y, Room.Directions dir, bool active)
    {
        int nx, ny;
        if (!TryGetNeighbor(x, y, dir, out nx, out ny)) return false;

        bool originalHere = rooms[x, y].IsWallActive(dir);
        Room.Directions opposite = GetOppositeDirection(dir);
        bool originalThere = rooms[nx, ny].IsWallActive(opposite);

        SetWallState(x, y, dir, active);

        bool safe = HasRequiredOpenExits(x, y) && HasRequiredOpenExits(nx, ny);

        SetWallState(x, y, dir, originalHere);
        SetWallState(nx, ny, opposite, originalThere);

        return safe;
    }

    private bool EnsureMinimumOpenExits()
    {
        bool changed = false;

        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                int targetOpenExits = GetRequiredOpenExitCount(x, y);
                while (CountOpenExits(x, y) < targetOpenExits)
                {
                    if (!OpenOneMoreExit(x, y)) break;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private bool OpenOneMoreExit(int x, int y)
    {
        foreach (Room.Directions dir in new[] { Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT })
        {
            if (rooms[x, y].IsWallActive(dir))
            {
                int nx, ny;
                if (!TryGetNeighbor(x, y, dir, out nx, out ny)) continue;
                SetWallState(x, y, dir, false);
                return true;
            }
        }

        return false;
    }

    private bool IsMazeSafe()
    {
        if (!IsMazeConnected()) return false;
        if (FindBridges().Count > 0) return false;

        for (int x = 0; x < numX; x++)
        {
            for (int y = 0; y < numY; y++)
            {
                if (CountOpenExits(x, y) < GetRequiredOpenExitCount(x, y)) return false;
            }
        }

        return true;
    }

    private int GetRequiredOpenExitCount(int x, int y)
    {
        int possibleExits = 0;
        if (y < numY - 1) possibleExits++;
        if (x < numX - 1) possibleExits++;
        if (y > 0) possibleExits++;
        if (x > 0) possibleExits++;

        int target = 2;
        if (currentDifficulty == Difficulty.Medium || currentDifficulty == Difficulty.Hard) target = 3;

        return Mathf.Min(target, possibleExits);
    }

    private int CountOpenExits(int x, int y)
    {
        int openCount = 0;

        foreach (Room.Directions dir in new[] { Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT })
        {
            int nx, ny;
            if (!TryGetNeighbor(x, y, dir, out nx, out ny)) continue;
            if (!rooms[x, y].IsWallActive(dir)) openCount++;
        }

        return openCount;
    }

    private bool HasRequiredOpenExits(int x, int y)
    {
        return CountOpenExits(x, y) >= GetRequiredOpenExitCount(x, y);
    }

    private void SetWallState(int x, int y, Room.Directions dir, bool active)
    {
        if (!IsValidWallTarget(x, y, dir)) return;

        rooms[x, y].SetDirFlag(dir, active);

        int nx, ny;
        if (!TryGetNeighbor(x, y, dir, out nx, out ny)) return;

        Room.Directions opposite = GetOppositeDirection(dir);
        rooms[nx, ny].SetDirFlag(opposite, active);
    }

    private bool IsValidWallTarget(int x, int y, Room.Directions dir)
    {
        if (rooms == null) return false;
        if (x < 0 || x >= numX || y < 0 || y >= numY) return false;
        return dir == Room.Directions.TOP || dir == Room.Directions.RIGHT || dir == Room.Directions.BOTTOM || dir == Room.Directions.LEFT;
    }

    private bool IsSpecialOpening(int x, int y, Room.Directions dir)
    {
        return (x == 0 && y == 0 && dir == Room.Directions.BOTTOM)
            || (x == numX - 1 && y == numY - 1 && dir == Room.Directions.TOP);
    }

    private bool TryGetNeighbor(int x, int y, Room.Directions dir, out int nx, out int ny)
    {
        nx = x;
        ny = y;

        if (dir == Room.Directions.TOP && y < numY - 1) { ny++; return true; }
        if (dir == Room.Directions.RIGHT && x < numX - 1) { nx++; return true; }
        if (dir == Room.Directions.BOTTOM && y > 0) { ny--; return true; }
        if (dir == Room.Directions.LEFT && x > 0) { nx--; return true; }

        return false;
    }

    private Room.Directions GetOppositeDirection(Room.Directions dir)
    {
        if (dir == Room.Directions.TOP) return Room.Directions.BOTTOM;
        if (dir == Room.Directions.RIGHT) return Room.Directions.LEFT;
        if (dir == Room.Directions.BOTTOM) return Room.Directions.TOP;
        if (dir == Room.Directions.LEFT) return Room.Directions.RIGHT;

        return Room.Directions.NONE;
    }

    private bool GenerateStep()
    {
        if (stack.Count == 0) return true;
        Room r = stack.Peek();
        List<Tuple<Room.Directions, Room>> neighbours = new List<Tuple<Room.Directions, Room>>();
        
        int x = r.Index.x; int y = r.Index.y;
        if (y < numY - 1 && !rooms[x, y + 1].visited) neighbours.Add(new Tuple<Room.Directions, Room>(Room.Directions.TOP, rooms[x, y + 1]));
        if (x < numX - 1 && !rooms[x + 1, y].visited) neighbours.Add(new Tuple<Room.Directions, Room>(Room.Directions.RIGHT, rooms[x + 1, y]));
        if (y > 0 && !rooms[x, y - 1].visited) neighbours.Add(new Tuple<Room.Directions, Room>(Room.Directions.BOTTOM, rooms[x, y - 1]));
        if (x > 0 && !rooms[x - 1, y].visited) neighbours.Add(new Tuple<Room.Directions, Room>(Room.Directions.LEFT, rooms[x - 1, y]));

        if (neighbours.Count > 0)
        {
            var next = neighbours[UnityEngine.Random.Range(0, neighbours.Count)];
            next.Item2.visited = true;
            RemoveRoomWall(x, y, next.Item1);
            stack.Push(next.Item2);
        }
        else stack.Pop();
        return false;
    }

    private void Update() 
    { 
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            currentDifficulty = (Difficulty)(((int)currentDifficulty + 1) % 3);
            CreateMaze();
        }
    }
}