using FightingGame.UI.Framework;

namespace FightingGame.UI;

/// <summary>One button of the online lobby menu.</summary>
/// <param name="FriendIndex">For friend choices, the index into the lobby's listed friends; otherwise -1.</param>
public readonly record struct LobbyMenuItem(LobbyChoice Choice, MenuText Label, bool Enabled = true, int FriendIndex = -1);
