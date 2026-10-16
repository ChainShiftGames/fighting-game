using System.Net;
using Netcode.Online;

namespace FightingGame.Online;

/// <summary>The other PC of an online match: how to reach it, and which player each side is.</summary>
/// <param name="Address">An <see cref="IPEndPoint"/> for direct UDP, or the transport's own endpoint (a Steam ID).</param>
/// <param name="Name">Shown in the overlay; the address when there is no name.</param>
/// <param name="LocalPort">UDP port this PC listens on; 0 for relay transports (Steam), which have none.</param>
public sealed record OnlineOpponent(
    EndPoint Address, string Name, bool LocalIsPlayer1, PeerTransport Transport, int LocalPort);
