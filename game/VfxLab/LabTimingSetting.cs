using System;
using FightingGame.VfxLab.Files;

namespace FightingGame.VfxLab;

/// <summary>One timing field of a sheet with its slider range; <paramref name="Key"/> is its JSON name.</summary>
public sealed record LabTimingSetting(string Key, string Label, float Min, float Max, float Step, Func<LabTiming, float> Read, Func<LabTiming, float, LabTiming> With);
