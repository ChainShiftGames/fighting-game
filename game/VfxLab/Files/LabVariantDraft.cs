using System.Collections.Generic;

namespace FightingGame.VfxLab.Files;

/// <summary>A variant as written in a sheet (or a fork in the feedback file), before what it extends is filled in.</summary>
/// <param name="Extends">The id of the variant it starts from, or null.</param>
public sealed record LabVariantDraft(
    string Id,
    string Name,
    string Pitch,
    string? Extends,
    IReadOnlyDictionary<LabPartKind, LabPartDraft> Parts,
    IReadOnlyDictionary<string, LabValue> Values);
