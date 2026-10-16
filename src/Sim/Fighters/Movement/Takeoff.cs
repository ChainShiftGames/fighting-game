namespace Sim.Fighters.Movement;

/// <summary>A jump starting this frame: the state machine picks it, movement turns it into velocity.</summary>
public enum Takeoff
{
    None,
    Jump,
    HighJump,

    /// <summary>A double jump: spends one of the character's air actions.</summary>
    AirJump,
}
