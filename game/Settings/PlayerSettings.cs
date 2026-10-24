namespace FightingGame.Settings;

/// <summary>Settings players change (ARCHITECTURE.md §6). They never affect game logic results.</summary>
/// <remarks>Limits and defaults are in <see cref="PlayerSettingLimits"/>; the options menu lists them (<c>UI/Settings</c>).</remarks>
public sealed record PlayerSettings
{
    public ScreenMode ScreenMode { get; init; } = ScreenMode.Windowed;

    /// <summary>The window's size in pixels while <see cref="ScreenMode"/> is windowed.</summary>
    public int WindowWidth { get; init; } = PlayerSettingLimits.DefaultWindowWidth;

    public int WindowHeight { get; init; } = PlayerSettingLimits.DefaultWindowHeight;

    /// <summary>On by default; off lowers input latency at the cost of tearing (ADR-0004 §3).</summary>
    public bool Vsync { get; init; } = true;

    /// <summary>Volumes in percent, 0 to <see cref="PlayerSettingLimits.MaxVolume"/>, one per audio bus (<see cref="AudioBus"/>).</summary>
    public int MasterVolume { get; init; } = PlayerSettingLimits.MaxVolume;

    public int MusicVolume { get; init; } = PlayerSettingLimits.MaxVolume;

    public int EffectsVolume { get; init; } = PlayerSettingLimits.MaxVolume;

    public int VoiceVolume { get; init; } = PlayerSettingLimits.MaxVolume;

    /// <summary>
    /// Online input delay the player fixed instead of the automatic one (ADR-0005 §4), within
    /// <c>InputDelaySettings.MinOverrideFrames</c>–<c>MaxOverrideFrames</c>; null for automatic, the default.
    /// </summary>
    public int? InputDelayOverrideFrames { get; init; }

    /// <summary>The menus' translation locale, one of <see cref="PlayerSettingLimits.Languages"/>.</summary>
    public string Language { get; init; } = PlayerSettingLimits.DefaultLanguage;
}
