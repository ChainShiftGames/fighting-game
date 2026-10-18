using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FightingGame.VfxLab.Files;

namespace FightingGame.VfxLab;

/// <summary>
/// One sheet open in the Lab with its feedback: the variants (forks included), which ones a round shows, the values a
/// variant is drawn with, and the devs' changes to them.
/// </summary>
public sealed class LabSession
{
    /// <summary>The round choice that shows every round.</summary>
    public const string AllRounds = "*";

    /// <summary>The round the devs' forks are in.</summary>
    public const string ForkRound = "forks";

    private Dictionary<string, LabVariant> _variants = new(StringComparer.Ordinal);

    public LabSession(LabSheet sheet, LabFeedback feedback)
    {
        Sheet = sheet;
        Feedback = feedback;
        Resolve();
    }

    public LabSheet Sheet { get; }

    public LabFeedback Feedback { get; }

    /// <summary>Why some forks were left out (what they were forked from is gone); empty when none were.</summary>
    public string Problem { get; private set; } = string.Empty;

    public IReadOnlyDictionary<string, LabVariant> Variants => _variants;

    /// <summary>The sheet's timing with the devs' changes.</summary>
    public LabTiming Timing => LabTimingSettings.Apply(Sheet.Timing, Feedback.Timing);

    /// <summary>The variants <paramref name="options"/> show, in sheet order, forks last.</summary>
    public List<LabVariant> Shown(LabViewOptions options)
    {
        List<LabVariant> shown = [];
        foreach ((string round, string id) in Order())
        {
            if (!_variants.TryGetValue(id, out LabVariant? variant))
            {
                continue;
            }

            VariantFeedback? feedback = Feedback.Variants.GetValueOrDefault(id);
            bool inRound = options.Round == AllRounds || options.Round == round;
            bool solo = options.Solo is null || options.Solo == id;
            bool picked = !options.PicksOnly || feedback?.Pick == true;
            bool visible = options.ShowHidden || feedback?.Hidden != true || options.Solo == id;
            if (inRound && solo && picked && visible)
            {
                shown.Add(variant);
            }
        }

        return shown;
    }

    /// <summary>The name of the round <paramref name="variantId"/> is in.</summary>
    public string RoundNameOf(string variantId)
    {
        foreach (LabRound round in Sheet.Rounds)
        {
            if (round.Variants.Any(variant => variant.Id == variantId))
            {
                return round.Name;
            }
        }

        return "Fork";
    }

    /// <summary>
    /// The values <paramref name="variant"/> is drawn with: the style's palette, then the variant's own values (unless
    /// the palette is locked), then the devs' changes.
    /// </summary>
    public Dictionary<string, LabValue> ValuesFor(LabVariant variant, bool lockPalette)
    {
        Dictionary<string, LabValue> values = new(Sheet.Style.Palette, StringComparer.Ordinal);
        foreach ((string key, LabValue value) in variant.Values)
        {
            if (!lockPalette || !Sheet.Style.Palette.ContainsKey(key))
            {
                values[key] = value;
            }
        }

        foreach ((string key, LabValue value) in Feedback.Variants.GetValueOrDefault(variant.Id)?.Values ?? [])
        {
            values[key] = value;
        }

        return values;
    }

    /// <summary>Saves <paramref name="variantId"/> with the devs' changes as a new variant and returns it.</summary>
    public LabVariant Fork(string variantId)
    {
        LabVariant parent = _variants[variantId];
        int number = 1;
        while (_variants.ContainsKey($"{variantId}_fork{number}"))
        {
            number++;
        }

        Dictionary<string, LabValue> changes = new(Feedback.Variants.GetValueOrDefault(variantId)?.Values ?? [], StringComparer.Ordinal);
        LabFork fork = new($"{variantId}_fork{number}", variantId, $"{parent.Name} · fork {number}", changes);
        Feedback.Forks.Add(fork);
        Resolve();
        return _variants[fork.Id];
    }

    /// <summary>The variant's values as JSON, to paste in the chat or a sheet.</summary>
    public string SettingsText(LabVariant variant)
    {
        Dictionary<string, LabValue> values = new(variant.Values, StringComparer.Ordinal);
        foreach ((string key, LabValue value) in Feedback.Variants.GetValueOrDefault(variant.Id)?.Values ?? [])
        {
            values[key] = value;
        }

        var text = new { variant = variant.Id, extends = variant.Extends, values = new SortedDictionary<string, LabValue>(values, StringComparer.Ordinal) };
        return JsonSerializer.Serialize(text, LabSheetFile.Json);
    }

    private IEnumerable<(string Round, string Id)> Order()
    {
        foreach (LabRound round in Sheet.Rounds)
        {
            foreach (LabVariantDraft variant in round.Variants)
            {
                yield return (round.Id, variant.Id);
            }
        }

        foreach (LabFork fork in Feedback.Forks)
        {
            yield return (ForkRound, fork.Id);
        }
    }

    private void Resolve()
    {
        List<LabVariantDraft> drafts = Sheet.Rounds.SelectMany(round => round.Variants).ToList();
        HashSet<string> ids = new(drafts.Select(draft => draft.Id), StringComparer.Ordinal);
        List<string> dropped = [];
        foreach (LabFork fork in Feedback.Forks)
        {
            // A fork can be forked again, so it may extend an earlier fork.
            if (ids.Contains(fork.From) && ids.Add(fork.Id))
            {
                drafts.Add(fork.ToDraft());
            }
            else
            {
                dropped.Add(fork.Id);
            }
        }

        _variants = VariantInheritance.Resolve(drafts, out string error) ?? new Dictionary<string, LabVariant>(StringComparer.Ordinal);
        Problem = error.Length > 0 ? error : dropped.Count > 0 ? $"forks left out (their variant is gone): {string.Join(", ", dropped)}" : string.Empty;
    }
}
