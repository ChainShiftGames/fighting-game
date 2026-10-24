using System.Collections.Generic;
using FightingGame.Settings;
using FightingGame.UI.Settings;
using Xunit;

namespace FightingGame.Tests.UI.Settings;

public sealed class WindowSizeChoicesTests
{
    [Fact]
    public void OffersTheCommonSizesThatFitTheScreenSmallestFirst()
    {
        Assert.Equal(
            [(1152, 648), (1280, 720), (1280, 800)],
            Sizes(WindowSizeChoices.For(1280, 800, new PlayerSettings())));
    }

    [Fact]
    public void TheDefaultAndTheSavedSizeAreAlwaysThere()
    {
        var saved = new PlayerSettings { WindowWidth = 1000, WindowHeight = 700 };

        Assert.Equal([(1000, 700), (1280, 720)], Sizes(WindowSizeChoices.For(1024, 700, saved)));
    }

    private static List<(int Width, int Height)> Sizes(IReadOnlyList<SettingChoice<(int Width, int Height)>> choices)
    {
        var sizes = new List<(int Width, int Height)>();
        foreach (SettingChoice<(int Width, int Height)> choice in choices)
        {
            sizes.Add(choice.Value);
        }

        return sizes;
    }
}
