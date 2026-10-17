namespace Sim.Modules.CharacterGauge;

/// <summary>A character gauge's slice of the match state, in its owner's mechanic slot.</summary>
/// <remarks>The gauge comes first, so scenarios read it as <c>p1 mechanic[0]</c>.</remarks>
public struct CharacterGaugeState : IModuleState
{
    public int Gauge;

    /// <summary>The round the gauge was last set up for; a new round sets it up again.</summary>
    public int Round;
}
