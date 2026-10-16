using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;

namespace Sim.State;

/// <summary>One fighter's part of the match state. Positions and velocities in subpixels.</summary>
/// <remarks>Gameplay systems add their own fields here as they are built (P1-08 onwards).</remarks>
public struct FighterState
{
    public IntVector2 Position;
    public IntVector2 Velocity;

    /// <summary>+1 facing right, -1 facing left.</summary>
    public int Facing;

    public int Health;

    /// <summary>What the fighter is doing; the move state machine (P1-08) owns it.</summary>
    public FighterAction Action;

    /// <summary>Index of the playing move in the character's move list.</summary>
    public int Move;

    /// <summary>Frame of <see cref="Move"/>, 1 on the frame it was entered; basic moves loop back to 1.</summary>
    public int MoveFrame;

    /// <summary>What the current move has done so far (hit, block, whiff), for its cancel windows.</summary>
    public CancelOn MoveOutcome;

    /// <summary>Hitstun or blockstun left; counts down at the start of each of the fighter's frames.</summary>
    public int StunFramesLeft;

    /// <summary>1 while in the air; set by movement (P1-09).</summary>
    public int IsAirborne;

    /// <summary>Hitstop and time scale, managed by the tick pipeline's time control.</summary>
    public FighterTiming Timing;

    public PendingCancel Cancel;
}
