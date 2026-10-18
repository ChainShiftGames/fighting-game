namespace FightingGame.Presentation.World;

/// <summary>How one move is animated on a fighter model, from the character's art file (ADR-0028).</summary>
/// <param name="Clip">The clip to play; null means the clip named after the move ID.</param>
/// <param name="Contacts">Where its active and recovery poses start; null plays it frame for frame.</param>
/// <param name="File">
/// The model file (<c>.glb</c>, or <c>.fbx</c> for test models) holding the clip; null means the character's model.
/// </param>
/// <param name="Loop">Overrides whether the clip loops; null keeps the file's setting.</param>
/// <param name="InPlace">Removes the root bone's horizontal travel, for clips not authored in place.</param>
public sealed record AnimationSettings(
    string? Clip,
    ContactFrames? Contacts,
    string? File = null,
    bool? Loop = null,
    bool InPlace = false);
