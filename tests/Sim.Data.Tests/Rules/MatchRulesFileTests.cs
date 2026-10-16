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

        Assert.Equal((12, 2, 150_000), (rules.MotionWindow, rules.ButtonBuffer, rules.RoundStartDistance));
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
