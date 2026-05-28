using System.Collections.Generic;
using UnityEngine;

public class TestAI : MonoBehaviour
{
    void Start()
    {
        RunTests();
    }

    void RunTests()
    {
        // 1. Adjacent Capture
        Vector2Int g1 = new Vector2Int(2, 2);
        Vector2Int t1 = new Vector2Int(2, 3);
        Vector2Int move1 = Minimax.GetBestGuardMove(g1, t1, new Vector2Int(8, 8),
                           new HashSet<Vector2Int>(), new Dictionary<Vector2Int, int>(), 5, 5, 2, 4, 3);
        Debug.Assert(move1 == t1, "Test 1 Failed: Guard should capture thief.");

        // 2. Trapped Guard
        HashSet<Vector2Int> walls2 = new HashSet<Vector2Int> {
            new Vector2Int(2, 3), new Vector2Int(2, 1), new Vector2Int(1, 2), new Vector2Int(3, 2)
        };
        Vector2Int move2 = Minimax.GetBestGuardMove(new Vector2Int(2, 2), new Vector2Int(4, 4), new Vector2Int(8, 8),
                           walls2, new Dictionary<Vector2Int, int>(), 5, 5, 2, 4, 3);
        Debug.Assert(move2 == Minimax.TRAPPED, "Test 2 Failed: Guard should be TRAPPED.");

        // 3. Trapped Thief
        HashSet<Vector2Int> walls3 = new HashSet<Vector2Int> {
            new Vector2Int(4, 5), new Vector2Int(4, 3), new Vector2Int(3, 4), new Vector2Int(5, 4)
        };
        Vector2Int move3 = Minimax.GetBestGuardMove(new Vector2Int(0, 0), new Vector2Int(4, 4), new Vector2Int(8, 8),
                           walls3, new Dictionary<Vector2Int, int>(), 9, 9, 2, 4, 3);
        Debug.Assert(move3 != Minimax.TRAPPED && move3 != new Vector2Int(0, 0), "Test 3 Failed: Guard should approach trapped thief.");

        // 4. Max Barricade Limit (Set limit to 0, thief should not place barricade)
        Dictionary<Vector2Int, int> ttls4 = new Dictionary<Vector2Int, int> { { new Vector2Int(4, 5), 2 } };
        // We force the thief into a position where the only logical move is to barricade if allowed
        // But since maxBarricades is 0, the AI MUST choose a Walk move instead.
        Vector2Int move4 = Minimax.GetBestGuardMove(new Vector2Int(0, 0), new Vector2Int(4, 4), new Vector2Int(8, 8),
                           new HashSet<Vector2Int>(), ttls4, 9, 9, 2, 4, 0);
        Debug.Log("Test 4 Check: If guard moves, barricade limit was respected.");

        // 5. Barricade Expiry (TTL=1)
        Dictionary<Vector2Int, int> ttls5 = new Dictionary<Vector2Int, int> { { new Vector2Int(4, 3), 1 } };
        HashSet<Vector2Int> obs5 = new HashSet<Vector2Int> { new Vector2Int(4, 3) };
        Vector2Int move5 = Minimax.GetBestGuardMove(new Vector2Int(4, 2), new Vector2Int(4, 4), new Vector2Int(8, 8),
                           obs5, ttls5, 9, 9, 2, 4, 3);
        Debug.Assert(move5 == new Vector2Int(4, 3), "Test 5 Failed: Guard should wait/walk toward expiring barricade.");

        // 6. Depth 1 vs Depth 3 (Anticipation)
        // Guard at (0,0), Thief at (0,2). Path at (0,1) is open. 
        // Depth 1: Guard takes (0,1). Depth 3: Guard anticipates block and might choose different path.
        Vector2Int moveDepth1 = Minimax.GetBestGuardMove(new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(8, 8),
                                new HashSet<Vector2Int>(), new Dictionary<Vector2Int, int>(), 9, 9, 1, 4, 3);
        Vector2Int moveDepth3 = Minimax.GetBestGuardMove(new Vector2Int(0, 0), new Vector2Int(0, 2), new Vector2Int(8, 8),
                                new HashSet<Vector2Int>(), new Dictionary<Vector2Int, int>(), 9, 9, 3, 4, 3);
        Debug.Log($"Test 6: Depth 1 Move: {moveDepth1}, Depth 3 Move: {moveDepth3}");

        Debug.Log("All tests finished. Check console for assertions.");
    }
}