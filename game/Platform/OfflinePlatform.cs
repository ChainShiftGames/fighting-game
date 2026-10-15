namespace FightingGame.Platform;

/// <summary>No store platform (Steam not running, dev and test builds): local play only.</summary>
public sealed class OfflinePlatform : IPlatformServices
{
    public string PlatformName => "Offline";

    public string PlayerName => "Player";

    // No platform, no one to play online with; direct UDP matches don't need matchmaking.
    public IMatchmaking? OpenMatchmaking() => null;

    // Nothing to poll without a platform.
    public void Update()
    {
    }

    // Holds no platform connection.
    public void Dispose()
    {
    }
}
