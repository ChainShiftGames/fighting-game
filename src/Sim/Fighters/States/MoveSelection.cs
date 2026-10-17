using System;
using Sim.Characters;
using Sim.Modules;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Picks the move a fighter starts this frame, if any, in the character's input priority order.</summary>
public sealed class MoveSelection
{
    private readonly IMoveInput _input;

    public MoveSelection(IMoveInput input)
    {
        _input = input;
    }

    /// <summary>Any non-basic move usable in the fighter's stance (from a free action).</summary>
    public int FindFreeMove(in FighterState fighter, in InputHistory history, CharacterData character)
    {
        ReadOnlySpan<MoveData> moves = character.Moves;
        for (int move = 0; move < moves.Length; move++)
        {
            if (moves[move].Category != MoveCategory.Basic && CanStart(in fighter, in history, in moves[move]))
            {
                return move;
            }
        }

        return CharacterData.NoMove;
    }

    /// <summary>A move an open cancel window of the current move allows; windows are checked in data order.</summary>
    /// <remarks>
    /// Only a move that was input is offered to <paramref name="rules"/>; a refused one doesn't stop the search.
    /// </remarks>
    public int FindCancel(
        in FighterState fighter, in InputHistory history, CharacterData character, CancelPermission rules)
    {
        MoveData current = character.Moves[fighter.Move];
        foreach (CancelData cancel in character.CancelsOf(in current))
        {
            if (!IsOpen(in fighter, in cancel))
            {
                continue;
            }

            int move = FindCancelTarget(in fighter, in history, character, in cancel, in current, rules);
            if (move != CharacterData.NoMove)
            {
                return move;
            }
        }

        return CharacterData.NoMove;
    }

    /// <summary>An open cancel window of the current move allows a jump cancel.</summary>
    public static bool HasOpenJumpCancel(in FighterState fighter, CharacterData character)
    {
        foreach (CancelData cancel in character.CancelsOf(in character.Moves[fighter.Move]))
        {
            if (cancel.JumpCancel != 0 && IsOpen(in fighter, in cancel))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsOpen(in FighterState fighter, in CancelData cancel) =>
        fighter.MoveFrame >= cancel.FirstFrame && fighter.MoveFrame <= cancel.LastFrame &&
        (cancel.On & fighter.MoveOutcome) != 0;

    private int FindCancelTarget(
        in FighterState fighter,
        in InputHistory history,
        CharacterData character,
        in CancelData cancel,
        in MoveData current,
        CancelPermission rules)
    {
        ReadOnlySpan<MoveData> moves = character.Moves;
        ReadOnlySpan<int> targets = character.TargetsOf(in cancel);
        for (int move = 0; move < moves.Length; move++)
        {
            // Basic moves are entered by the state machine itself, never through a cancel.
            bool isAllowed = moves[move].Category != MoveCategory.Basic &&
                ((moves[move].Category & cancel.IntoCategories) != 0 || targets.Contains(move));
            if (!isAllowed || !CanStart(in fighter, in history, in moves[move]))
            {
                continue;
            }

            CancelCheck check = new(
                rules.Fighter,
                current.Category,
                current.Stance,
                fighter.MoveOutcome,
                moves[move].Category,
                isJump: false);
            if (rules.Allows(in check))
            {
                return move;
            }
        }

        return CharacterData.NoMove;
    }

    private bool CanStart(in FighterState fighter, in InputHistory history, in MoveData move)
    {
        MoveStance stance = fighter.IsAirborne != 0 ? MoveStance.Air : MoveStance.Ground;
        return move.Stance == stance && _input.IsTriggered(in history, in move, fighter.Facing);
    }
}
