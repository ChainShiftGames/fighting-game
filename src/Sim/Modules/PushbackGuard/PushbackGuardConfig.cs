namespace Sim.Modules.PushbackGuard;

/// <summary>The pushback guard module's settings in its <c>MatchRules</c> slot (developer settings).</summary>
public struct PushbackGuardConfig : IModuleConfig
{
    /// <summary>Input word bits of the guard buttons (e.g. A and B: bits 4 and 5 = 48).</summary>
    public int ButtonMask;

    /// <summary>How many of them must be held while blocking (2: both).</summary>
    public int ButtonCount;

    /// <summary>How far a guarded block pushes, in thousandths of the hit's pushback (2000 = twice as far).</summary>
    public int PushbackPermille;

    /// <summary>Meter each guarded block costs, in hundredths of a percent; without it the block is a normal one.</summary>
    public int MeterCost;

    /// <summary>Meter each frame of guarding in blockstun drains, in hundredths of a percent (all there is if less).</summary>
    public int DrainPerFrame;

    /// <summary>1: a guarded block takes no chip damage (Faultless Defense); 0: chip as usual.</summary>
    public int NoChip;
}
