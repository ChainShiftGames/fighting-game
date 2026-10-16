namespace Sim.Data.Online;

/// <summary>How online matches run, from <c>netcode/online.json</c> (developer settings, ARCHITECTURE.md §6).</summary>
/// <remarks>Netcode only, so not part of the data hash: each PC may use its own values without changing results.</remarks>
/// <param name="RollbackFrames">Rollback window (ADR-0003): most frames predicted ahead, and so rolled back.</param>
public sealed record OnlineSettings(int RollbackFrames);
