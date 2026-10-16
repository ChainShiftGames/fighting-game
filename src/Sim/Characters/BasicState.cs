namespace Sim.Characters;

/// <summary>
/// States the move state machine enters on its own (P1-08). Each maps to a basic move that supplies its frames
/// and boxes.
/// </summary>
public enum BasicState
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
    AirDash,
    AirBackdash,

    /// <summary>Held by the opponent's throw until the tech window closes (P1-23).</summary>
    Thrown,

    /// <summary>A throw was teched: both fighters separate (P1-23).</summary>
    ThrowTech,

    /// <summary>Blocked in the air (air block module, P1-24); ends in a landing on touching down.</summary>
    AirBlockstun,
}
