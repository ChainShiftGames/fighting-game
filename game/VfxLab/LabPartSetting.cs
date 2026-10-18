using System;
using FightingGame.VfxLab.Files;

namespace FightingGame.VfxLab;

/// <summary>One setting of a part that isn't a shader uniform (its clock, size or card), with its slider range.</summary>
/// <param name="FlightOnly">Only the flight part has it.</param>
public sealed record LabPartSetting(string Name, string Label, float Min, float Max, float Step, Func<LabPart, float> Default, bool FlightOnly = false)
{
    /// <summary>The key a variant's values hold it under: <c>flight.spin</c>.</summary>
    public string KeyFor(LabPartKind part) => $"{part.ToString().ToLowerInvariant()}.{Name}";
}
