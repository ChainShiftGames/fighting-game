using System;
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
    public void RepositorySettingsUseTheEightFrameRollbackWindow()
    {
        OnlineSettings settings = OnlineSettingsLoader.Load(new DirectoryDataSource(Fixture.RepositoryDataRoot));

        Assert.Equal(8, settings.RollbackFrames);
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
    public void AMissingFileIsReported()
    {
        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal(OnlineSchema.File, error.File);
    }

    [Fact]
    public void AnUnknownKeyIsReported()
    {
        _data.SetText(OnlineSchema.File, File(8).Replace("}", ", \"inputDelay\": 2 }", StringComparison.Ordinal));

        DataError error = Assert.Single(
            Assert.Throws<DataValidationException>(() => OnlineSettingsLoader.Load(_data)).Errors);
        Assert.Equal("inputDelay", error.Field);
    }

    private static string File(int rollbackFrames) =>
        $"{{ \"schemaVersion\": 1, \"rollbackFrames\": {rollbackFrames} }}";
}
