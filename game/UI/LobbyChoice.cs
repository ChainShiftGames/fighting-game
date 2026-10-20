namespace FightingGame.UI;

/// <summary>What a button of the online lobby menu does (<see cref="LobbyMenuItems"/>).</summary>
public enum LobbyChoice
{
    Host,

    /// <summary>Join the listed friend's lobby.</summary>
    JoinFriend,

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
