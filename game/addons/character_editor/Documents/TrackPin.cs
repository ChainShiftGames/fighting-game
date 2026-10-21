namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>A frame of a move whose clip frame is pinned on the animation track, and what pins it.</summary>
/// <param name="Frame">The move frame, from 1.</param>
/// <param name="Clip">The clip frame it shows, in clip frames at 60 fps.</param>
public readonly record struct TrackPin(int Frame, int Clip, TrackPinKind Kind);
