using Sim.Characters;
using Sim.Combat;
using Sim.Events;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;

namespace Sim.Throws;

/// <summary>The throw part of the hit resolution step: grabs, throw against throw, throw against strike.</summary>
/// <remarks>
/// Runs before strikes resolve (<see cref="HitResolutionStage"/>), on what <see cref="ThrowDetectionStage"/> found,
/// and decides from the state as the step started, so neither port wins:
/// - Both fighters' throws touch on the same frame: a tech for both (<see cref="ThrowTechs"/>).
/// - A throw touches and the defender's strike touches the thrower on the same frame:
///   <see cref="MatchRules.ThrowVsStrike"/> decides. Either the throw grabs and the strike is dropped, or the strike
///   lands and the throw doesn't grab (its move plays on, and can't grab once the thrower is hit).
/// - Otherwise the throw grabs: the thrower's move records a hit (it can't connect again; its on-hit cancel windows
///   open) and the defender is held (<see cref="ThrowHolds"/>) for the tech window, which
///   <see cref="ThrowHoldStage"/> runs from the next frame on. A grab has no hitstop and deals no damage yet.
/// </remarks>
public sealed class ThrowResolutionStage : ITickStage
{
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly ThrowContacts _contacts;
    private readonly HitContacts _strikes;
    private readonly ThrowPriority _throwVsStrike;
    private readonly int _techWindowFrames;

    public ThrowResolutionStage(
        CharacterData player1, CharacterData player2, ThrowContacts contacts, HitContacts strikes, in MatchRules rules)
    {
        _player1 = player1;
        _player2 = player2;
        _contacts = contacts;
        _strikes = strikes;
        _throwVsStrike = rules.ThrowVsStrike;
        _techWindowFrames = rules.ThrowTechWindowFrames;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        bool byPlayer1 = _contacts.Touches(0);
        bool byPlayer2 = _contacts.Touches(1);
        if (byPlayer1 && byPlayer2)
        {
            ThrowTechs.Separate(ref state, _player1, _player2);
        }
        else if (byPlayer1 || byPlayer2)
        {
            Resolve(ref state, thrower: byPlayer1 ? 0 : 1);
        }
    }

    private void Resolve(ref MatchState state, int thrower)
    {
        int defender = MatchState.PlayerCount - 1 - thrower;
        if (_strikes.Touches(defender))
        {
            if (_throwVsStrike == ThrowPriority.Strike)
            {
                return;
            }

            _strikes.Cancel(defender);
        }

        ref FighterState throwerState = ref state.Fighters[thrower];
        throwerState.MoveOutcome |= CancelOn.Hit;
        CharacterData defenderCharacter = defender == 0 ? _player1 : _player2;
        ThrowHolds.Start(ref state.Fighters[defender], throwerState.Move, _techWindowFrames, defenderCharacter);
        state.Events.Emit(
            state.Frame, FrameEventKind.ThrowGrab, thrower, _contacts.PointOf(thrower), throwerState.Move);
    }
}
