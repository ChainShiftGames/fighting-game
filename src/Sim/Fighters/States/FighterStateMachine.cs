using System;
using Sim.Characters;
using Sim.Fighters.Movement;
using Sim.Input;
using Sim.Modules;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Advances one fighter's move state machine by one of its own frames; no character-specific code.</summary>
/// <remarks>
/// Per frame, at most one transition: a pending cancel (core request, honoured from any state) first; otherwise the
/// move frame advances and the current action decides. Free actions start moves in input priority, then dashes, then
/// follow the held direction; in the air, air actions (double jump, air dash) come after moves. Attacks cancel
/// through their data windows where the <see cref="CancelPermission"/> allow it (into moves first, then a jump) and end
/// into a free action. Jumps go through <see cref="JumpActions"/>. A thrown
/// fighter is held until the throw stages let it go (<c>Sim.Throws</c>); a throw tech ends into a free action. A
/// KO'd fighter (<see cref="KnockOut"/>) never wakes up from a knockdown. The machine reads only the fighter's own
/// state, input and character data, so both fighters are treated alike.
/// </remarks>
public sealed class FighterStateMachine
{
    private readonly MoveSelection _moves;
    private readonly IMoveInput _input;
    private readonly JumpActions _jumps;

    public FighterStateMachine(IMoveInput input)
    {
        _input = input;
        _moves = new MoveSelection(input);
        _jumps = new JumpActions(input);
    }

    /// <summary>Advances with every data cancel window usable, as when no modules are involved (unit tests).</summary>
    public void Advance(ref FighterState fighter, in InputHistory history, CharacterData character) =>
        Advance(ref fighter, in history, character, CancelPermission.EveryWindow);

    public void Advance(ref FighterState fighter, in InputHistory history, CharacterData character, CancelPermission cancels)
    {
        // A takeoff is only this frame's request to movement.
        fighter.Takeoff = Takeoff.None;
        if (TryTakePendingCancel(ref fighter, character))
        {
            return;
        }

        MoveData move = character.Moves[fighter.Move];
        StepFrame(ref fighter, in move);
        switch (fighter.Action)
        {
            case FighterAction.Attack:
                AdvanceAttack(ref fighter, in history, character, in move, cancels);
                break;
            case FighterAction.Prejump:
                JumpActions.AdvancePrejump(ref fighter, in history, character);
                break;
            case FighterAction.AirDash or FighterAction.AirBackdash:
                AdvanceAirDash(ref fighter, in history, character, in move);
                break;
            case FighterAction.Thrown:
                break;
            case FighterAction.Landing or FighterAction.Backdash or FighterAction.WakeUp or FighterAction.ThrowTech:
                EndIntoFreeAfter(ref fighter, move.TotalFrames, in history, character);
                break;
            case FighterAction.Dash:
                AdvanceDash(ref fighter, in history, character, in move);
                break;
            case FighterAction.Knockdown:
                AdvanceKnockdown(ref fighter, character, in move);
                break;
            case FighterAction.AirHitstun:
                EndOnLanding(ref fighter, FighterAction.Knockdown, character);
                break;
            case FighterAction.AirBlockstun:
                AdvanceAirBlockstun(ref fighter, in history, character);
                break;
            case FighterAction.Hitstun or FighterAction.CrouchHitstun or FighterAction.Blockstun
                or FighterAction.CrouchBlockstun:
                AdvanceStun(ref fighter, in history, character);
                break;
            default:
                AdvanceFree(ref fighter, in history, character);
                break;
        }

        _jumps.DecideHeight(ref fighter, in history);
    }

    // A cancel the core accepted (ADR-0011): taken from any action. A target the character doesn't have, or a basic
    // move (only the state machine enters those), is a module bug and fails loudly.
    private static bool TryTakePendingCancel(ref FighterState fighter, CharacterData character)
    {
        if (fighter.Cancel.IsPending == 0)
        {
            return false;
        }

        int target = fighter.Cancel.TargetMove;
        if (target >= character.Moves.Length || character.Moves[target].Category == MoveCategory.Basic)
        {
            throw new InvalidOperationException(
                $"A cancel into move {target} can't be taken: {character.Id} has no such attack.");
        }

        fighter.Cancel = default;
        MoveEntry.EnterAttack(ref fighter, target);
        return true;
    }

