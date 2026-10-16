namespace Sim.Data.Characters.Reading;

/// <summary>A move's length and active frames (1-based, inclusive), as the box and cancel readers check them.</summary>
public readonly record struct MoveFrames(int Total, int FirstActive, int LastActive)
{
    public bool HasActiveFrames => FirstActive > 0;

    /// <summary>Bounds used when the frame data itself was invalid, so later fields don't report follow-up errors.</summary>
    public static MoveFrames Unknown => new(CharacterSchema.MaxMoveFrames, 1, CharacterSchema.MaxMoveFrames);
}
