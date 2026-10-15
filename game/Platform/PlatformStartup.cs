using Godot;
using Platform.Steam;

namespace FightingGame.Platform;

/// <summary>Picks the platform at startup: Steam when it's available, otherwise offline.</summary>
public static class PlatformStartup
{
    // Where the editor builds the C# solution for play. Exports load assemblies from disk, so Steam finds its
    // native library next to them and needs no folder.
    private const string EditorBuildFolder = "res://.godot/mono/temp/bin/Debug";

    public static IPlatformServices Start(uint steamAppId)
    {
        string? nativeLibraryFolder = OS.HasFeature("editor") ? ProjectSettings.GlobalizePath(EditorBuildFolder) : null;
        SteamSession? session = SteamSession.TryStart(steamAppId, nativeLibraryFolder, out string failureReason);
        if (session is null)
        {
            GD.Print($"Steam not available, running offline: {failureReason}");
            return new OfflinePlatform();
        }

        return new SteamPlatform(session);
    }
}
