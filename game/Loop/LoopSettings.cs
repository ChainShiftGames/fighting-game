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

    /// <summary>
    /// How far, in thousandths of a logic frame, a displayed frame may land on the wrong side of a logic-frame
    /// boundary and still run exactly one logic frame, while the display runs at about the logic rate. Absorbs
    /// frame-callback timing jitter so the count doesn't flip between 0 and 2 (stutter). From 0 (off) to below 500.
    /// </summary>
    public required int JitterTolerancePermille { get; init; }

    public static LoopSettings Default => new()
    {
        FramesPerSecond = 60,
        MaxCatchUpFrames = 4,
        JitterTolerancePermille = 250,
    };
}
