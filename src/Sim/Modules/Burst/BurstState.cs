namespace Sim.Modules.Burst;

/// <summary>The burst module's slice of the match state.</summary>
/// <remarks>As slice integers: 0 P1 gauge, 1 P1 bursts used, 2 P2 gauge, 3 P2 bursts used, 4 started.</remarks>
public struct BurstState : IModuleState
{
    public BurstFighters Fighters;

    /// <summary>1 once both fighters got the start gauge, on the match's first frame.</summary>
    public int IsStarted;
}
