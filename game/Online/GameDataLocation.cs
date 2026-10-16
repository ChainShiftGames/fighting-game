using System.IO;
using Godot;
using Sim.Data;
using Sim.Data.Sources;
using Sim.Data.Validation;

namespace FightingGame.Online;

/// <summary>
/// Where local matches load all game data from (ADR-0015 §3): the repository's <c>data/</c> folder when run from the
/// editor, the copy embedded in Sim.Data in exported builds.
/// </summary>
public sealed class GameDataLocation
{
    // Developer setting: where characters come from while data/ has none (relative to the repository root). The
    // Sim.Data test fixture is a small valid character; remove this once Testman (P1-17) is in data/.
    private const string PlaceholderCharacterFolder = "tests/Sim.Data.Tests/Fixtures/data";

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

    /// <summary>Loads and validates everything, reading the files again so data edits show up in the next match.</summary>
    /// <exception cref="DataValidationException">Any file is invalid.</exception>
    /// <exception cref="InvalidDataException">There is no character to play, not even a placeholder.</exception>
    public GameData Load()
    {
        IDataSource source = Folder is null ? new EmbeddedDataSource() : FolderSource(Folder);
        GameData data = new GameDataLoader(source).Load();
        if (data.Characters.Count == 0)
        {
            throw new InvalidDataException(
                $"No characters in {Folder ?? "the embedded data"}: add one under data/characters/ to play.");
        }

        return data;
    }

    private static IDataSource FolderSource(string folder)
    {
        IDataSource source = new DirectoryDataSource(folder);
        string placeholder = Path.GetFullPath(Path.Combine(folder, "..", PlaceholderCharacterFolder));
        if (!Directory.Exists(placeholder))
        {
            return source;
        }

        var withPlaceholder = new PlaceholderCharacterSource(source, new DirectoryDataSource(placeholder));
        if (withPlaceholder.UsesPlaceholder)
        {
            GD.PushWarning($"{folder} has no characters yet: using the placeholder characters in {placeholder}.");
        }

        return withPlaceholder;
    }
}
