using System.Collections.Generic;

namespace FightingGame.VfxLab.Files;

/// <summary>A dev's tweaked copy of a variant, saved from the Lab as a variant of its own to drill down on.</summary>
/// <param name="From">The variant it was forked from; it extends that variant.</param>
/// <param name="Values">The values it was forked with, on top of what <paramref name="From"/> has.</param>
public sealed record LabFork(string Id, string From, string Name, IReadOnlyDictionary<string, LabValue> Values)
{
    public LabVariantDraft ToDraft() => new(Id, Name, string.Empty, From, new Dictionary<LabPartKind, LabPartDraft>(), Values);
}
