using Sim.Characters;
using Sim.Events;
using Sim.Modules;
using Sim.Pipeline;
using Sim.Rules;
using Sim.State;

namespace Sim.Combat;

/// <summary>The hit resolution step of the tick: what this frame's contacts do, then the hit and block hooks.</summary>
/// <remarks>
/// For each attack <see cref="HitDetectionStage"/> found touching: blocked if the defender's stance stops its guard
/// (<see cref="Guarding"/>: stand or crouch block, air block through a module), else a hit, with its counter hit bonus when it caught the defender attacking
/// (<see cref="CounterHits"/>), as the modules adjust it (<see cref="IHitAdjustmentHook"/>; a hit they make whiff
/// doesn't connect). The attacker's move records the outcome (for its cancel windows, and so it
/// can't connect again); the defender takes the hit or block (<see cref="HitReactions"/>); both fighters get the
/// hit's hitstop, the longer one if two hits trade. Hitstop counts from the next frame (<see cref="TimeControl"/>).
/// Same-frame exchanges (<see cref="HitContacts.IsSameFrameExchange"/>, which counts projectile touches too) follow
/// <see cref="MatchRules.TradeRule"/>; projectiles resolve after this stage (<c>Sim.Entities</c>).
/// Both contacts are decided from the state as the
/// step started and written together, so neither port wins a trade (symmetry rule of <see cref="ITickStage"/>).
/// Afterwards each contact emits its event (followed by a counter hit event on a counter) and runs the module hooks,
/// P1's attack first; modules see the frame fully resolved, and their requests apply at the end of the frame.
/// </remarks>
public sealed class HitResolutionStage : ITickStage
{
    private readonly CharacterData _player1;
    private readonly CharacterData _player2;
    private readonly HitContacts _contacts;
    private readonly ModuleLineup _modules;
    private readonly TradeRule _tradeRule;
    private readonly CounterHits _counterHits;
    private readonly ShortcutDamage _shortcutDamage;
    private readonly Pushback _pushback;
    private readonly Guarding _guarding;

    public HitResolutionStage(
        CharacterData player1,
        CharacterData player2,
        HitContacts contacts,
        ModuleLineup modules,
        TradeRule tradeRule,
        CounterHits counterHits,
        ShortcutDamage shortcutDamage,
        Guarding guarding,
        Pushback pushback)
    {
        _player1 = player1;
        _player2 = player2;
        _contacts = contacts;
        _modules = modules;
        _tradeRule = tradeRule;
        _counterHits = counterHits;
        _shortcutDamage = shortcutDamage;
        _guarding = guarding;
        _pushback = pushback;
    }

    public void Run(ref MatchState state, in FrameActivity activity)
    {
        if (_contacts.IsSameFrameExchange && _tradeRule == TradeRule.Clash)
        {
            Clash(ref state);
            return;
        }

        FighterState player1 = state.Fighters[0];
        FighterState player2 = state.Fighters[1];
        Connection byPlayer1 = Decide(ref state, 0, _player1, in player1, in player2, _player2);
        Connection byPlayer2 = Decide(ref state, 1, _player2, in player2, in player1, _player1);
        if (!byPlayer1.IsMade && !byPlayer2.IsMade)
        {
            return;
        }

        MarkAttacker(ref player1, in byPlayer1);
        MarkAttacker(ref player2, in byPlayer2);
        React(ref player2, in byPlayer1, _player2, player1.Facing);
        React(ref player1, in byPlayer2, _player1, player2.Facing);
        int hitstop = Longer(HitstopOf(in byPlayer1), HitstopOf(in byPlayer2));
        Hitstop.Extend(ref player1.Timing, hitstop);
        Hitstop.Extend(ref player2.Timing, hitstop);
        state.Fighters[0] = player1;
        state.Fighters[1] = player2;

        Emit(ref state, 0, in byPlayer1);
        Emit(ref state, 1, in byPlayer2);
        RunHooks(ref state, 0, in byPlayer1);
        RunHooks(ref state, 1, in byPlayer2);
    }

