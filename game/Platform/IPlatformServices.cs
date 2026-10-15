using System;

namespace FightingGame.Platform;

/// <summary>Store-platform features (Steam or none), so the game runs the same with and without Steam.</summary>
public interface IPlatformServices : IDisposable
{
    /// <summary>Shown to the player, e.g. "Steam" or "Offline".</summary>
    string PlatformName { get; }

    string PlayerName { get; }

    /// <summary>Starts looking for an online opponent; null when this platform has no online play.</summary>
    IMatchmaking? OpenMatchmaking();

    /// <summary>Lets the platform do its per-frame work. Called once per displayed frame.</summary>
    void Update();
}
