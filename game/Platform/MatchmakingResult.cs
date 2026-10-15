using System.Net;
using Netcode.Online;

namespace FightingGame.Platform;

/// <summary>An opponent found by <see cref="IMatchmaking"/>: who they are, which port they play, and how to reach them.</summary>
public sealed record MatchmakingResult(EndPoint Opponent, string OpponentName, bool LocalIsPlayer1, PeerTransport Transport);
