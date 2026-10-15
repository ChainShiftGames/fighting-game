using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Steamworks;

namespace Platform.Steam;

/// <summary>A connection to the Steam client running on this PC. Start it once; dispose it to shut Steam down.</summary>
public sealed class SteamSession : IDisposable
{
    // Steam reads the app ID from this variable when Steam didn't launch the game, so dev builds need no
    // steam_appid.txt next to the executable.
    private const string AppIdVariable = "SteamAppId";

    private static int _resolverRegistered;

    private bool _disposed;

    private SteamSession()
    {
    }

    /// <summary>The Steam user's display name.</summary>
    public string PlayerName => SteamFriends.GetPersonaName();

    /// <summary>
    /// Connects to Steam as <paramref name="appId"/>. Returns null, with the reason, when Steam isn't available:
    /// not installed or running, nobody logged in, or the native library missing.
    /// </summary>
    /// <param name="nativeLibraryFolder">
    /// Folder holding the Steam native library, when it can't be found next to Steamworks.NET.dll: hosts that load
    /// assemblies from memory (the Godot editor) leave them without a location. Null to look next to the assembly.
    /// </param>
    public static SteamSession? TryStart(uint appId, string? nativeLibraryFolder, out string failureReason)
    {
        Environment.SetEnvironmentVariable(AppIdVariable, appId.ToString(CultureInfo.InvariantCulture));
        RegisterNativeLibraryResolver(nativeLibraryFolder);
        try
        {
            if (!Packsize.Test() || !DllCheck.Test())
            {
                failureReason = "the Steam libraries don't match this platform or Steamworks.NET version";
                return null;
            }

            ESteamAPIInitResult result = SteamAPI.InitEx(out string message);
            if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
            {
                failureReason = $"{result}: {message}";
                return null;
            }
        }
        catch (DllNotFoundException exception)
        {
            failureReason = $"Steam API library not found ({exception.Message})";
            return null;
        }

        failureReason = string.Empty;
        return new SteamSession();
    }

    // Godot loads game assemblies in its own load context, which only finds native libraries listed in deps.json.
    // The Steam library is copied next to Steamworks.NET.dll instead (Platform.Steam.csproj), so load it from there.
    private static void RegisterNativeLibraryResolver(string? nativeLibraryFolder)
    {
        if (Interlocked.Exchange(ref _resolverRegistered, 1) == 0)
        {
            NativeLibrary.SetDllImportResolver(
                typeof(SteamAPI).Assembly,
                (libraryName, assembly, _) => LoadFromFolder(libraryName, nativeLibraryFolder ?? AssemblyFolder(assembly)));
        }
    }

    private static string? AssemblyFolder(Assembly assembly) =>
        assembly.Location.Length == 0 ? null : Path.GetDirectoryName(assembly.Location);

    // Zero falls back to .NET's normal search.
    private static IntPtr LoadFromFolder(string libraryName, string? folder)
    {
        string extension = OperatingSystem.IsWindows() ? ".dll" : ".so";
        return folder is not null && NativeLibrary.TryLoad(Path.Combine(folder, libraryName + extension), out IntPtr handle)
            ? handle
            : IntPtr.Zero;
    }

    /// <summary>Delivers Steam's pending callbacks. Call once per displayed frame.</summary>
    public void RunCallbacks() => SteamAPI.RunCallbacks();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SteamAPI.Shutdown();
    }
}
