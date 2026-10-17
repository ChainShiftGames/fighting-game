using Sim.Characters;
using Sim.Fighters.Movement;
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

    /// <summary>Hitstun or blockstun frames left after this one; the fighter acts again on the frame after it reaches 0.</summary>
    public int StunFramesLeft;

    /// <summary>1 while in the air; movement sets it on takeoff and clears it on landing.</summary>
    /// <remarks>A system that launches a grounded fighter (e.g. a launcher) sets it with an upward velocity.</remarks>
    public int IsAirborne;

    /// <summary>The jump the state machine started this frame, for movement to launch; None on other frames.</summary>
    public Takeoff Takeoff;

    /// <summary>That jump's direction relative to facing: +1 forward, -1 back, 0 straight up.</summary>
    public int TakeoffDirection;

    /// <summary>1 while the current prejump leads to a high jump (decided by the input that started it).</summary>
    public int IsHighJump;

    /// <summary>Double jumps and air dashes used since the fighter left the ground.</summary>
    /// <remarks>The state machine spends them; movement refills them on landing.</remarks>
    public int AirActionsUsed;

    /// <summary>Hitstop and time scale, managed by the tick pipeline's time control.</summary>
    public FighterTiming Timing;

    public PendingCancel Cancel;

    /// <summary>Move categories the fighter can't start in this frame's state machine step (move lock request).</summary>
    /// <remarks>Set by the core request applier; the state machines step clears it after each step.</remarks>
    public MoveCategory LockedMoveCategories;

    /// <summary>The category of the attack the state machine started this frame (e.g. a super), for modules.</summary>
    /// <remarks>None on frames that start no attack; the state machines step clears it before each step.</remarks>
    public MoveCategory StartedMoveCategory;

    /// <summary>Frames the fighter can't be thrown, counting this one: wake-up and landing protection.</summary>
    public int ThrowInvulnerableFramesLeft;

    /// <summary>While <see cref="FighterAction.Thrown"/>: frames left to tech; the throw lands at 0.</summary>
    public int ThrowTechFramesLeft;

    /// <summary>While <see cref="FighterAction.Thrown"/>: the thrower's move, whose hit and release apply.</summary>
    public int ThrownByMove;
}
