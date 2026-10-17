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

    [Fact]
    public void RepositorySettingsWarnAfterThreeSecondsAndEndAfterTen()
    {
        OnlineSettings settings = OnlineSettingsLoader.Load(new DirectoryDataSource(Fixture.RepositoryDataRoot));

        Assert.Equal(new DisconnectSettings(3000, 10_000), settings.Disconnect);
    }

    [Fact]
    public void ReadsTheDisconnectTimeouts()
    {
        _data.SetText(OnlineSchema.File, File(8, Delay(1, 2), Disconnect(500, 2000)));

        Assert.Equal(new DisconnectSettings(500, 2000), OnlineSettingsLoader.Load(_data).Disconnect);
    }

    [Theory]
    [InlineData(2000, 2000, "disconnect.endMatchAfterMilliseconds")]
    [InlineData(3000, 2000, "disconnect.endMatchAfterMilliseconds")]
    [InlineData(OnlineSchema.MinDisconnectWarningMilliseconds - 1, 2000, "disconnect.warningAfterMilliseconds")]
    [InlineData(500, OnlineSchema.MaxDisconnectMilliseconds + 1, "disconnect.endMatchAfterMilliseconds")]
    public void DisconnectTimeoutsOutOfOrderOrRangeAreReported(int warningAfter, int endMatchAfter, string field)
    {
        _data.SetText(OnlineSchema.File, File(8, Delay(1, 2), Disconnect(warningAfter, endMatchAfter)));

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal(field, error.Field);
    }

    [Fact]
    public void MissingDisconnectTimeoutsAreReported()
    {
        _data.SetText(
            OnlineSchema.File,
            $"{{ \"schemaVersion\": 1, \"rollbackFrames\": 8, \"automaticInputDelay\": {Delay(1, 2)}, \"desync\": {Desync(600)} }}");

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal("disconnect", error.Field);
    }

    [Fact]
    public void RepositorySettingsKeepTenSecondsOfDesyncHistory()
    {
        OnlineSettings settings = OnlineSettingsLoader.Load(new DirectoryDataSource(Fixture.RepositoryDataRoot));

        Assert.Equal(new DesyncSettings(600), settings.Desync);
    }

    [Theory]
    [InlineData(OnlineSchema.MinDesyncHistoryFrames)]
    [InlineData(OnlineSchema.MaxDesyncHistoryFrames)]
    public void ReadsTheDesyncHistory(int frames)
    {
        _data.SetText(OnlineSchema.File, File(8, Delay(1, 2), Disconnect(3000, 10_000), Desync(frames)));

        Assert.Equal(new DesyncSettings(frames), OnlineSettingsLoader.Load(_data).Desync);
    }

    [Theory]
    [InlineData(OnlineSchema.MinDesyncHistoryFrames - 1)]
    [InlineData(OnlineSchema.MaxDesyncHistoryFrames + 1)]
    public void ADesyncHistoryOutOfRangeIsReported(int frames)
    {
        _data.SetText(OnlineSchema.File, File(8, Delay(1, 2), Disconnect(3000, 10_000), Desync(frames)));

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal("desync.historyFrames", error.Field);
    }

    [Fact]
    public void AMissingDesyncSectionIsReported()
    {
        _data.SetText(
            OnlineSchema.File,
            $"{{ \"schemaVersion\": 1, \"rollbackFrames\": 8, \"automaticInputDelay\": {Delay(1, 2)}, " +
            $"\"disconnect\": {Disconnect(3000, 10_000)} }}");

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal("desync", error.Field);
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
        _data.SetText(
            OnlineSchema.File,
            $"{{ \"schemaVersion\": 1, \"rollbackFrames\": 8, \"disconnect\": {Disconnect(3000, 10_000)}, \"desync\": {Desync(600)} }}");

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
        File(rollbackFrames, automaticDelay, Disconnect(3000, 10_000));

    private static string File(int rollbackFrames, string automaticDelay, string disconnect) =>
        File(rollbackFrames, automaticDelay, disconnect, Desync(600));

    private static string File(int rollbackFrames, string automaticDelay, string disconnect, string desync) =>
        $"{{ \"schemaVersion\": 1, \"rollbackFrames\": {rollbackFrames}, \"automaticInputDelay\": {automaticDelay}, " +
        $"\"disconnect\": {disconnect}, \"desync\": {desync} }}";

    private static string Desync(int historyFrames) => $"{{ \"historyFrames\": {historyFrames} }}";

    private static string Disconnect(int warningAfterMilliseconds, int endMatchAfterMilliseconds) =>
        $"{{ \"warningAfterMilliseconds\": {warningAfterMilliseconds}, " +
        $"\"endMatchAfterMilliseconds\": {endMatchAfterMilliseconds} }}";

    private static string Delay(int minimumFrames, int maximumFrames) =>
        $"{{ \"minimumFrames\": {minimumFrames}, \"maximumFrames\": {maximumFrames}, \"pingStepMilliseconds\": 40, " +
        "\"pingSampleIntervalFrames\": 30, \"pingSamples\": 9 }";
}
