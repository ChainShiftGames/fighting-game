namespace Sim.Data.Online;

/// <summary>
/// When an online match warns about and gives up on a silent connection (ADR-0005 §4), from
/// <c>netcode/online.json</c>. Developer settings.
/// </summary>
/// <param name="WarningAfterMilliseconds">Silence from the other PC before the "connection interrupted" warning.</param>
/// <param name="EndMatchAfterMilliseconds">
/// Silence before the match ends without a result; more than <paramref name="WarningAfterMilliseconds"/>.
/// </param>
public sealed record DisconnectSettings(int WarningAfterMilliseconds, int EndMatchAfterMilliseconds);
