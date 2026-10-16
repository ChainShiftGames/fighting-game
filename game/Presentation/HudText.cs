using System;
using System.Globalization;

namespace FightingGame.Presentation;

/// <summary>What the HUD shows, worked out from the game state.</summary>
public static class HudText
{
    /// <summary>Share of the health bar to fill, 0 to 1.</summary>
    public static float HealthFraction(int health, int maxHealth) =>
        maxHealth <= 0 ? 0f : Math.Clamp((float)health / maxHealth, 0f, 1f);

    /// <summary>Whole seconds left, rounded up, so the timer shows 1 until the last frame runs out.</summary>
    /// <remarks>
    /// 0 frames reads as no time limit (∞) because nothing starts the timer yet: rounds and the timer come with
    /// P1-13, which decides how time-out shows.
    /// </remarks>
    public static string Timer(int framesLeft, int framesPerSecond) => framesLeft <= 0
        ? "∞"
        : ((framesLeft + framesPerSecond - 1) / framesPerSecond).ToString(CultureInfo.InvariantCulture);

    public static string Round(int round) => string.Create(CultureInfo.InvariantCulture, $"ROUND {round}");
}
