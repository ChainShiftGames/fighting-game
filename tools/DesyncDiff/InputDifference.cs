using Netcode.Desync;

namespace DesyncDiff;

/// <summary>A frame whose confirmed inputs differ between the dumps: an input sync bug, not a logic one.</summary>
public sealed record InputDifference(DumpedInputs First, DumpedInputs Second);
