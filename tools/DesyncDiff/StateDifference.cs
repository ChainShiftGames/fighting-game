using System.Collections.Generic;

namespace DesyncDiff;

/// <summary>The fields that differ in one frame's state, which both dumps hold.</summary>
public sealed record StateDifference(int Frame, IReadOnlyList<FieldDifference> Fields);
