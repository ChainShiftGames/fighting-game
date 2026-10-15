namespace Netcode.Online;

/// <summary>One reading of a transport's link to the other PC (developer overlay, P0-22).</summary>
/// <param name="PingMilliseconds">Round trip as the transport measures it, without the game's own frame pacing.</param>
/// <param name="IsRelayed">Whether packets go through a relay server rather than straight to the other PC.</param>
/// <param name="DeliveredPercent">Share of the other PC's packets that arrive, or -1 while the transport doesn't know.</param>
public readonly record struct TransportLinkStats(int PingMilliseconds, bool IsRelayed, int DeliveredPercent);
