using Sim.Characters;
using Sim.Fighters.States;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;

namespace Sim.Throws;

/// <summary>Wake-up and landing throw protection (<see cref="FighterState.ThrowInvulnerableFramesLeft"/>).</summary>
/// <remarks>
/// Runs at the end of the movement step, for each fighter that advances. The count includes the current frame:
/// - During wake-up it is held at <see cref="MatchRules.WakeUpThrowInvulnerableFrames"/> + 1, so the fighter can't
///   be thrown for that many frames after the wake-up ends (and not during it, whatever its move data says).
/// - On the frame a fighter touches down (on the ground after movement, still in an air action) it is set to
///   <see cref="MatchRules.LandingThrowInvulnerableFrames"/>: that many frames, the touchdown frame included. The
///   landing move starts on the next frame.
/// - Otherwise it counts down by one.
/// </remarks>
public sealed class ThrowProtectionStage : ITickStage
{
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly int _afterWakeUp;
    private readonly int _afterLanding;

    public ThrowProtectionStage(CharacterData player1, CharacterData player2, in MatchRules rules)
    {
        _player1 = player1;
        _player2 = player2;
        _afterWakeUp = rules.WakeUpThrowInvulnerableFrames;
        _afterLanding = rules.LandingThrowInvulnerableFrames;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        if (activity.Advances(0))
        {
            Update(ref state.Fighters[0], _player1);
        }

        if (activity.Advances(1))
        {
            Update(ref state.Fighters[1], _player2);
        }
    }

    private void Update(ref FighterState fighter, CharacterData character)
    {
        if (fighter.Action == FighterAction.WakeUp)
        {
            fighter.ThrowInvulnerableFramesLeft = _afterWakeUp + 1;
        }
        else if (fighter.IsAirborne == 0 && IsAirAction(in fighter, character))
        {
            fighter.ThrowInvulnerableFramesLeft = _afterLanding;
        }
        else if (fighter.ThrowInvulnerableFramesLeft > 0)
        {
            fighter.ThrowInvulnerableFramesLeft--;
        }
    }

    // What the state machine only turns into a landing on the next frame. Air hitstun lands in a knockdown, which
    // can't be thrown anyway.
    private static bool IsAirAction(in FighterState fighter, CharacterData character) => fighter.Action switch
    {
        FighterAction.Jump or FighterAction.AirDash or FighterAction.AirBackdash => true,
        FighterAction.Attack => character.Moves[fighter.Move].Stance == MoveStance.Air,
        _ => false,
    };
}
