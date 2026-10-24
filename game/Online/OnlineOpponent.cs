using System.Net;
using Netcode.Online;

namespace FightingGame.Online;

/// <summary>The other PC of an online match: how to reach it, and which player each side is.</summary>
/// <param name="Address">An <see cref="IPEndPoint"/> for direct UDP, or the transport's own endpoint (a Steam ID).</param>
/// <param name="Name">Shown in the overlay; the address when there is no name.</param>
/// <param name="LocalPort">UDP port this PC listens on; 0 for relay transports (Steam), which have none.</param>
public sealed record OnlineOpponent(
    EndPoint Address, string Name, bool LocalIsPlayer1, PeerTransport Transport, int LocalPort)
{
    /// <summary>P1's pick as <see cref="OnlinePickText"/>; empty plays the default lineup's character (direct UDP).</summary>
    public string Player1Pick { get; init; } = string.Empty;

    /// <summary>P2's pick as <see cref="OnlinePickText"/>; empty plays the default lineup's character.</summary>
    public string Player2Pick { get; init; } = string.Empty;

    /// <summary>
    /// This opponent with both players' picks on their ports: both PCs put the host's pick on P1, so they build the
    /// same lineup (ADR-0042).
    /// </summary>
    public OnlineOpponent WithPicks(string localPick, string opponentPick) => this with
    {
        Player1Pick = LocalIsPlayer1 ? localPick : opponentPick,
        Player2Pick = LocalIsPlayer1 ? opponentPick : localPick,
    };
}
