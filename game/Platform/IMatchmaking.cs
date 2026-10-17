using System;
using Platform.Steam.Lobbies;

namespace FightingGame.Platform;

/// <summary>Finds an online opponent through the platform's lobby: host or accept an invite, both ready, rematch.</summary>
public interface IMatchmaking : IDisposable
{
    /// <summary>The lobby's stage, players and actions, for the lobby menu.</summary>
    LobbyFlow Lobby { get; }

    /// <summary>The opponent of the match the lobby starts now, and how to reach them; once per match, else null.</summary>
    MatchmakingResult? TakeMatchToStart();
}
