using Sim.Characters;
using Sim.Fighters.States;
using Sim.Math;
using Sim.Rules;
using Sim.State;

namespace Sim.Combat;

/// <summary>Counter hits and punish counters (core, ADR-0017): which one a hit is, and what it adds to the hit.</summary>
/// <remarks>
/// A hit is a counter hit when the defender is in an attack's startup or active frames, and a punish counter in its
/// recovery, on the defender's move frame as the hit lands. Anything else is a plain hit: basic actions (walks,
/// dashes, backdashes, landing, stun) and attacks without active frames. Blocks are never counters. Fighter attacks
/// and projectiles follow the same rules. The bonus is the hit's own from data, else the match rules' default.
/// Built once at match start.
/// </remarks>
public sealed class CounterHits
{
    private const int Percent = 100;

    private readonly CounterBonus _counterHit;
    private readonly CounterBonus _punishCounter;

    public CounterHits(in MatchRules rules)
    {
        _counterHit = rules.CounterHit;
        _punishCounter = rules.PunishCounter;
    }

    public static CounterHitKind KindAgainst(in FighterState defender, CharacterData defenderCharacter)
    {
        if (defender.Action != FighterAction.Attack)
        {
            return CounterHitKind.None;
        }

        return MovePhases.Of(in defenderCharacter.Moves[defender.Move], defender.MoveFrame) switch
        {
            MovePhase.Startup or MovePhase.Active => CounterHitKind.CounterHit,
            MovePhase.Recovery => CounterHitKind.PunishCounter,
            _ => CounterHitKind.None,
        };
    }

    /// <summary>The hit with the bonus of <paramref name="kind"/> added: hitstun, hitstop and damage.</summary>
    public HitData Apply(in HitData hit, CounterHitKind kind)
    {
        if (kind == CounterHitKind.None)
        {
            return hit;
        }

        CounterBonus bonus = BonusOf(in hit, kind);
        return hit with
        {
            Damage = hit.Damage + IntMath.MulDiv(hit.Damage, bonus.ExtraDamagePercent, Percent),
            Hitstun = hit.Hitstun + bonus.ExtraHitstun,
            Hitstop = hit.Hitstop + bonus.ExtraHitstop,
        };
    }

    private CounterBonus BonusOf(in HitData hit, CounterHitKind kind)
    {
        CounterBonus own = kind == CounterHitKind.CounterHit ? hit.CounterHit : hit.PunishCounter;
        if (own.IsSet != 0)
        {
            return own;
        }

        return kind == CounterHitKind.CounterHit ? _counterHit : _punishCounter;
    }
}
