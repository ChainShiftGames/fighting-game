using System.Collections.Generic;

namespace FightingGame.VfxLab.Files;

/// <summary>One take on the effect: a shader per part and values for their uniforms, with what it extends filled in.</summary>
/// <param name="Pitch">One or two sentences on the idea, shown under its view.</param>
/// <param name="Values">By uniform name, or <c>part.setting</c> for a part setting (see <see cref="LabPartSettings"/>).</param>
public sealed record LabVariant(
    string Id,
    string Name,
    string Pitch,
    string? Extends,
    IReadOnlyDictionary<LabPartKind, LabPart> Parts,
    IReadOnlyDictionary<string, LabValue> Values);
