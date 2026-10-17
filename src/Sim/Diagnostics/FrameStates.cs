using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;

namespace Sim.Diagnostics;

/// <summary>
/// Reports a fighter's frame state (ADR-0007): startup, active, recovery, stun and invincibility, read from its state
/// and its character's move data. Training mode's frame meter shows it every frame.
/// </summary>
/// <remarks>
/// Read only from the state the logic already keeps, so reporting adds nothing to the snapshot or the checksum. Free
/// means the move state machine would take a new input this frame (its free actions).
/// </remarks>
public static class FrameStates
{
    public static FighterFrameState Of(in FighterState fighter, CharacterData character)
    {
        MoveData move = character.Moves[fighter.Move];
        return new FighterFrameState(KindOf(in fighter, in move), move.IsInvulnerableOn(fighter.MoveFrame));
    }

    private static FrameKind KindOf(in FighterState fighter, in MoveData move) => fighter.Action switch
    {
        FighterAction.Attack => AttackPhase(fighter.MoveFrame, in move),
        FighterAction.Idle or FighterAction.Crouch or FighterAction.WalkForward or FighterAction.WalkBack
            or FighterAction.Jump => FrameKind.Free,
        FighterAction.Landing => FrameKind.Recovery,
        FighterAction.Hitstun or FighterAction.CrouchHitstun or FighterAction.AirHitstun
            or FighterAction.Thrown => FrameKind.Hitstun,
        FighterAction.Blockstun or FighterAction.CrouchBlockstun or FighterAction.AirBlockstun => FrameKind.Blockstun,
        FighterAction.Knockdown or FighterAction.WakeUp => FrameKind.Knockdown,
        _ => FrameKind.Movement,
    };

    // A move without hit or throw boxes has no active frames (FirstActiveFrame 0): all of it is recovery.
    private static FrameKind AttackPhase(int frame, in MoveData move)
    {
        if (move.FirstActiveFrame == 0)
        {
            return FrameKind.Recovery;
        }

        if (frame < move.FirstActiveFrame)
        {
            return FrameKind.Startup;
        }

        return frame <= move.LastActiveFrame ? FrameKind.Active : FrameKind.Recovery;
    }
}
