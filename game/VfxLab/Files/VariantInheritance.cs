using System;
using System.Collections.Generic;

namespace FightingGame.VfxLab.Files;

/// <summary>
/// Fills in what each variant extends: parts field by field and values key by key, the variant's own winning. A name
/// or pitch left empty comes from the variant it extends.
/// </summary>
public static class VariantInheritance
{
    /// <returns>Every variant by id, or null with <paramref name="error"/> when one extends a missing id or a loop.</returns>
    public static Dictionary<string, LabVariant>? Resolve(IEnumerable<LabVariantDraft> drafts, out string error)
    {
        Dictionary<string, LabVariantDraft> byId = new(StringComparer.Ordinal);
        foreach (LabVariantDraft draft in drafts)
        {
            if (!byId.TryAdd(draft.Id, draft))
            {
                error = $"two variants are called \"{draft.Id}\"";
                return null;
            }
        }

        Dictionary<string, LabVariant> resolved = new(StringComparer.Ordinal);
        foreach (LabVariantDraft draft in byId.Values)
        {
            List<LabVariantDraft>? chain = ChainOf(draft, byId, out error);
            if (chain is null)
            {
                return null;
            }

            LabVariant variant = Merge(chain);
            if (variant.Parts.Count == 0)
            {
                error = $"variant \"{draft.Id}\" has no part with a shader";
                return null;
            }

            resolved[draft.Id] = variant;
        }

        error = string.Empty;
        return resolved;
    }

    // The variant and what it extends, the variant first.
    private static List<LabVariantDraft>? ChainOf(LabVariantDraft draft, Dictionary<string, LabVariantDraft> byId, out string error)
    {
        List<LabVariantDraft> chain = [draft];
        HashSet<string> seen = new(StringComparer.Ordinal) { draft.Id };
        LabVariantDraft current = draft;
        while (current.Extends is { } parentId)
        {
            if (!byId.TryGetValue(parentId, out LabVariantDraft? parent))
            {
                error = $"variant \"{current.Id}\" extends \"{parentId}\", which isn't in the sheet";
                return null;
            }

            if (!seen.Add(parentId))
            {
                error = $"variant \"{draft.Id}\" extends itself through \"{parentId}\"";
                return null;
            }

            chain.Add(parent);
            current = parent;
        }

        error = string.Empty;
        return chain;
    }

    private static LabVariant Merge(List<LabVariantDraft> chain)
    {
        Dictionary<LabPartKind, LabPartDraft> parts = [];
        Dictionary<string, LabValue> values = new(StringComparer.Ordinal);
        string name = string.Empty;
        string pitch = string.Empty;
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            LabVariantDraft draft = chain[i];
            foreach ((LabPartKind kind, LabPartDraft part) in draft.Parts)
            {
                parts[kind] = part.Over(parts.GetValueOrDefault(kind));
            }

            foreach ((string key, LabValue value) in draft.Values)
            {
                values[key] = value;
            }

            name = draft.Name.Length > 0 ? draft.Name : name;
            pitch = draft.Pitch.Length > 0 ? draft.Pitch : pitch;
        }

        Dictionary<LabPartKind, LabPart> finished = [];
        foreach ((LabPartKind kind, LabPartDraft part) in parts)
        {
            if (part.ToPart() is { } done)
            {
                finished[kind] = done;
            }
        }

        LabVariantDraft leaf = chain[0];
        return new LabVariant(leaf.Id, name.Length > 0 ? name : leaf.Id, pitch, leaf.Extends, finished, values);
    }
}
