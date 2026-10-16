using System.Collections.Generic;
using Sim.Characters;

namespace Sim.Data.Characters.Reading;

/// <summary>A validated cancel window with its target move indexes, before they are packed into one array.</summary>
public sealed record CancelRecord(CancelData Cancel, IReadOnlyList<int> Targets);
