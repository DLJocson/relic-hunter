// =============================================================================
// AIState.cs — High-level behavior tracking enumeration
// =============================================================================

namespace RelicHunter.AI
{
    public enum AIState
    {
        Idle,
        Patrol,
        MinimaxTacticalCalculation,
        AStarFallbackRouting,
        Trapped
    }
}