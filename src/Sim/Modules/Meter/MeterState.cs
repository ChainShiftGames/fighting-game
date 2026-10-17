namespace Sim.Modules.Meter;

/// <summary>The meter module's slice of the match state; meter in hundredths of a percent.</summary>
/// <remarks>The meters come first, so scenarios read them as <c>module meter[0]</c> (P1) and <c>module meter[1]</c> (P2).</remarks>
public struct MeterState : IModuleState
{
    public int Player1Meter;
    public int Player2Meter;

    /// <summary>P1's x when this frame began, to tell whether it moved forward during the frame.</summary>
    public int Player1StartX;

    public int Player2StartX;

    /// <summary>The round the meters were last set up for; a new round sets them up again.</summary>
    public int Round;
}
