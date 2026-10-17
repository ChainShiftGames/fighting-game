using Sim.Characters;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Puts a fighter into a move; the only way any system changes what a fighter is doing.</summary>
/// <remarks>
/// The state machine uses it for its own transitions; hit resolution (P1-12) uses <see cref="EnterStun"/> and
/// <see cref="EnterBasic"/> for hit reactions. Entering resets the move frame to 1 and clears the move's outcome.
/// </remarks>
public static class MoveEntry
{
    /// <summary>Enters an attack (any non-basic move), applies its flags and reports its category as started this frame.</summary>
    public static void EnterAttack(ref FighterState fighter, int move, CharacterData character)
    {
        Enter(ref fighter, FighterAction.Attack, move);
        ref readonly MoveData data = ref character.Moves[move];
        fighter.Flags = (fighter.Flags & ~data.ClearsFlags) | data.SetsFlags;
        fighter.StartedMoveCategory = data.Category;
        // The state machine marks a move it started from a shortcut; any other entry (a module's cancel) isn't one.
        fighter.IsShortcutMove = 0;
    }

    /// <summary>Enters a basic action with the character's move for it, or its idle move if it defines none.</summary>
    public static void EnterBasic(ref FighterState fighter, FighterAction action, CharacterData character)
    {
        int move = character.BasicStateMove((BasicState)action);
        Enter(ref fighter, action, move == CharacterData.NoMove ? character.BasicStateMove(BasicState.Idle) : move);
    }

    /// <summary>
    /// Enters a knockdown; with <see cref="FighterState.KnockdownFrames"/> set by the last hit, it lasts that long
    /// (counted in stun frames) instead of the knockdown move's length.
    /// </summary>
    public static void EnterKnockdown(ref FighterState fighter, CharacterData character)
    {
        EnterBasic(ref fighter, FighterAction.Knockdown, character);
        fighter.StunFramesLeft = fighter.KnockdownFrames;
    }

    /// <summary>Enters a hitstun or blockstun action lasting <paramref name="stunFrames"/> of the fighter's frames.</summary>
    public static void EnterStun(
        ref FighterState fighter, FighterAction action, int stunFrames, CharacterData character)
    {
        EnterBasic(ref fighter, action, character);
        fighter.StunFramesLeft = stunFrames;
    }

    private static void Enter(ref FighterState fighter, FighterAction action, int move)
    {
        fighter.Action = action;
        fighter.Move = move;
        fighter.MoveFrame = 1;
        fighter.MoveOutcome = CancelOn.None;
        fighter.StunFramesLeft = 0;
        fighter.IsShortcutMove = 0;
        fighter.HitsConnected = 0;
    }
}
