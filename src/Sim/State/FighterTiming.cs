namespace Sim.State;

/// <summary>Per-fighter time control applied by the core (ADR-0011): hitstop and time scale.</summary>
/// <remarks>All zero means normal speed and no hitstop.</remarks>
public struct FighterTiming
{
    /// <summary>Frames the fighter still skips because of hitstop.</summary>
    public int HitstopFramesLeft;

    /// <summary>The fighter advances Numerator frames per Denominator match frames; 0/0 is normal speed.</summary>
    public int TimeScaleNumerator;

    public int TimeScaleDenominator;

    /// <summary>Match frames the time scale still lasts; back to normal speed at 0.</summary>
    public int TimeScaleFramesLeft;

    /// <summary>Sub-frame progress toward the next advanced frame, in Numerator units.</summary>
    public int TimeScaleProgress;
}
