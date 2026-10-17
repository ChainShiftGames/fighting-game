using System;
using System.Text.RegularExpressions;
using Sim.Characters;
using Sim.Data.Rules;
using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Sim.Rules;
using Xunit;

namespace Sim.Data.Tests.Rules;

public sealed class MatchRulesFileTests
{
    private readonly InMemoryDataSource _data = Fixture.InMemory();

    [Fact]
    public void RepositoryRulesStartEachFighter150PixelsFromTheCentre()
    {
        GameData data = Fixture.Load(new DirectoryDataSource(Fixture.RepositoryDataRoot));

        Assert.Equal(150_000, data.Rules.RoundStartDistance);
    }

    [Fact]
    public void CreateMatchRulesAppliesEveryValueFromData()
    {
        MatchRules rules = Fixture.Load(_data).CreateMatchRules();

        Assert.Equal(
            (12, 2, 150_000, 600_000, 640_000, TradeRule.Trade),
            (rules.MotionWindow, rules.ButtonBuffer, rules.RoundStartDistance, rules.WallDistance, rules.MaxPlayerDistance,
                rules.TradeRule));
    }

    [Fact]
    public void CreateMatchRulesAppliesTheRoundValues()
    {
        MatchRules rules = Fixture.Load(_data).CreateMatchRules();

        Assert.Equal(
            (5940, 2, 5, 120), (rules.RoundTimerFrames, rules.RoundsToWin, rules.MaxRounds, rules.RoundOverFrames));
    }

    [Fact]
    public void CreateMatchRulesAppliesTheEntityPoolSize()
    {
        Assert.Equal(4, Fixture.Load(_data).CreateMatchRules().EntitiesPerPlayer);
    }

