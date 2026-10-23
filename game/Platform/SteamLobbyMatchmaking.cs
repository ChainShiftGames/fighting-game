using Platform.Steam;
using Platform.Steam.Lobbies;
using Platform.Steam.Transport;

namespace FightingGame.Platform;

/// <summary>
/// Matchmaking through a Steam lobby and friend invites; matches run over Steam Networking Messages (ADR-0014).
/// Starts outside any lobby: one opens from the online screens or an accepted invite, never at boot, because a lobby
/// that isn't idle sends the game flow to the online screens.
/// </summary>
public sealed class SteamLobbyMatchmaking : IMatchmaking
{
    public SteamLobbyMatchmaking(SteamSession session)
    {
        Lobby = session.OpenLobby();
    }

    public LobbyFlow Lobby { get; }

    public MatchmakingResult? TakeMatchToStart() => Lobby.TakeMatchToStart() is { } match
        ? new MatchmakingResult(
            new SteamPeerEndPoint(match.OpponentSteamId),
            match.OpponentName,
            match.LocalIsPlayer1,
            SteamTransport.For(match.OpponentSteamId))
        : null;

    public void Dispose() => Lobby.Dispose();
}
