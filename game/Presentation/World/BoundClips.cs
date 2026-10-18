using System.Collections.Generic;

namespace FightingGame.Presentation.World;

/// <summary>A model's clips ready to seek: one per move that has one, and one per transition (null if it didn't load).</summary>
public sealed record BoundClips(Dictionary<string, BoundClip> Moves, BoundClip?[] Transitions);
