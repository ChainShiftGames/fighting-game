namespace FightingGame.Presentation.World;

/// <summary>What the retiming needs to know about one animation clip of a fighter model (ADR-0028).</summary>
/// <param name="LengthFrames">The clip's length in frames at 60 fps (its length in seconds × 60).</param>
/// <param name="Loops">Whether the clip loops (idle, walk): frames past its end wrap instead of holding.</param>
/// <param name="Contacts">Where the clip's active and recovery poses start; null plays it frame for frame.</param>
public readonly record struct AnimationClipTiming(float LengthFrames, bool Loops, ContactFrames? Contacts = null);
