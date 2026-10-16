namespace Sim.Combat;

/// <summary>How a defender is guarding against an attack this frame, if at all.</summary>
public enum BlockStance
{
    None,

    /// <summary>Holding back: blocks mids and overheads.</summary>
    Standing,

    /// <summary>Holding down-back: blocks mids and lows.</summary>
    Crouching,

    /// <summary>Holding back in the air, where a module allows it (air block): blocks anything blockable.</summary>
    Air,
}
