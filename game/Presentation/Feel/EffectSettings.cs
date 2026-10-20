namespace FightingGame.Presentation.Feel;

/// <summary>
/// The event effects' feel (developer settings, presentation), from <see cref="EffectSettingsFile"/>: read at match
/// start, so a change applies when the match restarts.
/// </summary>
public sealed record EffectSettings(ShakeSettings Shake, SparkFeelSettings Sparks, RumbleSettings Rumble);
