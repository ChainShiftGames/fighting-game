namespace FightingGame.UI.Online;

/// <summary>What a button of the online screens does (<see cref="LobbyMenuItems"/>).</summary>
public enum LobbyChoice
{
    Host,

    /// <summary>Join the listed friend's lobby.</summary>
    JoinFriend,

    /// <summary>Matchmaking with strangers: listed, not there yet.</summary>
    QuickMatch,

    InviteThroughOverlay,

    /// <summary>Invite the listed friend.</summary>
    InviteFriend,

    /// <summary>Ready for the first match, or for a rematch.</summary>
    Ready,

    /// <summary>Leave the lobby, staying in the online menu.</summary>
    Leave,

    /// <summary>Leave the lobby (if in one) and go back to the main menu.</summary>
    MainMenu,
}
