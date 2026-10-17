using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// A move laid out frame by frame for the timeline: its phases, then a row per box kind, cancel window, velocity
/// and projectile spawns. Ranges are shown as written, even outside the move, so mistakes are visible.
/// </summary>
public sealed class MoveTimeline
{
    private static readonly (string Kind, string Label, TimelineSpanKind Span)[] _boxKinds =
    [
        ("hurt", "Hurt boxes", TimelineSpanKind.HurtBox),
        ("hit", "Hit boxes", TimelineSpanKind.HitBox),
        ("throw", "Throw boxes", TimelineSpanKind.ThrowBox),
        ("push", "Push boxes", TimelineSpanKind.PushBox),
    ];

    private MoveTimeline(FramePhases? phases, IReadOnlyList<TimelineRow> rows)
    {
        Phases = phases;
        Rows = rows;
        int frames = phases?.TotalFrames ?? 0;
        foreach (TimelineRow row in rows)
        {
            foreach (TimelineSpan span in row.Spans)
            {
                frames = Math.Max(frames, span.LastFrame);
            }
        }

        FrameCount = frames;
    }

    /// <summary>The move's phases; null when its frame data can't be read.</summary>
    public FramePhases? Phases { get; }

    public IReadOnlyList<TimelineRow> Rows { get; }

    /// <summary>Frames to draw: the move's length, or further when a range runs past its end.</summary>
    public int FrameCount { get; }

    public static MoveTimeline Of(JsonObject move)
    {
        FramePhases? phases = FramePhases.Of(move);
        List<TimelineRow> rows = [];
        if (phases is not null)
        {
            rows.Add(new TimelineRow("Frames", PhaseSpans(phases)));
        }

        if (Range(move[MoveFields.Invulnerable], TimelineSpanKind.Invulnerable) is TimelineSpan invulnerable)
        {
            rows.Add(new TimelineRow("Invulnerable", [invulnerable]));
        }

        AddBoxRows(move[MoveFields.Boxes] as JsonArray ?? [], rows);
        int window = 0;
        foreach (JsonNode? cancel in move[MoveFields.Cancels] as JsonArray ?? [])
        {
            window++;
            List<string> onNames = [];
            foreach (JsonNode? name in cancel?[MoveFields.On] as JsonArray ?? [])
            {
                onNames.Add(JsonNodeValue.Text(name) ?? "?");
            }

            string on = string.Join(", ", onNames);
            rows.Add(new TimelineRow($"Cancel {window} ({on})", Ranges([cancel], TimelineSpanKind.Cancel)));
        }

        AddRowIfAny(rows, "Velocity", Ranges(move[MoveFields.Velocity] as JsonArray ?? [], TimelineSpanKind.Velocity));
        AddRowIfAny(rows, "Projectiles", Spawns(move[MoveFields.Projectiles] as JsonArray ?? []));
        return new MoveTimeline(phases, rows);
    }

    private static List<TimelineSpan> PhaseSpans(FramePhases phases)
    {
        if (phases.IsBasic)
        {
            return [new TimelineSpan(1, phases.Duration, TimelineSpanKind.Loop)];
        }

        List<TimelineSpan> spans = [];
        if (phases.Startup > 1)
        {
            spans.Add(new TimelineSpan(1, phases.Startup - 1, TimelineSpanKind.Startup));
        }

        spans.Add(new TimelineSpan(phases.FirstActiveFrame, phases.LastActiveFrame, TimelineSpanKind.Active));
        if (phases.Recovery > 0)
        {
            spans.Add(new TimelineSpan(phases.LastActiveFrame + 1, phases.TotalFrames, TimelineSpanKind.Recovery));
        }

        return spans;
    }

    private static void AddBoxRows(JsonArray boxes, List<TimelineRow> rows)
    {
        List<JsonNode?> unknown = [.. boxes];
        foreach ((string kind, string label, TimelineSpanKind span) in _boxKinds)
        {
            List<JsonNode?> ofKind = unknown.FindAll(box => JsonNodeValue.Text(box?[MoveFields.Kind]) == kind);
            unknown.RemoveAll(ofKind.Contains);
            AddRowIfAny(rows, label, Ranges(ofKind, span));
        }

        AddRowIfAny(rows, "Other boxes", Ranges(unknown, TimelineSpanKind.OtherBox));
    }

    private static void AddRowIfAny(List<TimelineRow> rows, string label, List<TimelineSpan> spans)
    {
        if (spans.Count > 0)
        {
            rows.Add(new TimelineRow(label, spans));
        }
    }

    private static List<TimelineSpan> Ranges(IEnumerable<JsonNode?> items, TimelineSpanKind kind)
    {
        List<TimelineSpan> spans = [];
        foreach (JsonNode? item in items)
        {
            if (Range(item, kind) is TimelineSpan span)
            {
                spans.Add(span);
            }
        }

        return spans;
    }

    private static TimelineSpan? Range(JsonNode? item, TimelineSpanKind kind) =>
        JsonNodeValue.Int(item?[MoveFields.FirstFrame]) is int first
        && JsonNodeValue.Int(item?[MoveFields.LastFrame]) is int last
            ? new TimelineSpan(first, last, kind)
            : null;

    private static List<TimelineSpan> Spawns(JsonArray projectiles)
    {
        List<TimelineSpan> spans = [];
        foreach (JsonNode? projectile in projectiles)
        {
            if (JsonNodeValue.Int(projectile?[MoveFields.SpawnFrame]) is int frame)
            {
                spans.Add(new TimelineSpan(frame, frame, TimelineSpanKind.ProjectileSpawn));
            }
        }

        return spans;
    }
}
