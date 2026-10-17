using System;
using System.Linq;
using Sim.Characters;
using Sim.Data.Rules;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Xunit;

namespace Sim.Data.Tests.Characters;

/// <summary>ADR-0023: fighter flags, move conditions and costs, and follow-up cancel windows in data.</summary>
/// <remarks>The fixture's rules/flags.json names stance (bit 0, cleared when hit) and install (bit 1).</remarks>
public sealed class MoveConditionTests
{
    private const string Stance = "stance";
    private static readonly string _move5A = Fixture.MoveFile("5a");
    private static readonly string _moveIdle = Fixture.MoveFile("idle");

    private readonly InMemoryDataSource _data = Fixture.InMemory();

    [Fact]
    public void FlagsAreReadInBitOrderWithTheOnesAHitClears()
    {
        GameData data = Load();

        Assert.Equal([Stance, "install"], data.Flags.Names.Names);
        Assert.Equal(1, data.Flags.ClearedWhenHit);
        Assert.Equal(1, data.CreateMatchRules().FlagsClearedWhenHit);
    }

    [Fact]
    public void ConditionsFlagsAndCostBecomeMasksAndAmounts()
    {
        AddTo5A("""
              "requires": { "flags": ["install"], "notFlags": ["stance"] },
              "setsFlags": ["stance"],
              "clearsFlags": ["install"],
              "cost": { "channel": 2, "amount": 2500 },
              "followUpOnly": true,
            """);

        MoveData move = Move5A();

        Assert.Equal((2, 1, 1, 2), (move.RequiredFlags, move.ForbiddenFlags, move.SetsFlags, move.ClearsFlags));
        Assert.Equal((2, 2500, 1), (move.CostChannel, move.CostAmount, move.IsFollowUpOnly));
    }

    [Fact]
    public void AMoveWithoutConditionsIsFree()
    {
        MoveData move = Move5A();

        Assert.Equal((0, 0, 0, 0, 0), (move.RequiredFlags, move.ForbiddenFlags, move.SetsFlags, move.CostAmount, move.IsFollowUpOnly));
    }

    [Fact]
    public void AFollowUpWindowIsMarked()
    {
        _data.Replace(_move5A, "\"into\": [\"5a\"] }", "\"into\": [\"5a\"], \"followUp\": true }");

        CharacterData character = Load().Character(Fixture.CharacterId);

        ReadOnlySpan<CancelData> cancels = character.CancelsOf(in character.Moves[1]);
        Assert.Equal((0, 1), (cancels[0].IsFollowUp, cancels[1].IsFollowUp));
    }

    [Theory]
    [InlineData("\"requires\": { \"flags\": [\"stnace\"] },", "requires.flags[0]")]
    [InlineData("\"requires\": { \"flags\": [\"stance\"], \"notFlags\": [\"stance\"] },", "requires.notFlags")]
    [InlineData("\"setsFlags\": [\"nope\"],", "setsFlags[0]")]
    [InlineData("\"cost\": { \"channel\": 8, \"amount\": 1 },", "cost.channel")]
    [InlineData("\"cost\": { \"channel\": 0, \"amount\": 0 },", "cost.amount")]
    public void AWrongConditionIsReported(string fields, string field)
    {
        AddTo5A(fields);

        DataError error = SingleError();
        Assert.Equal((_move5A, field), (error.File, error.Field));
    }

    [Fact]
    public void AFollowUpWindowMustNameItsMovesOnly()
    {
        _data.Replace(_move5A, "\"intoCategories\": [\"special\", \"super\"] }",
            "\"intoCategories\": [\"special\", \"super\"], \"followUp\": true }");

        DataError error = SingleError();
        Assert.Equal((_move5A, "cancels[0].followUp"), (error.File, error.Field));
    }

    [Fact]
    public void BasicMovesHaveNoConditions()
    {
        _data.Replace(_moveIdle, "\"category\": \"basic\",", "\"category\": \"basic\", \"setsFlags\": [\"stance\"],");

        DataError error = SingleError();
        Assert.Equal((_moveIdle, "setsFlags"), (error.File, error.Field));
    }

    [Theory]
    [InlineData("""{ "name": "stance", "clearedWhenHit": true }, { "name": "stance", "clearedWhenHit": false }""", "flags[1].name")]
    [InlineData("""{ "name": "Stance", "clearedWhenHit": true }""", "flags[0].name")]
    [InlineData("""{ "name": "stance" }""", "flags[0].clearedWhenHit")]
    public void AWrongFlagListIsReported(string flags, string field)
    {
        _data.SetText(FighterFlagsSchema.File, $$"""{ "schemaVersion": 1, "flags": [{{flags}}] }""");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(Load).Errors);
        Assert.Equal((FighterFlagsSchema.File, field), (error.File, error.Field));
    }

    [Fact]
    public void MoreThanThirtyTwoFlagsAreReported()
    {
        string flags = string.Join(", ", Enumerable.Range(0, 33)
            .Select(i => $$"""{ "name": "flag_{{i}}", "clearedWhenHit": false }"""));
        _data.SetText(FighterFlagsSchema.File, $$"""{ "schemaVersion": 1, "flags": [{{flags}}] }""");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(Load).Errors);
        Assert.Equal((FighterFlagsSchema.File, "flags"), (error.File, error.Field));
    }

    [Fact]
    public void TheFlagsAreInTheDataHash()
    {
        GameData before = Load();

        _data.Replace(FighterFlagsSchema.File, "\"clearedWhenHit\": false", "\"clearedWhenHit\": true");

        Assert.NotEqual(before.Hash, Load().Hash);
    }

    private void AddTo5A(string fields) =>
        _data.Replace(_move5A, "\"usableIn\": \"ground\",", "\"usableIn\": \"ground\",\n" + fields);

    private MoveData Move5A() => Load().Character(Fixture.CharacterId).Moves[1];

    private GameData Load() => Fixture.Load(_data);

    private DataError SingleError() => Assert.Single(Assert.Throws<DataValidationException>(Load).Errors);
}