    [Theory]
    [InlineData("\"entitiesPerPlayer\": 4", "\"entitiesPerPlayer\": 0")]
    [InlineData("\"entitiesPerPlayer\": 4", "\"entitiesPerPlayer\": 9")]
    public void AnEntityPoolSizeOutsideTheStateSlotsIsReported(string from, string to)
    {
        _data.Replace(MatchRulesSchema.File, from, to);

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, "entitiesPerPlayer"), (error.File, error.Field));
        Assert.Contains("from 1 to 8", error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateMatchRulesAppliesTheCounterHitBonuses()
    {
        MatchRules rules = Fixture.Load(_data).CreateMatchRules();

        Assert.Equal(new CounterBonus { ExtraHitstun = 2, ExtraHitstop = 2, ExtraDamagePercent = 20 }, rules.CounterHit);
        Assert.Equal(
            new CounterBonus { ExtraHitstun = 4, ExtraHitstop = 4, ExtraDamagePercent = 20 }, rules.PunishCounter);
    }

    [Fact]
    public void CreateMatchRulesAppliesTheThrowValues()
    {
        MatchRules rules = Fixture.Load(_data).CreateMatchRules();

        Assert.Equal(
            (5, 3, 3, ThrowPriority.Throw),
            (rules.ThrowTechWindowFrames, rules.WakeUpThrowInvulnerableFrames, rules.LandingThrowInvulnerableFrames,
                rules.ThrowVsStrike));
    }

    [Theory]
    [InlineData("\"throwTechWindowFrames\": 5", "\"throwTechWindowFrames\": 0", "throwTechWindowFrames")]
    [InlineData(
        "\"landingThrowInvulnerableFrames\": 3", "\"landingThrowInvulnerableFrames\": 100",
        "landingThrowInvulnerableFrames")]
    [InlineData("\"throwVsStrike\": \"throw\"", "\"throwVsStrike\": \"both\"", "throwVsStrike")]
    public void AWrongThrowValueNamesTheField(string from, string to, string field)
    {
        _data.Replace(MatchRulesSchema.File, from, to);

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, field), (error.File, error.Field));
    }

    [Theory]
    [InlineData("counterHitExtraHitstun")]
    [InlineData("punishCounterExtraDamagePercent")]
    public void AMissingCounterHitValueIsReported(string field)
    {
        _data.SetText(MatchRulesSchema.File, WithoutField(_data.GetText(MatchRulesSchema.File), field));

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, field), (error.File, error.Field));
    }

    [Theory]
    [InlineData("\"counterHitExtraHitstop\": 2", "\"counterHitExtraHitstop\": -1", "counterHitExtraHitstop")]
    [InlineData(
        "\"punishCounterExtraDamagePercent\": 20", "\"punishCounterExtraDamagePercent\": 1001",
        "punishCounterExtraDamagePercent")]
    public void ACounterHitValueOutOfRangeNamesTheField(string from, string to, string field)
    {
        _data.Replace(MatchRulesSchema.File, from, to);

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, field), (error.File, error.Field));
    }

    [Theory]
    [InlineData("entitiesPerPlayer")]
    [InlineData("roundTimerFrames")]
    [InlineData("roundsToWin")]
    [InlineData("maxRounds")]
    [InlineData("roundOverFrames")]
    public void AMissingRoundValueIsReported(string field)
    {
        _data.SetText(MatchRulesSchema.File, WithoutField(_data.GetText(MatchRulesSchema.File), field));

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, field), (error.File, error.Field));
    }

    [Theory]
    [InlineData("\"roundTimerFrames\": 5940", "\"roundTimerFrames\": 0", "roundTimerFrames")]
    [InlineData("\"roundsToWin\": 2", "\"roundsToWin\": 0", "roundsToWin")]
    [InlineData("\"roundOverFrames\": 120", "\"roundOverFrames\": 0", "roundOverFrames")]
    [InlineData("\"roundOverFrames\": 120", "\"roundOverFrames\": 601", "roundOverFrames")]
    public void ARoundValueOutOfRangeNamesTheField(string from, string to, string field)
    {
        _data.Replace(MatchRulesSchema.File, from, to);

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, field), (error.File, error.Field));
    }

    [Fact]
    public void ARoundLimitTooShortToDecideAMatchIsReported()
    {
        _data.Replace(MatchRulesSchema.File, "\"maxRounds\": 5", "\"maxRounds\": 2");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, "maxRounds"), (error.File, error.Field));
        Assert.Contains("roundsToWin", error.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("trade", TradeRule.Trade)]
    [InlineData("clash", TradeRule.Clash)]
    public void TheTradeRuleIsReadIntoTheMatchRules(string name, TradeRule expected)
    {
        _data.Replace(MatchRulesSchema.File, "\"tradeRule\": \"trade\"", $"\"tradeRule\": \"{name}\"");

        Assert.Equal(expected, Fixture.Load(_data).CreateMatchRules().TradeRule);
    }

    [Fact]
    public void AnUnknownTradeRuleNamesTheFieldAndTheValidNames()
    {
        _data.Replace(MatchRulesSchema.File, "\"tradeRule\": \"trade\"", "\"tradeRule\": \"p1_wins\"");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, "tradeRule"), (error.File, error.Field));
        Assert.Contains("clash", error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingTradeRuleIsReported()
    {
        _data.SetText(
            MatchRulesSchema.File,
            """
            { "schemaVersion": 1, "roundStartDistance": 150000, "wallDistance": 600000, "maxPlayerDistance": 640000,
              "roundTimerFrames": 5940, "roundsToWin": 2, "maxRounds": 5, "roundOverFrames": 120,
              "entitiesPerPlayer": 4, "counterHitExtraHitstun": 2, "counterHitExtraHitstop": 2,
              "counterHitExtraDamagePercent": 20, "punishCounterExtraHitstun": 4, "punishCounterExtraHitstop": 4,
              "punishCounterExtraDamagePercent": 20, "throwTechWindowFrames": 5, "wakeUpThrowInvulnerableFrames": 3,
              "landingThrowInvulnerableFrames": 3, "throwVsStrike": "throw", "shortcutDamagePercent": 80,
              "shortcutCooldownFrames": 0 }
            """);

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, "tradeRule"), (error.File, error.Field));
    }

    [Theory]
    [InlineData("1200001", "wallDistance")]
    [InlineData("300000", "roundStartDistance")]
    public void AMaxPlayerDistanceThatDoesNotFitTheStageIsReported(string maxPlayerDistance, string reasonNames)
    {
        _data.Replace(MatchRulesSchema.File, "640000", maxPlayerDistance);

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, "maxPlayerDistance"), (error.File, error.Field));
        Assert.Contains(reasonNames, error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void WallsAtTheStageCentreAreReported()
    {
        _data.Replace(MatchRulesSchema.File, "600000", "0");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, "wallDistance"), (error.File, error.Field));
    }

    [Fact]
    public void RoundsStartingInsideTheWallAreReported()
    {
        _data.Replace(MatchRulesSchema.File, "600000", "150000");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, "roundStartDistance"), (error.File, error.Field));
        Assert.Contains("wallDistance", error.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ADistanceOutOfRangeNamesTheFieldAndReason()
    {
        _data.Replace(MatchRulesSchema.File, "150000", "-1");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((MatchRulesSchema.File, "roundStartDistance"), (error.File, error.Field));
    }

    [Fact]
    public void AMissingFileIsReported()
    {
        _data.Remove(MatchRulesSchema.File);

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal(MatchRulesSchema.File, error.File);
    }

    [Fact]
    public void AMoveWhoseMotionIsNotInTheMotionSetIsReported()
    {
        _data.Replace(Fixture.MoveFile("5a"), "\"motion\": \"5\"", "\"motion\": \"999\"");

        DataError error = Assert.Single(Assert.Throws<DataValidationException>(() => Fixture.Load(_data)).Errors);
        Assert.Equal((Fixture.MoveFile("5a"), "input.motion"), (error.File, error.Field));
        Assert.Contains("motion 999", error.Reason, StringComparison.Ordinal);
    }

    // Every round, entity pool and counter hit value follows tradeRule in the file, so each has a comma before it.
    private static string WithoutField(string json, string field) =>
        Regex.Replace(json, $@",\s*""{field}"":\s*\d+", string.Empty);
}
