using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RelicHunter.Maze
{
    public class GenerateMaze : MonoBehaviour
    {
        public enum Difficulty { Easy, Medium, Hard }
        private static readonly Vector2Int PlayerSpawnCell = new Vector2Int(0, 0);
        private const float ExitTopPercent = 0.10f;
        private const float GuardTopPercent = 0.20f;
        private const int GuardDistanceRelaxationAttemptLimit = 25;

        [Header("Settings")]
        [SerializeField] Difficulty currentDifficulty = Difficulty.Easy;
        [SerializeField] GameObject roomPrefab;
        [SerializeField] bool autoGenerateOnStart = false;
        [SerializeField] bool allowDebugRegenerate = false;

        public Vector2Int ExitCell { get; private set; } = new Vector2Int(-1, -1);
        public Vector2Int GuardCell { get; private set; } = new Vector2Int(-1, -1);
        public Room.Directions ExitOpeningDirection { get; private set; }
        public int GridWidth => numX;
        public int GridHeight => numY;
        public bool IsReady => rooms != null && !generating;

        public event Action OnGenerationComplete;

        Room[,] rooms = null;
        int numX, numY;
        float roomWidth, roomHeight;
        Stack<Room> stack = new Stack<Room>();
        bool generating = false;

        private void Start()
        {
            if (roomPrefab == null) return;
            GetRoomSize();
            if (autoGenerateOnStart)
                CreateMaze();
        }

        public void GenerateForRound(int width, int height, int seed)
        {
            numX = width;
            numY = height;
            SyncDifficultyFromGridSize();
            UnityEngine.Random.InitState(seed);
            if (roomPrefab == null)
            {
                Debug.LogError("[GenerateMaze] Room prefab is not assigned.");
                return;
            }
            if (roomWidth <= 0f || roomHeight <= 0f) GetRoomSize();
            FrameCamera();
            CreateMaze();
        }

        public void SyncDifficultyFromGridSize()
        {
            if (numX >= 15 || numY >= 15) currentDifficulty = Difficulty.Hard;
            else if (numX >= 12 || numY >= 12) currentDifficulty = Difficulty.Medium;
            else currentDifficulty = Difficulty.Easy;
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
                case Difficulty.Easy: numX = 9; numY = 9; break;
                case Difficulty.Medium: numX = 12; numY = 12; break;
                case Difficulty.Hard: numX = 15; numY = 15; break;
            }
        }

        public void CreateMaze()
        {
            if (generating) return;
            if (numX <= 0 || numY <= 0) UpdateGridSize();

            foreach (Transform child in transform) Destroy(child.gameObject);

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

            foreach (var room in rooms)
            {
                room.SetDirFlag(Room.Directions.TOP, true);
                room.SetDirFlag(Room.Directions.RIGHT, true);
                room.SetDirFlag(Room.Directions.BOTTOM, true);
                room.SetDirFlag(Room.Directions.LEFT, true);
            }

            rooms[0, 0].SetDirFlag(Room.Directions.BOTTOM, false);

            stack.Clear();
            stack.Push(rooms[0, 0]);
            rooms[0, 0].visited = true;

            StartCoroutine(Coroutine_Generate());
        }

        public void PrepareCameraForDimensions(int width, int height)
        {
            numX = width;
            numY = height;
            if (roomWidth <= 0f || roomHeight <= 0f)
                GetRoomSize();
            FrameCamera();
        }

        public void FrameCamera()
        {
            if (Camera.main == null) return;

            float centerX = ((numX - 1) * roomWidth) / 2f;
            float centerY = ((numY - 1) * roomHeight) / 2f;
            Camera.main.transform.position = new Vector3(centerX, centerY, -10f);
            float maxDim = Mathf.Max(numX * roomWidth, numY * roomHeight);
            Camera.main.orthographicSize = maxDim * 0.6f;
        }

        public bool CanPass(Vector2Int from, Vector2Int to)
        {
            if (rooms == null) return false;
            Vector2Int delta = to - from;
            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1) return false;
            if (to.x < 0 || to.x >= numX || to.y < 0 || to.y >= numY) return false;
            if (from.x < 0 || from.x >= numX || from.y < 0 || from.y >= numY) return false;

            Room.Directions dir = DirectionFromDelta(delta);
            return dir != Room.Directions.NONE && !rooms[from.x, from.y].IsWallActive(dir);
        }

        public Vector3 GridToWorld(Vector2Int cell)
        {
            Room room = GetRoom(cell.x, cell.y);
            if (room == null)
            {
                return transform.position + new Vector3(cell.x * roomWidth, cell.y * roomHeight, 0f);
            }

            // Room prefab pivots are centered; instances are placed at (i * roomWidth, j * roomHeight).
            Vector3 world = room.transform.position;
            world.z = -0.1f;
            return world;
        }

        public void ClearExitRoomFloorTint()
        {
            if (rooms == null || ExitCell.x < 0 || ExitCell.y < 0) return;

            Room exitRoom = rooms[ExitCell.x, ExitCell.y];
            if (exitRoom == null) return;

            Transform floor = exitRoom.transform.Find("Square");
            if (floor == null) return;

            SpriteRenderer floorRenderer = floor.GetComponent<SpriteRenderer>();
            if (floorRenderer == null) return;

            floorRenderer.color = new Color(0.29803923f, 0.17254902f, 0f, 1f);
            floorRenderer.sortingOrder = 0;
        }

        private void HideFloorGridSeams()
        {
            if (rooms == null) return;

            const float overlapScale = 1.04f;

            for (int x = 0; x < numX; x++)
            {
                for (int y = 0; y < numY; y++)
                {
                    Room room = rooms[x, y];
                    if (room == null) continue;

                    Transform floor = room.transform.Find("Square");
                    if (floor == null) continue;

                    Vector3 scale = floor.localScale;
                    floor.localScale = new Vector3(scale.x * overlapScale, scale.y * overlapScale, scale.z);
                }
            }
        }

        public Room GetRoom(int x, int y)
        {
            if (rooms == null || x < 0 || x >= numX || y < 0 || y >= numY) return null;
            return rooms[x, y];
        }

        private Room.Directions DirectionFromDelta(Vector2Int delta)
        {
            if (delta == Vector2Int.up) return Room.Directions.TOP;
            if (delta == Vector2Int.down) return Room.Directions.BOTTOM;
            if (delta == Vector2Int.right) return Room.Directions.RIGHT;
            if (delta == Vector2Int.left) return Room.Directions.LEFT;
            return Room.Directions.NONE;
        }

        private void RemoveRoomWall(int x, int y, Room.Directions dir)
        {
            bool isStart = (x == 0 && y == 0 && dir == Room.Directions.BOTTOM);
            bool isExit = IsExitOpening(x, y, dir);

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
            for (int i = 0; i < extraConnections; i++)
            {
                int x = UnityEngine.Random.Range(1, numX - 1);
                int y = UnityEngine.Random.Range(1, numY - 1);
                RemoveRoomWall(x, y, (Room.Directions)UnityEngine.Random.Range(0, 4));
            }


            for (int i = 0; i < 3; i++)
            {
                int rx = UnityEngine.Random.Range(1, numX - 2);
                int ry = UnityEngine.Random.Range(1, numY - 2);
                for (int x = rx; x <= rx + 1; x++)
                {
                    for (int y = ry; y <= ry + 1; y++)
                    {
                        rooms[x, y].SetDirFlag(Room.Directions.TOP, false);
                        rooms[x, y].SetDirFlag(Room.Directions.RIGHT, false);
                        rooms[x, y].SetDirFlag(Room.Directions.BOTTOM, false);
                        rooms[x, y].SetDirFlag(Room.Directions.LEFT, false);
                    }
                }
            }


            EnsureMazeFullySafe();
            SelectProceduralSpawns();
            HideFloorGridSeams();

            generating = false;
            OnGenerationComplete?.Invoke();
        }

        private void SelectProceduralSpawns()
        {
            List<DistanceCell> reachableCells = BuildReachableDistanceMap();
            if (reachableCells.Count == 0)
            {
                ApplyFallbackSpawns();
                return;
            }

            reachableCells.Sort((left, right) => right.Distance.CompareTo(left.Distance));

            Vector2Int exitCell = PickExitCell(reachableCells);
            Vector2Int guardCell = PickGuardCell(reachableCells, exitCell);

            ExitCell = exitCell;
            GuardCell = guardCell;
            ExitOpeningDirection = PickExitOpeningDirection(ExitCell);

            if (rooms != null && ExitCell.x >= 0 && ExitCell.y >= 0)
                rooms[ExitCell.x, ExitCell.y].SetDirFlag(ExitOpeningDirection, false);
        }

        private List<DistanceCell> BuildReachableDistanceMap()
        {
            List<DistanceCell> result = new List<DistanceCell>();
            if (rooms == null || numX <= 0 || numY <= 0)
                return result;

            if (!IsInsideGrid(PlayerSpawnCell))
                return result;

            int[,] distances = new int[numX, numY];
            for (int x = 0; x < numX; x++)
            {
                for (int y = 0; y < numY; y++)
                {
                    distances[x, y] = -1;
                }
            }

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(PlayerSpawnCell);
            distances[PlayerSpawnCell.x, PlayerSpawnCell.y] = 0;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentDistance = distances[current.x, current.y];

                foreach (Room.Directions dir in new[] { Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT })
                {
                    if (rooms[current.x, current.y].IsWallActive(dir))
                        continue;

                    int nx, ny;
                    if (!TryGetNeighbor(current.x, current.y, dir, out nx, out ny))
                        continue;

                    if (distances[nx, ny] >= 0)
                        continue;

                    distances[nx, ny] = currentDistance + 1;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }

            for (int x = 0; x < numX; x++)
            {
                for (int y = 0; y < numY; y++)
                {
                    if (distances[x, y] < 0)
                        continue;

                    Vector2Int cell = new Vector2Int(x, y);
                    if (cell == PlayerSpawnCell)
                        continue;

                    result.Add(new DistanceCell(cell, distances[x, y]));
                }
            }

            return result;
        }

        private Vector2Int PickRandomFromTopPercent(List<DistanceCell> sortedCells, float topPercent, Vector2Int? excludedCell)
        {
            if (sortedCells == null || sortedCells.Count == 0)
                return PlayerSpawnCell;

            int subsetCount = Mathf.Max(1, Mathf.CeilToInt(sortedCells.Count * topPercent));
            int startIndex = 0;
            int endIndex = Mathf.Min(sortedCells.Count, subsetCount);
            List<Vector2Int> candidates = CollectCandidateSlice(sortedCells, startIndex, endIndex, excludedCell);

            if (candidates.Count == 0)
                candidates = CollectCandidateSlice(sortedCells, 0, sortedCells.Count, excludedCell);

            if (candidates.Count == 0)
                return PlayerSpawnCell;

            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private Vector2Int PickExitCell(List<DistanceCell> sortedCells)
        {
            if (sortedCells == null || sortedCells.Count == 0)
                return PlayerSpawnCell;

            int minimumDistance = Mathf.FloorToInt((GridWidth + GridHeight) * 0.7f);
            while (minimumDistance >= 0)
            {
                int attempts = 0;
                while (attempts < GuardDistanceRelaxationAttemptLimit)
                {
                    Vector2Int candidate = sortedCells[UnityEngine.Random.Range(0, sortedCells.Count)].Cell;
                    attempts++;

                    int pathDistance = GetReachableDistanceFromPlayer(candidate, sortedCells);
                    if (pathDistance >= minimumDistance)
                        return candidate;
                }

                minimumDistance--;
            }

            return GetBestNonPlayerCell(PlayerSpawnCell);
        }

        private Vector2Int PickFallbackDistinctCell(List<DistanceCell> sortedCells, Vector2Int excludedCell)
        {
            List<Vector2Int> fallback = CollectCandidateSlice(sortedCells, 0, sortedCells.Count, excludedCell);
            if (fallback.Count > 0)
                return fallback[UnityEngine.Random.Range(0, fallback.Count)];

            return GetBestNonPlayerCell(excludedCell);
        }

        private Vector2Int PickGuardCell(List<DistanceCell> sortedCells, Vector2Int exitCell)
        {
            List<Vector2Int> prioritizedCandidates = CollectGuardCandidates(sortedCells, exitCell);
            if (prioritizedCandidates.Count == 0)
                return GetBestNonPlayerCell(exitCell);

            int minimumDistance = Mathf.FloorToInt((numX + numY) * 0.4f);
            while (minimumDistance >= 0)
            {
                Vector2Int candidate = TryPickGuardCandidate(prioritizedCandidates, exitCell, minimumDistance, GuardDistanceRelaxationAttemptLimit);
                if (candidate.x >= 0)
                    return candidate;

                minimumDistance--;
            }

            return GetBestNonPlayerCell(exitCell);
        }

        private List<Vector2Int> CollectGuardCandidates(List<DistanceCell> sortedCells, Vector2Int exitCell)
        {
            int subsetCount = Mathf.Max(1, Mathf.CeilToInt(sortedCells.Count * GuardTopPercent));
            List<Vector2Int> candidates = CollectCandidateSlice(sortedCells, 0, Mathf.Min(sortedCells.Count, subsetCount), exitCell);

            if (candidates.Count == 0)
                candidates = CollectCandidateSlice(sortedCells, 0, sortedCells.Count, exitCell);

            return candidates;
        }

        private Vector2Int TryPickGuardCandidate(List<Vector2Int> candidates, Vector2Int exitCell, int minimumDistance, int attemptLimit)
        {
            if (candidates == null || candidates.Count == 0)
                return new Vector2Int(-1, -1);

            int attempts = 0;
            while (attempts < attemptLimit)
            {
                List<Vector2Int> shuffled = new List<Vector2Int>(candidates);
                for (int index = 0; index < shuffled.Count; index++)
                {
                    int swapIndex = UnityEngine.Random.Range(index, shuffled.Count);
                    Vector2Int temp = shuffled[index];
                    shuffled[index] = shuffled[swapIndex];
                    shuffled[swapIndex] = temp;
                }

                for (int index = 0; index < shuffled.Count && attempts < attemptLimit; index++)
                {
                    Vector2Int candidate = shuffled[index];
                    attempts++;

                    int pathDistance = GetWalkableDistance(candidate, exitCell);
                    if (pathDistance > minimumDistance)
                        return candidate;
                }
            }

            return new Vector2Int(-1, -1);
        }

        private int GetWalkableDistance(Vector2Int start, Vector2Int goal)
        {
            if (!IsInsideGrid(start) || !IsInsideGrid(goal))
                return -1;

            if (start == goal)
                return 0;

            int[,] distances = new int[numX, numY];
            for (int x = 0; x < numX; x++)
            {
                for (int y = 0; y < numY; y++)
                {
                    distances[x, y] = -1;
                }
            }

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            distances[start.x, start.y] = 0;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentDistance = distances[current.x, current.y];

                foreach (Room.Directions dir in new[] { Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT })
                {
                    if (rooms[current.x, current.y].IsWallActive(dir))
                        continue;

                    int nx, ny;
                    if (!TryGetNeighbor(current.x, current.y, dir, out nx, out ny))
                        continue;

                    if (distances[nx, ny] >= 0)
                        continue;

                    distances[nx, ny] = currentDistance + 1;
                    if (nx == goal.x && ny == goal.y)
                        return distances[nx, ny];

                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }

            return -1;
        }

        private int GetReachableDistanceFromPlayer(Vector2Int cell, List<DistanceCell> sortedCells)
        {
            for (int index = 0; index < sortedCells.Count; index++)
            {
                if (sortedCells[index].Cell == cell)
                    return sortedCells[index].Distance;
            }

            return -1;
        }

        private List<Vector2Int> CollectCandidateSlice(List<DistanceCell> sortedCells, int startIndex, int endIndex, Vector2Int? excludedCell)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();

            for (int index = startIndex; index < endIndex; index++)
            {
                Vector2Int cell = sortedCells[index].Cell;
                if (cell == PlayerSpawnCell)
                    continue;

                if (excludedCell.HasValue && cell == excludedCell.Value)
                    continue;

                candidates.Add(cell);
            }

            return candidates;
        }

        private Vector2Int GetBestNonPlayerCell(Vector2Int excludedCell)
        {
            for (int x = numX - 1; x >= 0; x--)
            {
                for (int y = numY - 1; y >= 0; y--)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (cell == PlayerSpawnCell || cell == excludedCell)
                        continue;

                    return cell;
                }
            }

            return excludedCell != PlayerSpawnCell ? excludedCell : PlayerSpawnCell;
        }

        private void ApplyFallbackSpawns()
        {
            ExitCell = GetBestNonPlayerCell(PlayerSpawnCell);
            GuardCell = GetBestNonPlayerCell(ExitCell);

            if (GuardCell == ExitCell)
                GuardCell = PlayerSpawnCell;

            ExitOpeningDirection = PickExitOpeningDirection(ExitCell);

            if (rooms != null && ExitCell.x >= 0 && ExitCell.y >= 0)
                rooms[ExitCell.x, ExitCell.y].SetDirFlag(ExitOpeningDirection, false);
        }

        private bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < numX && cell.y >= 0 && cell.y < numY;
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private struct DistanceCell
        {
            public Vector2Int Cell;
            public int Distance;

            public DistanceCell(Vector2Int cell, int distance)
            {
                Cell = cell;
                Distance = distance;
            }
        }

        private Room.Directions PickExitOpeningDirection(Vector2Int cell)
        {
            List<Room.Directions> borderOptions = new List<Room.Directions>();
            if (cell.y == numY - 1) borderOptions.Add(Room.Directions.TOP);
            if (cell.x == numX - 1) borderOptions.Add(Room.Directions.RIGHT);
            if (cell.y == 0) borderOptions.Add(Room.Directions.BOTTOM);
            if (cell.x == 0) borderOptions.Add(Room.Directions.LEFT);

            if (borderOptions.Count > 0)
                return borderOptions[UnityEngine.Random.Range(0, borderOptions.Count)];

            Room.Directions[] interiorDirs = {
            Room.Directions.TOP, Room.Directions.RIGHT, Room.Directions.BOTTOM, Room.Directions.LEFT
        };
            return interiorDirs[UnityEngine.Random.Range(0, interiorDirs.Length)];
        }

        private bool IsExitOpening(int x, int y, Room.Directions dir)
        {
            if (ExitCell.x < 0 || ExitCell.y < 0) return false;
            return ExitCell.x == x && ExitCell.y == y && ExitOpeningDirection == dir;
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
                || IsExitOpening(x, y, dir);
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

#if UNITY_EDITOR
    private void Update()
    {
        if (!allowDebugRegenerate) return;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentDifficulty = (Difficulty)(((int)currentDifficulty + 1) % 3);
            UpdateGridSize();
            CreateMaze();
        }
    }
#endif
    }
}