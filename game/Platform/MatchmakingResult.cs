using System.Net;
using Netcode.Online;

namespace FightingGame.Platform;

/// <summary>An opponent found by <see cref="IMatchmaking"/>: who they are, which port they play, and how to reach them.</summary>
public sealed record MatchmakingResult(EndPoint Opponent, string OpponentName, bool LocalIsPlayer1, PeerTransport Transport)
{
    /// <summary>This player's character pick as the game wrote it (ADR-0042).</summary>
    public string LocalPick { get; init; } = string.Empty;

    /// <summary>The opponent's character pick as their game wrote it.</summary>
    public string OpponentPick { get; init; } = string.Empty;
}
