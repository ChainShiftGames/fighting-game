namespace Sim.Modules.Burst;

/// <summary>One fighter's part of the burst module's state.</summary>
public struct BurstFighter
{
    /// <summary>From 0 up to the config's max gauge.</summary>
    public int Gauge;

    /// <summary>Bursts used this match, for presentation and tests.</summary>
    public int BurstsUsed;
}
