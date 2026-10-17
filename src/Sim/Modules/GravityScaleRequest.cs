namespace Sim.Modules;

/// <summary>Core request: change how hard gravity pulls one fighter until another request changes it (combo limits).</summary>
/// <remarks>Kept in the fighter's state until the next request or the next round; only ever heavier, never lighter.</remarks>
public struct GravityScaleRequest
{
    /// <summary>Extra gravity in thousandths of the character's own: 0 is normal, 500 pulls 1.5 times as hard.</summary>
    public int ExtraPermille;
}
