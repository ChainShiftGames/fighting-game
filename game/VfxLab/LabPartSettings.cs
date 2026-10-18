using System.Collections.Generic;
using FightingGame.VfxLab.Files;

namespace FightingGame.VfxLab;

/// <summary>The part settings every part has a slider for (developer settings, presentation), and how to read them.</summary>
public static class LabPartSettings
{
    public static readonly LabPartSetting Spin = new("spin", "Turn per frame", 0f, 0.6f, 0.005f, part => part.Spin);
    public static readonly LabPartSetting Scale = new("scale", "Size", 0.25f, 3f, 0.01f, _ => 1f);
    public static readonly LabPartSetting CardWidth = new("cardWidth", "Card width (m)", 0.4f, 6f, 0.05f, part => part.CardWidth);
    public static readonly LabPartSetting CardHeight = new("cardHeight", "Card height (m)", 0.4f, 6f, 0.05f, part => part.CardHeight);
    public static readonly LabPartSetting GrowFrames = new("growFrames", "Grow frames", 0f, 40f, 1f, part => part.GrowFrames, FlightOnly: true);
    public static readonly LabPartSetting StartScale = new("startScale", "Start size", 0.02f, 1f, 0.01f, part => part.StartScale, FlightOnly: true);

    public static IReadOnlyList<LabPartSetting> All { get; } = [Spin, Scale, GrowFrames, StartScale, CardWidth, CardHeight];

    /// <summary>The setting's value for <paramref name="part"/>: from <paramref name="values"/> when set there, else the part's.</summary>
    public static float Read(LabPartSetting setting, LabPartKind kind, LabPart part, IReadOnlyDictionary<string, LabValue> values) =>
        values.TryGetValue(setting.KeyFor(kind), out LabValue value) ? value.First : setting.Default(part);
}
