using FightingGame.UI.Framework;
using Platform.Steam.Lobbies;

namespace FightingGame.UI.Online;

/// <summary>What the online screens say about a <see cref="LobbyNotice"/>: a failed lobby, the opponent leaving or lost.</summary>
public static class LobbyNoticeText
{
    public static MenuText For(LobbyNotice notice) => notice.Kind switch
    {
        LobbyNoticeKind.CreateFailed => MenuText.Key("ONLINE_NOTICE_CREATE_FAILED", notice.Detail),
        LobbyNoticeKind.JoinFailed => MenuText.Key("ONLINE_NOTICE_JOIN_FAILED", notice.Detail),
        LobbyNoticeKind.OpponentLeft => MenuText.Key("ONLINE_NOTICE_OPPONENT_LEFT", notice.Detail),
        LobbyNoticeKind.ConnectionLost => MenuText.Key("ONLINE_NOTICE_CONNECTION_LOST", notice.Detail),
        _ => MenuText.Empty,
    };
}
