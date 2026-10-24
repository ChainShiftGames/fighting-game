namespace FightingGame.UI.Settings;

/// <summary>How long the options menu waits for a "Keep this?" answer before reverting a video change.</summary>
/// <remarks>Developer setting (ARCHITECTURE.md §6); <see cref="Default"/> is the one place it is set.</remarks>
public sealed record SettingsMenuTiming(long KeepMilliseconds)
{
    public static SettingsMenuTiming Default { get; } = new(KeepMilliseconds: 10_000);
}
