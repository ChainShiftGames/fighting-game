namespace Sim.Modules;

/// <summary>Core request: replace one fighter's remaining hitstun or blockstun (e.g. instant block).</summary>
/// <remarks>
/// Sets <c>FighterState.StunFramesLeft</c>, which only matters while the fighter is in a stun action, so a module
/// asks for it right after the hit or block that put the fighter there.
/// </remarks>
public struct StunOverrideRequest
{
    /// <summary>Stun frames left after this frame, counted as <c>FighterState.StunFramesLeft</c> counts them.</summary>
    public int Frames;
}
