using System;
using System.Collections.Generic;

namespace FightingGame.Addons.ControlPanel.Documents;

/// <summary>
/// One developer setting the control panel edits (ARCHITECTURE.md §6): the value at <see cref="Path"/> in
/// <see cref="File"/>, with the range the file's loader accepts so the panel never offers a value it rejects alone.
/// </summary>
public sealed record PanelSetting
{
    private PanelSetting(
        string label,
        PanelFile file,
        string path,
        SettingKind kind,
        double min,
        double max,
        double step,
        IReadOnlyList<string> choices)
    {
        Label = label;
        File = file;
        Path = path;
        Kind = kind;
        Min = min;
        Max = max;
        Step = step;
        Choices = choices;
    }

    public string Label { get; }

    public PanelFile File { get; }

    /// <summary>Where the value is in the file (<see cref="JsonPathField"/>), e.g. <c>stats.gravity</c>.</summary>
    public string Path { get; }

    public SettingKind Kind { get; }

    public double Min { get; }

    public double Max { get; }

    /// <summary>The arrow increment; any value in range can be typed.</summary>
    public double Step { get; init; }

    /// <summary>The names a <see cref="SettingKind.Choice"/> may take, in the order the panel lists them.</summary>
    public IReadOnlyList<string> Choices { get; }

    public SettingUnit Unit { get; init; }

    /// <summary>One line on what the setting does, shown as the row's tooltip.</summary>
    public string Hint { get; init; } = string.Empty;

    /// <summary>Digits after the point the file keeps: 0 for whole numbers.</summary>
    public int Decimals => Kind == SettingKind.Decimal ? 3 : 0;

    public static PanelSetting Whole(string label, PanelFile file, string path, int min, int max, int step = 1) =>
        new(label, file, path, SettingKind.Whole, min, max, step, []);

    public static PanelSetting Decimal(string label, PanelFile file, string path, double min, double max, double step) =>
        new(label, file, path, SettingKind.Decimal, min, max, step, []);

    public static PanelSetting Flag(string label, PanelFile file, string path) =>
        new(label, file, path, SettingKind.Flag, 0, 1, 1, []);

    public static PanelSetting Choice(string label, PanelFile file, string path, IReadOnlyList<string> choices) =>
        new(label, file, path, SettingKind.Choice, 0, choices.Count - 1, 1, choices);

    /// <summary>The value clamped into range and rounded the way the file stores it.</summary>
    public double Fit(double value) => Math.Round(Math.Clamp(value, Min, Max), Decimals, MidpointRounding.AwayFromZero);
}
