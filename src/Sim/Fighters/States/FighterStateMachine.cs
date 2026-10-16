using System;
using Sim.Characters;
using Sim.Input;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Advances one fighter's move state machine by one of its own frames; no character-specific code.</summary>
/// <remarks>
/// Per frame, at most one transition: a pending cancel (core request, honoured from any state) first; otherwise the
/// move frame advances and the current action decides. Free actions start moves in input priority, then dashes, then
/// follow the held direction. Attacks cancel through their data windows and end into a free action. The machine
/// reads only the fighter's own state, input and character data, so both fighters are treated alike.
/// </remarks>
public sealed class FighterStateMachine
{
    private readonly MoveSelection _moves;
    private readonly IMoveInput _input;

    public FighterStateMachine(IMoveInput input)
    {
        _input = input;
        _moves = new MoveSelection(input);
    }

    public void Advance(ref FighterState fighter, in InputHistory history, CharacterData character)
    {
        if (TryTakePendingCancel(ref fighter, character))
        {
            return;
        }

        MoveData move = character.Moves[fighter.Move];
        StepFrame(ref fighter, in move);
        switch (fighter.Action)
        {
            case FighterAction.Attack:
                AdvanceAttack(ref fighter, in history, character, in move);
                break;
            case FighterAction.Prejump:
                EndAfter(ref fighter, character.Stats.PrejumpFrames, FighterAction.Jump, character);
                break;
            case FighterAction.Landing or FighterAction.Backdash or FighterAction.WakeUp:
                EndAfter(ref fighter, move.TotalFrames, NeutralFor(in fighter, in history), character);
                break;
            case FighterAction.Dash:
                AdvanceDash(ref fighter, in history, character, in move);
                break;
            case FighterAction.Knockdown:
                EndAfter(ref fighter, move.TotalFrames, FighterAction.WakeUp, character);
                break;
            case FighterAction.AirHitstun:
                EndOnLanding(ref fighter, FighterAction.Knockdown, character);
                break;
            case FighterAction.Hitstun or FighterAction.CrouchHitstun or FighterAction.Blockstun
                or FighterAction.CrouchBlockstun:
                AdvanceStun(ref fighter, in history, character);
                break;
            default:
                AdvanceFree(ref fighter, in history, character);
                break;
        }
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

    // Advances the move frame; basic moves loop. Marks a whiff once the active frames pass without contact.
    private static void StepFrame(ref FighterState fighter, in MoveData move)
    {
        fighter.MoveFrame++;
        if (fighter.MoveFrame > move.TotalFrames && IsLooping(fighter.Action))
        {
            fighter.MoveFrame = 1;
        }

        bool hasMissed = move.FirstActiveFrame != 0 && fighter.MoveFrame > move.LastActiveFrame &&
            (fighter.MoveOutcome & (CancelOn.Hit | CancelOn.Block)) == 0;
        if (hasMissed)
        {
            fighter.MoveOutcome |= CancelOn.Whiff;
        }

        if (fighter.StunFramesLeft > 0)
        {
            fighter.StunFramesLeft--;
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

        FighterAction next = fighter.IsAirborne != 0 ? FighterAction.Jump : GroundActionFor(in fighter, in history);
        if (next != fighter.Action)
        {
            MoveEntry.EnterBasic(ref fighter, next, character);
        }
    }

    private void AdvanceAttack(
        ref FighterState fighter, in InputHistory history, CharacterData character, in MoveData move)
    {
        int cancel = _moves.FindCancel(in fighter, in history, character);
        if (cancel != CharacterData.NoMove)
        {
            MoveEntry.EnterAttack(ref fighter, cancel);
        }
        else if (move.Stance == MoveStance.Air && HasLanded(in fighter))
        {
            MoveEntry.EnterBasic(ref fighter, FighterAction.Landing, character);
        }
        else if (fighter.MoveFrame > move.TotalFrames)
        {
            MoveEntry.EnterBasic(ref fighter, NeutralFor(in fighter, in history), character);
        }
    }

    // A run (dash style from data) loops while forward is held and stops when it's released; a step dash plays its
    // move once.
    private static void AdvanceDash(
        ref FighterState fighter, in InputHistory history, CharacterData character, in MoveData move)
    {
        if (character.Stats.DashStyle != DashStyle.Run)
        {
            EndAfter(ref fighter, move.TotalFrames, NeutralFor(in fighter, in history), character);
            return;
        }

        if (!NumpadDirection.IsForward(NumpadDirection.Of(history.FramesAgo(0), fighter.Facing)))
        {
            MoveEntry.EnterBasic(ref fighter, NeutralFor(in fighter, in history), character);
        }
        else if (fighter.MoveFrame > move.TotalFrames)
        {
            fighter.MoveFrame = 1;
        }
    }

    private static void AdvanceStun(ref FighterState fighter, in InputHistory history, CharacterData character)
    {
        if (fighter.StunFramesLeft == 0)
        {
            MoveEntry.EnterBasic(ref fighter, NeutralFor(in fighter, in history), character);
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

    // Movement sets the airborne flag on the first jump frame, so a jump can't land on the frame it starts.
    private static bool HasLanded(in FighterState fighter) => fighter.IsAirborne == 0 && fighter.MoveFrame > 1;

    private static bool IsLooping(FighterAction action) => action is FighterAction.Idle or FighterAction.Crouch
        or FighterAction.WalkForward or FighterAction.WalkBack or FighterAction.Jump
        or FighterAction.Hitstun or FighterAction.CrouchHitstun or FighterAction.AirHitstun
        or FighterAction.Blockstun or FighterAction.CrouchBlockstun;
}
