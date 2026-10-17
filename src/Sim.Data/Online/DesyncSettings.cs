namespace Sim.Data.Online;

/// <summary>How much an online desync dump holds (ADR-0005 §5), from <c>netcode/online.json</c>. Developer settings.</summary>
/// <param name="HistoryFrames">
/// Newest frames of states and inputs kept for a dump; also how far one PC's checksums may run ahead of the other's.
/// </param>
public sealed record DesyncSettings(int HistoryFrames);
