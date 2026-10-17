using System;

namespace Netcode.Online;

/// <summary>
/// When an online match warns about and gives up on a silent connection (ADR-0005 §4). Developer settings, from
/// <c>data/netcode/online.json</c>.
/// </summary>
/// <param name="WarningAfter">Silence from the other PC before <see cref="ConnectionState.Interrupted"/>.</param>
/// <param name="EndMatchAfter">Silence before <see cref="ConnectionState.Disconnected"/>; longer than the warning.</param>
public sealed record DisconnectTimeouts(TimeSpan WarningAfter, TimeSpan EndMatchAfter);
