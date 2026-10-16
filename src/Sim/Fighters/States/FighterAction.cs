using Sim.Characters;

namespace Sim.Fighters.States;

/// <summary>What a fighter is doing: one of the basic states, or an attack (any move started by input).</summary>
/// <remarks>
/// The basic members have the same names and values as <see cref="BasicState"/>, so a basic action converts to the
/// basic state whose move it plays with a cast (a test checks they stay in step).
/// </remarks>
public enum FighterAction
{
    Idle,
    Crouch,
    WalkForward,
    WalkBack,
    Prejump,
    Jump,
    Landing,
    Dash,
    Backdash,
    Hitstun,
    CrouchHitstun,
    AirHitstun,
    Blockstun,
    CrouchBlockstun,
    Knockdown,
    WakeUp,

    /// <summary>A normal, special, super or throw: the move's own frame data runs it.</summary>
    Attack,
}
