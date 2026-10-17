using System;
using Sim.Characters;
using Sim.Modules;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Picks the move a fighter starts this frame, if any, in the character's input priority order.</summary>
/// <remarks>
/// A move starts from its motion and button, or from its special button shortcut (ADR-0020) while the fighter's
/// shortcut cooldown is over; <see cref="PickedByShortcut"/> tells which, for the move just returned. A move input by
/// a multi-step motion (236C) beats a shortcut; a shortcut beats a single-direction move (5C) the same press matches.
/// Without a shortcut input, the first move in the character's order wins.
/// </remarks>
public sealed class MoveSelection
{
    private readonly IMoveInput _input;

    public MoveSelection(IMoveInput input)
    {
        _input = input;
    }

    /// <summary>The move the last search returned came from its shortcut, not its motion (only then valid).</summary>
    public bool PickedByShortcut { get; private set; }

    /// <summary>Any non-basic move usable in the fighter's stance (from a free action).</summary>
    public int FindFreeMove(in FighterState fighter, in InputHistory history, CharacterData character)
    {
        int byMotion = FindFreeMove(in fighter, in history, character, byShortcut: false);
        if (BeatsShortcuts(byMotion, character))
        {
            return Picked(byMotion, byShortcut: false);
        }

        int byShortcut = FindFreeMove(in fighter, in history, character, byShortcut: true);
        return byShortcut != CharacterData.NoMove ? Picked(byShortcut, byShortcut: true) : Picked(byMotion, byShortcut: false);
    }

    /// <summary>A move an open cancel window of the current move allows; windows are checked in data order.</summary>
    /// <remarks>
    /// Only a move that was input is offered to <paramref name="rules"/>; a refused one doesn't stop the search.
    /// </remarks>
    public int FindCancel(
        in FighterState fighter, in InputHistory history, CharacterData character, CancelPermission rules)
    {
        int byMotion = FindCancel(in fighter, in history, character, rules, byShortcut: false);
        if (BeatsShortcuts(byMotion, character))
        {
            return Picked(byMotion, byShortcut: false);
        }

        int byShortcut = FindCancel(in fighter, in history, character, rules, byShortcut: true);
        return byShortcut != CharacterData.NoMove ? Picked(byShortcut, byShortcut: true) : Picked(byMotion, byShortcut: false);
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

    // A move from a multi-step motion keeps its motion even when a shortcut was input too, so it gets no trade-off.
    private bool BeatsShortcuts(int move, CharacterData character) =>
        move != CharacterData.NoMove && _input.HasMultiStepMotion(in character.Moves[move]);

    private int Picked(int move, bool byShortcut)
    {
        PickedByShortcut = byShortcut;
        return move;
    }

    private int FindFreeMove(in FighterState fighter, in InputHistory history, CharacterData character, bool byShortcut)
    {
        ReadOnlySpan<MoveData> moves = character.Moves;
        for (int move = 0; move < moves.Length; move++)
        {
            if (moves[move].Category != MoveCategory.Basic && CanStart(in fighter, in history, in moves[move], byShortcut))
            {
                return move;
            }
        }

        return CharacterData.NoMove;
    }

    private int FindCancel(
        in FighterState fighter,
        in InputHistory history,
        CharacterData character,
        CancelPermission rules,
        bool byShortcut)
    {
        MoveData current = character.Moves[fighter.Move];
        foreach (CancelData cancel in character.CancelsOf(in current))
        {
            if (!IsOpen(in fighter, in cancel))
            {
                continue;
            }

            int move = FindCancelTarget(in fighter, in history, character, in cancel, in current, rules, byShortcut);
            if (move != CharacterData.NoMove)
            {
                return move;
            }
        }

        return CharacterData.NoMove;
    }

    private int FindCancelTarget(
        in FighterState fighter,
        in InputHistory history,
        CharacterData character,
        in CancelData cancel,
        in MoveData current,
        CancelPermission rules,
        bool byShortcut)
    {
        ReadOnlySpan<MoveData> moves = character.Moves;
        ReadOnlySpan<int> targets = character.TargetsOf(in cancel);
        for (int move = 0; move < moves.Length; move++)
        {
            // Basic moves are entered by the state machine itself, never through a cancel.
            bool isAllowed = moves[move].Category != MoveCategory.Basic &&
                ((moves[move].Category & cancel.IntoCategories) != 0 || targets.Contains(move));
            if (!isAllowed || !CanStart(in fighter, in history, in moves[move], byShortcut))
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

    private bool CanStart(in FighterState fighter, in InputHistory history, in MoveData move, bool byShortcut)
    {
        MoveStance stance = fighter.IsAirborne != 0 ? MoveStance.Air : MoveStance.Ground;
        // A locked category (core request, e.g. a super without meter) is skipped, so the input falls through to the
        // next move in priority order.
        if (move.Stance != stance || (move.Category & fighter.LockedMoveCategories) != 0)
        {
            return false;
        }

        return byShortcut
            ? fighter.ShortcutCooldownLeft == 0 && _input.IsShortcutTriggered(in history, in move, fighter.Facing)
            : _input.IsTriggered(in history, in move, fighter.Facing);
    }
}
