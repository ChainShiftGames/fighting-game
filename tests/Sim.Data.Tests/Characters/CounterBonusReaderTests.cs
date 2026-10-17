using System;
using Sim.Characters;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Xunit;

namespace Sim.Data.Tests.Characters;

public sealed class CounterBonusReaderTests
{
    private const string Pushback = "\"pushback\": 12000";

    private readonly InMemoryDataSource _data = Fixture.InMemory();

    [Fact]
    public void AHitWithoutItsOwnBonusesLeavesThemToTheMatchRules()
    {
        HitData hit = HitOf5a();

        Assert.Equal((0, 0), (hit.CounterHit.IsSet, hit.PunishCounter.IsSet));
    }

    [Fact]
    public void AHitsOwnBonusIsReadAndMarkedAsSet()
    {
        _data.Replace(
            Fixture.MoveFile("5a"),
            Pushback,
            $"{Pushback}, \"punishCounter\": {{ \"extraHitstun\": 6, \"extraHitstop\": 3, \"extraDamagePercent\": 50 }}");

        HitData hit = HitOf5a();

        Assert.Equal(
            new CounterBonus { IsSet = 1, ExtraHitstun = 6, ExtraHitstop = 3, ExtraDamagePercent = 50 },
            hit.PunishCounter);
        Assert.Equal(0, hit.CounterHit.IsSet);
    }

    [Fact]
    public void AnIncompleteOwnBonusNamesTheMissingField()
    {
        _data.Replace(
            Fixture.MoveFile("5a"), Pushback, $"{Pushback}, \"counterHit\": {{ \"extraHitstun\": 6, \"extraHitstop\": 3 }}");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => HitOf5a()).Errors);
        Assert.Equal((Fixture.MoveFile("5a"), "hits[0].counterHit.extraDamagePercent"), (error.File, error.Field));
    }

    private HitData HitOf5a()
    {
        CharacterData character = Fixture.Load(_data).Character(Fixture.CharacterId);
        int move = Array.IndexOf(character.MoveIds.ToArray(), "5a");
        return character.HitsOf(in character.Moves[move])[0];
    }
}
