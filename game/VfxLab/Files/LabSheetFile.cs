using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FightingGame.VfxLab.Files;

/// <summary>
/// Reads a VFX Lab sheet, <c>VfxLab/Sheets/&lt;id&gt;/sheet.json</c> (developer setting, presentation; ADR-0029). An
/// invalid sheet is refused with the reason, never fixed up.
/// </summary>
public static class LabSheetFile
{
    public const int CurrentVersion = 1;

    /// <summary>Options for every Lab file: camelCase keys, comments allowed, unknown keys refused.</summary>
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new LabValueJson() },
    };

    /// <param name="folder">The sheet's folder; shader paths that aren't <c>res://</c> are relative to it.</param>
    /// <param name="error">Why the sheet was refused; empty when it was read.</param>
    public static LabSheet? Read(string json, string folder, out string error)
    {
        SheetJson? file;
        try
        {
            file = JsonSerializer.Deserialize<SheetJson>(json, Json);
        }
        catch (JsonException exception)
        {
            error = exception.Message;
            return null;
        }

        error = Check(file);
        if (error.Length > 0)
        {
            return null;
        }

        List<LabRound> rounds = file!.Rounds.Select(round => new LabRound(
            round.Id,
            round.Name ?? round.Id,
            round.Note ?? string.Empty,
            round.Variants.Select(variant => variant.ToDraft(folder)).ToList())).ToList();
        if (VariantInheritance.Resolve(rounds.SelectMany(round => round.Variants), out error) is null)
        {
            return null;
        }

        StyleJson style = file.Style ?? new StyleJson();
        return new LabSheet(
            file.Title ?? "Untitled effect",
            file.Brief ?? string.Empty,
            new LabStyle(style.Character ?? string.Empty, style.Motifs ?? string.Empty, style.Palette ?? []),
            file.Timing ?? new LabTiming(),
            file.Ranges ?? [],
            rounds);
    }

    /// <summary>Where a part's shader is: a <c>res://</c> path as written, anything else relative to the sheet's folder.</summary>
    public static string Resolve(string shader, string folder) =>
        shader.StartsWith("res://", StringComparison.Ordinal) ? shader : $"{folder}/{shader}";

    private static string Check(SheetJson? file) => file switch
    {
        null => "the file is empty",
        { LabVersion: not CurrentVersion } => $"labVersion is {file.LabVersion}, this build reads {CurrentVersion}",
        { Rounds.Count: 0 } => "the sheet has no rounds",
        _ when file.Rounds.FirstOrDefault(round => round.Variants.Count == 0) is { } empty =>
            $"round \"{empty.Id}\" has no variants",
        _ when file.Rounds.GroupBy(round => round.Id).FirstOrDefault(group => group.Count() > 1) is { } twice =>
            $"two rounds are called \"{twice.Key}\"",
        _ when file.Timing is { } timing && (timing.FlightFrames < 1f || timing.ImpactFrames < 1f || timing.ChargeFrames < 0f) =>
            "timing needs flightFrames and impactFrames of 1 or more and chargeFrames of 0 or more",
        _ when BadRange(file.Ranges) is { } key => $"range \"{key}\" must be [min, max] or [min, max, step] with min below max",
        _ => string.Empty,
    };

    private static string? BadRange(Dictionary<string, float[]>? ranges) =>
        ranges?.FirstOrDefault(range => range.Value.Length is < 2 or > 3 || range.Value[0] >= range.Value[1]).Key;

    private sealed class SheetJson
    {
        [JsonRequired]
        public int LabVersion { get; init; }

        public string? Title { get; init; }

        public string? Brief { get; init; }

        public StyleJson? Style { get; init; }

        public LabTiming? Timing { get; init; }

        public Dictionary<string, float[]>? Ranges { get; init; }

        [JsonRequired]
        public List<RoundJson> Rounds { get; init; } = [];
    }

    private sealed class StyleJson
    {
        public string? Character { get; init; }

        public string? Motifs { get; init; }

        public Dictionary<string, LabValue>? Palette { get; init; }
    }

    private sealed class RoundJson
    {
        [JsonRequired]
        public string Id { get; init; } = string.Empty;

        public string? Name { get; init; }

        public string? Note { get; init; }

        [JsonRequired]
        public List<VariantJson> Variants { get; init; } = [];
    }

    private sealed class VariantJson
    {
        [JsonRequired]
        public string Id { get; init; } = string.Empty;

        public string? Name { get; init; }

        public string? Pitch { get; init; }

        public string? Extends { get; init; }

        public LabPartDraft? Charge { get; init; }

        public LabPartDraft? Flight { get; init; }

        public LabPartDraft? Impact { get; init; }

        public Dictionary<string, LabValue>? Values { get; init; }

        public LabVariantDraft ToDraft(string folder)
        {
            Dictionary<LabPartKind, LabPartDraft> parts = [];
            Add(parts, LabPartKind.Charge, Charge, folder);
            Add(parts, LabPartKind.Flight, Flight, folder);
            Add(parts, LabPartKind.Impact, Impact, folder);
            return new LabVariantDraft(Id, Name ?? string.Empty, Pitch ?? string.Empty, Extends, parts, Values ?? []);
        }

        private static void Add(Dictionary<LabPartKind, LabPartDraft> parts, LabPartKind kind, LabPartDraft? part, string folder)
        {
            if (part is not null)
            {
                parts[kind] = part.Shader is { } shader ? part with { Shader = Resolve(shader, folder) } : part;
            }
        }
    }
}
