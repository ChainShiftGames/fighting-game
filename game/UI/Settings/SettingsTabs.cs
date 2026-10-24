using System;
using System.Collections.Generic;
using System.Globalization;
using FightingGame.Settings;
using FightingGame.UI.Framework;
using Netcode.Online;

namespace FightingGame.UI.Settings;

/// <summary>
/// The options menu's tabs and the player settings in each: the one place an entry is added. A new setting is its
/// value in <see cref="PlayerSettings"/>, one entry here and, when it acts on the engine, an
/// <see cref="IPlayerSettingApplier"/> (<see cref="PlayerSettingAppliers"/>).
/// </summary>
/// <remarks>Only player settings belong here, and none may change logic results (ARCHITECTURE.md §6).</remarks>
public static class SettingsTabs
{
    private const int VolumeStep = 5;

    /// <param name="windowSizes">The sizes offered for the window (<see cref="WindowSizeChoices"/>).</param>
    public static IReadOnlyList<SettingsTab> Create(IReadOnlyList<SettingChoice<(int Width, int Height)>> windowSizes) =>
    [
        new(MenuText.Key("MENU_SETTINGS_TAB_VIDEO"), Video(windowSizes), MenuText.Empty),
        new(MenuText.Key("MENU_SETTINGS_TAB_AUDIO"), Audio(), MenuText.Empty),
        new(MenuText.Key("MENU_SETTINGS_TAB_DISPLAY"), [], MenuText.Key("MENU_SETTINGS_DISPLAY_COMING")),
        new(MenuText.Key("MENU_SETTINGS_TAB_CONTROLS"), [], MenuText.Key("MENU_SETTINGS_CONTROLS_COMING")),
        new(MenuText.Key("MENU_SETTINGS_TAB_ONLINE"), [InputDelay()], MenuText.Empty),
        new(MenuText.Key("MENU_SETTINGS_TAB_LANGUAGE"), [Language()], MenuText.Empty),
        new(MenuText.Key("MENU_SETTINGS_TAB_ACCESSIBILITY"), [], MenuText.Key("MENU_SETTINGS_ACCESSIBILITY_COMING")),
    ];

    private static ISettingEntry[] Video(IReadOnlyList<SettingChoice<(int Width, int Height)>> windowSizes) =>
    [
        new ChoiceSetting<ScreenMode>(
            "MENU_SETTINGS_SCREEN_MODE",
            "MENU_SETTINGS_SCREEN_MODE_INFO",
            [
                new(ScreenMode.Windowed, MenuText.Key("MENU_SETTINGS_SCREEN_WINDOWED")),
                new(ScreenMode.Borderless, MenuText.Key("MENU_SETTINGS_SCREEN_BORDERLESS")),
                new(ScreenMode.Fullscreen, MenuText.Key("MENU_SETTINGS_SCREEN_FULLSCREEN")),
            ],
            settings => settings.ScreenMode,
            (settings, mode) => settings with { ScreenMode = mode })
        {
            NeedsKeeping = true,
        },
        new ChoiceSetting<(int Width, int Height)>(
            "MENU_SETTINGS_WINDOW_SIZE",
            string.Empty,
            windowSizes,
            settings => (settings.WindowWidth, settings.WindowHeight),
            (settings, size) => settings with { WindowWidth = size.Width, WindowHeight = size.Height })
        {
            NeedsKeeping = true,
            IsEnabled = settings => settings.ScreenMode == ScreenMode.Windowed,
            DisabledDescriptionKey = "MENU_SETTINGS_WINDOW_SIZE_WINDOWED_ONLY",
        },
        new ToggleSetting(
            "MENU_SETTINGS_VSYNC",
            "MENU_SETTINGS_VSYNC_INFO",
            settings => settings.Vsync,
            (settings, on) => settings with { Vsync = on }),
    ];

    private static ISettingEntry[] Audio() =>
    [
        Volume("MENU_SETTINGS_VOLUME_MASTER", settings => settings.MasterVolume, (s, v) => s with { MasterVolume = v }),
        Volume("MENU_SETTINGS_VOLUME_MUSIC", settings => settings.MusicVolume, (s, v) => s with { MusicVolume = v }),
        Volume("MENU_SETTINGS_VOLUME_EFFECTS", settings => settings.EffectsVolume, (s, v) => s with { EffectsVolume = v }),
        Volume("MENU_SETTINGS_VOLUME_VOICE", settings => settings.VoiceVolume, (s, v) => s with { VoiceVolume = v }),
    ];

    private static SliderSetting Volume(
        string labelKey, Func<PlayerSettings, int> get, Func<PlayerSettings, int, PlayerSettings> set) =>
        new(labelKey, string.Empty, (0, PlayerSettingLimits.MaxVolume, VolumeStep), get, set);

    // Auto, then each delay a player may fix (ADR-0005 §4).
    private static ChoiceSetting<int?> InputDelay()
    {
        var choices = new List<SettingChoice<int?>> { new(null, MenuText.Key("MENU_SETTINGS_INPUT_DELAY_AUTO")) };
        for (int frames = InputDelaySettings.MinOverrideFrames; frames <= InputDelaySettings.MaxOverrideFrames; frames++)
        {
            string count = frames.ToString(CultureInfo.InvariantCulture);
            choices.Add(new(frames, MenuText.Key("MENU_SETTINGS_INPUT_DELAY_FIXED", count)));
        }

        return new ChoiceSetting<int?>(
            "MENU_SETTINGS_INPUT_DELAY",
            "MENU_SETTINGS_INPUT_DELAY_INFO",
            choices,
            settings => settings.InputDelayOverrideFrames,
            (settings, frames) => settings with { InputDelayOverrideFrames = frames });
    }

    // Each language is named in its own language, so a player who can't read the current one still finds theirs.
    private static ChoiceSetting<string> Language() =>
        new(
            "MENU_SETTINGS_LANGUAGE",
            string.Empty,
            [new(PlayerSettingLimits.DefaultLanguage, MenuText.Key("MENU_SETTINGS_LANGUAGE_EN"))],
            settings => settings.Language,
            (settings, language) => settings with { Language = language });
}
