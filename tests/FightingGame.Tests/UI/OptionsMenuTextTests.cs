using System;
using FightingGame.Settings;
using FightingGame.UI;
using Xunit;

namespace FightingGame.Tests.UI;

public sealed class OptionsMenuTextTests
{
    [Fact]
    public void InputDelayCyclesFromAutoThroughEachFixedDelay()
    {
        int? frames = null;
        string labels = InputDelayOption.Label(frames);
        for (int i = 0; i < 6; i++)
        {
            frames = InputDelayOption.Next(frames);
            labels += " " + InputDelayOption.Label(frames);
        }

        Assert.Equal("Auto 0 1 2 3 4 Auto", labels);
    }

    [Fact]
    public void ShowsEachSettingWithItsKey()
    {
        string text = OptionsMenuText.Describe(new PlayerSettings { Vsync = false, InputDelayOverrideFrames = 2 });

        Assert.Contains("1   Vsync: off", text, StringComparison.Ordinal);
        Assert.Contains("2   Input delay (online): 2", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ShowsTheAutomaticDelayAsAuto()
    {
        string text = OptionsMenuText.Describe(new PlayerSettings());

        Assert.Contains("1   Vsync: on", text, StringComparison.Ordinal);
        Assert.Contains("2   Input delay (online): Auto", text, StringComparison.Ordinal);
    }
}
