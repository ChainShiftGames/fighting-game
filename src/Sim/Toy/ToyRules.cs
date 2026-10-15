namespace Sim.Toy;

/// <summary>
/// Tuning for the toy simulation (developer settings). Spike-only: real gameplay settings live in MatchRules
/// and data from Phase 1 (ADR-0011).
/// </summary>
public readonly record struct ToyRules
{
    public required int WalkSpeed { get; init; }
    public required int JumpVelocity { get; init; }
    public required int Gravity { get; init; }
    public required int StageHalfWidth { get; init; }
    public required int StartDistance { get; init; }

    /// <summary>Values that feel reasonable at 60 Hz, in subpixels per frame.</summary>
    public static ToyRules Default => new()
    {
        WalkSpeed = 4_000,
        JumpVelocity = 18_000,
        Gravity = 1_000,
        StageHalfWidth = 400_000,
        StartDistance = 150_000,
    };
}
