namespace Sim.Fighters.States;

/// <summary>Where a move is in its frame data; also what the frame meter shows (ADR-0007).</summary>
public enum MovePhase
{
    /// <summary>A basic move (idle, walk, stun…): no startup, active or recovery.</summary>
    None,
    Startup,
    Active,
    Recovery,
}
