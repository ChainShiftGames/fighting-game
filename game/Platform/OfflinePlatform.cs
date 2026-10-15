namespace FightingGame.Platform;

/// <summary>No store platform (Steam not running, dev and test builds): local play only.</summary>
public sealed class OfflinePlatform : IPlatformServices
{
    public string PlatformName => "Offline";

    public string PlayerName => "Player";

    // Nothing to poll without a platform.
    public void Update()
    {
    }

    // Holds no platform connection.
    public void Dispose()
    {
    }
}
