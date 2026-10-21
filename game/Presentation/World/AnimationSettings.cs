using System.Collections.Generic;

namespace FightingGame.Presentation.World;

/// <summary>How one move is animated on a fighter model, from the character's art file (ADR-0028).</summary>
/// <param name="Clip">The clip to play; null means the clip named after the move ID.</param>
/// <param name="Contacts">Where its active and recovery poses start; null plays it frame for frame.</param>
/// <param name="File">
/// The model file (<c>.glb</c>, or <c>.fbx</c> for test models) holding the clip; null means the character's model.
/// </param>
/// <param name="Loop">Overrides whether the clip loops; null keeps the file's setting.</param>
/// <param name="InPlace">Removes the root bone's horizontal travel, for clips not authored in place.</param>
/// <param name="ClipFrom">Where the part of the clip played starts, in clip frames at 60 fps; null for its start.</param>
/// <param name="ClipTo">Where that part ends; null for the clip's end.</param>
/// <param name="Fit">Stretches or squeezes the part over the whole move instead of playing it frame for frame.</param>
/// <param name="HoldHeight">Holds the root bone at its height at <paramref name="ClipFrom"/>, for a clip that jumps
/// by itself while the logic already moves the fighter up.</param>
/// <param name="HoldFrames">Shows each pose this many frames (2 or 3); null uses the art file's setting.</param>
/// <param name="CrossfadeFrames">Frames this move's clip fades in over; null uses the art file's setting, 0 snaps
/// (a 4-frame prejump is itself the transition).</param>
/// <param name="Speed">Plays the clip faster or slower, so a walk cycle's feet keep up with the logic's walk speed.</param>
/// <param name="Reverse">Plays the clip backwards, e.g. a forward walk as a walk back when a pack has none.</param>
/// <param name="Keys">Timing keys inside the contact frames' spans, in phase order; null for none.</param>
public sealed record AnimationSettings(
    string? Clip,
    ContactFrames? Contacts,
    string? File = null,
    bool? Loop = null,
    bool InPlace = false,
    int? ClipFrom = null,
    int? ClipTo = null,
    bool Fit = false,
    bool HoldHeight = false,
    int? HoldFrames = null,
    int? CrossfadeFrames = null,
    float Speed = 1f,
    bool Reverse = false,
    IReadOnlyList<TimingKey>? Keys = null)
{
    /// <summary>A move the art file doesn't list: the clip named after it, frame for frame.</summary>
    public static AnimationSettings Default { get; } = new(null, null);
}
