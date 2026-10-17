using Sim.Data.Online;
using Sim.Data.Sources;
using Sim.Data.Tests.Support;
using Sim.Data.Validation;
using Xunit;

namespace Sim.Data.Tests.Online;

public sealed class OnlineSettingsLoaderTests
{
    private readonly InMemoryDataSource _data = new();

    [Fact]
    public void RepositorySettingsUseTheEightFrameRollbackWindowAndAOneToTwoFrameDelay()
    {
        OnlineSettings settings = OnlineSettingsLoader.Load(new DirectoryDataSource(Fixture.RepositoryDataRoot));

        Assert.Equal(8, settings.RollbackFrames);
        Assert.Equal(1, settings.AutomaticInputDelay.MinimumFrames);
        Assert.Equal(2, settings.AutomaticInputDelay.MaximumFrames);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(OnlineSchema.MaxRollbackFrames)]
    public void ReadsTheRollbackWindow(int frames)
    {
        _data.SetText(OnlineSchema.File, File(frames));

        Assert.Equal(frames, OnlineSettingsLoader.Load(_data).RollbackFrames);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(OnlineSchema.MaxRollbackFrames + 1)]
    public void ARollbackWindowOutOfRangeIsReported(int frames)
    {
        _data.SetText(OnlineSchema.File, File(frames));

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal((OnlineSchema.File, "rollbackFrames"), (error.File, error.Field));
    }

    [Fact]
    public void ReadsTheAutomaticInputDelay()
    {
        _data.SetText(OnlineSchema.File, File(8, Delay(0, OnlineSchema.MaxInputDelayFrames)));

        Assert.Equal(
            new AutomaticInputDelaySettings(0, OnlineSchema.MaxInputDelayFrames, 40, 30, 9),
            OnlineSettingsLoader.Load(_data).AutomaticInputDelay);
    }

    [Fact]
    public void AnAutomaticDelayRangeThatRunsBackwardsIsReported()
    {
        _data.SetText(OnlineSchema.File, File(8, Delay(2, 1)));

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal("automaticInputDelay.maximumFrames", error.Field);
    }

    [Fact]
    public void AnAutomaticDelayAboveThePlayersRangeIsReported()
    {
        _data.SetText(OnlineSchema.File, File(8, Delay(1, OnlineSchema.MaxInputDelayFrames + 1)));

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal("automaticInputDelay.maximumFrames", error.Field);
    }

    [Fact]
    public void AMissingAutomaticDelayIsReported()
    {
        _data.SetText(OnlineSchema.File, "{ \"schemaVersion\": 1, \"rollbackFrames\": 8 }");

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal("automaticInputDelay", error.Field);
    }

    [Fact]
    public void AMissingFileIsReported()
    {
        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal(OnlineSchema.File, error.File);
    }

    [Fact]
    public void AnUnknownKeyIsReported()
    {
        _data.SetText(OnlineSchema.File, File(8).Insert(1, " \"inputDelay\": 2,"));

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal("inputDelay", error.Field);
    }

    private static string File(int rollbackFrames) => File(rollbackFrames, Delay(1, 2));

    private static string File(int rollbackFrames, string automaticDelay) =>
        $"{{ \"schemaVersion\": 1, \"rollbackFrames\": {rollbackFrames}, \"automaticInputDelay\": {automaticDelay} }}";

    private static string Delay(int minimumFrames, int maximumFrames) =>
        $"{{ \"minimumFrames\": {minimumFrames}, \"maximumFrames\": {maximumFrames}, \"pingStepMilliseconds\": 40, " +
        "\"pingSampleIntervalFrames\": 30, \"pingSamples\": 9 }";
}
