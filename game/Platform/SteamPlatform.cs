using Platform.Steam;

namespace FightingGame.Platform;

/// <summary>Platform features from the Steam client.</summary>
public sealed class SteamPlatform : IPlatformServices
{
    private readonly SteamSession _session;

    public SteamPlatform(SteamSession session)
    {
        _session = session;
        PlayerName = session.PlayerName;
    }

    public string PlatformName => "Steam";

    public string PlayerName { get; }

    public IMatchmaking? OpenMatchmaking() => new SteamLobbyMatchmaking(_session);

    public void Update() => _session.RunCallbacks();

    public void Dispose() => _session.Dispose();
}
