using System.Collections.Generic;

namespace DesyncDiff;

/// <summary>Everything that differs between the two dumps of one desync.</summary>
/// <param name="Warnings">Header problems that make the comparison suspect (different builds, same port twice).</param>
/// <param name="States">Each frame whose state both dumps hold, oldest first; identical ones have no fields.</param>
/// <param name="FramesInOneDumpOnly">States only one dump holds, so they can't be compared.</param>
/// <param name="Inputs">Frames whose confirmed inputs differ.</param>
/// <param name="SharedInputFrames">How many frames' inputs both dumps hold.</param>
public sealed record DumpDiff(
    IReadOnlyList<string> Warnings,
    IReadOnlyList<StateDifference> States,
    IReadOnlyList<int> FramesInOneDumpOnly,
    IReadOnlyList<InputDifference> Inputs,
    int SharedInputFrames);
