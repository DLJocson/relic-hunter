#if UNITY_EDITOR
using System.Collections.Generic;
using RelicHunter.AI;
using UnityEngine;

namespace RelicHunter.AI.Editor
{
    /// <summary>
    /// Manual minimax regression checks. Add this component to a scene object to run on Play.
    /// </summary>
    public class Test_Minimax : MonoBehaviour
    {
        void Start()
        {
            RunTests();
        }

        void RunTests()
        {
            Vector2Int g1 = new Vector2Int(2, 2);
            Vector2Int t1 = new Vector2Int(2, 3);
            Vector2Int move1 = Minimax.GetBestGuardMove(g1, t1, new Vector2Int(8, 8),
                new HashSet<Vector2Int>(), new Dictionary<Vector2Int, int>(), 5, 5, 2, 4, 3);
            Debug.Assert(move1 == t1, "Test 1 Failed: Guard should capture thief.");

            HashSet<Vector2Int> walls2 = new HashSet<Vector2Int> {
                new Vector2Int(2, 3), new Vector2Int(2, 1), new Vector2Int(1, 2), new Vector2Int(3, 2)
            };
            Vector2Int move2 = Minimax.GetBestGuardMove(new Vector2Int(2, 2), new Vector2Int(4, 4), new Vector2Int(8, 8),
                walls2, new Dictionary<Vector2Int, int>(), 5, 5, 2, 4, 3);
            Debug.Assert(move2 == Minimax.TRAPPED, "Test 2 Failed: Guard should be TRAPPED.");

            HashSet<Vector2Int> walls3 = new HashSet<Vector2Int> {
                new Vector2Int(4, 5), new Vector2Int(4, 3), new Vector2Int(3, 4), new Vector2Int(5, 4)
            };
            Vector2Int move3 = Minimax.GetBestGuardMove(new Vector2Int(0, 0), new Vector2Int(4, 4), new Vector2Int(8, 8),
                walls3, new Dictionary<Vector2Int, int>(), 9, 9, 2, 4, 3);
            Debug.Assert(move3 != Minimax.TRAPPED && move3 != new Vector2Int(0, 0), "Test 3 Failed: Guard should approach trapped thief.");

            Dictionary<Vector2Int, int> ttls4 = new Dictionary<Vector2Int, int> { { new Vector2Int(4, 5), 2 } };
            Minimax.GetBestGuardMove(new Vector2Int(0, 0), new Vector2Int(4, 4), new Vector2Int(8, 8),
                new HashSet<Vector2Int>(), ttls4, 9, 9, 2, 4, 0);
            Debug.Log("Test 4 Check: If guard moves, barricade limit was respected.");

            Dictionary<Vector2Int, int> ttls5 = new Dictionary<Vector2Int, int> { { new Vector2Int(4, 3), 1 } };
            HashSet<Vector2Int> obs5 = new HashSet<Vector2Int> { new Vector2Int(4, 3) };
            Vector2Int move5 = Minimax.GetBestGuardMove(new Vector2Int(4, 2), new Vector2Int(4, 4), new Vector2Int(8, 8),
                obs5, ttls5, 9, 9, 2, 4, 3);
            Debug.Assert(move5 == new Vector2Int(4, 3), "Test 5 Failed: Guard should wait/walk toward expiring barricade.");

            Vector2Int moveDepth1 = Minimax.GetBestGuardMove(new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(8, 8),
                new HashSet<Vector2Int>(), new Dictionary<Vector2Int, int>(), 9, 9, 1, 4, 3);
            Vector2Int moveDepth3 = Minimax.GetBestGuardMove(new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(8, 8),
                new HashSet<Vector2Int>(), new Dictionary<Vector2Int, int>(), 9, 9, 3, 4, 3);
            Debug.Log($"Test 6: Depth 1 Move: {moveDepth1}, Depth 3 Move: {moveDepth3}");

            Debug.Log("All tests finished. Check console for assertions.");
        }
    }
}
#endif
