namespace FightingGame.Settings;

/// <summary>The audio buses in <c>res://default_bus_layout.tres</c>; each has a player volume.</summary>
public static class AudioBus
{
    public const string Master = "Master";
    public const string Music = "Music";

    /// <summary>Hit, block and other match sounds.</summary>
    public const string Effects = "Effects";

    /// <summary>Character voices and the announcer.</summary>
    public const string Voice = "Voice";
}