    // Advances the move frame; basic moves loop. Marks a whiff once the active frames pass without contact (a clash
    // is contact too, so it opens no whiff cancel).
    private static void StepFrame(ref FighterState fighter, in MoveData move)
    {
        fighter.MoveFrame++;
        if (fighter.MoveFrame > move.TotalFrames && IsLooping(fighter.Action))
        {
            fighter.MoveFrame = 1;
        }

        bool hasMissed = move.FirstActiveFrame != 0 && fighter.MoveFrame > move.LastActiveFrame &&
            (fighter.MoveOutcome & (CancelOn.Hit | CancelOn.Block | CancelOn.Clash)) == 0;
        if (hasMissed)
        {
            fighter.MoveOutcome |= CancelOn.Whiff;
        }
    }

    private void AdvanceFree(ref FighterState fighter, in InputHistory history, CharacterData character)
    {
        if (fighter.Action == FighterAction.Jump && HasLanded(in fighter))
        {
            MoveEntry.EnterBasic(ref fighter, FighterAction.Landing, character);
            return;
        }

        int move = _moves.FindFreeMove(in fighter, in history, character);
        if (move != CharacterData.NoMove)
        {
            MoveEntry.EnterAttack(ref fighter, move);
            return;
        }

        if (fighter.IsAirborne != 0 && _jumps.TryStartAirAction(ref fighter, in history, character))
        {
            return;
        }

        FighterAction next = fighter.IsAirborne != 0 ? FighterAction.Jump : GroundActionFor(in fighter, in history);
        if (next != fighter.Action)
        {
            MoveEntry.EnterBasic(ref fighter, next, character);
        }
    }

    private void AdvanceAttack(
        ref FighterState fighter, in InputHistory history, CharacterData character, in MoveData move, CancelPermission cancels)
    {
        int cancel = _moves.FindCancel(in fighter, in history, character, cancels);
        if (cancel != CharacterData.NoMove)
        {
            MoveEntry.EnterAttack(ref fighter, cancel);
        }
        else if (TryJumpCancel(ref fighter, in history, character, in move, cancels))
        {
            // Already in the prejump or double jump.
        }
        else if (move.Stance == MoveStance.Air && HasLanded(in fighter))
        {
            MoveEntry.EnterBasic(ref fighter, FighterAction.Landing, character);
        }
        else if (fighter.MoveFrame > move.TotalFrames)
        {
            EnterFree(ref fighter, in history, character);
        }
    }

    // A jump cancel needs an open window allowing it and the jump input; the rules are asked only then.
    private bool TryJumpCancel(
        ref FighterState fighter, in InputHistory history, CharacterData character, in MoveData move, CancelPermission cancels)
    {
        if (!MoveSelection.HasOpenJumpCancel(in fighter, character) ||
            !_jumps.IsJumpCancelInput(in fighter, in history, character))
        {
            return false;
        }

        CancelCheck check = new(
            cancels.Fighter, move.Category, move.Stance, fighter.MoveOutcome, MoveCategory.None, isJump: true);
        if (!cancels.Allows(in check))
        {
            return false;
        }

        _jumps.StartJumpCancel(ref fighter, in history, character);
        return true;
    }

    // A run (dash style from data) loops while forward is held and stops when it's released; a step dash plays its
    // move once.
    private void AdvanceDash(
        ref FighterState fighter, in InputHistory history, CharacterData character, in MoveData move)
    {
        if (character.Stats.DashStyle != DashStyle.Run)
        {
            EndIntoFreeAfter(ref fighter, move.TotalFrames, in history, character);
            return;
        }

        if (!NumpadDirection.IsForward(NumpadDirection.Of(history.FramesAgo(0), fighter.Facing)))
        {
            EnterFree(ref fighter, in history, character);
        }
        else if (fighter.MoveFrame > move.TotalFrames)
        {
            fighter.MoveFrame = 1;
        }
    }

    private void AdvanceAirDash(
        ref FighterState fighter, in InputHistory history, CharacterData character, in MoveData move)
    {
        if (HasLanded(in fighter))
        {
            MoveEntry.EnterBasic(ref fighter, FighterAction.Landing, character);
            return;
        }

        EndIntoFreeAfter(ref fighter, move.TotalFrames, in history, character);
    }

