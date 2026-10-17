namespace Sim.Modules.NegativePenalty;

/// <summary>The negative penalty module's settings in its <c>MatchRules</c> slot (developer settings).</summary>
public struct NegativePenaltyConfig : IModuleConfig
{
    /// <summary>Retreat frames that build up to a penalty.</summary>
    public int PenaltyFrames;

    /// <summary>Retreat frames each frame of moving forward takes off again (never below 0).</summary>
    public int ForwardRecovery;

    /// <summary>Meter a penalty drains, in hundredths of a percent; all there is when the fighter has less.</summary>
    public int MeterLoss;
}
