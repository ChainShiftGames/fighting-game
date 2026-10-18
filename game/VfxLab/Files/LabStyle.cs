using System.Collections.Generic;

namespace FightingGame.VfxLab.Files;

/// <summary>The character's look every variant starts from, so new effects stay on-model.</summary>
/// <param name="Motifs">The shapes and touches its effects share, in words.</param>
/// <param name="Palette">Uniform values every variant gets unless it sets its own (or the Lab locks the palette).</param>
public sealed record LabStyle(string Character, string Motifs, IReadOnlyDictionary<string, LabValue> Palette);
