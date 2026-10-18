using System.Collections.Generic;

namespace FightingGame.VfxLab.Files;

/// <summary>A set of variants shown together: a wide first round of directions, then deeper rounds on the picks.</summary>
/// <param name="Note">What this round explores, or what the devs asked for.</param>
public sealed record LabRound(string Id, string Name, string Note, IReadOnlyList<LabVariantDraft> Variants);
