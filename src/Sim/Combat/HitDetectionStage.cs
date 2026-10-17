using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.Pipeline;
using Sim.State;

namespace Sim.Combat;

/// <summary>The hit detection step of the tick: whose hitboxes touch the opponent's hurtboxes this frame.</summary>
/// <remarks>
/// Runs after collision, on the final positions of the frame, for every fighter whether it advances or not (a
/// fighter in hitstop keeps its boxes). An attack touches when its hitboxes on the current move frame overlap the
/// opponent's hurtboxes, the move's hit for that frame hasn't connected yet (each hit connects once, ADR-0022), the
/// opponent isn't invulnerable on its move frame, and a knocked-down opponent is only touched by an OTG hit. Both fighters are checked against the state as the step started; it only records
/// contacts in <see cref="HitContacts"/>, so what they do is up to <see cref="HitResolutionStage"/>.
/// </remarks>
public sealed class HitDetectionStage : ITickStage
{
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly HitContacts _contacts;

    public HitDetectionStage(CharacterData player1, CharacterData player2, HitContacts contacts)
    {
        _player1 = player1;
        _player2 = player2;
        _contacts = contacts;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        _contacts.Clear();
        Detect(0, _player1, in state.Fighters[0], _player2, in state.Fighters[1]);
        Detect(1, _player2, in state.Fighters[1], _player1, in state.Fighters[0]);
    }

    private void Detect(
        int attacker,
        CharacterData attackerCharacter,
        in FighterState attackerState,
        CharacterData defenderCharacter,
        in FighterState defender)
    {
        MoveData move = attackerCharacter.Moves[attackerState.Move];
        int hit = attackerCharacter.HitIndexAt(in move, attackerState.MoveFrame);
        bool canTouch = hit >= 0 && (attackerState.HitsConnected & (1 << hit)) == 0 &&
            !defenderCharacter.Moves[defender.Move].IsInvulnerableOn(defender.MoveFrame) &&
            (defender.Action != FighterAction.Knockdown || attackerCharacter.HitsOf(in move)[hit].Otg != 0);
        if (canTouch && HitboxOverlap.TryFind(
            attackerCharacter, in attackerState, defenderCharacter, in defender, out IntVector2 point))
        {
            _contacts.Record(attacker, point);
        }
    }
}
