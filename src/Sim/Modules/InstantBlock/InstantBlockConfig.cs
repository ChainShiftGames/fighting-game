namespace Sim.Modules.InstantBlock;

/// <summary>The instant block module's settings in its <c>MatchRules</c> slot (developer settings).</summary>
/// <remarks>
/// TODO(P3-13): an instant block gives meter in the reference games. Once the meter module defines a core way to
/// grant meter, add a <c>meterGain</c> field here; until then instant block gives none, so the modules stay apart.
/// TODO: core pushback is applied now (Sim.Collision.PushbackStage); a pushback reward needs a pushback request in
/// ADR-0011 first (P3-15 adds one).
/// </remarks>
public struct InstantBlockConfig : IModuleConfig
{
    /// <summary>
    /// A block is instant when the defender has held back for at most this many frames, counting the block's frame:
    /// 1 means back was first pressed on the frame the attack connected.
    /// </summary>
    public int WindowFrames;

    /// <summary>
    /// Anti-mash: no instant block when back was also held in this many frames before the current press began.
    /// 0 turns the lockout off.
    /// </summary>
    public int LockoutFrames;

    /// <summary>Blockstun frames an instant block takes off (never below 0).</summary>
    public int BlockstunReduction;
}
