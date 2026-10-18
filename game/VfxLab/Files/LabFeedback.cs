using System;
using System.Collections.Generic;

namespace FightingGame.VfxLab.Files;

/// <summary>Everything the devs said in the Lab about one sheet: per variant, their forks and their timing changes.</summary>
public sealed class LabFeedback
{
    public Dictionary<string, VariantFeedback> Variants { get; init; } = new(StringComparer.Ordinal);

    public List<LabFork> Forks { get; init; } = [];

    /// <summary>Timing changes by <see cref="LabTiming"/> field name (camelCase).</summary>
    public Dictionary<string, float> Timing { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The feedback on <paramref name="variantId"/>, added empty when there is none yet.</summary>
    public VariantFeedback For(string variantId)
    {
        if (!Variants.TryGetValue(variantId, out VariantFeedback? feedback))
        {
            feedback = new VariantFeedback();
            Variants[variantId] = feedback;
        }

        return feedback;
    }
}
