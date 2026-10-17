using System;
using FightingGame.Settings;
using Xunit;

namespace FightingGame.Tests.Settings;

public sealed class PlayerSettingsJsonTests
{
    [Fact]
    public void DefaultsToVsyncOnAndAutomaticInputDelay()
    {
        PlayerSettings settings = PlayerSettingsJson.Read("{}", out string warning);

        Assert.Equal(new PlayerSettings { Vsync = true, InputDelayOverrideFrames = null }, settings);
        Assert.Empty(warning);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(4)]
    public void KeepsWhatWasSaved(int? inputDelayOverrideFrames)
    {
        var saved = new PlayerSettings { Vsync = false, InputDelayOverrideFrames = inputDelayOverrideFrames };

        PlayerSettings loaded = PlayerSettingsJson.Read(PlayerSettingsJson.Write(saved), out string warning);

        Assert.Equal(saved, loaded);
        Assert.Empty(warning);
    }

    [Fact]
    public void SavesTheDelayUnderItsCamelCaseName()
    {
        string json = PlayerSettingsJson.Write(new PlayerSettings { InputDelayOverrideFrames = 3 });

        Assert.Contains("\"inputDelayOverrideFrames\": 3", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void FallsBackToAutomaticForADelayOutOfRange(int frames)
    {
        PlayerSettings settings = PlayerSettingsJson.Read(
            $"{{ \"vsync\": false, \"inputDelayOverrideFrames\": {frames} }}", out string warning);

        Assert.Null(settings.InputDelayOverrideFrames);
        Assert.False(settings.Vsync);
        Assert.Contains("inputDelayOverrideFrames", warning, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ \"inputDelayOverrideFrames\": \"two\" }")]
    public void FallsBackToTheDefaultsForAnUnreadableFile(string json)
    {
        PlayerSettings settings = PlayerSettingsJson.Read(json, out string warning);

        Assert.Equal(new PlayerSettings(), settings);
        Assert.NotEmpty(warning);
    }
}
