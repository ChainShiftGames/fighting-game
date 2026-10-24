namespace Platform.Steam.Lobbies;

/// <summary>The last thing that went wrong or changed on its own in the lobby, and the name or code that goes with it.</summary>
/// <param name="Detail">The opponent's name or Steam's result code; empty for <see cref="LobbyNoticeKind.None"/>.</param>
public readonly record struct LobbyNotice(LobbyNoticeKind Kind, string Detail)
{
    public static LobbyNotice None { get; } = new(LobbyNoticeKind.None, string.Empty);

    public bool IsNone => Kind == LobbyNoticeKind.None;
}
