namespace FightingGame.Presentation.World;

/// <summary>How one move is animated on a fighter model, from the character's art file (ADR-0028).</summary>
/// <param name="Clip">The clip to play; null means the clip named after the move ID.</param>
/// <param name="Contacts">Where its active and recovery poses start; null plays it frame for frame.</param>
public sealed record AnimationSettings(string? Clip, ContactFrames? Contacts);
