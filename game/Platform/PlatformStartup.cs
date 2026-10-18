using System;
using System.IO;
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
        SteamSession? session;
        string failureReason;
        try
        {
            session = SteamSession.TryStart(steamAppId, nativeLibraryFolder, out failureReason);
        }
        catch (Exception exception) when (exception is FileLoadException or FileNotFoundException or BadImageFormatException)
        {
            // Steamworks.NET is vendored for win-x64 and linux-x64 only; elsewhere (a Mac) it fails to load as soon as
            // TryStart is compiled, before TryStart's own checks can run.
            session = null;
            failureReason = $"Steamworks.NET doesn't load on this platform ({exception.Message})";
        }

        if (session is null)
        {
            GD.Print($"Steam not available, running offline: {failureReason}");
            return new OfflinePlatform();
        }

        return new SteamPlatform(session);
    }
}
