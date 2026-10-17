using System;
using FightingGame.Training;
using Netcode.Drivers.Training;
using Xunit;

namespace FightingGame.Tests.Training;

public sealed class TrainingSettingsJsonTests
{
    [Fact]
    public void DefaultsToAStandingDummy()
    {
        TrainingSettings settings = TrainingSettingsJson.Read("{}", out string warning);

        Assert.Equal(DummyMode.Stand, settings.Dummy);
        Assert.True(settings.ShowFrameMeter);
        Assert.Empty(warning);
    }

    [Theory]
    [InlineData(DummyMode.BlockAll)]
    [InlineData(DummyMode.Record)]
    public void KeepsWhatWasSaved(DummyMode dummy)
    {
        var saved = new TrainingSettings { Dummy = dummy };

        TrainingSettings loaded = TrainingSettingsJson.Read(TrainingSettingsJson.Write(saved), out string warning);

        Assert.Equal(saved, loaded);
        Assert.Empty(warning);
    }

    [Fact]
    public void KeepsTheRecordingSlotAndPlaybackStyle()
    {
        var saved = new TrainingSettings { Slot = 4, Playback = PlaybackStyle.Random, ShowFrameMeter = false };

        string json = TrainingSettingsJson.Write(saved);

        Assert.Equal(saved, TrainingSettingsJson.Read(json, out _));
        Assert.Contains("\"playback\": \"random\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("choice", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SavesTheDummyModeByName()
    {
        string json = TrainingSettingsJson.Write(new TrainingSettings { Dummy = DummyMode.BlockAfterFirstHit });

        Assert.Contains("\"dummy\": \"blockAfterFirstHit\"", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"dummy\": \"sleep\"}")]
    [InlineData("{\"dummy\": 2}")]
    public void FallsBackToTheDefaultsWithAWarning(string json)
    {
        TrainingSettings settings = TrainingSettingsJson.Read(json, out string warning);

        Assert.Equal(new TrainingSettings(), settings);
        Assert.NotEmpty(warning);
    }
}
