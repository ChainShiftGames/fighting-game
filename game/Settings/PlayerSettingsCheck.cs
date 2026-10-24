using System;
using System.Collections.Generic;
using System.Globalization;
using Netcode.Online;

namespace FightingGame.Settings;

/// <summary>
/// Replaces each <see cref="PlayerSettings"/> value outside its limits (<see cref="PlayerSettingLimits"/>) with its
/// default, saying why: one bad value never costs the player the others.
/// </summary>
public static class PlayerSettingsCheck
{
    /// <param name="warnings">Gets one line per value replaced.</param>
    public static PlayerSettings Fix(PlayerSettings settings, List<string> warnings)
    {
        var defaults = new PlayerSettings();
        if (!Enum.IsDefined(settings.ScreenMode))
        {
            warnings.Add($"screenMode is not one of {string.Join(", ", Enum.GetNames<ScreenMode>())}, using {defaults.ScreenMode}");
            settings = settings with { ScreenMode = defaults.ScreenMode };
        }

        if (settings.WindowWidth is < PlayerSettingLimits.MinWindowWidth or > PlayerSettingLimits.MaxWindowSide ||
            settings.WindowHeight is < PlayerSettingLimits.MinWindowHeight or > PlayerSettingLimits.MaxWindowSide)
        {
            warnings.Add($"window size {settings.WindowWidth}×{settings.WindowHeight} is out of range, " +
                $"using {defaults.WindowWidth}×{defaults.WindowHeight}");
            settings = settings with { WindowWidth = defaults.WindowWidth, WindowHeight = defaults.WindowHeight };
        }

        settings = settings with
        {
            MasterVolume = Volume("masterVolume", settings.MasterVolume, warnings),
            MusicVolume = Volume("musicVolume", settings.MusicVolume, warnings),
            EffectsVolume = Volume("effectsVolume", settings.EffectsVolume, warnings),
            VoiceVolume = Volume("voiceVolume", settings.VoiceVolume, warnings),
        };

        if (settings.InputDelayOverrideFrames is { } frames and
            (< InputDelaySettings.MinOverrideFrames or > InputDelaySettings.MaxOverrideFrames))
        {
            warnings.Add($"inputDelayOverrideFrames {frames} is outside {InputDelaySettings.MinOverrideFrames}–" +
                $"{InputDelaySettings.MaxOverrideFrames}, using the automatic delay");
            settings = settings with { InputDelayOverrideFrames = null };
        }

        if (settings.Language is null || !Contains(PlayerSettingLimits.Languages, settings.Language))
        {
            warnings.Add($"language \"{settings.Language}\" has no translation, using {defaults.Language}");
            settings = settings with { Language = defaults.Language };
        }

        return settings;
    }

    private static int Volume(string name, int volume, List<string> warnings)
    {
        if (volume is >= 0 and <= PlayerSettingLimits.MaxVolume)
        {
            return volume;
        }

        warnings.Add(string.Create(
            CultureInfo.InvariantCulture,
            $"{name} {volume} is outside 0–{PlayerSettingLimits.MaxVolume}, using {PlayerSettingLimits.MaxVolume}"));
        return PlayerSettingLimits.MaxVolume;
    }

    private static bool Contains(IReadOnlyList<string> values, string value)
    {
        foreach (string candidate in values)
        {
            if (candidate == value)
            {
                return true;
            }
        }

        return false;
    }
}
