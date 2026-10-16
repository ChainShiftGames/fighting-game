using Sim.Characters;
using Sim.Combat;
using Sim.Fighters.States;
using Sim.Rules;
using Sim.State;
using Xunit;
using static Sim.Tests.Combat.CombatCharacter;

namespace Sim.Tests.Combat;

public sealed class CounterHitsTests
{
    private static readonly CharacterData _character = Create();

    private static readonly CounterBonus _rulesCounterHit =
        new() { ExtraHitstun = 2, ExtraHitstop = 3, ExtraDamagePercent = 20 };

    private static readonly CounterBonus _rulesPunishCounter =
        new() { ExtraHitstun = 4, ExtraHitstop = 5, ExtraDamagePercent = 50 };

    private readonly CounterHits _counterHits = new(RulesWithBonuses());

    // Attacks here are active on frames 2-3 of 10: frame 1 is startup and 4-10 recovery.
    [Theory]
    [InlineData(1, CounterHitKind.CounterHit)]
    [InlineData(2, CounterHitKind.CounterHit)]
    [InlineData(3, CounterHitKind.CounterHit)]
    [InlineData(4, CounterHitKind.PunishCounter)]
    [InlineData(10, CounterHitKind.PunishCounter)]
    public void AnAttackingDefenderIsCounteredByItsMovePhase(int moveFrame, CounterHitKind expected)
    {
        FighterState defender = At(0, 1, Mid, moveFrame);

        Assert.Equal(expected, CounterHits.KindAgainst(in defender, _character));
    }

    [Theory]
    [InlineData(FighterAction.Idle)]
    [InlineData(FighterAction.Hitstun)]
    [InlineData(FighterAction.Blockstun)]
    [InlineData(FighterAction.Backdash)]
    public void ADefenderThatIsNotAttackingTakesAPlainHit(FighterAction action)
    {
        FighterState defender = At(0, 1) with { Action = action };

        Assert.Equal(CounterHitKind.None, CounterHits.KindAgainst(in defender, _character));
    }

    // An attack without active frames (here one with only invulnerable frames) has no startup or recovery to catch.
    [Fact]
    public void AnAttackWithoutActiveFramesIsNotCountered()
    {
        FighterState defender = At(0, 1, Invulnerable, 5);

        Assert.Equal(CounterHitKind.None, CounterHits.KindAgainst(in defender, _character));
    }

    [Fact]
    public void APlainHitIsUnchanged()
    {
        Assert.Equal(MidHit, _counterHits.Apply(in MidHit, CounterHitKind.None));
    }

    [Fact]
    public void ACounterHitAddsTheRulesBonusToStunStopAndDamage()
    {
        HitData hit = _counterHits.Apply(in MidHit, CounterHitKind.CounterHit);

        Assert.Equal((360, 14, 11), (hit.Damage, hit.Hitstun, hit.Hitstop));
        Assert.Equal((MidHit.ChipDamage, MidHit.Blockstun), (hit.ChipDamage, hit.Blockstun));
    }

    [Fact]
    public void APunishCounterAddsItsOwnRulesBonus()
    {
        HitData hit = _counterHits.Apply(in MidHit, CounterHitKind.PunishCounter);

        Assert.Equal((450, 16, 13), (hit.Damage, hit.Hitstun, hit.Hitstop));
    }

    [Fact]
    public void AHitsOwnBonusReplacesTheRulesDefault()
    {
        HitData own = MidHit with
        {
            CounterHit = new CounterBonus { IsSet = 1, ExtraHitstun = 10, ExtraHitstop = 0, ExtraDamagePercent = 0 },
        };

        HitData counterHit = _counterHits.Apply(in own, CounterHitKind.CounterHit);
        HitData punishCounter = _counterHits.Apply(in own, CounterHitKind.PunishCounter);

        Assert.Equal((300, 22, 8), (counterHit.Damage, counterHit.Hitstun, counterHit.Hitstop));
        Assert.Equal((450, 16, 13), (punishCounter.Damage, punishCounter.Hitstun, punishCounter.Hitstop));
    }

    [Fact]
    public void ExtraDamageRoundsDown()
    {
        HitData hit = _counterHits.Apply(MidHit with { Damage = 99 }, CounterHitKind.CounterHit);

        Assert.Equal(99 + 19, hit.Damage);
    }

    private static MatchRules RulesWithBonuses()
    {
        MatchRules rules = default;
        rules.CounterHit = _rulesCounterHit;
        rules.PunishCounter = _rulesPunishCounter;
        return rules;
    }
}
