using System.Collections.Generic;

namespace FightingGame.Presentation.World;

/// <summary>A character's art file: what draws it and how its model's clips match its moves (ADR-0028).</summary>
/// <param name="Art">Placeholder or model; a character without an art file is a placeholder.</param>
/// <param name="Model">The <c>.glb</c> (see <see cref="FighterArtFile.Resolve"/>); empty for a placeholder.</param>
/// <param name="Scale">Multiplies the model's size, for a model not authored in metres.</param>
/// <param name="TurnDegrees">Turns the model about Y, for a model not facing +X.</param>
/// <param name="Animations">Per move ID; a move missing here plays the clip named after it, frame for frame.</param>
/// <param name="Transitions">Clips shown when entering a move from certain others; the first that fits is used.</param>
public sealed record FighterArtSettings(
    FighterArtKind Art,
    string Model,
    float Scale,
    float TurnDegrees,
    IReadOnlyDictionary<string, AnimationSettings> Animations,
    IReadOnlyList<ClipTransition> Transitions)
{
    public static FighterArtSettings Placeholder { get; } =
        new(FighterArtKind.Placeholder, string.Empty, 1f, 0f, new Dictionary<string, AnimationSettings>(), []);

    /// <summary>The index of the first transition from <paramref name="fromMove"/> into <paramref name="toMove"/>; -1 if none.</summary>
    public int TransitionBetween(string fromMove, string toMove)
    {
        for (int i = 0; i < Transitions.Count; i++)
        {
            if (Transitions[i].Joins(fromMove, toMove))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>How a move is animated: its own entry, or the clip named after it in the model, frame for frame.</summary>
    public AnimationSettings For(string moveId) =>
        Animations.TryGetValue(moveId, out AnimationSettings? settings) ? settings : new AnimationSettings(null, null);

    /// <summary>The clip a move plays: its own entry's clip, or the move ID.</summary>
    public string ClipFor(string moveId) => For(moveId).Clip ?? moveId;

    public ContactFrames? ContactsFor(string moveId) => For(moveId).Contacts;
}
