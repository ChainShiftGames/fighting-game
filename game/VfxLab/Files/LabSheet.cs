using System.Collections.Generic;

namespace FightingGame.VfxLab.Files;

/// <summary>One effect being designed in the VFX Lab (ADR-0029): the brief, the style, the timing and rounds of variants.</summary>
/// <param name="Ranges">Slider ranges by uniform name, [min, max] or [min, max, step], for uniforms without a hint_range.</param>
public sealed record LabSheet(
    string Title,
    string Brief,
    LabStyle Style,
    LabTiming Timing,
    IReadOnlyDictionary<string, float[]> Ranges,
    IReadOnlyList<LabRound> Rounds);
