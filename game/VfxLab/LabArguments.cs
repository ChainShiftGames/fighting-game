using System;
using System.Collections.Generic;
using System.Globalization;

namespace FightingGame.VfxLab;

/// <summary>
/// The Lab's command line, after <c>--</c>: which sheet and view to open, and <c>--lab-capture</c> to render one frame
/// to a PNG and quit, so agents can check their work (ADR-0029 §8).
/// </summary>
public sealed record LabArguments
{
    public string? Sheet { get; init; }

    public string? Round { get; init; }

    public string? Solo { get; init; }

    public int? Columns { get; init; }

    public LabFramingKind? Framing { get; init; }

    public LabBackdrop? Backdrop { get; init; }

    public int? Impact { get; init; }

    /// <summary>Where to save the capture; null runs the Lab normally.</summary>
    public string? Capture { get; init; }

    /// <summary>The frame to capture, counted from the loop's start.</summary>
    public int Frame { get; init; }

    public (int Width, int Height)? Size { get; init; }

    /// <param name="error">The first argument that couldn't be read; empty when all were.</param>
    public static LabArguments Parse(IReadOnlyList<string> args, out string error)
    {
        LabArguments parsed = new();
        error = string.Empty;
        for (int i = 0; i < args.Count; i++)
        {
            string key = args[i];
            string value = i + 1 < args.Count ? args[i + 1] : string.Empty;
            LabArguments? next = key switch
            {
                "--lab-sheet" => parsed with { Sheet = value },
                "--lab-round" => parsed with { Round = value },
                "--lab-solo" => parsed with { Solo = value },
                "--lab-capture" => parsed with { Capture = value },
                "--lab-columns" when Whole(value) is { } columns => parsed with { Columns = Math.Clamp(columns, 1, 5) },
                "--lab-frame" when Whole(value) is { } frame => parsed with { Frame = frame },
                "--lab-impact" when Whole(value) is { } impact => parsed with { Impact = Math.Clamp(impact, 0, 3) },
                "--lab-framing" when Enum.TryParse(value, true, out LabFramingKind framing) => parsed with { Framing = framing },
                "--lab-backdrop" when Enum.TryParse(value, true, out LabBackdrop backdrop) => parsed with { Backdrop = backdrop },
                "--lab-size" when SizeOf(value) is { } size => parsed with { Size = size },
                _ => null,
            };
            if (next is null)
            {
                if (key.StartsWith("--lab-", StringComparison.Ordinal) && error.Length == 0)
                {
                    error = $"can't read {key} {value}";
                }

                continue;
            }

            parsed = next;
            i++;
        }

        return parsed;
    }

    /// <summary>Puts the view choices on <paramref name="options"/>.</summary>
    public void ApplyTo(LabViewOptions options)
    {
        options.Round = Round ?? options.Round;
        options.Solo = Solo ?? options.Solo;
        options.Columns = Columns ?? options.Columns;
        options.Framing = Framing ?? options.Framing;
        options.Backdrop = Backdrop ?? options.Backdrop;
        options.Impact = Impact is { } impact ? (Presentation.World.Projectiles.ImpactKind)impact : options.Impact;
    }

    private static int? Whole(string text) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : null;

    private static (int, int)? SizeOf(string text) =>
        text.Split('x') is [string w, string h] && Whole(w) is { } width && Whole(h) is { } height && width > 0 && height > 0
            ? (width, height)
            : null;
}
