using System.Collections.Generic;
using System.Globalization;
using FightingGame.Settings;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings;

/// <summary>
/// The window sizes the options menu offers: the common ones that fit the screen, plus the default and the saved size
/// so neither is ever missing from the list. Smallest first.
/// </summary>
public static class WindowSizeChoices
{
    // 16:9 sizes, and 16:10 for the Steam Deck (1280×800) and laptops.
    private static readonly (int Width, int Height)[] _common =
    [
        (1152, 648), (1280, 720), (1280, 800), (1366, 768), (1600, 900), (1920, 1080), (1920, 1200), (2560, 1440),
        (3840, 2160),
    ];

    /// <param name="screenWidth">The usable screen area; sizes larger than it aren't offered.</param>
    public static IReadOnlyList<SettingChoice<(int Width, int Height)>> For(
        int screenWidth, int screenHeight, PlayerSettings saved)
    {
        var sizes = new List<(int Width, int Height)>();
        foreach ((int width, int height) in _common)
        {
            if (width <= screenWidth && height <= screenHeight)
            {
                sizes.Add((width, height));
            }
        }

        var defaults = new PlayerSettings();
        AddMissing(sizes, (defaults.WindowWidth, defaults.WindowHeight));
        AddMissing(sizes, (saved.WindowWidth, saved.WindowHeight));
        sizes.Sort((first, second) => (first.Width * first.Height).CompareTo(second.Width * second.Height));

        var choices = new List<SettingChoice<(int Width, int Height)>>(sizes.Count);
        foreach ((int width, int height) size in sizes)
        {
            choices.Add(new SettingChoice<(int Width, int Height)>(size, Label(size.width, size.height)));
        }

        return choices;
    }

    private static MenuText Label(int width, int height) =>
        MenuText.Key(
            "MENU_SETTINGS_WINDOW_SIZE_VALUE",
            width.ToString(CultureInfo.InvariantCulture),
            height.ToString(CultureInfo.InvariantCulture));

    private static void AddMissing(List<(int Width, int Height)> sizes, (int Width, int Height) size)
    {
        if (!sizes.Contains(size))
        {
            sizes.Add(size);
        }
    }
}