    // Stun N keeps the fighter stunned for its next N frames and frees it on the one after, like hitstop: checked
    // before counting down, so frame advantage is hitstun minus the attacker's remaining frames, as in frame data.
    private void AdvanceStun(ref FighterState fighter, in InputHistory history, CharacterData character)
    {
        if (fighter.StunFramesLeft == 0)
        {
            EnterFree(ref fighter, in history, character);
            return;
        }

        fighter.StunFramesLeft--;
    }

    // Air blockstun lands into a landing whatever is left of it; otherwise it ends like ground stun, into the air.
    private void AdvanceAirBlockstun(ref FighterState fighter, in InputHistory history, CharacterData character)
    {
        if (HasLanded(in fighter))
        {
            MoveEntry.EnterBasic(ref fighter, FighterAction.Landing, character);
            return;
        }

        AdvanceStun(ref fighter, in history, character);
    }

    // A KO'd fighter stays down on the knockdown's last frame until the next round resets it; others wake up.
    private static void AdvanceKnockdown(ref FighterState fighter, CharacterData character, in MoveData move)
    {
        if (!KnockOut.IsKnockedOut(in fighter))
        {
            EndAfter(ref fighter, move.TotalFrames, FighterAction.WakeUp, character);
        }
        else if (fighter.MoveFrame > move.TotalFrames)
        {
            fighter.MoveFrame = move.TotalFrames;
        }
    }

    private void EndIntoFreeAfter(
        ref FighterState fighter, int frames, in InputHistory history, CharacterData character)
    {
        if (fighter.MoveFrame > frames)
        {
            EnterFree(ref fighter, in history, character);
        }
    }

    // The first free frame after something ends can already start a move (a buffered press out of recovery);
    // otherwise the fighter follows the held direction.
    private void EnterFree(ref FighterState fighter, in InputHistory history, CharacterData character)
    {
        MoveEntry.EnterBasic(ref fighter, NeutralFor(in fighter, in history), character);
        int move = _moves.FindFreeMove(in fighter, in history, character);
        if (move != CharacterData.NoMove)
        {
            MoveEntry.EnterAttack(ref fighter, move);
        }
    }

    private static void EndAfter(ref FighterState fighter, int frames, FighterAction next, CharacterData character)
    {
        if (fighter.MoveFrame > frames)
        {
            MoveEntry.EnterBasic(ref fighter, next, character);
        }
    }

    private static void EndOnLanding(ref FighterState fighter, FighterAction next, CharacterData character)
    {
        if (HasLanded(in fighter))
        {
            MoveEntry.EnterBasic(ref fighter, next, character);
        }
    }

    // The free action the held direction asks for once something ends.
    private static FighterAction NeutralFor(in FighterState fighter, in InputHistory history) =>
        NeutralActions.For(NumpadDirection.Of(history.FramesAgo(0), fighter.Facing), fighter.IsAirborne != 0);

    // Dashes start only from free ground actions, not straight out of another action.
    private FighterAction GroundActionFor(in FighterState fighter, in InputHistory history)
    {
        if (_input.IsDashTriggered(in history, fighter.Facing))
        {
            return FighterAction.Dash;
        }

        return _input.IsBackdashTriggered(in history, fighter.Facing)
            ? FighterAction.Backdash
            : NeutralFor(in fighter, in history);
    }

    // Movement sets the airborne flag on the takeoff frame, after this step, so the state machine first sees a jump
    // on its second frame; landing is then just the flag clearing (a looping jump move's frame number says nothing).
    private static bool HasLanded(in FighterState fighter) => fighter.IsAirborne == 0;

    private static bool IsLooping(FighterAction action) => action is FighterAction.Idle or FighterAction.Crouch
        or FighterAction.WalkForward or FighterAction.WalkBack or FighterAction.Jump
        or FighterAction.Hitstun or FighterAction.CrouchHitstun or FighterAction.AirHitstun
        or FighterAction.Blockstun or FighterAction.CrouchBlockstun or FighterAction.Thrown
        or FighterAction.AirBlockstun;
}
