using System.Collections.Generic;

namespace FightingGame.Presentation.World;

/// <summary>A character's art file: what draws it and how its model's clips match its moves (ADR-0028).</summary>
/// <param name="Art">Placeholder or model; a character without an art file is a placeholder.</param>
/// <param name="Model">The <c>.glb</c>, relative to the art file's folder; empty for a placeholder.</param>
/// <param name="Scale">Multiplies the model's size, for a model not authored in metres.</param>
/// <param name="TurnDegrees">Turns the model about Y, for a model not facing +X.</param>
/// <param name="Animations">Per move ID; a move missing here plays the clip named after it, frame for frame.</param>
public sealed record FighterArtSettings(
    FighterArtKind Art,
    string Model,
    float Scale,
    float TurnDegrees,
    IReadOnlyDictionary<string, AnimationSettings> Animations)
{
    public static FighterArtSettings Placeholder { get; } =
        new(FighterArtKind.Placeholder, string.Empty, 1f, 0f, new Dictionary<string, AnimationSettings>());

    /// <summary>The clip a move plays: its own entry's clip, or the move ID.</summary>
    public string ClipFor(string moveId) =>
        Animations.TryGetValue(moveId, out AnimationSettings? settings) && settings.Clip is { } clip ? clip : moveId;

    public ContactFrames? ContactsFor(string moveId) =>
        Animations.TryGetValue(moveId, out AnimationSettings? settings) ? settings.Contacts : null;
}
