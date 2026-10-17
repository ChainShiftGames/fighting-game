using FightingGame.Settings;

namespace FightingGame.UI;

/// <summary>What the placeholder options menu says: each player setting, its current value and the key that changes it.</summary>
public static class OptionsMenuText
{
    public static string Describe(PlayerSettings settings) =>
        "Options\n\n" +
        $"1   Vsync: {(settings.Vsync ? "on" : "off")}\n" +
        $"2   Input delay (online): {InputDelayOption.Label(settings.InputDelayOverrideFrames)}\n\n" +
        "The input delay applies from the next online match.\n" +
        "F12 or Esc   close\n";
}
