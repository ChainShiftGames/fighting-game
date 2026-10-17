using Sim.Characters;
using Sim.Fighters.Movement;
using Sim.Input;
using Sim.State;

namespace Sim.Fighters.States;

/// <summary>Starts jumps for the state machine: prejump into takeoff, double jumps and air dashes.</summary>
/// <remarks>
/// A jump's height is decided by the input that starts its prejump, its arc by the direction held at takeoff.
/// Double jumps and air dashes share the character's air action budget, which movement refills on landing.
/// Velocities are movement's job: this only sets the action and <see cref="FighterState.Takeoff"/>.
/// </remarks>
public sealed class JumpActions
{
    private readonly IMoveInput _input;

    public JumpActions(IMoveInput input)
    {
        _input = input;
    }

    /// <summary>Call after every transition: decides the height of a prejump that started this frame.</summary>
    public void DecideHeight(ref FighterState fighter, in InputHistory history)
    {
        if (fighter.Action == FighterAction.Prejump && fighter.MoveFrame == 1)
        {
            fighter.IsHighJump = _input.IsHighJumpTriggered(in history, fighter.Facing) ? 1 : 0;
        }
    }

    public static void AdvancePrejump(ref FighterState fighter, in InputHistory history, CharacterData character)
    {
        if (fighter.MoveFrame > character.Stats.PrejumpFrames)
        {
            Start(ref fighter, in history, character, fighter.IsHighJump != 0 ? Takeoff.HighJump : Takeoff.Jump);
            fighter.IsHighJump = 0;
        }
    }

    /// <summary>Starts an air dash, backward air dash or double jump if input and the budget allows it.</summary>
    public bool TryStartAirAction(ref FighterState fighter, in InputHistory history, CharacterData character)
    {
        if (fighter.AirActionsUsed >= character.Stats.AirActions)
        {
            return false;
        }

        if (_input.IsDashTriggered(in history, fighter.Facing))
        {
            MoveEntry.EnterBasic(ref fighter, FighterAction.AirDash, character);
        }
        else if (_input.IsBackdashTriggered(in history, fighter.Facing))
        {
            MoveEntry.EnterBasic(ref fighter, FighterAction.AirBackdash, character);
        }
        else if (_input.IsAirJumpTriggered(in history, fighter.Facing))
        {
            Start(ref fighter, in history, character, Takeoff.AirJump);
        }
        else
        {
            return false;
        }

        fighter.AirActionsUsed++;
        return true;
    }

    /// <summary>
    /// The input for a jump cancel out of an attack: on the ground up held, as for a jump from a free action; in the
    /// air a new jump input with air actions left, as for a double jump.
    /// </summary>
    public bool IsJumpCancelInput(in FighterState fighter, in InputHistory history, CharacterData character)
    {
        if (fighter.IsAirborne == 0)
        {
            return NumpadDirection.IsUp(NumpadDirection.Of(history.FramesAgo(0), fighter.Facing));
        }

        return fighter.AirActionsUsed < character.Stats.AirActions &&
            _input.IsAirJumpTriggered(in history, fighter.Facing);
    }

    /// <summary>Starts the jump cancel <see cref="IsJumpCancelInput"/> found: a prejump, or a double jump.</summary>
    public void StartJumpCancel(ref FighterState fighter, in InputHistory history, CharacterData character)
    {
        if (fighter.IsAirborne == 0)
        {
            MoveEntry.EnterBasic(ref fighter, FighterAction.Prejump, character);
            return;
        }

        Start(ref fighter, in history, character, Takeoff.AirJump);
        fighter.AirActionsUsed++;
    }

    private static void Start(
        ref FighterState fighter, in InputHistory history, CharacterData character, Takeoff takeoff)
    {
        MoveEntry.EnterBasic(ref fighter, FighterAction.Jump, character);
        int direction = NumpadDirection.Of(history.FramesAgo(0), fighter.Facing);
        fighter.Takeoff = takeoff;
        fighter.TakeoffDirection =
            NumpadDirection.IsForward(direction) ? 1 : NumpadDirection.IsBack(direction) ? -1 : 0;
    }
}
