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
}
