using System.Collections.Generic;
using Platform.Steam.Lobbies;

namespace FightingGame.UI.Online;

/// <summary>The players in the lobby, P1 first: this player and, once there, the opponent.</summary>
public static class LobbyMembers
{
    /// <param name="localName">This player's name on the platform.</param>
    public static List<LobbyMember> Of(LobbyFlow lobby, string localName)
    {
        int localSide = lobby.IsHost ? 0 : 1;
        var you = new LobbyMember(localSide, localName, IsYou: true, lobby.IsLocalReady);
        if (!lobby.HasOpponent)
        {
            return [you];
        }

        var opponent = new LobbyMember(1 - localSide, lobby.OpponentName, IsYou: false, lobby.IsOpponentReady);
        return localSide == 0 ? [you, opponent] : [opponent, you];
    }
}
