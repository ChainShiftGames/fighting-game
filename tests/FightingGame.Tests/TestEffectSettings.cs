using FightingGame.Presentation.Feel;

namespace FightingGame.Tests;

/// <summary>
/// Fixed effect settings for effect tests, so tuning the shipped file in the control panel never breaks them. The
/// values are the file's first ones (ADR-0032).
/// </summary>
internal static class TestEffectSettings
{
    public static EffectSettings Standard { get; } = new(
        new ShakeSettings(
            LifetimeFrames: 8, HitPixels: 3f, CounterPixels: 5f, ThrowPixels: 4f, KnockOutPixels: 8f, VerticalShare: 0.5f),
        new SparkFeelSettings(
            LightHitstop: 8,
            HeavyHitstop: 20,
            CounterWeight: 0.3f,
            BlockShare: 0.6f,
            TearFromWeight: 0.5f,
            TechWeight: 0.3f,
            Scale: 1f),
        new RumbleSettings(HeavyStrength: 0.8f, LightStrength: 0.3f, HitSeconds: 0.15f, KnockOutSeconds: 0.4f));
}
