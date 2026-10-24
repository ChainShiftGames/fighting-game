using FightingGame.UI.Framework;

namespace FightingGame.UI.Online;

/// <summary>One button of the online screens.</summary>
/// <param name="FriendIndex">For friend choices, the index into the lobby's listed friends; otherwise -1.</param>
public readonly record struct LobbyMenuItem(LobbyChoice Choice, MenuText Label, bool Enabled = true, int FriendIndex = -1)
{
    /// <summary>The line under the button while it has the cursor; may be empty.</summary>
    public MenuText Description { get; init; } = MenuText.Empty;
}
