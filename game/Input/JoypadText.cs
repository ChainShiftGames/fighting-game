namespace FightingGame.Input;

/// <summary>One line about a joypad and the slot it fills, for the log and the developer overlay.</summary>
public static class JoypadText
{
    /// <param name="slot">The roster slot it fills, or -1 when it isn't used.</param>
    public static string Describe(JoypadInfo joypad, int slot)
    {
        string kind = joypad.IsSteamInput ? " (Steam Input)" : string.Empty;
        string use = slot >= 0
            ? $"pad slot {slot}"
            : joypad.IgnoredBySteam ? "ignored: Steam Input re-presents it" : "unused";
        return $"{joypad.Name}{kind}: {use}";
    }
}