    private Connection Decide(
        ref MatchState state,
        int attacker,
        CharacterData attackerCharacter,
        in FighterState attackerState,
        in FighterState defender,
        CharacterData defenderCharacter)
    {
        if (!_contacts.Touches(attacker))
        {
            return Connection.None;
        }

        // Detection only reports a touch on a frame one of the move's hits covers.
        MoveData move = attackerCharacter.Moves[attackerState.Move];
        int hitIndex = attackerCharacter.HitIndexAt(in move, attackerState.MoveFrame);
        HitData hit = attackerCharacter.HitsOf(in move)[hitIndex];
        int defenderIndex = MatchState.PlayerCount - 1 - attacker;
        AirGuardCheck check = new(attacker, defenderIndex, move.Category, move.Stance, isProjectile: false);
        BlockStance stance = _guarding.StanceOf(
            ref state, in defender, in attackerState, state.Inputs[defenderIndex].FramesAgo(0), in check);
        bool isBlocked = Blocking.Stops(stance, hit.Guard);
        CounterHitKind counter =
            isBlocked ? CounterHitKind.None : CounterHits.KindAgainst(in defender, defenderCharacter);
        HitData landed = _shortcutDamage.Apply(_counterHits.Apply(in hit, counter), in attackerState);
        if (isBlocked)
        {
            _modules.AdjustBlock(ref state, new FighterContact(attacker, defenderIndex), ref landed);
        }
        else if (!_modules.AdjustHit(ref state, new FighterContact(attacker, defenderIndex), ref landed))
        {
            return Connection.None;
        }

        return new Connection
        {
            Outcome = isBlocked ? HitOutcome.Block : HitOutcome.Hit,
            Stance = isBlocked ? stance : BlockStance.None,
            Hit = landed,
            Counter = counter,
            Move = attackerState.Move,
            HitIndex = hitIndex,
        };
    }

    // Every touching move is spent and nobody is hurt; each attacker reports the clash where its attack met the other.
    // In an exchange with a projectile only one fighter's attack may have touched.
    private void Clash(ref MatchState state)
    {
        for (int fighter = 0; fighter < MatchState.PlayerCount; fighter++)
        {
            if (!_contacts.Touches(fighter))
            {
                continue;
            }

            ref FighterState attacker = ref state.Fighters[fighter];
            CharacterData character = fighter == 0 ? _player1 : _player2;
            int hit = character.HitIndexAt(in character.Moves[attacker.Move], attacker.MoveFrame);
            attacker.MoveOutcome |= CancelOn.Clash;
            attacker.HitsConnected |= hit >= 0 ? 1 << hit : 0;
            state.Events.Emit(state.Frame, FrameEventKind.Clash, fighter, _contacts.PointOf(fighter), attacker.Move);
        }
    }

    private static void MarkAttacker(ref FighterState attacker, in Connection connection)
    {
        if (connection.IsMade)
        {
            attacker.MoveOutcome |= connection.Outcome == HitOutcome.Hit ? CancelOn.Hit : CancelOn.Block;
            attacker.HitsConnected |= 1 << connection.HitIndex;
        }
    }

    private void React(ref FighterState defender, in Connection connection, CharacterData character, int attackFacing)
    {
        if (connection.Outcome == HitOutcome.Hit)
        {
            HitReactions.TakeHit(ref defender, connection.Hit, character, attackFacing);
        }
        else if (connection.Outcome == HitOutcome.Block)
        {
            HitReactions.Block(ref defender, connection.Hit, connection.Stance, character);
        }

        if (connection.IsMade)
        {
            _pushback.Start(ref defender, connection.Hit.Pushback, attackFacing, pushesAttacker: true);
        }
    }

    private static int HitstopOf(in Connection connection) => connection.IsMade ? connection.Hit.Hitstop : 0;

    private static int Longer(int frames, int otherFrames) => frames > otherFrames ? frames : otherFrames;

    private void Emit(ref MatchState state, int attacker, in Connection connection)
    {
        if (connection.IsMade)
        {
            FrameEventKind kind = connection.Outcome == HitOutcome.Hit ? FrameEventKind.Hit : FrameEventKind.Block;
            state.Events.Emit(state.Frame, kind, attacker, _contacts.PointOf(attacker), connection.Move);
            CounterHitEvent.Emit(ref state, connection.Counter, attacker, _contacts.PointOf(attacker));
        }
    }

    private void RunHooks(ref MatchState state, int attacker, in Connection connection)
    {
        FighterContact contact = new(attacker, defender: MatchState.PlayerCount - 1 - attacker);
        if (connection.Outcome == HitOutcome.Hit)
        {
            _modules.RunOnHit(ref state, in contact);
        }
        else if (connection.Outcome == HitOutcome.Block)
        {
            _modules.RunOnBlock(ref state, in contact);
        }
    }
}
