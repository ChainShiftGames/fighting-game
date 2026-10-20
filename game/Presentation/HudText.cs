using System;
using System.Globalization;

namespace FightingGame.Presentation;

/// <summary>What the HUD shows, worked out from the game state.</summary>
public static class HudText
{
    /// <summary>Share of the health bar to fill, 0 to 1.</summary>
    public static float HealthFraction(int health, int maxHealth) =>
        maxHealth <= 0 ? 0f : Math.Clamp((float)health / maxHealth, 0f, 1f);

    /// <summary>Whole seconds left, rounded up, so the timer shows 1 until the last frame runs out and 0 at time-out.</summary>
    public static string Timer(int framesLeft, int framesPerSecond) =>
        ((Math.Max(framesLeft, 0) + framesPerSecond - 1) / framesPerSecond).ToString(CultureInfo.InvariantCulture);

    /// <summary>Share of one meter segment filled, 0 to 1, when the meter is split into equal segments filled in order.</summary>
    public static float MeterSegmentFill(int meter, int segment, int segments, int fullMeter)
    {
        float perSegment = (float)fullMeter / segments;
        return Math.Clamp((meter - (segment * perSegment)) / perSegment, 0f, 1f);
    }

    public static string Round(int round) => string.Create(CultureInfo.InvariantCulture, $"ROUND {round}");
}
