using Sim.Characters;
using Sim.Combat;
using Sim.Events;
using Sim.Fighters.States;
using Sim.Math;
using Sim.Modules;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Throws;

/// <summary>The throw holds of the hit resolution step: the tech window, then the throw lands.</summary>
/// <remarks>
/// Runs first in the step, for holds started on earlier frames (<see cref="ThrowResolutionStage"/>). On each frame
/// the held fighter advances (hitstop and freezes pause the window), in this order:
/// - The thrower is stunned, knocked down or in a throw itself (e.g. a projectile hit it): the hold breaks and the
///   held fighter is let go into its idle move.
/// - The held fighter input a throw tech (<see cref="ThrowTechInput"/>): both fighters tech (<see cref="ThrowTechs"/>).
/// - Otherwise the window counts down; when it closes the throw lands. The held fighter is released at the move's
///   release point in front of (or, for a back throw, behind) the thrower and takes the throw's hit: damage and
///   reaction (<see cref="HitReactions"/>, never a counter hit). Both fighters get its hitstop, the throw hit event
///   is emitted and the modules' throw hooks run. Pushboxes and walls sort out the release point in the next
///   collision step, and both fighters turn to face each other once they are free again.
/// A held fighter that left the hold some other way (a module's cancel, a hit) just has the hold forgotten. Each
/// fighter's hold is handled on its own, so the order of the fighters doesn't matter.
/// </remarks>
public sealed class ThrowHoldStage : ITickStage
{
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly ThrowTechInput _player1Tech;
    private readonly ThrowTechInput _player2Tech;
    private readonly ModuleLineup _modules;

    public ThrowHoldStage(
        CharacterData player1,
        CharacterData player2,
        ThrowTechInput player1Tech,
        ThrowTechInput player2Tech,
        ModuleLineup modules)
    {
        _player1 = player1;
        _player2 = player2;
        _player1Tech = player1Tech;
        _player2Tech = player2Tech;
        _modules = modules;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        for (int held = 0; held < MatchState.PlayerCount; held++)
        {
            if (activity.Advances(held))
            {
                Advance(ref state, held);
            }
        }
    }

    private void Advance(ref MatchState state, int held)
    {
        ref FighterState defender = ref state.Fighters[held];
        if (!ThrowHolds.IsHeld(in defender))
        {
            ThrowHolds.Clear(ref defender);
            return;
        }

        int thrower = MatchState.PlayerCount - 1 - held;
        CharacterData defenderCharacter = held == 0 ? _player1 : _player2;
        ThrowTechInput tech = held == 0 ? _player1Tech : _player2Tech;
        if (Throwability.IsStunnedOrInThrow(state.Fighters[thrower].Action))
        {
            ThrowHolds.Clear(ref defender);
            MoveEntry.EnterBasic(ref defender, FighterAction.Idle, defenderCharacter);
        }
        else if (tech.IsTriggered(in state.Inputs[held]))
        {
            ThrowTechs.Separate(ref state, _player1, _player2);
        }
        else if (--defender.ThrowTechFramesLeft == 0)
        {
            Land(ref state, thrower, held, defenderCharacter);
        }
    }

    private void Land(ref MatchState state, int thrower, int held, CharacterData defenderCharacter)
    {
        ref FighterState throwerState = ref state.Fighters[thrower];
        ref FighterState defender = ref state.Fighters[held];
        CharacterData throwerCharacter = thrower == 0 ? _player1 : _player2;
        int throwMove = defender.ThrownByMove;
        MoveData move = throwerCharacter.Moves[throwMove];

        // Validated data gives every move with a throw box exactly one hit (schema v1).
        HitData hit = throwerCharacter.HitsOf(in move)[0];
        ThrowHolds.Clear(ref defender);
        int releaseX = throwerState.Position.X + (throwerState.Facing * move.ThrowReleaseX);
        defender.Position = new IntVector2(releaseX, defender.Position.Y);
        HitReactions.TakeHit(ref defender, in hit, defenderCharacter);
        Hitstop.Extend(ref throwerState.Timing, hit.Hitstop);
        Hitstop.Extend(ref defender.Timing, hit.Hitstop);
        state.Events.Emit(state.Frame, FrameEventKind.ThrowHit, thrower, defender.Position, throwMove);
        _modules.RunOnThrow(ref state, new FighterContact(thrower, held));
    }
}
