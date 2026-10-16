using System.IO;
using Godot;
using Sim.Data;
using Sim.Data.Online;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Online;

/// <summary>
/// Where local matches load all game data from (ADR-0015 §3): the repository's <c>data/</c> folder when run from the
/// editor, the copy embedded in Sim.Data in exported builds.
/// </summary>
public sealed class GameDataLocation
{
    private GameDataLocation(string? folder)
    {
        Folder = folder;
    }

    /// <summary>The data folder's full path, which the debug panel writes developer settings into; null when embedded.</summary>
    public string? Folder { get; }

    /// <summary>The data folder next to <c>game/</c> in editor runs (also <c>godot --path game</c>), else the embedded data.</summary>
    public static GameDataLocation Choose()
    {
        if (!OS.HasFeature("editor"))
        {
            return new GameDataLocation(null);
        }

        string folder = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "data"));
        if (Directory.Exists(folder))
        {
            return new GameDataLocation(folder);
        }

        GD.PushWarning($"No data folder at {folder}: using the data embedded in Sim.Data.");
        return new GameDataLocation(null);
    }

    /// <summary>The online match settings (developer settings), read again for every online match.</summary>
    /// <exception cref="DataValidationException">The file is missing or invalid.</exception>
    public OnlineSettings LoadOnlineSettings() => OnlineSettingsLoader.Load(Source());

    /// <summary>Loads and validates everything, reading the files again so data edits show up in the next match.</summary>
    /// <exception cref="DataValidationException">Any file is invalid.</exception>
    /// <exception cref="InvalidDataException">There is no character to play.</exception>
    public GameData Load()
    {
        GameData data = new GameDataLoader(Source()).Load();
        if (data.Characters.Count == 0)
        {
            throw new InvalidDataException(
                $"No characters in {Folder ?? "the embedded data"}: add one under data/characters/ to play.");
        }

        return data;
    }

    private IDataSource Source() => Folder is null ? new EmbeddedDataSource() : new DirectoryDataSource(Folder);
}
