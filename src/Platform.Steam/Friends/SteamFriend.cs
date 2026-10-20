namespace Platform.Steam.Friends;

/// <summary>A Steam friend: their ID, current display name and the game lobby they are in (0: none or unknown).</summary>
/// <remarks>The lobby is only known for friends listed as running this game (<see cref="FriendsInGame"/>).</remarks>
public readonly record struct SteamFriend(ulong SteamId, string Name, ulong LobbyId = SteamFriend.NoLobby)
{
    public const ulong NoLobby = 0;

    /// <summary>Whether the friend is in a lobby this player can join.</summary>
    public bool HasLobby => LobbyId != NoLobby;
}
