using System;
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
            (12, 2, 150_000, 600_000, 640_000),
            (rules.MotionWindow, rules.ButtonBuffer, rules.RoundStartDistance, rules.WallDistance, rules.MaxPlayerDistance));
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
}
