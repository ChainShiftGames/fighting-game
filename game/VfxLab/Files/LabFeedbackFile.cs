using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FightingGame.VfxLab.Files;

/// <summary>
/// Reads and writes a sheet's <c>feedback.json</c> (ADR-0029 §6). The Lab writes it; agents read it to plan the next
/// round. Keys are written sorted and empty entries left out, so its diffs stay small.
/// </summary>
public static class LabFeedbackFile
{
    public const int CurrentVersion = 1;

    /// <param name="error">Why the file was refused; empty when it was read.</param>
    public static LabFeedback? Read(string json, out string error)
    {
        FeedbackJson? file;
        try
        {
            file = JsonSerializer.Deserialize<FeedbackJson>(json, LabSheetFile.Json);
        }
        catch (JsonException exception)
        {
            error = exception.Message;
            return null;
        }

        if (file is null || file.FeedbackVersion != CurrentVersion)
        {
            error = $"feedbackVersion must be {CurrentVersion}";
            return null;
        }

        error = string.Empty;
        return new LabFeedback
        {
            Variants = new Dictionary<string, VariantFeedback>(file.Variants ?? [], StringComparer.Ordinal),
            Forks = file.Forks ?? [],
            Timing = new Dictionary<string, float>(file.Timing ?? [], StringComparer.Ordinal),
        };
    }

    public static string Write(LabFeedback feedback)
    {
        SortedDictionary<string, VariantFeedback> variants = new(StringComparer.Ordinal);
        foreach ((string id, VariantFeedback entry) in feedback.Variants.Where(pair => !pair.Value.IsEmpty))
        {
            variants[id] = new VariantFeedback
            {
                Pick = entry.Pick,
                Rating = entry.Rating,
                Hidden = entry.Hidden,
                Note = entry.Note,
                Values = new Dictionary<string, LabValue>(new SortedDictionary<string, LabValue>(entry.Values, StringComparer.Ordinal)),
            };
        }

        FeedbackJson file = new()
        {
            FeedbackVersion = CurrentVersion,
            Variants = new Dictionary<string, VariantFeedback>(variants),
            Forks = feedback.Forks,
            Timing = new Dictionary<string, float>(new SortedDictionary<string, float>(feedback.Timing, StringComparer.Ordinal)),
        };
        return JsonSerializer.Serialize(file, LabSheetFile.Json) + "\n";
    }

    private sealed class FeedbackJson
    {
        public int FeedbackVersion { get; init; }

        public Dictionary<string, VariantFeedback>? Variants { get; init; }

        public List<LabFork>? Forks { get; init; }

        public Dictionary<string, float>? Timing { get; init; }
    }
}
