namespace Sim.Modules.NegativePenalty;

/// <summary>The negative penalty module's slice of the match state.</summary>
/// <remarks>The retreat counters come first, so scenarios read them as <c>module negative_penalty[0]</c> (P1) and <c>[1]</c> (P2).</remarks>
public struct NegativePenaltyState : IModuleState
{
    public int Player1Retreat;
    public int Player2Retreat;

    /// <summary>P1's x when this frame began, to tell which way it moved during the frame.</summary>
    public int Player1StartX;

    public int Player2StartX;

    /// <summary>The round the counters were last reset for; a new round starts them at 0.</summary>
    public int Round;
}
