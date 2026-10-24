using System;
using FightingGame.Settings;
using Xunit;

namespace FightingGame.Tests.Settings;

public sealed class PlayerSettingsJsonTests
{
    private static readonly PlayerSettings _changed = new()
    {
        ScreenMode = ScreenMode.Borderless,
        WindowWidth = 1920,
        WindowHeight = 1080,
        Vsync = false,
        MasterVolume = 80,
        MusicVolume = 0,
        EffectsVolume = 55,
        VoiceVolume = 100,
        InputDelayOverrideFrames = 2,
        Language = "en",
    };

    [Fact]
    public void DefaultsToAWindowedVsyncedFullVolumeEnglishGameWithAutomaticInputDelay()
    {
        PlayerSettings settings = PlayerSettingsJson.Read("{}", out string warning);

        Assert.Equal(
            new PlayerSettings
            {
                ScreenMode = ScreenMode.Windowed,
                WindowWidth = 1280,
                WindowHeight = 720,
                Vsync = true,
                MasterVolume = 100,
                MusicVolume = 100,
                EffectsVolume = 100,
                VoiceVolume = 100,
                InputDelayOverrideFrames = null,
                Language = "en",
            },
            settings);
        Assert.Empty(warning);
    }

    [Fact]
    public void EveryValueSurvivesARoundTrip()
    {
        PlayerSettings loaded = PlayerSettingsJson.Read(PlayerSettingsJson.Write(_changed), out string warning);

        Assert.Equal(_changed, loaded);
        Assert.Empty(warning);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(4)]
    public void KeepsTheInputDelayThatWasSaved(int? inputDelayOverrideFrames)
    {
        var saved = new PlayerSettings { InputDelayOverrideFrames = inputDelayOverrideFrames };

        Assert.Equal(saved, PlayerSettingsJson.Read(PlayerSettingsJson.Write(saved), out _));
    }

    [Fact]
    public void WritesTheSchemaVersionFirstAndNamesInCamelCase()
    {
        string json = PlayerSettingsJson.Write(_changed);

        Assert.StartsWith("{\n  \"schemaVersion\": 2,", json.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.Contains("\"inputDelayOverrideFrames\": 2", json, StringComparison.Ordinal);
        Assert.Contains("\"screenMode\": \"borderless\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void AFileFromBeforeTheVersionKeepsItsValuesAndGetsDefaultsForTheRest()
    {
        PlayerSettings settings = PlayerSettingsJson.Read(
            "{\n  \"vsync\": false,\n  \"inputDelayOverrideFrames\": 3\n}", out string warning);

        Assert.Equal(new PlayerSettings { Vsync = false, InputDelayOverrideFrames = 3 }, settings);
        Assert.Empty(warning);
    }

    [Fact]
    public void AFileFromANewerGameIsReadByTheNamesThisOneKnows()
    {
        PlayerSettings settings = PlayerSettingsJson.Read(
            "{ \"schemaVersion\": 9, \"vsync\": false, \"somethingNew\": 1 }", out string warning);

        Assert.Equal(new PlayerSettings { Vsync = false }, settings);
        Assert.Contains("newer", warning, StringComparison.Ordinal);
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
    [InlineData("\"screenMode\": \"huge\"", "screenMode")]
    [InlineData("\"screenMode\": 7", "screenMode")]
    [InlineData("\"windowWidth\": 10", "window size")]
    [InlineData("\"windowHeight\": 99999", "window size")]
    [InlineData("\"masterVolume\": 101", "masterVolume")]
    [InlineData("\"musicVolume\": -5", "musicVolume")]
    [InlineData("\"effectsVolume\": 400", "effectsVolume")]
    [InlineData("\"voiceVolume\": -1", "voiceVolume")]
    [InlineData("\"language\": \"xx\"", "language")]
    [InlineData("\"language\": null", "language")]
    public void AnInvalidValueFallsBackToItsDefaultAloneWithAWarning(string invalid, string named)
    {
        PlayerSettings settings = PlayerSettingsJson.Read(
            $"{{ \"schemaVersion\": 2, \"vsync\": false, {invalid} }}", out string warning);

        Assert.Equal(new PlayerSettings { Vsync = false }, settings);
        Assert.Contains(named, warning, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1, 2]")]
    [InlineData("{ \"inputDelayOverrideFrames\": \"two\" }")]
    public void FallsBackToTheDefaultsForAnUnreadableFile(string json)
    {
        PlayerSettings settings = PlayerSettingsJson.Read(json, out string warning);

        Assert.Equal(new PlayerSettings(), settings);
        Assert.NotEmpty(warning);
    }
}
