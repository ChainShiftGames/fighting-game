using System.Collections.Generic;
using Sim.Characters;

namespace Sim.Data.Characters.Reading;

/// <summary>A validated move file. <see cref="Move"/>'s ranges are filled in when the character is assembled.</summary>
public sealed record MoveRecord(
    MoveData Move,
    IReadOnlyList<BoxData> Boxes,
    IReadOnlyList<CancelRecord> Cancels,
    IReadOnlyList<HitData> Hits);
