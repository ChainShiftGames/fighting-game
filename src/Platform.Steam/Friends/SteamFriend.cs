namespace Platform.Steam.Friends;

/// <summary>A Steam friend: their ID and current display name.</summary>
public readonly record struct SteamFriend(ulong SteamId, string Name);
