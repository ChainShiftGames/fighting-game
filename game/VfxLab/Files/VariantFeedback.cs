using System;
using System.Collections.Generic;

namespace FightingGame.VfxLab.Files;

/// <summary>What a dev said about one variant in the Lab: a pick, a rating, a note and the values they changed.</summary>
public sealed class VariantFeedback
{
    public bool Pick { get; set; }

    /// <summary>1 to 5 stars; 0 when not rated.</summary>
    public int Rating { get; set; }

    /// <summary>Rejected: left out of the grid unless the Lab shows hidden variants.</summary>
    public bool Hidden { get; set; }

    public string Note { get; set; } = string.Empty;

    /// <summary>Values the dev changed, by uniform name or <c>part.setting</c>.</summary>
    public Dictionary<string, LabValue> Values { get; set; } = new(StringComparer.Ordinal);

    public bool IsEmpty => !Pick && Rating == 0 && !Hidden && Note.Length == 0 && Values.Count == 0;
}
