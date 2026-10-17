namespace Sim.Data.Online;

/// <summary>How online matches run, from <c>netcode/online.json</c> (developer settings, ARCHITECTURE.md §6).</summary>
/// <remarks>Netcode only, so not part of the data hash: each PC may use its own values without changing results.</remarks>
/// <param name="RollbackFrames">Rollback window (ADR-0003): most frames predicted ahead, and so rolled back.</param>
/// <param name="AutomaticInputDelay">How the input delay follows the ping (ADR-0005 §4).</param>
/// <param name="Disconnect">When a silent connection is warned about and ends the match (ADR-0005 §4).</param>
public sealed record OnlineSettings(
    int RollbackFrames, AutomaticInputDelaySettings AutomaticInputDelay, DisconnectSettings Disconnect);
