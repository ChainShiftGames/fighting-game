namespace Sim.Characters;

/// <summary>A character's movement and health values. Distances in subpixels, speeds in subpixels per frame.</summary>
/// <remarks>Only 4-byte fields, so the struct has no padding and hashes byte for byte.</remarks>
public readonly record struct CharacterStats
{
    public int Health { get; init; }

    public int WalkForwardSpeed { get; init; }

    public int WalkBackSpeed { get; init; }

    public DashStyle DashStyle { get; init; }

    public int PrejumpFrames { get; init; }

    /// <summary>Upward speed on the first airborne frame.</summary>
    public int JumpVelocity { get; init; }

    public int HighJumpVelocity { get; init; }

    public int JumpForwardSpeed { get; init; }

    public int JumpBackSpeed { get; init; }

    /// <summary>Subtracted from the upward speed every airborne frame.</summary>
    public int Gravity { get; init; }

    /// <summary>Double jumps and air dashes share this per-jump budget.</summary>
    public int AirActions { get; init; }
}
