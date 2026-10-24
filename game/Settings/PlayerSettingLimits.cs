using System.Collections.Generic;

namespace FightingGame.Settings;

/// <summary>The values each <see cref="PlayerSettings"/> entry may take, and its defaults where they aren't obvious.</summary>
public static class PlayerSettingLimits
{
    public const int MaxVolume = 100;

    // The window opens at 1280×720 until the player picks another size; smaller than 640×360 is unreadable.
    public const int DefaultWindowWidth = 1280;
    public const int DefaultWindowHeight = 720;
    public const int MinWindowWidth = 640;
    public const int MinWindowHeight = 360;
    public const int MaxWindowSide = 7680;

    public const string DefaultLanguage = "en";

    /// <summary>The locales the translation table has a column for (<c>Assets/UI/menu_text.csv</c>).</summary>
    public static IReadOnlyList<string> Languages { get; } = [DefaultLanguage];
}
