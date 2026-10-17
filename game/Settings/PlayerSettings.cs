namespace FightingGame.Settings;

/// <summary>Settings players change (ARCHITECTURE.md §6). They never affect game logic results.</summary>
public sealed record PlayerSettings
{
    /// <summary>On by default; off lowers input latency at the cost of tearing (ADR-0004 §3).</summary>
    public bool Vsync { get; init; } = true;

    /// <summary>
    /// Online input delay the player fixed instead of the automatic one (ADR-0005 §4), within
    /// <c>InputDelaySettings.MinOverrideFrames</c>–<c>MaxOverrideFrames</c>; null for automatic, the default.
    /// </summary>
    public int? InputDelayOverrideFrames { get; init; }
}
