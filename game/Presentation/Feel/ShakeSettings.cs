namespace FightingGame.Presentation.Feel;

/// <summary>How hard and how long the view shakes on contact (developer settings, presentation).</summary>
/// <param name="LifetimeFrames">How long a shake lasts, in logic frames.</param>
/// <param name="HitPixels">Starting strength of a hit's shake, in screen pixels.</param>
/// <param name="CounterPixels">Starting strength of a counter or punish counter hit's shake.</param>
/// <param name="ThrowPixels">Starting strength of a throw's shake.</param>
/// <param name="KnockOutPixels">Starting strength of the knock-out shake.</param>
/// <param name="VerticalShare">Vertical movement as a share of the horizontal one.</param>
public sealed record ShakeSettings(
    int LifetimeFrames,
    float HitPixels,
    float CounterPixels,
    float ThrowPixels,
    float KnockOutPixels,
    float VerticalShare);
