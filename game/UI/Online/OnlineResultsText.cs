using System.Collections.Generic;
using FightingGame.UI.Framework;
using Netcode.Handshake;
using Platform.Steam.Lobbies;

namespace FightingGame.UI.Online;

/// <summary>What the results screen says after an online match: why it never started, or who wants the rematch.</summary>
public static class OnlineResultsText
{
    public static List<MenuText> Describe(LobbyFlow lobby, HandshakeRefusal refusal)
    {
        List<MenuText> lines = PreMatchCheckText.For(refusal);
        if (lines.Count > 0 || lobby.Stage != LobbyStage.Rematch)
        {
            return lines;
        }

        string opponent = lobby.IsOpponentReady ? "LOBBY_STATE_REMATCH" : "LOBBY_STATE_NOT_YET";
        lines.Add(MenuText.Key("LOBBY_OPPONENT_IS", MenuText.Plain(lobby.OpponentName), MenuText.Key(opponent)));
        return lines;
    }

    /// <summary>Whether the rematch can be offered: not when the checks refused the match (it would refuse again).</summary>
    public static bool CanRematch(HandshakeRefusal refusal) => refusal == HandshakeRefusal.None;
}
