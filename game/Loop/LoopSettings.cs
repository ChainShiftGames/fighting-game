namespace FightingGame.Loop;

/// <summary>Timing of the logic loop (developer settings, ADR-0004).</summary>
public readonly record struct LoopSettings
{
    /// <summary>Logic frames per second. Fixed by ADR-0004; a setting only so tests and tools can name it.</summary>
    public required int FramesPerSecond { get; init; }

    /// <summary>
    /// Most logic frames run in one displayed frame after a hitch. Time beyond that is dropped, so the match slows
    /// down instead of jumping ahead.
    /// </summary>
    public required int MaxCatchUpFrames { get; init; }

    public static LoopSettings Default => new() { FramesPerSecond = 60, MaxCatchUpFrames = 4 };
}
